using UnityEngine;

namespace ABI.CCK.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Returns the full path of a transform in the scene hierarchy.
        /// </summary>
        public static string GetPath(this Transform transform)
        {
            if (transform.parent == null) return transform.name;
            return transform.parent.GetPath() + "/" + transform.name;
        }
        
        /// <summary>
        /// Returns the transforms root index and relative path from the root.
        /// </summary>
        public static (int, string) GetIndexAndPath(this Transform transform)
        {
            Transform root = transform.root;
            string path = transform.GetPath();
            
            // Find the index of the root object in the scene
            var sceneRoots = root.gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
                if (sceneRoots[i].transform == root) return (i, path);
            
            throw new System.Exception("Root object not found in scene.");
        }
    }
}