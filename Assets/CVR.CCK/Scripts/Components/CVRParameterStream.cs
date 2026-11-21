using System;
using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;
using UnityEngine.Animations;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Parameter Stream")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-parameter-stream")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRParameterStream : MonoBehaviour
    {
        public enum ReferenceType
        {
            World = 0,
            Avatar = 1,
            Spawnable = 2
        }

        [NotKeyable]
        public ReferenceType referenceType = ReferenceType.World;

        public List<CVRParameterStreamEntry> entries = new();

        public bool onlyUpdateWhenHeld;
        public bool onlyUpdateWhenAttached;
        public bool onlyUpdateWhenControlled;
        public bool onlyUpdateWhenOwner;

        private void OnEnable()
        {
            // ignored, required to make the enabled checkbox display
        }
    }
    
    #region CVRParameterStreamEntry Class

    [Serializable]
    public class CVRParameterStreamEntry
    {
        public enum Type
        {
            // General
            TimeSeconds = 0,
            TimeSecondsUtc = 10,
            DeviceMode = 20,
            HeadsetOnHead = 30,
            GameFocused = 31,
            ZoomFactor = 40,
            ZoomFactorCurve = 50,
            // Eyes
            EyeMovementLeftX = 60,
            EyeMovementLeftY = 70,
            EyeMovementRightX = 80,
            EyeMovementRightY = 90,
            EyeBlinkingLeft = 100,
            EyeBlinkingRight = 110,
            // Lip Sync
            VisemeLevel = 120,
            // General cnt
            TimeSinceHeadsetRemoved = 130,
            TimeSinceGameUnfocused = 131,
            TimeSinceAFK = 132,
            TimeSinceLocalAvatarLoaded = 140,
            LocalWorldDownloadPercentage = 150,
            LocalFPS = 160,
            LocalPing = 170,
            LocalPlayerCount = 180,
            LocalTimeSinceFirstWorldJoin = 190,
            LocalTimeSinceWorldJoin = 200,
            LocalPlayerMuted = 210,
            LocalPlayerHudEnabled = 220,
            LocalPlayerNameplatesEnabled = 230,
            LocalPlayerHeight = 240,
            LocalPlayerLeftControllerType = 250,
            LocalPlayerRightControllerType = 251,
            LocalPlayerFullBodyEnabled = 260,
            // Input
            TriggerLeftValue = 270,
            TriggerRightValue = 280,
            GripLeftValue = 290,
            GripRightValue = 300,
            GrippedObjectLeft = 310,
            GrippedObjectRight = 320,
            // Avatar
            AvatarHeight = 400,
            AvatarUpright = 401,
            // Transform
            TransformGlobalPositionX = 500,
            TransformGlobalPositionY = 501,
            TransformGlobalPositionZ = 502,
            TransformGlobalRotationX = 510,
            TransformGlobalRotationY = 511,
            TransformGlobalRotationZ = 512,
            TransformLocalPositionX = 520,
            TransformLocalPositionY = 521,
            TransformLocalPositionZ = 522,
            TransformLocalRotationX = 530,
            TransformLocalRotationY = 531,
            TransformLocalRotationZ = 532,
            // Fluid Volume
            FluidVolumeSubmerged = 600,
            FluidVolumeDepth = 601,
            FluidVolumeTimeSinceEntered = 602,
            FluidVolumeTimeSinceExit = 603,
            // Input Car
            InputCarSteering = 1000,
            InputCarAccelerate = 1001,
            InputCarBrake = 1002,
            InputCarHandbrake = 1003,
            InputCarBoost = 1004,
            // Input
            InputMovementX = 1100,
            InputMovementY = 1101,
            InputLookX = 1110,
            InputLookY = 1111,
            InputJump = 1120,
            // Instance
            CurrentInstanceType = 1400,
            IsPrivateInstance = 1401,
            IsPublicInstance = 1402,
            IsGroupInstance = 1403,
            //TODO: Add this once group id is actually sent from GS
            //IsFeaturedGroupInstance = 1404,
            // RigidBody
            RigidBodySpeed = 1600,
            RigidBodyVelocityX = 1601,
            RigidBodyVelocityY = 1602,
            RigidBodyVelocityZ = 1603,
            RigidBodyLocalVelocityX = 1604,
            RigidBodyLocalVelocityY = 1605,
            RigidBodyLocalVelocityZ = 1606,
            RigidBodyAcceleration = 1610,
            RigidBodyAccelerationX = 1611,
            RigidBodyAccelerationY = 1612,
            RigidBodyAccelerationZ = 1613,
            RigidBodyLocalAccelerationX = 1614,
            RigidBodyLocalAccelerationY = 1615,
            RigidBodyLocalAccelerationZ = 1616,
            // WheelHubController
            WheelHubControllerForwardSpeed = 1700,
            WheelHubControllerSpeedFactor = 1701,
            WheelHubControllerGrounded = 1710,
            // WheelController
            WheelControllerGrounded = 1720,
            WheelControllerRPM = 1721,
            // Damage System
            CurrentHealth = 1800,
            CurrentArmor = 1801,
            CurrentShield = 1802,
            // NavMeshAgent
            NavMeshAgentSpeed = 1900,
            NavMeshAgentRemainingDistance = 1901,
            NavMeshAgentHasPath = 1902,
            NavMeshAgentOnOffMeshLink = 1903,
            NavMeshAgentOffMeshLinkType = 1904,
            // Leg
            LegIsOnGround = 2000,
            // Menu
            IsAnyMenuOpen = 3000,
            IsMainMenuOpen = 3001,
            IsQuickMenuOpen = 3002,
            // General (cnt cnt)
            ChatBoxIsTyping = 10000,
            // Seed
            SeedOwner = 90000,
            SeedInstance = 90001,
            // User
            IsOwner = 90010,
        }

        public Type type = Type.TimeSeconds;

        public enum TargetType
        {
            Animator = 0,
            VariableBuffer = 1,
            AvatarAnimator = 2,
            CustomFloat = 3
        }

        public TargetType targetType = TargetType.Animator;

        public enum ApplicationType
        {
            Override = 0,
            AddToCurrent = 10,
            AddToStatic = 21,
            SubtractFromCurrent = 30,
            SubtractFromStatic = 41,
            SubtractWithCurrent = 50,
            SubtractWithStatic = 61,
            MultiplyWithCurrent = 70,
            MultiplyWithStatic = 81,
            CompareLessThen = 91,
            CompareLessThenEquals = 101,
            CompareEquals = 111,
            CompareMoreThenEquals = 121,
            CompareMoreThen = 131,
            Mod = 141,
            Pow = 151
        }

        public ApplicationType applicationType = ApplicationType.Override;

        public float staticValue;

        public GameObject target;

        public string parameterName;
    }
    
    #endregion
}
