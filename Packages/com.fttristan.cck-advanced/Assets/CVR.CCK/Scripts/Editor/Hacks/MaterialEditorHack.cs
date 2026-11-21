using System;
using System.Reflection;
using UnityEditor;

namespace CVR.CCKEditor.Hacks
{
    // Same bullshit we have running the game in-editor/playmode that we harmony patch away.
    // Thank you unity for doing this for whatever fucking reason when we sneeze at materials.
    public static class MaterialEditorReflectionUtil
    {
        private static readonly MethodInfo BeginNoApplyMethod;
        private static readonly MethodInfo EndNoApplyMethod;

        static MaterialEditorReflectionUtil()
        {
            var type = typeof(MaterialEditor);
            BeginNoApplyMethod = type.GetMethod("BeginNoApplyMaterialPropertyDrawers", BindingFlags.NonPublic | BindingFlags.Static);
            EndNoApplyMethod = type.GetMethod("EndNoApplyMaterialPropertyDrawers", BindingFlags.NonPublic | BindingFlags.Static);
            if (BeginNoApplyMethod == null) throw new MissingMethodException("BeginNoApplyMaterialPropertyDrawers not found via reflection.");
            if (EndNoApplyMethod == null) throw new MissingMethodException("EndNoApplyMaterialPropertyDrawers not found via reflection.");
        } 
    
        public static void BeginNoApply() => BeginNoApplyMethod.Invoke(null, null);
        public static void EndNoApply() => EndNoApplyMethod.Invoke(null, null);
    }
}