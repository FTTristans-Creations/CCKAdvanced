using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
    
namespace CVR.CCKEditor.Extensions
{
    public static class SceneExtensions
    {
        public static void MoveSceneToTop(this Scene targetScene)
        {
            if (!targetScene.IsValid()) return;

            Scene firstScene = default;
            bool foundFirst = false;
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || scene == targetScene) 
                    continue;
                
                firstScene = scene;
                foundFirst = true;
                break;
            }

            if (!foundFirst) return;

            EditorSceneManager.MoveSceneBefore(targetScene, firstScene);
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}