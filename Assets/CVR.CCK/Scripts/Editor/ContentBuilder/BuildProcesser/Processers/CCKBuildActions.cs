using CCK.CE.Runtime.BuildActions;
using JetBrains.Annotations;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder.Processors
{
    [UsedImplicitly]
    public class CCKBuildActionProcessor : CCKBuildProcessor
    {
        public override int CallbackOrder => 0; // Shouldn't matter

        public override void OnPreProcessAvatar(GameObject avatar) => ExecuteAllBuildActions();
        public override void OnPreProcessSpawnable(GameObject spawnable) => ExecuteAllBuildActions();
        public override void OnPreProcessWorld(GameObject world) => ExecuteAllBuildActions();

        private void ExecuteAllBuildActions()
        {
            var buildActions = GetAllComponents<CCKBuildAction>();

            foreach (var buildAction in buildActions)
                buildAction.ExecuteActions();
        }
    }
}