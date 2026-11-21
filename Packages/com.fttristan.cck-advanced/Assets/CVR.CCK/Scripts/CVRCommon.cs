using System;
using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ABI.CCK.Scripts
{
    public static class CVRCommon
    {
        // TODO: Should be governed by API via remote config, like upload limits are
        public const long MaxAvatarSpawnableBundleSize = 200L * 1024 * 1024; // 200MB
        public const long MaxWorldBundleSize = 1L * 1024 * 1024 * 1024; // 1GB
        
        public static readonly UnityVersion MinimumUnityVersion = new("2021.3.19f1");
        public static readonly UnityVersion MaximumUnityVersion = new("2022.3.61f1");

        public static bool IsUnityVersionSupported()
        {
            UnityVersion currentVersion = UnityVersion.GetCurrent();
            return currentVersion >= MinimumUnityVersion && currentVersion <= MaximumUnityVersion;
        }

#if UNITY_EDITOR
        public static readonly List<BuildTarget> SupportedBuildTargets = new()
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android
        };

        public static bool IsSupportedBuildTarget()
        {
            return SupportedBuildTargets.Contains(EditorUserBuildSettings.activeBuildTarget);
        }
#endif
        
        // CONFIGURABLE VALUES - START //
        public const string BaseVersion = "4.0.1";
        public const string ReleaseType = ReleaseTypes.Release; // Preview, Release
        public const int PreviewNumber = 1;
        // CONFIGURABLE VALUES - END //

        public static class ReleaseTypes
        {
            public const string Release = "Release";
            public const string Preview = "Preview";
        }

        public static string CCK_VERSION
            => BaseVersion;

        public static string CCK_VERSION_ASSET_INFO
        {
            get
            {
                // ReSharper disable once HeuristicUnreachableCode
                if (ReleaseType == "Release") return BaseVersion;
                return $"{BaseVersion}-{ReleaseType.ToLower()}";
            }
        }
        public static string CCK_VERSION_FULL
            => $"{BaseVersion}-{ReleaseType}";

        public const string LOCAL_PARAMETER_PREFIX = "#";
        public const string LOCOMOTION_EMOTES_LAYER_NAME = "Locomotion/Emotes";
        public const string HAND_LEFT_LAYER_NAME = "LeftHand";
        public const string HAND_RIGHT_LAYER_NAME = "RightHand";
        public const string TOGGLES_LAYER_NAME = "Toggles";

        public const float AVATAR_BIT_LIMIT = 3200f;
        public const float SPAWNABLE_BIT_LIMIT = 40f;
        
        // None string (checked for by a bunch of systems)
        public const string NONE_OR_EMPTY = "-none-";

        // Core parameters used by Avatars
        public static readonly string[] CoreParameters =
        {
            // Synced core parameters
            "MovementX", "MovementY", "Grounded", "Emote", "GestureLeft",
            "GestureRight", "Toggle", "Sitting", "Crouching",
            "CancelEmote", "Prone", "Flying", "Swimming",
            
            // Local core parameters
            "IsLocal", "IsFriend", "VelocityX", "VelocityY", "VelocityZ",
            "AFK", "GestureLeftIdx", "GestureRightIdx",
            "DistanceTo", "VisemeIdx", "VisemeLoudness"
        };
        
        // Default pointer types used by the Pointers & Triggers system
        public static readonly string[] DefaultPointerTypes =
        {
            "Mouth",
            "Index",
            "Grab",

            "Head",
            "Torso",

            "Hand",
            "LeftHand",
            "RightHand",

            "Foot",
            "LeftFoot",
            "RightFoot",

            "Finger",
            "LeftIndex",
            "LeftMiddle",
            "LeftRing",
            "LeftLittle",
            "RightIndex",
            "RightMiddle",
            "RightRing",
            "RightLittle"
        };

        // -1f to 6f, 0-1f is Fist weight
        public enum GestureIndex
        {
            HandOpen,
            Neutral,
            Fist,
            ThumbsUp,
            HandGun,
            Fingerpoint,
            Victory,
            RockNRoll
        }

        public static string GetUnityPackageName(bool isWasm = false)
        {
            string wasmSuffix = isWasm ? "_WASM" : "";
            return ReleaseType == ReleaseTypes.Release
                ? $"CCK{wasmSuffix}_{BaseVersion}_{ReleaseType}.unitypackage"
                : $"CCK{wasmSuffix}_{BaseVersion}_{ReleaseType}.{PreviewNumber}.unitypackage";
        }

#if UNITY_EDITOR

        // This is all jank :>

        public static bool NonCoreFilter(AnimatorControllerParameter param)
        {
            return !CoreParameters.Contains(param.name);
        }

        public static bool NonNullFilter(AnimatorControllerParameter param)
        {
            return !string.IsNullOrWhiteSpace(param.name);
        }

        public static bool NonLocalFilter(AnimatorControllerParameter param)
        {
            return !param.name.StartsWith(LOCAL_PARAMETER_PREFIX);
        }

        private static IEnumerable<T> FilterParameters<T>(AnimatorControllerParameter[] parameters,
            Func<AnimatorControllerParameter, T> selector, params Predicate<AnimatorControllerParameter>[] filters)
        {
            return parameters.Where(p =>
                {
                    if (!NonNullFilter(p))
                        return false;

                    foreach (var filter in filters)
                        if (filter != null && !filter(p))
                            return false;

                    return true;
                })
                .Select(selector);
        }

        public static List<AnimatorControllerParameter> GetParametersFromController(
            RuntimeAnimatorController controller, params Predicate<AnimatorControllerParameter>[] filters)
        {
            UnityEditor.Animations.AnimatorController animatorController;

            if (controller is AnimatorOverrideController overrideController)
                animatorController = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            else
                animatorController = controller as UnityEditor.Animations.AnimatorController;

            return animatorController == null
                ? new List<AnimatorControllerParameter>()
                : FilterParameters(animatorController.parameters, p => p, filters).ToList();
        }

        public static List<string> GetParametersFromControllerAsString(RuntimeAnimatorController controller,
            params Predicate<AnimatorControllerParameter>[] filters)
        {
            UnityEditor.Animations.AnimatorController animatorController;

            if (controller is AnimatorOverrideController overrideController)
                animatorController = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            else
                animatorController = controller as UnityEditor.Animations.AnimatorController;

            return animatorController == null
                ? new List<string>()
                : FilterParameters(animatorController.parameters, p => p.name, filters).ToList();
        }

        public static List<AnimatorControllerParameter> GetParametersFromAnimator(Animator animator,
            params Predicate<AnimatorControllerParameter>[] filters)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return new List<AnimatorControllerParameter>();

            UnityEditor.Animations.AnimatorController animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                animatorController = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

            return animatorController == null
                ? new List<AnimatorControllerParameter>()
                : FilterParameters(animatorController.parameters, p => p, filters).ToList();
        }

        public static List<string> GetParametersFromAnimatorAsString(Animator animator,
            params Predicate<AnimatorControllerParameter>[] filters)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return new List<string>();

            UnityEditor.Animations.AnimatorController animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                animatorController = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

            return animatorController == null
                ? new List<string>()
                : FilterParameters(animatorController.parameters, p => p.name, filters).ToList();
        }
#endif
    }
}