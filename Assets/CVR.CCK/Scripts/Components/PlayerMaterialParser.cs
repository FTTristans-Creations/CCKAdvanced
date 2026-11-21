using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("/")] // Deprecated
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "player-material-parser")]
    [CVRComponent(ComponentStatus.Deprecated)]
    public class PlayerMaterialParser : MonoBehaviour
    {
        public Material targetMaterial;
        public string playerRootPositions = "_PlayerRootPositions";
        public string playerHipPositions = "_PlayerHipPositions";
        public string playerHeadPositions = "_PlayerHeadPositions";
        public string playerLeftHandPositions = "_PlayerLeftHandPositions";
        public string playerRightHandPositions = "_PlayerRightHandPositions";
        public string playerChestPositions = "_PlayerChestPositions";
        public string playerLeftFootPositions = "_PlayerLeftFootPositions";
        public string playerRightFootPositions = "_PlayerRightFootPositions";
    }
}