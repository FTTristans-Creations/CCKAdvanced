using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    internal static class CCKBuildProcessorManager
    {
        internal static List<CCKBuildProcessor> CreateProcessors()
        {
            CCKBuildProcessorSettings settings = CCKBuildProcessorSettings.Instance;

            var processors = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    typeof(CCKBuildProcessor).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsInterface)
                .Where(type => settings.IsProcessorEnabled(type.FullName))
                .Select(type => (CCKBuildProcessor)Activator.CreateInstance(type))
                .OrderBy(processor => settings.GetProcessorOrder(processor))
                .ToList();

            StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("CCKBuildProcessorManager :: Found the following processors:");
            foreach (CCKBuildProcessor processor in processors) 
                sb.AppendLine(processor.GetType().FullName);
            Debug.Log(sb.ToString());

            return processors;
        }

        #region Avatar
        
        public static void ValidateAvatarBuild(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnWantsToBuildAvatar(buildAsset.RootObject);
        }

        public static void ProcessPreBuildAvatar(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPreProcessAvatar(buildAsset.RootObject);
        }

        public static void ProcessPostBuildAvatar(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPostProcessAvatar();
        }
        
        #endregion Avatar
        
        #region Spawnable
        
        public static void ValidateSpawnableBuild(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnWantsToBuildSpawnable(buildAsset.RootObject);
        }
        
        public static void ProcessPreBuildSpawnable(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPreProcessSpawnable(buildAsset.RootObject);
        }
        
        public static void ProcessPostBuildSpawnable(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPostProcessSpawnable();
        }
        
        #endregion Spawnable

        #region World
        
        public static void ValidateWorldBuild(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnWantsToBuildWorld(buildAsset.RootObject);
        }
        
        public static void ProcessPreBuildWorld(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPreProcessWorld(buildAsset.RootObject);
        }
        
        public static void ProcessPostBuildWorld(TempBuildAsset buildAsset)
        {
            foreach (CCKBuildProcessor processor in buildAsset.Processors)
                processor.OnPostProcessWorld();
        }
        
        #endregion World
    }
}