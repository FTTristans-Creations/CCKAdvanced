using JetBrains.Annotations;

namespace ABI_RC.Core
{
    [PublicAPI]
    public static class CVRLayers
    {
        public const int Default = 0;
        public const int TransparentFX = 1;

        /// <summary>
        /// <para>PlayerSphereProxy.cs - Needs to be ignored by raycasts, while interactable with GravityZones</para>
        /// <para>GravityZone.cs - Needs to be ignored by raycasts, while interactable with PlayerSphereProxy</para>
        /// </summary>
        public const int IgnoreRaycast = 2;

        public const int Layer3 = 3;
        public const int Water = 4;
        public const int UI = 5;
        public const int PassPlayer = 6;
        public const int BlockPlayer = 7;
        
        /// <para>PlayerNonKinematicProxy.cs - Needs to be ignored by raycasts, particle system collision need to interact with it</para>
        public const int PlayerLocal = 8;
        public const int PlayerClone = 9;
        public const int PlayerNetwork = 10;

        /// <summary>
        /// CVRPickupObject.cs - Used for the telepathic grab collider to prevent ControllerRay from interacting with it normally. ?
        /// </summary>
        public const int MirrorReflection = 11;

        public const int CameraOnly = 12;
        
        /// <summary>
        /// Houses the always rendered global post processing volume for the AMDStutterFix.
        /// See CVRTools.ApplyForcedCameraSettings for more details.
        /// </summary>
        public const int CVRReserved2 = 13;

        /// <summary>
        /// CVRMirror.cs - Used to prevent mirrors from rendering other mirrors, caused visual issues
        /// (you can't do recursive reflections like that anyway)
        /// </summary>
        public const int CVRReserved3 = 14;

        public const int UIInternal = 15;

        // User Generated Content
        public const int CVRContent1 = 16;
        public const int CVRContent2 = 17;
        public const int CVRContent3 = 18;
        public const int CVRContent4 = 19;
        public const int CVRContent5 = 20;
        public const int CVRContent6 = 21;
        public const int CVRContent7 = 22;
        public const int CVRContent8 = 23;
        public const int CVRContent9 = 24;
        public const int CVRContent10 = 25;
        public const int CVRContent11 = 26;
        public const int CVRContent12 = 27;
        public const int CVRContent13 = 28;
        public const int CVRContent14 = 29;
        public const int CVRContent15 = 30;
        public const int CVRContent16 = 31;

        /// <summary>
        /// Add layers here to cause Lua to freak out if someone tries using them.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsInternalLayer(int value)
        {
            // This is more-so "illegal" layers, as there are few more "internal" ones.
            // While PlayerLocal is "internal", there are valid uses for it in worlds rn
            
            switch (value)
            {
                case PlayerClone:
                case UIInternal:
                    return true;
            }

            return false;
        }
        
        // TODO: Avatar / Prop allowed layer check here ?
        // Using scripting you can change to layers initially filtered from avatars/props.
    }
}