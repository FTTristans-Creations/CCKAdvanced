using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.ContentBuilder;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Context
{
    public abstract class BaseValidationContext
    {
        public HashSet<Object> VisitedAssets { get; set; } = new();
        
        public Component RootComponent { get; protected set; }
        public GameObject RootObject { get; protected set; }

        public virtual IEnumerable<Object> GetObjectsToProcess()
        {
            foreach (Component comp in GetRootComponents())
            {
                if (!comp || comp.gameObject.CompareTag("EditorOnly")) continue;
                if (typeof(ICCKEditorOnly).IsAssignableFrom(comp.GetType())) continue;
                yield return comp;
            }
        }

        protected abstract IEnumerable<Component> GetRootComponents();
        public virtual bool IsAdvancedTag(Transform transform, CVRAvatarAdvancedTaggingEntry.Tags tag) => false;
    }

    public sealed class AvatarValidationContext : BaseValidationContext
    {
        private readonly CVRAvatar _avatar;

        public AvatarValidationContext(GameObject rootObject)
        {
            RootObject = rootObject;
            _avatar = rootObject.GetComponent<CVRAvatar>();
            RootComponent = _avatar;
        }

        protected override IEnumerable<Component> GetRootComponents()
            => RootObject.GetComponentsInChildren<Component>(true);


        public override bool IsAdvancedTag(Transform transform, CVRAvatarAdvancedTaggingEntry.Tags tag)
        {
            if (!_avatar || !_avatar.enableAdvancedTagging)
                return false;

            var list = _avatar.advancedTaggingList;
            int count = list.Count;
            
            for (int i = 0; i < count; i++)
            {
                var entry = list[i];
                if ((entry.tags & tag) == 0) continue;
                
                GameObject go = entry.gameObject;
                if (!go) continue;
                
                if (transform.IsChildOf(go.transform)) 
                    return true;
            }
            
            return false;
        }
    }

    public sealed class SpawnableValidationContext : BaseValidationContext
    {
        public SpawnableValidationContext(GameObject rootObject)
        {
            RootObject = rootObject;
            RootComponent = rootObject.GetComponent<CVRSpawnable>();
        }

        protected override IEnumerable<Component> GetRootComponents()
            => RootObject.GetComponentsInChildren<Component>(true);
    }

    public sealed class WorldValidationContext : BaseValidationContext
    {
        public WorldValidationContext(GameObject sceneRoot)
        {
            RootObject = sceneRoot;
            RootComponent = sceneRoot.GetComponent<CVRWorld>();
        }

        public override IEnumerable<Object> GetObjectsToProcess()
        {
            if (RenderSettings.skybox) yield return RenderSettings.skybox;
            foreach (Object o in base.GetObjectsToProcess()) yield return o;
        }

        protected override IEnumerable<Component> GetRootComponents()
        {
            var all = Resources.FindObjectsOfTypeAll<Component>();
            foreach (var comp in all)
            {
                if (!comp || comp.gameObject.scene != RootObject.scene) continue;
                yield return comp;
            }
        }
    }
}