using System;
using ABI.CCK.Components;

namespace CVR.CCKEditor.TestMode
{
    /// <summary>
    /// Manager for the built-in CCK playmode testing (Experimental).
    /// 
    /// Test Mode will run the build processors for the selected content and automatically enter playmode.
    /// The original content or scene will be disabled and the processed content will be active instead.
    /// All WASM modules will be built and loaded in Test Mode.
    /// </summary>
    public static partial class CCKTestModeManager
    {
        public static bool IsInTestMode { get; private set; }
        
        public static Action OnEnterTestMode;
        public static Action OnExitTestMode;

        public static void TestContent(params CVRAssetInfo[] assetInfos)
            => InternalTestSpecificContent(assetInfos);
        
        public static void ExitTestMode() 
            => InternalExitTestMode();
    }
}