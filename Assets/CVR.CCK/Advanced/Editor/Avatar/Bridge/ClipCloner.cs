#if CVR_CCK_EXISTS
using UnityEditor;
using UnityEngine;

namespace AvatarBridge
{
    // Object.Instantiate carries an AnimationClip's curves but is not
    // dependable for its clip settings (Loop Time, Loop Pose, Cycle
    // Offset, ...), so every clip clone re-applies them explicitly.
    internal static class ClipCloner
    {
        internal static AnimationClip Clone(AnimationClip src)
        {
            var clone = UnityEngine.Object.Instantiate(src);
            AnimationUtility.SetAnimationClipSettings(clone,
                AnimationUtility.GetAnimationClipSettings(src));
            return clone;
        }
    }
}
#endif
