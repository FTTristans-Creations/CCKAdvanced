using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ABI.CCK.Components;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;
using CVR.CCKEditor.Hacks;
using CVR.CCKEditor.Validations.Context;
using CVR.CCKEditor.Validations.Steps;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations
{
    public class ValidationResult
    {
        public static ValidationResult Ok => new()
        {
            Severity = ValidationSeverity.None,
            Message = null
        };
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
        
        // How much it increments the validation section counters
        public virtual int IssueCount => 1;
        
        public ContentTags EnforcedTags { get; set; }
        public string DocsUrl { get; set; }
    }

    public class DetailedValidationResult : ValidationResult
    {
        // The objects which offend the validation
        public HashSet<Object> RootObjects { get; set; } = new();
        
        // The mapping from RootObject -> object it was found on,
        // and then object it was found on -> the object even before that.
        public Dictionary<Object, HashSet<Object>> Hierarchy { get; set; }
        
        // We want to count each individual issue despite combining them into one validation result
        public override int IssueCount => RootObjects.Count;
        
        public bool DisplayShowAllButton => RootObjects.Count != 0;

        public void SelectAll()
        {
            EditorWindowHacks.FocusInspectorWindow();
            var pool = ArrayPool<Object>.Shared;
            var array = pool.Rent(RootObjects.Count);
            int index = 0;
            foreach (Object obj in RootObjects) array[index++] = obj;
            Selection.objects = index == array.Length ? array : array[..index];
            pool.Return(array, clearArray: true);
        }

        public bool DisplayAutoFixButton => AutoFix != null;
        public Action AutoFix { get; set; }

        public bool DisplayOpenDocsButton => !string.IsNullOrEmpty(DocsUrl);
        public void OpenDocs() => Application.OpenURL(DocsUrl);
    }

    public enum ValidationSeverity
    {
        None,
        Info,
        Warning,
        Error
    }
    
    public class ValidationPipeline
    {
        static ValidationPipeline()
        {
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaBoneCloth, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaBoneSpring, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaMeshCloth, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaMeshSpring, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaRenderDeformer, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth.MagicaVirtualDeformer, MagicaCloth"));
            AddIfNotNull(Type.GetType("MagicaCloth2.MagicaCloth, MagicaClothV2"));
            return;
            void AddIfNotNull(Type type) { if (type != null) TypesThatAreSuperFat.Add(type); }
        }
        
        private static readonly HashSet<Type> TypesThatAreSuperFat = new();
        private static readonly HashSet<Type> TypesWithNoObjectRefs = new();

        // Steps that only run once at the start of the pipeline
        private readonly List<IValidationStep> _rootSteps = new();
        // Steps that run for every object in the pipeline
        private readonly List<IValidationStep> _pipelineSteps = new();

        public void AddRootStep(IValidationStep step) => _rootSteps.Add(step);
        public void ClearRootSteps() => _rootSteps.Clear();
        public void AddPipelineStep(IValidationStep step) => _pipelineSteps.Add(step);
        public void ClearPipelineSteps() => _pipelineSteps.Clear();
        
        public async Task<ValidationPipelineResult> ExecuteAsync(
            BaseValidationContext context,
            Action<float> onProgress = null)
        {
            ValidationPipelineResult result = new()
            {
                StartTime = DateTime.Now,
                TotalSteps = _pipelineSteps.Count
            };
            
            // This validation result is special. It is used to gather all instances of broken clips.
            DetailedValidationResult brokenClipsResult = new()
            {
                Severity = ValidationSeverity.Warning,
                Message = "Found Animation Clip(s) with missing references. " +
                          "Either the target component you are animating or the referenced object is missing. " +
                          "In the case of Materials, this means that asset & its children are included in your build, " +
                          "despite being unused.",
                RootObjects = new(),
                Hierarchy = new(),
            };

            Stopwatch stopwatch = new();
            const long maxFrameBudget = 33;
            const long minYieldThreshold = 1;
            const int progressUpdateInterval = 100;

            var pending = new Queue<(Component sourceComp, Object target)>();
            foreach (Object c in context.GetObjectsToProcess())
                pending.Enqueue((c as Component, c)); 
            
            // Add specific things we need to check
            if (context.RootComponent is CVRAvatar avatar)
            {
                // We need to associate the override controller with the animator for validation purposes.
                if (avatar.overrides && avatar.TryGetComponent(out Animator animator))
                    pending.Enqueue((animator, avatar.overrides));
            }
            
            int processed = 0;
            int total = pending.Count;

            stopwatch.Start();
            
            // Run the root steps first (kinda hacky, slapped in)
            foreach (IValidationStep step in _rootSteps)
                step.ProcessObject(context, context.RootComponent, null);
            
            // Run pipeline steps on all objects
            while (pending.Count > 0)
            {
                MaterialEditorReflectionUtil.BeginNoApply();
                
                (Component sourceComp, Object current) = pending.Dequeue();
                if (!current || !context.VisitedAssets.Add(current))
                {
                    // Should usually be shared materials, shaders, and mesh
                    // Debug.Log("Skipped already checked asset: " + current.GetType().Name + " " + sourceComp.GetType().Name);
                    continue;
                }
                
                // REMINDER: Do not VisitedAssets.Contains on the current object, we just added it above.
                // Only do so for child objects or objects that are referenced by the current object.

                if (TypesThatAreSuperFat.Contains(current.GetType()))
                    continue; // Slows down the processing by a bunch for no benefit

                switch (current)
                {
                    // Found something we weren't expecting. Log so we can investigate.
                    default:
                        //Debug.Log("Ran into some type we were not expecting. " +
                        //          "It is not a component, gameobject, or scriptable object. Skipping type: " + current.GetType());
                        break;
                 
                    // Known types we don't care to log about.
                    case UnityEngine.Avatar:
                    case AnimatorController:
                    case TerrainData:
                    case MonoScript:
                    case LightmapParameters:
                    case Font:
                    case Sprite:
                    case RenderTexture:
                    case AudioMixerGroup: // AudioMixerGroupController is internal
                        break;
                    
                    // We explicitly scan the properties in StreamingMipmapsStep, so we do not need to serialize the thing
                    // here for crawling the properties this way, it is also VERY FUCKING EXPENSIVE.
                    case Material:
                    // The rest of these are known to not have any object references, so crawling their properties
                    // is needless and slow.
                    case Shader:
                    case ComputeShader:
                    case Transform:
                    case AudioListener:
                    case AudioReverbZone:
                    case WindZone:
                    case Mesh:
                    case Texture2D:
                    case AudioClip:
                        foreach (IValidationStep step in _pipelineSteps) step.ProcessObject(context, sourceComp, current);
                        break;
                    
                    // Likely some sort of data object, we will parse it, although it likely doesn't help us much.
                    case ScriptableObject:
                    {
                        foreach (IValidationStep step in _pipelineSteps)
                            step.ProcessObject(context, sourceComp, current);

                        if (TypesWithNoObjectRefs.Contains(current.GetType()))
                            continue; // This component is known to not have object references

                        bool hasObjectReferenceProperty = false;
                        
                        using SerializedObject so = new(current);
                        SerializedProperty it = so.GetIterator();
                        while (it.NextVisible(true))
                        {
                            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                            hasObjectReferenceProperty = true;
                            
                            Object refObj = it.objectReferenceValue;
                            if (!refObj 
                                || context.VisitedAssets.Contains(refObj)) // Safe to check as it is a child object of current
                                continue;
                            pending.Enqueue((sourceComp, refObj));
                        }
                        
                        // We scanned the type for nothing, ensure we do not make the same mistake again
                        if (!hasObjectReferenceProperty) TypesWithNoObjectRefs.Add(current.GetType());
                        
                        break;
                    }
                    
                    // We want to parse clips for their material references.
                    case AnimationClip clip:
                    {
                        // foreach (IValidationStep step in _pipelineSteps)
                        //     step.ProcessObject(sourceComp, clip);

                        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                        foreach (EditorCurveBinding binding in bindings)
                        {
                            // Debug.Log("Processing binding: " + binding.propertyName + " on " + binding.path + " for type " + binding.type);
                            var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                            foreach (ObjectReferenceKeyframe key in keyframes)
                            {
                                if (!key.value)
                                {
                                    brokenClipsResult.RootObjects.Add(clip);
                                    continue;
                                }
                                
                                if (context.VisitedAssets.Contains(key.value)) // Safe to check as it is a child object of current
                                    continue;

                                Component newSource = sourceComp;
                                
                                // CVRAvatar holds clip direct clip references in the autogen
                                if (newSource is CVRAvatar 
                                    && newSource.TryGetComponent(out Animator avatarAnimator))
                                    newSource = avatarAnimator;

                                // This is hacky, but I need the Streaming Mips step to be able to associate the
                                // textures with usage on a renderer, so we overwrite the source component.
                                if (key.value is Material && sourceComp is Animator animator)
                                {
                                    Transform target = animator.transform.Find(binding.path);
                                    Component component = target?.GetComponent(binding.type);
                                    if (target && component) newSource = component;
                                    else
                                    {
                                        brokenClipsResult.RootObjects.Add(clip);
                                        continue;
                                    }
                                }

                                pending.Enqueue((newSource, key.value));
                            }
                        }
                        break;
                    }
                    
                    case Animator animator when animator.runtimeAnimatorController:
                    {
                        // Animator/RuntimeAnimatorController does not reference the clips themselves as object references,
                        // so we must manually add the clips to the queue to scan them for things.
                        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
                        foreach (AnimationClip clip in controller.animationClips)
                        {
                            if (!clip 
                                || context.VisitedAssets.Contains(clip)) // Safe to check as it is a child object of current
                                continue;
                            pending.Enqueue((animator, clip));
                        }
                        break;
                    }

                    // Jank to ensure the controller is scanned for clips.
                    case AnimatorOverrideController overrideController when sourceComp is Animator animator:
                    {
                        if (!context.VisitedAssets.Contains(overrideController))
                            pending.Enqueue((animator, overrideController));

                        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                        overrideController.GetOverrides(overrides);

                        foreach ((AnimationClip original, AnimationClip replacement) in overrides)
                        {
                            if (original && !context.VisitedAssets.Contains(original))
                                pending.Enqueue((animator, original));
                            if (replacement && !context.VisitedAssets.Contains(replacement))
                                pending.Enqueue((animator, replacement));
                        }

                        break;
                    }
                    
                    case GameObject go:
                    {
                        foreach (IValidationStep step in _pipelineSteps) 
                            step.ProcessObject(context, go.transform, null);
                        
                        // Crawl into any prefabs referenced directly by any gameobject.
                        // This is only needed just in case we find a GO that was not referenced within a Component.
                        if (!go.scene.IsValid())
                        {
                            Transform rootOfPrefab = go.transform.root;
                            if (context.VisitedAssets.Contains(rootOfPrefab)) // Safe to check as it is a child object of current
                                continue; // Already scanned this prefab
                            
                            foreach (Component child in rootOfPrefab.GetComponentsInChildren<Component>(true))
                                if (child && !context.VisitedAssets.Contains(child)) // Safe to check as it is a child object of current
                                    pending.Enqueue((child, child));
                            
                            continue;
                        }
                        break;
                    }
                    
                    case Component comp:
                    {
                        // var go = comp.gameObject;
                        foreach (IValidationStep step in _pipelineSteps)
                            step.ProcessObject(context, comp, null);
                        
                        if (TypesWithNoObjectRefs.Contains(current.GetType()))
                            continue; // This component is known to not have object references

                        bool hasObjectReferenceProperty = false;
                        
                        using SerializedObject so = new(comp);
                        SerializedProperty it = so.GetIterator();
                        while (it.NextVisible(true))
                        {
                            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                            hasObjectReferenceProperty = true;
                            
                            Object refObj = it.objectReferenceValue;
                            if (!refObj
                                || context.VisitedAssets.Contains(refObj)) // Safe to check as it is a child object of current
                                continue;

                            // Crawl into prefab referenced by a gameobject
                            if (refObj is GameObject refGo && !refGo.scene.IsValid())
                            {
                                Transform rootOfPrefab = refGo.transform.root;
                                if (context.VisitedAssets.Contains(rootOfPrefab)) // Safe to check as it is a child object of current
                                    continue; // Already scanned this prefab
                                
                                foreach (Component child in rootOfPrefab.GetComponentsInChildren<Component>(true))
                                    if (child && !context.VisitedAssets.Contains(child)) // Safe to check as it is a child object of current
                                        pending.Enqueue((comp, child));
                                
                                continue;
                            }
                            
                            // Crawl into prefab referenced by a component
                            if (refObj is Component refComp && !refComp.gameObject.scene.IsValid())
                            {
                                Transform rootOfPrefab = refComp.transform.root;
                                if (context.VisitedAssets.Contains(rootOfPrefab)) // Safe to check as it is a child object of current
                                    continue; // Already scanned this prefab
                                
                                foreach (Component child in rootOfPrefab.GetComponentsInChildren<Component>(true))
                                    if (child && !context.VisitedAssets.Contains(child)) // Safe to check as it is a child object of current
                                        pending.Enqueue((comp, child));
                                
                                continue;
                            }

                            pending.Enqueue((comp, refObj));
                        }
                        
                        // We scanned the type for nothing, ensure we do not make the same mistake again
                        if (!hasObjectReferenceProperty) TypesWithNoObjectRefs.Add(current.GetType());
                        
                        break;
                    }
                }

                processed++;
                if (processed % progressUpdateInterval == 0 || pending.Count == 0)
                    onProgress?.Invoke((float)processed / Mathf.Max(1, total));

                if (stopwatch.ElapsedMilliseconds >= maxFrameBudget)
                {
                    if (stopwatch.ElapsedMilliseconds >= minYieldThreshold)
                        await Task.Yield();
                    stopwatch.Restart();
                }
            }

            MaterialEditorReflectionUtil.EndNoApply();

            // Collect root step results
            foreach (IValidationStep step in _rootSteps)
                result.StepResults.AddRange(step.GetResults());
            
            // Collect pipeline step results
            foreach (IValidationStep step in _pipelineSteps)
                result.StepResults.AddRange(step.GetResults());
            
            // Add the broken clips result if it has any issues
            if (brokenClipsResult.RootObjects.Count > 0)
                result.StepResults.Add(brokenClipsResult);
            
            result.StepResults = result.StepResults
                .OrderBy(r => r is DetailedValidationResult) // ValidationResult first
                .ThenBy(r => r.Severity) // Then by severity (Error > Warning > Info > None)
                .ToList();
            
            result.EndTime = DateTime.Now;
            result.IsSuccess = result.StepResults.All(r => r.Severity != ValidationSeverity.Error);

            // Debug.Log($"Processed {processed} objects");
            return result;
        }
    }

    public class ValidationPipelineResult
    {
        public bool IsSuccess { get; set; } = true;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public int TotalSteps { get; set; }
        public List<ValidationResult> StepResults { get; set; } = new();

        public ContentTags GetEnforcedTagsFromSteps()
        {
            ContentTags result = new();
            foreach (ValidationResult step in StepResults) result |= step.EnforcedTags;
            return result;
        }
    }
}