using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

#if LTCGI_INCLUDED
using pi.LTCGI;
#endif

namespace ABI.CCK.Components
{
    public partial class CCK_CVRLTCGIAdapterEditor
    {
        public static void GenerateCVRLTCGIAdapter(Scene scene)
        {
#if LTCGI_INCLUDED

            // Ignore if there is no controller
            if (LTCGI_Controller.Singleton == null) return;

            // Update the materials on upload
            LTCGI_Controller.Singleton.UpdateMaterials();

            // And if configured also precompute the LOD Texture Arrays
            if (LTCGI_Controller.Singleton.PrecomputeOnBuild)
                LTCGI_Controller.Singleton.CreateLODTextureArrays();

            LTCGI_UdonAdapter udonAdapter = LTCGI_Controller.Singleton.GetComponent<LTCGI_UdonAdapter>();

            if (udonAdapter == null)
            {
                Debug.LogWarning($"Found a {nameof(LTCGI_Controller)} component, but not a {nameof(LTCGI_UdonAdapter)}. LTCGI won't work...");
                return;
            }

            // Check for all CVRLTCGIAdapter in scene
            CVRLTCGIAdapter[] cvrAdapters = FindObjectsOfType<CVRLTCGIAdapter>(true);

            Debug.LogWarning($"There are Multiple {nameof(CVRLTCGIAdapter)} Components ({cvrAdapters.Length}) in " +
                             "the scene. This can cause unintended issues.");

            CVRLTCGIAdapter cvrAdapter = null;

            if (cvrAdapters.Length > 0)
                cvrAdapter = cvrAdapters[0];

            // If there is not a CVR adapter in scene, create one
            if (cvrAdapter == null)
                cvrAdapter = udonAdapter.gameObject.AddComponent<CVRLTCGIAdapter>();

            // Set the current version for this script
            cvrAdapter.version = CVRLTCGIAdapter.Versions.V1;

            // Fill the serialized data with data from the udonAdapter
            cvrAdapter.serializedDataV1 = new CVRLTCGIAdapter.DataV1(udonAdapter);

            // Attempt to get the ltcgi package version
            const string ltcgiPackageName = "at.pimaker.ltcgi";
            UnityEditor.PackageManager.PackageInfo packageInfo = CCKCommonTools.GetPackageInfo(ltcgiPackageName);
            if (packageInfo != null)
                cvrAdapter.ltgciVersion = packageInfo.version;
            else
                Debug.LogWarning($"Failed to find the package {ltcgiPackageName}, unable to get it's version...");

#endif
        }
    }
}