using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CVR.CCKEditor.ContentBuilder.Processors
{
    /// <summary>
    /// Removes editor-only components and game objects from the content before building.
    /// </summary>
    [UsedImplicitly]
    internal class EditorOnlyCleanupProcessor : CCKBuildProcessor
    {
        private const string EditorOnlyTag = "EditorOnly";
        
        public override int CallbackOrder => int.MaxValue; // Run absolute last
        
        public override void OnPreProcessAvatar(GameObject avatar)
        {
            CleanupEditorContent();
            CleanupEditorOnlyGameObjects();
            
            // Clean up the controller assigned in the Animator on the avatar object.
            if (avatar.TryGetComponent(out Animator animator))
                animator.runtimeAnimatorController = null;
        }

        public override void OnPreProcessSpawnable(GameObject spawnable)
        {
            CleanupEditorContent();
            CleanupEditorOnlyGameObjects();
        }

        public override void OnPreProcessWorld(GameObject world) 
            => CleanupEditorContent();
        
        /// <summary>
        /// Removes all components inheriting ICCKEditorOnly.
        /// </summary>
        private void CleanupEditorContent()
        {
            // Remove all components that implement ICCKEditorOnly
            var editorOnlyComponents = GetAllComponents<ICCKEditorOnly>();
            foreach (ICCKEditorOnly component in editorOnlyComponents) 
                if (component is Component c) Object.DestroyImmediate(c);
        }

        // This must be done manually for Avatars & Props.
        // Unity automatically handles this when building scenes.
        private void CleanupEditorOnlyGameObjects()
        {
            // Remove all game objects that have the EditorOnly tag
            var allTransforms = GetAllComponents<Transform>();
            foreach (Transform transform in allTransforms)
                if (transform && transform.CompareTag(EditorOnlyTag)) Object.DestroyImmediate(transform.gameObject);
        }
    }
}