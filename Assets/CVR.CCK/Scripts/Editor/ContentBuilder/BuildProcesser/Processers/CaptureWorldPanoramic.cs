using System.IO;
using CVR.CCKEditor.Tools;
using JetBrains.Annotations;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder.Processors
{
    [UsedImplicitly]
    internal class CaptureWorldPanoramic : CCKBuildProcessor
    {
        public override int CallbackOrder => 10000;
        
        public override void OnPreProcessWorld(GameObject world)
        {
            if (BuildPurpose != BuildPurpose.OnlinePublish) return;
            
            // Capture from spawn point
            Texture2D pano = CCKCommonTools.CapturePortalPanoramic(world);
            File.WriteAllBytes(BuilderState.PortalPanoramicCachePath, pano.EncodeToPNG());
            Object.DestroyImmediate(pano);
        }
    }
}