#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Scripts;
using ABI.CCK.Scripts.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRParameterStreamEditor
    {
        #region Parameter Stream Search Box Categories
        
        private static readonly string[] CategorizedTypes =
        {
            // General
            "General/Time Seconds",
            "General/Time Seconds Utc",
            "General/Device Mode",
            "General/Headset On Head",
            "General/Game Focused",
            "General/Zoom Factor",
            "General/Zoom Factor Curve",
            // Eyes
            "Eye Tracking/Eye Movement Left X",
            "Eye Tracking/Eye Movement Left Y",
            "Eye Tracking/Eye Movement Right X",
            "Eye Tracking/Eye Movement Right Y",
            "Eye Tracking/Eye Blinking Left",
            "Eye Tracking/Eye Blinking Right",
            // Lip Sync
            "Lip Sync/Viseme Level",
            // General cnt
            "General/Time Since Headset Removed",
            "General/Time Since Game Unfocused",
            "General/Time Since AFK",
            "General/Time Since Local Avatar Loaded",
            "General/Local World Download Percentage",
            "General/Local FPS",
            "General/Local Ping",
            "General/Local Player Count",
            "General/Local Time Since First World Join",
            "General/Local Time Since World Join",
            "General/Local Player Muted",
            "General/Local Player Hud Enabled",
            "General/Local Player Nameplates Enabled",
            "General/Local Player Height",
            "General/Local Player Left Controller Type",
            "General/Local Player Right Controller Type",
            "General/Local Player Full Body Enabled",
            // Input
            "Input/Trigger Left Value",
            "Input/Trigger Right Value",
            "Input/Grip Left Value",
            "Input/Grip Right Value",
            "Input/Gripped Object Left",
            "Input/Gripped Object Right",
            // Avatar
            "Avatar/Avatar Height",
            "Avatar/Avatar Upright",
            // Transform
            "Transform/Transform Global Position X",
            "Transform/Transform Global Position Y",
            "Transform/Transform Global Position Z",
            "Transform/Transform Global Rotation X",
            "Transform/Transform Global Rotation Y",
            "Transform/Transform Global Rotation Z",
            "Transform/Transform Local Position X",
            "Transform/Transform Local Position Y",
            "Transform/Transform Local Position Z",
            "Transform/Transform Local Rotation X",
            "Transform/Transform Local Rotation Y",
            "Transform/Transform Local Rotation Z",
            // Fluid Volume
            "Fluid Volume/Fluid Volume Submerged",
            "Fluid Volume/Fluid Volume Depth",
            "Fluid Volume/Fluid Volume Time Since Entered",
            "Fluid Volume/Fluid Volume Time Since Exit",
            // Input Car
            "Input Car/Input Car Steering",
            "Input Car/Input Car Accelerate",
            "Input Car/Input Car Brake",
            "Input Car/Input Car Handbrake",
            "Input Car/Input Car Boost",
            // Input
            "Input/Input Movement X",
            "Input/Input Movement Y",
            "Input/Input Look X",
            "Input/Input Look Y",
            "Input/Input Jump",
            // Instance
            "Instance/Current Instance Type",
            "Instance/Is Private Instance",
            "Instance/Is Public Instance",
            "Instance/Is Group Instance",
            // Rigidbody
            "Rigidbody/Speed",
            "Rigidbody/Velocity X",
            "Rigidbody/Velocity Y",
            "Rigidbody/Velocity Z",
            "Rigidbody/Local Velocity X",
            "Rigidbody/Local Velocity Y",
            "Rigidbody/Local Velocity Z",
            "Rigidbody/Acceleration",
            "Rigidbody/Acceleration X",
            "Rigidbody/Acceleration Y",
            "Rigidbody/Acceleration Z",
            "Rigidbody/Local Acceleration X",
            "Rigidbody/Local Acceleration Y",
            "Rigidbody/Local Acceleration Z",
            // WheelHubController
            "WheelHubController/Current Speed",
            "WheelHubController/Speed Factor",
            "WheelHubController/Is Grounded",
            // WheelController
            "WheelController/Is Grounded",
            "WheelController/RPM",
            // Damage System
            "Damage System/Current Health",
            "Damage System/Current Armor",
            "Damage System/Current Shield",
            // NavMeshAgent
            "NavMeshAgent/Speed",
            "NavMeshAgent/Remaining Distance",
            "NavMeshAgent/Has Path",
            "NavMeshAgent/On OffMeshLink",
            "NavMeshAgent/OffMeshLink Type",
            // Leg
            "Leg/Is on ground",
            // Menu
            "Menu/Is Any Menu Open",
            "Menu/Is Main Menu Open",
            "Menu/Is Quick Menu Open",
            // General (cnt cnt)
            "General/ChatBox Is Typing",
            // Seed
            "Seed/Seed Owner",
            "Seed/Seed Instance",
            // User
            "User/Is Owner",
        };

        private static readonly string[] CategorizedHelpText =
        {
            // General
            "The current System time in seconds since midnight including milliseconds as fractions",
            "The current System utc time in seconds since midnight including milliseconds as fractions",
            "Checks if you are currently in VR (1:Yes, 0:No)",
            "Checks if your headset is currently on your head (1:Yes, 0:No)",
            "Checks if the game window is focused (1:Yes, 0:No)",
            "The players current zoom input (Desktop only; 0:no zoom input, 1:max zoom input)",
            "The players current zoom level (Desktop only; 0:no zoom, 1:max zoom)",
            // Eyes
            "The horizontal movement of the left eye (-1 to +1 where 0 is centered)",
            "The vertical movement of the left eye (-1 to +1 where 0 is centered)",
            "The horizontal movement of the right eye (-1 to +1 where 0 is centered)",
            "The vertical movement of the right eye (-1 to +1 where 0 is centered)",
            "The current blink progress of the left eye (0:open, 1:closed)",
            "The current blink progress of the right eye (0:open, 1:closed)",
            // Lip Sync
            "The loudness of the voice input for single blendshape or jaw bone, the index of the currently used viseme otherwise\n0.000:sil\n0.071:PP\n0.143:FF\n0.214:TH\n0.286:DD\n0.357:kk\n0.429:CH\n0.500:SS\n0.571:nn\n0.643:RR\n0.714:aa\n0.786:E\n0.857:ih\n0.929:oh\n1.000:ou",
            // General cnt
            "The time in seconds since the VR headset was removed from the head",
            "The time in seconds since the game window became unfocused",
            "The time in seconds since the local player went AFK",
            "The time in seconds since the local avatar finished loading",
            "The download progress for the next world (0:0%, 1:100%)",
            "The FPS of the local player",
            "The ping to the game server of the local player",
            "The amount players in the current online instance",
            "The time in seconds since the first world join after starting the game",
            "The time in seconds since the last world join",
            "Checks if the local player is muted (1:Yes, 0:No)",
            "Checks if the local player has their HUD enabled (1:Yes, 0:No)",
            "Checks if the local player can currently see nameplates (1:Yes, 0:No)",
            "The current height of the player in cm (set in the menu)",
            "The type of the left VR controller\n0:None\n1:Other\n2:Vive\n3:MixedReality\n4:HpMotion\n5:Index\n6:Oculus\n7:Pico\n8:XRHand\n9:ViveCosmos",
            "The type of the right VR controller\n0:None\n1:Other\n2:Vive\n3:MixedReality\n4:HpMotion\n5:Index\n6:Oculus\n7:Pico\n8:XRHand\n9:ViveCosmos",
            "Checks if full body tracking is enabled and enough trackers are present (1:Yes, 0:No)",
            // Input
            "How far the trigger on your left VR controller is pressed (0 to 1)",
            "How far the trigger on your right VR controller is pressed or the left mouse button or the gamepads right trigger (0 to 1)",
            "How far the grip on your left VR controller is pressed (0 to 1)",
            "How far the grip on your right VR controller is pressed or the right mouse button or the gamepads left trigger (0 to 1)",
            "Checks if you have an object grabbed with your left hand (1:Yes, 0:No)",
            "Checks if you have an object grabbed with your right hand or in desktop mode (1:Yes, 0:No)",
            // Avatar
            "The viewpoint height of the current avatar in meters",
            "How upright the avatar stands (0: on the floor, 1:completely upright)",
            // Transform
            "The X component of the transforms global position",
            "The Y component of the transforms global position",
            "The Z component of the transforms global position",
            "The X component of the transforms global rotation in euler angles (0 to 360)",
            "The Y component of the transforms global rotation in euler angles (0 to 360)",
            "The Z component of the transforms global rotation in euler angles (0 to 360)",
            "The X component of the transforms local position",
            "The Y component of the transforms local position",
            "The Z component of the transforms local position",
            "The X component of the transforms local rotation in euler angles (0 to 360)",
            "The Y component of the transforms local rotation in euler angles (0 to 360)",
            "The Z component of the transforms local rotation in euler angles (0 to 360)",
            // Fluid Volume
            "Checks if the object is submerged in a fluid (1:Yes, 0:No)",
            "How submerged the object is (0:0%, 1:100%)",
            "The time in seconds since a fluid volume was entered",
            "The time in seconds since a fluid volume was exited",
            // Input Car
            "Gets the steering input (-1 to 1)\nVR: left stick\nKeyboard: A and D\nGamepad: left stick",
            "Gets the accelerate input\nVR: right trigger(0 to 1) left trigger (0 to -1)\nKeyboard: W:1 S:-1\nGamepad: right trigger(0 to 1) left trigger (0 to -1)",
            "Gets the brake input (0 to 1)\nVR: left trigger (0 to 1)\nKeyboard: S:1\nGamepad: left trigger (0 to 1)",
            "Gets the handbrake input (0 to 1)\nVR: right stick down\nKeyboard: Space\nGamepad: south face button",
            "Gets the boost input (0 to 1)\nVR: right stick up\nKeyboard: F\nGamepad: east face button",
            // Input
            "Gets the horizontal movement input (-1 to 1)\nVR: left stick\nKeyboard: A and D\nGamepad: left stick",
            "Gets the vertical movement input (-1 to 1)\nVR: left stick\nKeyboard: W and S\nGamepad: left stick",
            "Gets the horizontal look input (-1 to 1)\nVR: right stick\nMouse\nGamepad: right stick",
            "Gets the vertical look input (-1 to 1)\nVR: right stick\nMouse\nGamepad: right stick",
            "Gets the jump input (0 to 1)\nVR: Depending on binding\nKeyboard: Space\nGamepad:south face button",
            // Instance
            "Gets the current instance type\n0:Public\n1:Friends of Friends\n2:Friends\n3:Group Members Only\n4:Everyone Can Invite\n5:Owner Must Invite\n6:Group Friends Of Members\n7:Group Public",
            "Is the current instance a Private instance (1:Yes, 0:No)",
            "Is the current instance a Public instance (1:Yes, 0:No)",
            "Is the current instance a Group instance (1:Yes, 0:No)",
            // Rigidbody
            "Gets the current speed of the rigidbody in m/s",
            "Gets the X component of the rigidbody velocity in m/s in world space",
            "Gets the Y component of the rigidbody velocity in m/s in world space",
            "Gets the Z component of the rigidbody velocity in m/s in world space",
            "Gets the X component of the rigidbody velocity in m/s in local space",
            "Gets the Y component of the rigidbody velocity in m/s in local space",
            "Gets the Z component of the rigidbody velocity in m/s in local space",
            "Gets the current acceleration of the rigidbody in m/s²",
            "Gets the X component of the rigidbody acceleration in m/s² in world space",
            "Gets the Y component of the rigidbody acceleration in m/s² in world space",
            "Gets the Z component of the rigidbody acceleration in m/s² in world space",
            "Gets the X component of the rigidbody acceleration in m/s² in local space",
            "Gets the Y component of the rigidbody acceleration in m/s² in local space",
            "Gets the Z component of the rigidbody acceleration in m/s² in local space",
            // WheelHubController
            "Gets the forward speed of the wheel hub controller in m/s. Reads negative when reversing.",
            "Gets the speed factor of the wheel hub controller (0 to 1 where 1 is defined max speed)",
            "Checks if at least one Wheel touches the floor (1:Yes, 0:No)",
            // WheelController
            "Checks if the wheel touches the ground (1:Yes, 0:No)",
            "The current RPM of the Wheel\nYou can multiply the value with 0.016667 to get rotations per second instead",
            // Damage System
            "The current Health of an object health component or the players health",
            "The current Armor of an object health component or the players health",
            "The current Shield of an object health component or the players health",
            // NavMeshAgent
            "The current speed of a NavMeshAgent",
            "The remaining distance of a NavMeshAgent to its target",
            "Checks if the NavMeshAgent has a path to its target (1:Yes, 0:No)",
            "Checks if the NavMeshAgent is on an OffMeshLink (1:Yes, 0:No)",
            "Gets the type of the current OffMeshLink the NavMeshAgent is on\n0:Manual\n1:DropDown\n2:JumpAcross",
            // Leg
            "Checks if the leg is currently touching the ground (1:Yes, 0:No)",
            // Menu
            "Checks if any menu is currently open (1:Yes, 0:No)",
            "Checks if the main menu is currently open (1:Yes, 0:No)",
            "Checks if the quick menu is currently open (1:Yes, 0:No)",
            // General (cnt cnt)
            "Whether the player is typing in ChatBox or not (1:Yes, 0:No)",
            // Seed
            "A generated number relative to the spawner/wearer of a props/avatar",
            "A generated number relative to the instance id of a prop",
            //User
            "Checks if the current user is the spawner of a prop or if the avatar is local",
        };

        #endregion
        
        private ReorderableList _streamEntriesList;
        private int _selectedStreamEntry = -1;
        
        private void InitializeStreamEntriesList()
        {
            if (_streamEntriesList != null)
                return;

            _streamEntriesList ??= new ReorderableList(serializedObject, m_EntriesProp, 
                true, true, true, true)
            {
                drawHeaderCallback = OnDrawHeaderStreamEntry,
                onMouseUpCallback = OnMouseUpStreamEntry,
                drawElementCallback = OnDrawElementStreamEntry,
                elementHeightCallback = OnHeightElementStreamEntry,
                onChangedCallback = OnChangedStreamEntry,
                list = _stream.entries,
                index = -1
            };
        }
        
        private void DrawEntries()
        {
            if (!InnerFoldout(ref _guiEntriesFoldout, L("StreamEntries").text)) 
                return;
            
            InitializeStreamEntriesList();
            using (new SetIndentLevelScope(0))
            {
                using (new EditorGUILayout.VerticalScope(new GUIStyle() { padding = new RectOffset(15, 0, 5, 5) }))
                    _streamEntriesList.DoLayoutList();
            }
        }
        
        #region ReorderableListDrawing Stream Entries
        
        private void OnDrawHeaderStreamEntry(Rect rect)
        {
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 35, EditorGUIUtility.singleLineHeight);
            GUI.Label(labelRect, string.Format(L("StreamEntriesHeader").text, _streamEntriesList.count));
            EditorGUIExtensions.UtilityMenu(rect, _streamEntriesList, m_EntriesProp);
        }
        
        private void OnMouseUpStreamEntry(ReorderableList list)
        {
            if (list.index != _selectedStreamEntry)
            {
                _selectedStreamEntry = list.index;
            }
            else
            {
                if (list.index == -1)
                    return;
                
                list.Deselect(_selectedStreamEntry);
                list.index = _selectedStreamEntry = -1;
                Repaint();
            }
        }
        
        private void OnDrawElementStreamEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _streamEntriesList.count) return;
            SerializedProperty fprSettingsEntry = _streamEntriesList.serializedProperty.GetArrayElementAtIndex(index);
            if (fprSettingsEntry == null || index >= _streamEntriesList.serializedProperty.arraySize)
                return;

            rect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);
            float spacing = EditorGUIUtility.singleLineHeight * 1.25f;
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            
            SerializedProperty targetTypeProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.targetType));
            SerializedProperty typeProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.type));
            SerializedProperty targetProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.target));
            
            SerializedProperty applicationTypeProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.applicationType));
            SerializedProperty staticValueProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.staticValue));
            SerializedProperty parameterNameProp = fprSettingsEntry.FindPropertyRelative(nameof(CVRParameterStreamEntry.parameterName));
            
            switch (_stream.referenceType)
            {
                case CVRParameterStream.ReferenceType.World:
                    DrawWorldEntry(rect, spacing, typeProp, targetTypeProp, targetProp, applicationTypeProp, staticValueProp, parameterNameProp);
                    break;
                case CVRParameterStream.ReferenceType.Avatar:
                    DrawAvatarEntry(rect, spacing, typeProp, targetTypeProp, targetProp, applicationTypeProp, staticValueProp, parameterNameProp);
                    break;
                case CVRParameterStream.ReferenceType.Spawnable:
                    DrawSpawnableEntry(rect, spacing, typeProp, targetTypeProp, targetProp, applicationTypeProp, staticValueProp, parameterNameProp);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(); // death
            }
            
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private float OnHeightElementStreamEntry(int index)
        {
            const float lineModifier = 1.25f;
            const float baseHeight = 4f;
            if (index >= _stream.entries.Count) 
                return baseHeight * lineModifier * EditorGUIUtility.singleLineHeight;
            
            float additionalHeight = 0f;
            
            if (_stream.entries[index].targetType == CVRParameterStreamEntry.TargetType.Animator)
                additionalHeight += 1f; // parameter name / warning
            
            if ((int)_stream.entries[index].applicationType % 5 == 1)
                additionalHeight += 1f; // static value
            
            return (baseHeight + additionalHeight) * lineModifier * EditorGUIUtility.singleLineHeight;
        }
        
        private void OnChangedStreamEntry(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
        }
        
        #endregion

        #region Reference Type - World

        private void DrawWorldEntry(Rect rect, float spacing,
            SerializedProperty typeProp, SerializedProperty targetTypeProp, 
            SerializedProperty targetProp, SerializedProperty applicationTypeProp,
            SerializedProperty staticValueProp, SerializedProperty parameterNameProp)
        {
            
            //EditorGUI.PropertyField(rect, typeProp, new GUIContent("Input"));
            
            // TODO: Add more overloads for AdvancedDropdownInput
            int currentInput = typeProp.enumValueIndex;
            int selectedInput = EditorGUIExtensions.CustomPopup(rect,
                L("Input").text,
                currentInput,
                CategorizedTypes,
                L("StreamInputs").text,
                CategorizedHelpText);
            if (selectedInput != currentInput) typeProp.enumValueIndex = selectedInput;
            rect.y += spacing;

            targetTypeProp.intValue = EditorGUI.Popup(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), 
                L("Output").text,
                targetTypeProp.intValue,
                new []{L("Animator").text, L("VariableBuffer").text}
            );
            rect.y += spacing;
            
            EditorGUI.PropertyField(rect, targetProp);
            GameObject targetObject = targetProp.objectReferenceValue as GameObject;
            
            // verify that the target is valid for the target type
            Animator animator = null;
            if (targetObject != null)
            {
                switch (targetTypeProp.intValue)
                {
                    case (int) CVRParameterStreamEntry.TargetType.Animator:
                        animator = targetObject.GetComponent<Animator>();
                        if (animator == null) targetProp.objectReferenceValue = targetObject = null;
                        break;
                    case (int) CVRParameterStreamEntry.TargetType.VariableBuffer:
                        CVRVariableBuffer varBuffer = targetObject.GetComponent<CVRVariableBuffer>();
                        if (varBuffer == null) targetProp.objectReferenceValue = targetObject = null;
                        break;
                }
            }

            if (targetTypeProp.intValue == (int)CVRParameterStreamEntry.TargetType.Animator)
            {
                rect.y += spacing; 
                
                if (animator == null)
                {   // displaying one-line warning, so i dont need to handle this in OnHeightElementStreamEntry :)
                    EditorGUI.HelpBox(rect, L("AnimatorComponentMissingWarning").text, MessageType.Warning);
                }
                else
                {
                    parameterNameProp.stringValue = EditorGUIExtensions.AdvancedDropdownInput(rect,
                        parameterNameProp.stringValue,
                        CVRCommon.GetParametersFromAnimatorAsString(animator), // has null check
                        L("Parameter").text, L("NoParameters").text);
                }
            }
            
            rect.y += spacing;

            EditorGUI.PropertyField(rect, applicationTypeProp);
            if (applicationTypeProp.intValue % 5 != 1) 
                return;
            
            rect.y += spacing;
            EditorGUI.PropertyField(rect, staticValueProp);
        }

        #endregion

        #region Reference Type - Avatar
        
        private void DrawAvatarEntry(Rect rect, float spacing,
            SerializedProperty typeProp, SerializedProperty targetTypeProp, 
            SerializedProperty targetProp, SerializedProperty applicationTypeProp,
            SerializedProperty staticValueProp, SerializedProperty parameterNameProp)
        {
            //EditorGUI.PropertyField(rect, typeProp, new GUIContent("Input"));
            
            // TODO: Add more overloads for AdvancedDropdownInput
            int currentInput = typeProp.enumValueIndex;
            int selectedInput = EditorGUIExtensions.CustomPopup(rect,
                L("Input").text,
                currentInput,
                CategorizedTypes,
                L("StreamInputs").text,
                CategorizedHelpText);
            if (selectedInput != currentInput) typeProp.enumValueIndex = selectedInput;
            rect.y += spacing;
            
            if (targetTypeProp.intValue is not (0 or 2))
                targetTypeProp.intValue = 2; // force to 2 (AvatarAnimator)
            
            // TargetType on Avatar is either Animator (0) or AvatarAnimator (2)
            int selectedType = EditorGUI.Popup(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), 
                L("Output").text,
                targetTypeProp.intValue / 2,
                new []{L("SubAnimator").text, L("AvatarAnimator").text}
            );
            if (selectedType != targetTypeProp.intValue)
                targetTypeProp.intValue = 2 * selectedType;
            
            rect.y += spacing;

            var parameters = new List<string>();

            if (targetTypeProp.intValue == (int)CVRParameterStreamEntry.TargetType.AvatarAnimator)
            {
                if (_avatar.overrides != null && _avatar.overrides is { } overrideController &&
                    overrideController.runtimeAnimatorController is AnimatorController animatorController)
                    parameters.AddRange(CVRCommon.GetParametersFromControllerAsString(animatorController, CVRCommon.NonCoreFilter));
                
                parameterNameProp.stringValue = EditorGUIExtensions.AdvancedDropdownInput(rect,
                    parameterNameProp.stringValue,
                    parameters,
                    L("Parameter").text, L("NoParameters").text);
            }
            else if (targetTypeProp.intValue == (int)CVRParameterStreamEntry.TargetType.Animator)
            {
                EditorGUI.PropertyField(rect, targetProp);
                rect.y += spacing;

                GameObject targetObject = targetProp.objectReferenceValue as GameObject;
            
                // verify that the target is valid for the target type
                Animator animator = null;
                if (targetObject != null)
                {
                    animator = targetObject.GetComponent<Animator>();
                    if (animator == null) targetProp.objectReferenceValue = targetObject = null;
                }
                
                if (animator == null)
                {   // displaying one-line warning, so i dont need to handle this in OnHeightElementStreamEntry :)
                    EditorGUI.HelpBox(rect, L("AnimatorComponentMissingWarning").text, MessageType.Warning);
                }
                else
                {
                    parameters = CVRCommon.GetParametersFromAnimatorAsString(animator);
                    parameterNameProp.stringValue = EditorGUIExtensions.AdvancedDropdownInput(rect,
                        parameterNameProp.stringValue,
                        parameters,
                        L("Parameter").text, L("NoParameters").text);
                }
            }
            
            rect.y += spacing;

            EditorGUI.PropertyField(rect, applicationTypeProp);
            if (applicationTypeProp.intValue % 5 != 1) 
                return;
            
            rect.y += spacing;
            EditorGUI.PropertyField(rect, staticValueProp);
        }

        #endregion

        #region Reference Type - Spawnable
        
        private void DrawSpawnableEntry(Rect rect, float spacing,
            SerializedProperty typeProp, SerializedProperty targetTypeProp, 
            SerializedProperty targetProp, SerializedProperty applicationTypeProp,
            SerializedProperty staticValueProp, SerializedProperty parameterNameProp)
        {
            //EditorGUI.PropertyField(rect, typeProp, new GUIContent("Input"));
            
            // TODO: Add more overloads for AdvancedDropdownInput
            int currentInput = typeProp.enumValueIndex;
            int selectedInput = EditorGUIExtensions.CustomPopup(rect,
                L("Input").text,
                currentInput,
                CategorizedTypes,
                L("StreamInputs").text,
                CategorizedHelpText);
            if (selectedInput != currentInput) typeProp.enumValueIndex = selectedInput;
            
            rect.y += spacing;
            
            if (targetTypeProp.intValue is not (0 or 3))
                targetTypeProp.intValue = 3; // force to 3 (CustomFloat)
            
            // TargetType on Spawnable is either Animator (0) or CustomFloat (3)
            int selectedType = EditorGUI.Popup(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), 
                L("Output").text,
                targetTypeProp.intValue / 3,
                new []{L("Animator").text, L("CustomFloat").text}
            );
            if (selectedType != targetTypeProp.intValue)
                targetTypeProp.intValue = 3 * selectedType;
            
            rect.y += spacing;

            var parameters = new List<string>();

            if (targetTypeProp.intValue == (int)CVRParameterStreamEntry.TargetType.CustomFloat)
            {
                if (_spawnable != null)
                    parameters.AddRange(from parameter in _spawnable.syncValues
                        where !string.IsNullOrWhiteSpace(parameter.name)
                        select parameter.name);
                
                parameterNameProp.stringValue = EditorGUIExtensions.AdvancedDropdownInput(rect,
                    parameterNameProp.stringValue,
                    parameters,
                    L("SyncedValue").text, L("NoSyncedValues").text);
            }
            else if (targetTypeProp.intValue == (int)CVRParameterStreamEntry.TargetType.Animator)
            {
                EditorGUI.PropertyField(rect, targetProp);
                rect.y += spacing;

                GameObject targetObject = targetProp.objectReferenceValue as GameObject;
            
                // verify that the target is valid for the target type
                Animator animator = null;
                if (targetObject != null)
                {
                    animator = targetObject.GetComponent<Animator>();
                    if (animator == null) targetProp.objectReferenceValue = targetObject = null;
                }
                
                if (animator == null)
                {   // displaying one-line warning, so i dont need to handle this in OnHeightElementStreamEntry :)
                    EditorGUI.HelpBox(rect, L("AnimatorComponentMissingWarning").text, MessageType.Warning);
                }
                else
                {
                    parameters = CVRCommon.GetParametersFromAnimatorAsString(animator);
                    parameterNameProp.stringValue = EditorGUIExtensions.AdvancedDropdownInput(rect,
                        parameterNameProp.stringValue,
                        parameters,
                        L("Parameter").text, L("NoParameters").text);
                }
            }
            
            rect.y += spacing;

            EditorGUI.PropertyField(rect, applicationTypeProp);
            if (applicationTypeProp.intValue % 5 != 1) 
                return;
            
            rect.y += spacing;
            EditorGUI.PropertyField(rect, staticValueProp);
        }

        #endregion
    }
}
#endif