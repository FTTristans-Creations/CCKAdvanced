using ABI.CCK.Scripts;
using AvatarBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Credits window for the CCK Advanced module: lists the creators of the
    /// bundled tools and the underlying ChilloutVR CCK. Styled with the
    /// AvatarBridge kit so it matches the other Advanced windows.
    /// </summary>
    public class CreditsWindow : EditorWindow
    {
        private const string AvatarBridgeUrl = "https://github.com/MrTactical/AvatarBridge";
        private const string ChilloutVrUrl = "https://chilloutvr.net";

        private VisualElement _header;
        private VisualElement _body;

        [MenuItem("ChilloutVR CCK/Advanced/Credits", false, 999)]
        private static void Open()
        {
            var window = GetWindow<CreditsWindow>("CCK Advanced Credits");
            window.minSize = new Vector2(420, 300);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            BridgeElements.Root(root);

            _header = new VisualElement();
            root.Add(_header);

            var scroll = BridgeElements.Scroll();
            root.Add(scroll);

            _body = new VisualElement();
            scroll.Add(_body);
            Rebuild();
        }

        private void Rebuild()
        {
            if (_body == null)
            {
                return;
            }
            _header.Clear();
            _header.Add(BridgeElements.Banner("CCK Advanced", "Credits", "v" + CVRCommon.CCK_VERSION));
            _body.Clear();

            var advanced = new BridgeElements.Card("CCK Advanced", "The Advanced module of the ChilloutVR CCK");
            advanced.Body.Add(BridgeElements.KeyValue("Creator", "FTTristan"));
            _body.Add(advanced);

            var bridge = new BridgeElements.Card("AvatarBridge", "Bundled VRChat-to-ChilloutVR converter");
            bridge.Body.Add(BridgeElements.KeyValue("Creator", "MrTactical"));
            bridge.Body.Add(BridgeElements.Row(
                BridgeElements.ExternalLink("AvatarBridge repository", AvatarBridgeUrl,
                    "Source, docs and issue tracker for the bundled AvatarBridge.")));
            _body.Add(bridge);

            var cck = new BridgeElements.Card("ChilloutVR CCK", "The underlying avatar/world content kit");
            cck.Body.Add(BridgeElements.KeyValue("Created by", "ChilloutVR Team"));
            cck.Body.Add(BridgeElements.Row(
                BridgeElements.ExternalLink("chilloutvr.net", ChilloutVrUrl,
                    "The ChilloutVR team and the CCK.")));
            _body.Add(cck);

            _body.Add(BridgeElements.Notice(Tone.Info,
                "AvatarBridge is bundled with CCK Advanced and remains the work of MrTactical.",
                "See the LICENSE.md and README.md in the Advanced folder for license details."));

            _body.Add(BridgeElements.Footer(ChilloutVrUrl));
        }
    }
}
