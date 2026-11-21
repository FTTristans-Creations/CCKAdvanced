using ABI.CCK.Components;
using CVR.CCK;
using UnityEngine;

[AddComponentMenu("")]
[HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-warp-point")]
[System.Serializable]
[CVRComponent(ComponentStatus.None)]
public class CVRWarpPoint : MonoBehaviour
{
    [Header("CVR Warp Point (Will teleport you to the position of this object on ui interaction.)")]
    public string warpPointName;
    public string warpPointDescription;
}
