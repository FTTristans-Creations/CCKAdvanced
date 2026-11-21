using System;
using System.Collections.Generic;
using System.Reflection;
using ABI.CCK.Scripts.Editor;
using ABI.CCK.Scripts.Editor.Tools;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CustomEditor(typeof(CVRInteractable))]
    public class CCK_CVRInteractableEditor : CCKBaseLocalizedEditor
    {
        // CVRInteractableAction
        private string[] s_TimerModes => new[] { L("TimerModeSingle").text, L("TimerModeRepeat").text, L("TimerModeDeactivateSelf").text };
        private string[] s_VariableComparisons => new[] { L("VariableComparisonBufferStatic").text, L("VariableComparisonBufferBuffer").text };
        private string[] s_VariableComparators => new[] { "==", ">=", ">", "<", "<=", "!=" };
        
        private static readonly (string[], CVRInteractableAction.ActionRegister[]) s_WorldTriggerTypes = EnumFilter
            .GetFilteredEnumValues<CVRInteractableAction.ActionRegister, CVRInteractableAction.AllowWorld>();
        
        private static readonly (string[], CVRInteractableAction.ActionRegister[]) s_PropTriggerTypes = EnumFilter
            .GetFilteredEnumValues<CVRInteractableAction.ActionRegister, CVRInteractableAction.AllowSpawnable>();
        
        // CVRInteractableActionOperation
        private string[] s_ObjectStates => new[] { L("ObjectStateEnable").text, L("ObjectStateDisable").text, L("ObjectStateToggle").text };
        private string[] s_ArithmeticConstellations => new[] { L("ArithmeticBufferStatic").text, L("ArithmeticBufferBuffer").text, L("ArithmeticBufferRandom").text };
        private string[] s_ArithmeticOperators => new[] { "+", "-", "*", "÷", "mod", "pow", "log" };
        private string[] s_SpawnableUpdateTypes => new[] { L("SpawnableUpdateOverride").text, L("SpawnableUpdateAdd").text, L("SpawnableUpdateSubtract").text, L("SpawnableUpdateToggle").text };
        private string[] s_ApfSetTypes => new[] { L("ApfSetTypeStatic").text, L("ApfSetTypeVariableBuffer").text };
        private string[] s_ApfSetTypesString => new[] { L("ApfSetTypeStatic").text, L("ApfSetTypeProperty").text };
        private string[] s_PlayerTeleportOptions => new[] { L("PlayerTeleportWorldUp").text, L("PlayerTeleportTargetUp").text, L("PlayerTeleportGravity").text, L("PlayerTeleportNone").text };
        private string[] s_DestroyGameObjectOptions => new[] { L("DestroyTarget").text, L("DestroyChildren").text };
        
        private static readonly (string[], CVRInteractableActionOperation.ActionType[]) s_WorldActionTypes = EnumFilter
            .GetFilteredEnumValues<CVRInteractableActionOperation.ActionType, CVRInteractableActionOperation.AllowWorld>();
        
        private static readonly (string[], CVRInteractableActionOperation.ActionType[]) s_PropActionTypes = EnumFilter
            .GetFilteredEnumValues<CVRInteractableActionOperation.ActionType, CVRInteractableActionOperation.AllowSpawnable>();
        
        #region Private Variables

        private CVRInteractable _interactable;
        private bool _isWithinStandaloneProp;

        #endregion Private Variables
        
        #region Serialized Properties
        
        private SerializedProperty m_TooltipProp;
        private SerializedProperty m_ActionsProp;
        
        private SerializedProperty m_OnEnterSeatProp;
        private SerializedProperty m_OnExitSeatProp;
        
        #endregion Serialized Properties
        
        #region Unity Events
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (!target) return;
            _interactable = (CVRInteractable)target;
            
            // Check if we are in a prefab
            if (!_interactable.gameObject.scene.IsValid())
                return;

            if (CCKCommonTools.FindFirstInstanceInScene<CVRWorld>(_interactable.gameObject.scene) == null)
                _isWithinStandaloneProp = _interactable.GetComponentInParent<CVRSpawnable>() != null;
            
            m_TooltipProp = serializedObject.FindProperty(nameof(CVRInteractable.tooltip));
            m_ActionsProp = serializedObject.FindProperty(nameof(CVRInteractable.actions));
            m_OnEnterSeatProp = serializedObject.FindProperty(nameof(CVRInteractable.onEnterSeat));
            m_OnExitSeatProp = serializedObject.FindProperty(nameof(CVRInteractable.onExitSeat));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (!_interactable) 
                return;

            DrawLocalizationWarning();
            
            Draw_VersionWarnings();
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Play Mode Actions", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                
                // loop through all actions and find the used triggers
                var actionTypes = _interactable.GetUsedActionTypes();
                
                // draw button to invoke each trigger manually
                foreach (var actionType in actionTypes)
                {
                    Rect controlRect = EditorGUILayout.GetControlRect();
                    if (GUI.Button(controlRect, actionType.ToString()))
                        _interactable.InvokeActionType(actionType);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            Separator();
            
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_TooltipProp, L("Tooltip"));
            
            EditorGUILayout.Space();

            Draw_TriggerEntries();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion Unity Events

        #region Drawing Methods

        private void Draw_VersionWarnings()
        {
            // Display a warning if it is a legacy setup
            if (_interactable.IsLegacyUiSetup)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.HelpBox(L("LegacyUiSetupWarning").text, MessageType.Warning);
                
                if (GUILayout.Button(L("AutoFix").text, GUILayout.Width(80), GUILayout.ExpandHeight(true))) 
                    _interactable.AutoFixLegacyUiSetup();
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private static bool DrawEntryHeader(SerializedProperty property, SerializedProperty arrayProp, int index, string foldoutHeader = null)
        {
            using (new EditorGUILayout.HorizontalScope("helpbox"))
            {
                // Draw custom foldout arrow
                property.isExpanded = EditorGUILayout.Toggle(property.isExpanded, EditorStyles.foldout, GUILayout.Width(15));
                
                // Draw the foldoutHeader if it exists
                if (foldoutHeader != null)
                {
                    Rect labelRect = EditorGUILayout.GetControlRect();
                    EditorGUI.LabelField(labelRect, foldoutHeader, EditorStyles.boldLabel);
                    if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                    {
                        property.isExpanded = !property.isExpanded;
                        Event.current.Use();
                    }
                }
            
                GUILayout.FlexibleSpace();
                
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    // Move Up
                    GUI.enabled = index > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(24)) && index > 0)
                        arrayProp.MoveArrayElement(index, index - 1);
                
                    // Move Down
                    GUI.enabled = index < arrayProp.arraySize - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(24)) && index < arrayProp.arraySize - 1)
                        arrayProp.MoveArrayElement(index, index + 1);
                    
                    GUI.enabled = true;
                }
                
                // Remove
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    arrayProp.DeleteArrayElementAtIndex(index);
                    return true;
                }
                
                return false;
            }
        }
        
        private void DrawEnumPopup<TEnum>(
            string label,
            SerializedProperty enumProp,
            (string[], TEnum[]) standaloneOptions,
            (string[], TEnum[]) worldOptions,
            bool isStandaloneProp)
            where TEnum : struct, Enum
        {
            TEnum enumValue = (TEnum)(object)enumProp.intValue;
            (string[] names, TEnum[] values) options = isStandaloneProp ? standaloneOptions : worldOptions;

            int selectedIndex = Array.IndexOf(options.values, enumValue);
            bool isValid = selectedIndex >= 0;

            var displayNames = new List<string>(options.names);
            var displayValues = new List<TEnum>(options.values);

            if (!isValid)
            {
                string invalidLabel = string.Format(L("InvalidEnum").text, enumValue.ToString());
                displayNames.Insert(0, invalidLabel);
                displayValues.Insert(0, enumValue);
                selectedIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            
            int newSelectedIndex = EditorGUIExtensions.CustomPopup(EditorGUILayout.GetControlRect(),
                label,
                selectedIndex,
                displayNames.ToArray(),
                label);
            
            if (EditorGUI.EndChangeCheck() && (isValid || newSelectedIndex != 0)) enumProp.intValue = Convert.ToInt32(displayValues[newSelectedIndex]);
            if (!isValid) EditorGUILayout.HelpBox(string.Format(L("InvalidEnumWarning").text, label.ToLower()), MessageType.Warning);
        }
        
        #region Draw Trigger Entries
        
        private void Draw_TriggerEntries()
        {
            if (GUILayout.Button(L("AddTrigger").text))
            {
                m_ActionsProp.AddWithDefaults(typeof(CVRInteractableAction));
                return; // exit early, drawing now will reset the properties we just set
            }
            if (m_ActionsProp.arraySize == 0) return;
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < m_ActionsProp.arraySize; i++)
            {
                SerializedProperty entry = m_ActionsProp.GetArrayElementAtIndex(i);
                
                SerializedProperty actionTypeProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.actionType));
                string selectedActionString = actionTypeProp.enumValueIndex >= 0 && actionTypeProp.enumValueIndex < actionTypeProp.enumDisplayNames.Length
                    ? actionTypeProp.enumDisplayNames[actionTypeProp.enumValueIndex]
                    : L("Invalid").text;
                
                SerializedProperty operationsProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.operations));
                int operationCount = operationsProp.arraySize;
                
                string foldoutHeader = string.Format(L("TriggerEntryHeader").text, selectedActionString, operationCount);

                bool wasRemoved = DrawEntryHeader(entry, m_ActionsProp, i, foldoutHeader);
                if (wasRemoved) return; // exit early if the entry was removed
                if (!entry.isExpanded) continue; // skip drawing the entry if it's not expanded
                
                EditorGUILayout.BeginVertical("GroupBox");
                Draw_TriggerEntry(entry);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
            return;
            #region Trigger Entry Drawing
            void Draw_TriggerEntry(SerializedProperty entry)
            {
                // Get all serialized properties for this entry
                #region Serialized Property Getters

                SerializedProperty operations = entry.FindPropertyRelative(nameof(CVRInteractableAction.operations));
                
                SerializedProperty actionTypeProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.actionType));
                SerializedProperty execTypeProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.execType));
                SerializedProperty guidProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.guid));
                
                SerializedProperty delayProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.delay));
                SerializedProperty layerMaskProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.layerMask));
                
                SerializedProperty floatValProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.floatVal));
                SerializedProperty floatVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableAction.floatVal2));
                SerializedProperty floatVal3Prop = entry.FindPropertyRelative(nameof(CVRInteractableAction.floatVal3));
                
                SerializedProperty boolValProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.boolVal));
                SerializedProperty boolVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableAction.boolVal2));
                
                SerializedProperty stringValProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.stringVal));
                
                SerializedProperty varBufferValProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.varBufferVal));
                SerializedProperty varBufferVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableAction.varBufferVal2));
                
                SerializedProperty allowedPointerProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.allowedPointer));
                SerializedProperty allowedTypesProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.allowedTypes));
                SerializedProperty specificParticleSystemsProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.specificParticleSystems));

                SerializedProperty interactionFilterProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.interactionFilter));
                SerializedProperty interactionInputProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.interactionInput));
                SerializedProperty interactionInputModifierProp = entry.FindPropertyRelative(nameof(CVRInteractableAction.interactionInputModifier));

                #endregion
                
                DrawEnumPopup(
                    L("TriggerType").text,
                    actionTypeProp,
                    s_PropTriggerTypes,
                    s_WorldTriggerTypes,
                    _isWithinStandaloneProp);

                #region Draw Trigger-Shared Fields
                
                // Draw broadcast type dropdown if on world (execTypeProp), unless its an APF trigger for some reason
                bool isApf = actionTypeProp.intValue 
                    is (int)CVRInteractableAction.ActionRegister.OnAPFTrigger
                    or (int)CVRInteractableAction.ActionRegister.OnAPFBoolChange
                    or (int)CVRInteractableAction.ActionRegister.OnAPFIntChange
                    or (int)CVRInteractableAction.ActionRegister.OnAPFFloatChange
                    or (int)CVRInteractableAction.ActionRegister.OnAPFStringChange;
                
                if (isApf)
                {
                    EditorGUILayout.PropertyField(stringValProp, L("Key"));
                }
                else
                {
                    // is fine to hide when standalone instead of enforcing value, as client will set the type to LocalNotNetworked on filter
                    if (!_isWithinStandaloneProp)
                    {
                        EditorGUILayout.PropertyField(execTypeProp, L("BroadcastType"));
                        // If it is not LocalNotNetworked, display the network id for this action
                        if (execTypeProp.intValue != (int)CVRInteractableAction.ExecutionType.LocalNotNetworked)
                            DrawNetworkIdField(guidProp);
                    }
                    EditorGUILayout.PropertyField(delayProp, L("DelaySeconds"));
                }
                
                #endregion

                #region Draw Trigger-Specific Fields
                
                switch (actionTypeProp.intValue)
                {
                    // Controller interaction
                    case (int) CVRInteractableAction.ActionRegister.OnInteractDown:
                    case (int) CVRInteractableAction.ActionRegister.OnInteractUp:
                    case (int) CVRInteractableAction.ActionRegister.OnHoverEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnHoverExit:
                        EditorGUILayout.PropertyField(floatValProp, L("Distance"));
                        break;
                    
                    // Collider / Trigger interaction
                    case (int) CVRInteractableAction.ActionRegister.OnTriggerEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnTriggerExit:
                    case (int) CVRInteractableAction.ActionRegister.OnColliderEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnColliderExit:
                        EditorGUILayout.PropertyField(layerMaskProp, L("LayerMask"));
                        break;
                    
                    // Player Trigger / Collider interaction
                    case (int) CVRInteractableAction.ActionRegister.OnPlayerTriggerEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnPlayerTriggerExit:
                    case (int) CVRInteractableAction.ActionRegister.OnPlayerColliderEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnPlayerColliderExit:
                        EditorGUILayout.PropertyField(boolValProp, L("LocalPlayer"));
                        EditorGUILayout.PropertyField(boolVal2Prop, L("RemotePlayer"));
                        if (!boolValProp.boolValue && !boolVal2Prop.boolValue) 
                            EditorGUILayout.HelpBox(L("AtLeastOnePlayerRequired").text, MessageType.Error);
                        break;
                    
                    // Timer
                    case (int) CVRInteractableAction.ActionRegister.OnTimer:
                        EditorGUILayout.PropertyField(floatValProp, L("InvokeRateSeconds"));
                        floatVal2Prop.floatValue = EditorGUILayout.Popup(L("TimerMode").text, (int)floatVal2Prop.floatValue, s_TimerModes);
                        break;
                    
                    // Variable Buffer Update
                    case (int) CVRInteractableAction.ActionRegister.OnVariableBufferUpdate:
                        EditorGUILayout.PropertyField(varBufferValProp, L("Value"));
                        break;
                    
                    // Variable Buffer Comparison
                    case (int) CVRInteractableAction.ActionRegister.OnVariableBufferComparision:
                        floatValProp.floatValue = EditorGUILayout.Popup(L("Type").text, (int)floatValProp.floatValue, s_VariableComparisons);
                        varBufferValProp.objectReferenceValue = EditorGUILayout.ObjectField(L("Value1").text, varBufferValProp.objectReferenceValue, typeof(CVRVariableBuffer), true);
                        
                        floatVal2Prop.floatValue = EditorGUILayout.Popup(L("Comparator").text, (int)floatVal2Prop.floatValue, s_VariableComparators);
                        
                        if (floatValProp.floatValue == 0) floatVal3Prop.floatValue = EditorGUILayout.FloatField(L("Value2").text, floatVal3Prop.floatValue);
                        else
                            varBufferVal2Prop.objectReferenceValue = EditorGUILayout.ObjectField(L("Value2").text, varBufferVal2Prop.objectReferenceValue, typeof(CVRVariableBuffer), true);
                        break;
                    
                    // Cron
                    case (int) CVRInteractableAction.ActionRegister.OnCron:
                        EditorGUILayout.PropertyField(stringValProp, L("CronExpression"));
                        break;
                    
                    // TODO: This is not actually implemented on-client
                    // World Trigger
                    case (int) CVRInteractableAction.ActionRegister.OnWorldTrigger:
                        EditorGUILayout.PropertyField(floatValProp, L("TriggerIndex"));
                        EditorGUILayout.HelpBox(L("WorldTriggerNotImplemented").text, MessageType.Warning);
                        break;
                    
                    // Particle Hit
                    case (int) CVRInteractableAction.ActionRegister.OnParticleHit:
                        Rect particleRect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(specificParticleSystemsProp));
                        EditorGUI.PropertyField(new Rect(particleRect.x + 15f, particleRect.y, particleRect.width - 19f, particleRect.height), specificParticleSystemsProp,
                            L("SpecificParticleSystems"));
                        break;
                    
                    // Pointer Interaction
                    case (int) CVRInteractableAction.ActionRegister.OnPointerEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnPointerExit:
                        Rect pointerRect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(allowedPointerProp));
                        EditorGUI.PropertyField(new Rect(pointerRect.x + 15f, pointerRect.y, pointerRect.width - 19f, pointerRect.height), allowedPointerProp,
                            L("AllowedPointers"));
                        Rect typesRect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(allowedTypesProp));
                        EditorGUI.PropertyField(new Rect(typesRect.x + 15f, typesRect.y, typesRect.width - 19f, typesRect.height), allowedTypesProp,
                            L("AllowedTypes"));
                        break;
                    
                    // Input Interaction
                    case (int) CVRInteractableAction.ActionRegister.OnInputDown:
                    case (int) CVRInteractableAction.ActionRegister.OnInputUp:
                        EditorGUILayout.PropertyField(interactionFilterProp, L("InteractionFilter"));
                        EditorGUILayout.PropertyField(interactionInputProp, L("InteractionInput"));
                        EditorGUILayout.PropertyField(interactionInputModifierProp, L("InteractionInputModifier"));
                        break;
                    
                    // Visible / Invisible
                    case (int) CVRInteractableAction.ActionRegister.OnBecameVisible:
                    case (int) CVRInteractableAction.ActionRegister.OnBecameInvisible:
                        if (_interactable != null && !_interactable.TryGetComponent(out Renderer _))
                            EditorGUILayout.HelpBox(L("RendererRequiredError").text, MessageType.Error);
                        break;
                    
                    // State Machine Callbacks
                    case (int) CVRInteractableAction.ActionRegister.OnStateMachineEnter:
                    case (int) CVRInteractableAction.ActionRegister.OnStateMachineExit:
                        if (_interactable != null && _interactable.GetComponentsInParent<Animator>() == null)
                            EditorGUILayout.HelpBox(L("AnimatorRequiredError").text, MessageType.Error);
                        EditorGUILayout.PropertyField(stringValProp, L("StateTag"));
                        break;
                }
                
                #endregion
                
                SharedComponentGUI.Separator();
                Draw_OperationEntries(operations, entry);
            }
            #endregion
        }
        
        #endregion
        
        #region Draw Operation Entries
        
        private void Draw_OperationEntries(SerializedProperty property, SerializedProperty parentProperty)
        {
            if (GUILayout.Button(L("AddAction").text))
            {
                property.AddWithDefaults(typeof(CVRInteractableActionOperation));
                return; // exit early, drawing now will reset the properties we just set
            }
            if (property.arraySize == 0) return;
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty entry = property.GetArrayElementAtIndex(i);
                
                SerializedProperty operationTypeProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.type));
                string selectedOperationString = operationTypeProp.enumValueIndex >= 0 && operationTypeProp.enumValueIndex < operationTypeProp.enumDisplayNames.Length
                    ? operationTypeProp.enumDisplayNames[operationTypeProp.enumValueIndex]
                    : L("Invalid").text;
                
                string foldoutHeader = selectedOperationString;
                
                bool wasRemoved = DrawEntryHeader(entry, property, i, foldoutHeader);
                if (wasRemoved) return; // exit early if the entry was removed
                if (!entry.isExpanded) continue; // skip drawing the entry if it's not expanded
                
                EditorGUILayout.BeginVertical("GroupBox");
                Draw_OperationEntry(entry);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
            return;
            #region Operation Entry Drawing

            void Draw_OperationEntry(SerializedProperty entry)
            {
                #region Serialized Property Getters

                SerializedProperty typeProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.type));
                SerializedProperty targetsProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.targets));

                SerializedProperty gameObjectValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.gameObjectVal));

                SerializedProperty floatValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.floatVal));
                SerializedProperty floatVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.floatVal2));
                SerializedProperty floatVal3Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.floatVal3));
                SerializedProperty floatVal4Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.floatVal4));
                
                SerializedProperty boolValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.boolVal));
                SerializedProperty boolVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.boolVal2));

                SerializedProperty stringValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.stringVal));
                SerializedProperty stringVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.stringVal2));
                SerializedProperty stringVal3Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.stringVal3));
                SerializedProperty stringVal4Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.stringVal4));

                SerializedProperty varBufferValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.varBufferVal));
                SerializedProperty varBufferVal2Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.varBufferVal2));
                SerializedProperty varBufferVal3Prop = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.varBufferVal3));

                SerializedProperty animationValProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.animationVal));
                SerializedProperty customEventProp = entry.FindPropertyRelative(nameof(CVRInteractableActionOperation.customEvent));

                #endregion

                // As many fields are recycled between operation types, we need to reset them when the type changes
                
                int cachedType = typeProp.intValue;
                DrawEnumPopup(
                    L("ActionType").text,
                    typeProp,
                    s_PropActionTypes,
                    s_WorldActionTypes,
                    _isWithinStandaloneProp);
                
                if (cachedType != typeProp.intValue)
                {
                    cachedType = typeProp.intValue; // Cache the new type
                    
                    entry.SetDefaultValuesForType(typeof(CVRInteractableActionOperation));
                    targetsProp.ClearArray(); // TODO: figure out why SetDefaultValuesForType doesn't reset this
                    
                    // ensure one target is always present
                    targetsProp.arraySize = 1;
                    
                    typeProp.intValue = cachedType; // Set the new type back
                    entry.serializedObject.ApplyModifiedProperties(); // Apply the changes to the serialized object
                }
                
                switch (typeProp.intValue)
                {
                    case (int) CVRInteractableActionOperation.ActionType.SetGameObjectActive:
                        EditorGUILayout.HelpBox(L("SetGameObjectActiveHelp").text, MessageType.None);
                        DrawTargetsProp();
                        
                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetGameObjectState").text, (int)floatValProp.floatValue, s_ObjectStates);
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetComponentActive:
                        EditorGUILayout.HelpBox(L("SetComponentActiveHelp").text, MessageType.None);
                        DrawTargetsProp();

                        // Displays the first targets components in a popup to choose from, client then sets active state of every instance
                        // of that component on all targets when running the action
                        
                        bool multipleOfSameComponent = false;
                        Type selectedType = null;
                        
                        SerializedProperty firstTargetProp = targetsProp.arraySize > 0 ? targetsProp.GetArrayElementAtIndex(0) : null;
                        if (firstTargetProp is { objectReferenceValue: GameObject gameObj })
                        {
                            var components = gameObj.GetComponents<Component>();
                            var componentNames = new List<string> { L("NoneOption").text };
                            var componentTypes = new List<Type> { null };
                            
                            int selectedIndex = 0;
                            for (int i = 1; i < components.Length; i++) // Skip first component (Transform)
                            {
                                Type type = components[i].GetType();
                                if (componentTypes.Contains(type))
                                {
                                    multipleOfSameComponent = true;   
                                    continue;
                                }
                                
                                componentNames.Add(type.Name);
                                componentTypes.Add(type);

                                // Check if this type matches the current selection
                                if (stringValProp.stringValue != type.AssemblyQualifiedName)
                                    continue;
                                
                                selectedIndex = componentTypes.Count - 1; // Index in the filtered arrays
                                selectedType = type;
                            }

                            int newIndex = EditorGUILayout.Popup(L("ComponentType").text, selectedIndex, componentNames.ToArray());
                            stringValProp.stringValue = newIndex > 0 ? componentTypes[newIndex].AssemblyQualifiedName : null;
                            if (newIndex > 0) selectedType = componentTypes[newIndex];
                        }

                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetComponentState").text, (int)floatValProp.floatValue, s_ObjectStates);
                        
                        if (stringValProp.stringValue != null) DrawWarningIfMissingRequiredComponentInTargets(Type.GetType(stringValProp.stringValue));
                        if (multipleOfSameComponent && selectedType != null) DrawInfoIfMultipleComponentsMatchTarget(selectedType);
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorFloatValue:
                        EditorGUILayout.HelpBox(L("SetAnimatorFloatValueHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(floatValProp, L("Value"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorBoolValue:
                        EditorGUILayout.HelpBox(L("SetAnimatorBoolValueHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(boolValProp, L("Value"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorIntValue:
                        EditorGUILayout.HelpBox(L("SetAnimatorIntValueHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(floatValProp, L("Value"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorTrigger:
                        EditorGUILayout.HelpBox(L("SetAnimatorTriggerHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.SpawnObject:
                        EditorGUILayout.HelpBox(L("SpawnObjectHelp").text, MessageType.None);
                        DrawTargetsProp(L("Objects"));
                        EditorGUILayout.PropertyField(gameObjectValProp, L("TargetParentObject"));
                        EditorGUILayout.PropertyField(boolValProp, L("AutoPickup"));
                        EditorGUILayout.PropertyField(boolVal2Prop, L("AutoAttach"));
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.TeleportPlayer:
                        EditorGUILayout.HelpBox(L("TeleportPlayerHelp").text, MessageType.None);
                        floatValProp.floatValue = EditorGUILayout.Popup(L("PlayerUpAlignment").text, (int)floatValProp.floatValue, s_PlayerTeleportOptions);
                        EditorGUILayout.PropertyField(gameObjectValProp, L("TargetLocation"));
                        EditorGUILayout.PropertyField(boolValProp, L("RelativeTeleport"));
                        EditorGUILayout.PropertyField(boolVal2Prop, L("PreserveVelocity"));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.TeleportObject:
                        EditorGUILayout.HelpBox(L("TeleportObjectHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(gameObjectValProp, L("TargetLocation"));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.ToggleAnimatorBoolValue:
                        EditorGUILayout.HelpBox(L("ToggleAnimatorBoolValueHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorFloatRandom:
                        EditorGUILayout.HelpBox(L("SetAnimatorFloatRandomHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(floatValProp, L("MinValue"));
                        EditorGUILayout.PropertyField(floatVal2Prop, L("MaxValue"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorBoolRandom:
                        EditorGUILayout.HelpBox(L("SetAnimatorBoolRandomHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(floatValProp, L("TrueChance"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorIntRandom: // There is no rounding involved, just raw cast?
                        EditorGUILayout.HelpBox(L("SetAnimatorIntRandomHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(floatValProp, L("MinValue"));
                        EditorGUILayout.PropertyField(floatVal2Prop, L("MaxValue"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorFloatByVar:
                        EditorGUILayout.HelpBox(L("SetAnimatorFloatByVarHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(varBufferValProp, L("VariableBufferValue"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorIntByVar:
                        EditorGUILayout.HelpBox(L("SetAnimatorIntByVarHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        EditorGUILayout.PropertyField(varBufferValProp, L("VariableBufferValue"));
                        DrawWarningIfMissingRequiredComponentInTargets(typeof(Animator));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.VariableBufferArithmetic:
                        EditorGUILayout.HelpBox(L("VariableBufferArithmeticHelp").text, MessageType.None);
                        floatValProp.floatValue = EditorGUILayout.Popup(L("Type").text, (int)floatValProp.floatValue, s_ArithmeticConstellations);
                        EditorGUILayout.PropertyField(varBufferValProp, L("Value1"));
                        floatVal2Prop.floatValue = EditorGUILayout.Popup(L("Operator").text, (int)floatVal2Prop.floatValue, s_ArithmeticOperators);
                        switch ((int) floatValProp.floatValue)
                        {
                            case 0:
                                EditorGUILayout.PropertyField(floatVal3Prop, L("Value2"));
                                break;
                            case 1:
                                EditorGUILayout.PropertyField(varBufferVal2Prop, L("Value2"));
                                break;
                            case 2:
                                EditorGUILayout.PropertyField(floatVal3Prop, L("Min"));
                                EditorGUILayout.PropertyField(floatVal4Prop, L("Max"));
                                break;
                        }
                        EditorGUILayout.PropertyField(varBufferVal3Prop, L("Result"));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.DisplayWorldDetailPage:
                        EditorGUILayout.HelpBox(L("DisplayWorldDetailPageHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("WorldId"));
                        DrawWarningIfGUIDIsInvalid(stringValProp.stringValue);
                        break;
                    /*case (int) CVRInteractableActionOperation.ActionType.DisplayInstanceDetailPage: // how tf do you get the instance id as a normal user? they dont persist either??
                        EditorGUILayout.HelpBox(L("DisplayInstanceDetailPageHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("InstanceId"));
                        DrawWarningIfGUIDIsInvalid(stringValProp.stringValue);
                        break;*/
                    case (int) CVRInteractableActionOperation.ActionType.DisplayAvatarDetailPage:
                        EditorGUILayout.HelpBox(L("DisplayAvatarDetailPageHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("AvatarId"));
                        DrawWarningIfGUIDIsInvalid(stringValProp.stringValue);
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.DisplaySpawnableDetailPage:
                        EditorGUILayout.HelpBox(L("DisplaySpawnableDetailPageHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("SpawnableId"));
                        DrawWarningIfGUIDIsInvalid(stringValProp.stringValue);
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.DisplayGroupDetailPage:
                        EditorGUILayout.HelpBox(L("DisplayGroupDetailPageHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("GroupId"));
                        DrawWarningIfGUIDIsInvalid(stringValProp.stringValue);
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.SitAtPosition:
                        EditorGUILayout.HelpBox(L("SitAtPositionHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(gameObjectValProp, L("SittingLocation"));
                        
                        // First target is used as Exit Location
                        SerializedProperty exitLocationProp = targetsProp.arraySize > 0 ? targetsProp.GetArrayElementAtIndex(0) : null;
                        if (exitLocationProp != null) EditorGUILayout.PropertyField(exitLocationProp, L("ExitLocation"));
                        
                        EditorGUILayout.PropertyField(animationValProp, L("OverwriteSitAnimation"));
                        EditorGUILayout.PropertyField(boolValProp, L("LockControls"));
                        
                        // This is scuffed tbh
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_OnEnterSeatProp, L("OnEnterSeat"));
                        EditorGUILayout.PropertyField(m_OnExitSeatProp, L("OnExitSeat"));
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.MethodCall:
                        EditorGUILayout.HelpBox(L("MethodCallHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(customEventProp, L("CustomEvent"), true);
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.SetSpawnableSyncValue:
                        EditorGUILayout.HelpBox(L("SetSpawnableSyncValueHelp").text, MessageType.None);
                        CVRSpawnable spawnable = _interactable.GetComponentInParent<CVRSpawnable>();
                        if (spawnable == null || !spawnable.useAdditionalValues)
                        {
                            EditorGUILayout.HelpBox(L("NoSpawnableOrValuesError").text, MessageType.Error);
                        }
                        else
                        {
                            var spawnableParameters = new List<string> { L("NoneOption").text };
                            foreach (CVRSpawnableValue syncValue in spawnable.syncValues) spawnableParameters.Add(syncValue.name);
                            floatVal2Prop.floatValue = EditorGUILayout.Popup(L("Parameter").text, (int) floatVal2Prop.floatValue + 1, spawnableParameters.ToArray()) - 1;
                            EditorGUILayout.PropertyField(floatValProp, L("Value"));
                            floatVal3Prop.floatValue = EditorGUILayout.Popup(L("UpdateMethod").text, (int) floatVal3Prop.floatValue, s_SpawnableUpdateTypes);
                        }
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.PlayAudio:
                    case (int) CVRInteractableActionOperation.ActionType.StopAudio:
                        EditorGUILayout.HelpBox(L("PlayStopAudioHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(gameObjectValProp, L("AudioSource"));
                        DrawWarningIfMissingRequiredComponentInGameObject(typeof(AudioSource));
                        break;

                    #region APF Operations
                    
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorFloatByAPF: // Not implemented feature tmk, something server side -> client message
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorIntByAPF:
                    case (int) CVRInteractableActionOperation.ActionType.SetAnimatorBoolByAPF:
                        EditorGUILayout.HelpBox(L("SetAnimatorByApfHelp").text, MessageType.None);
                        DrawTargetsProp();
                        EditorGUILayout.PropertyField(stringValProp, L("ParameterName"));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.SetVariableBufferByAPF:
                        EditorGUILayout.HelpBox(L("SetVariableBufferByApfHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(varBufferValProp, L("VariableBuffer"));
                        break;
                    
                    case (int) CVRInteractableActionOperation.ActionType.UpdateAPFTrigger:
                        EditorGUILayout.HelpBox(L("UpdateApfTriggerHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("Key"));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.UpdateAPFBool:
                        EditorGUILayout.HelpBox(L("UpdateApfBoolHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("Key"));
                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetVia").text, (int) floatValProp.floatValue, s_ApfSetTypesString);
                        EditorGUILayout.PropertyField(floatValProp.floatValue == 0 ? boolValProp : varBufferValProp, L("Value"));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.UpdateAPFInt:
                        EditorGUILayout.HelpBox(L("UpdateApfIntHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("Key"));
                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetVia").text, (int) floatValProp.floatValue, s_ApfSetTypes);
                        EditorGUILayout.PropertyField(floatValProp.floatValue == 0 ? floatVal2Prop : varBufferValProp, L("Value"));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.UpdateAPFFloat:
                        EditorGUILayout.HelpBox(L("UpdateApfFloatHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("Key"));
                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetVia").text, (int) floatValProp.floatValue, s_ApfSetTypes);
                        EditorGUILayout.PropertyField(floatValProp.floatValue == 0 ? floatVal2Prop : varBufferValProp, L("Value"));
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.UpdateAPFString:
                        EditorGUILayout.HelpBox(L("UpdateApfStringHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(stringValProp, L("Key"));

                        floatValProp.floatValue = EditorGUILayout.Popup(L("SetVia").text, (int)floatValProp.floatValue, s_ApfSetTypes);

                        if (floatValProp.floatValue == 0) // Set via direct value
                        {
                            EditorGUILayout.PropertyField(stringVal2Prop, L("Value"));
                        }
                        else // Set via property from a component
                        {
                            EditorGUILayout.PropertyField(gameObjectValProp, L("Target"));

                            if (gameObjectValProp.objectReferenceValue != null)
                            {
                                DrawComponentSelection(stringVal3Prop, gameObjectValProp, L("ComponentType").text);

                                if (!string.IsNullOrEmpty(stringVal3Prop.stringValue))
                                    DrawPropertySelection(stringVal4Prop, boolValProp, stringVal3Prop, (CVRInteractableAction.ActionRegister) parentProperty.FindPropertyRelative(nameof(CVRInteractableAction.actionType)).intValue);
                                else
                                    EditorGUILayout.HelpBox(L("SelectComponentToProceed").text, MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(L("SelectTargetToProceed").text, MessageType.Warning);
                            }
                        }
                        break;
                    case (int) CVRInteractableActionOperation.ActionType.SetPropertyByApf:
                        EditorGUILayout.HelpBox(L("SetPropertyByApfHelp").text, MessageType.None);

                        EditorGUILayout.PropertyField(gameObjectValProp, L("Target"));

                        if (gameObjectValProp.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox(L("SelectTargetToProceed").text, MessageType.Warning);
                            break;
                        }

                        DrawComponentSelection(stringVal3Prop, gameObjectValProp, L("Component").text);

                        if (string.IsNullOrEmpty(stringVal3Prop.stringValue))
                        {
                            EditorGUILayout.HelpBox(L("SelectComponentToProceed").text, MessageType.Warning);
                            break;
                        }

                        DrawPropertySelection(stringVal4Prop, boolValProp, stringVal3Prop, 
                            (CVRInteractableAction.ActionRegister)parentProperty.FindPropertyRelative(nameof(CVRInteractableAction.actionType)).intValue);

                        break;

                    case (int) CVRInteractableActionOperation.ActionType.SetPropertyByValue:
                        EditorGUILayout.HelpBox(L("SetPropertyByValueHelp").text, MessageType.None);

                        EditorGUILayout.PropertyField(varBufferValProp, L("VariableBuffer"));
                        EditorGUILayout.PropertyField(gameObjectValProp, L("Target"));

                        if (gameObjectValProp.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox(L("SelectTargetToProceed").text, MessageType.Warning);
                            break;
                        }

                        DrawComponentSelection(stringVal3Prop, gameObjectValProp, L("Component").text);

                        if (string.IsNullOrEmpty(stringVal3Prop.stringValue))
                        {
                            EditorGUILayout.HelpBox(L("SelectComponentToProceed").text, MessageType.Warning);
                            break;
                        }

                        DrawPropertySelection(stringVal4Prop, boolValProp, stringVal3Prop, 
                            (CVRInteractableAction.ActionRegister)parentProperty.FindPropertyRelative(nameof(CVRInteractableAction.actionType)).intValue);

                        break;
                    
                    #endregion APF Operations

                    case (int) CVRInteractableActionOperation.ActionType.DeleteGameObject:
                        EditorGUILayout.HelpBox(L("DeleteGameObjectHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(gameObjectValProp, L("Target"));
                        boolValProp.boolValue = EditorGUILayout.Popup(L("ToDestroy").text, boolValProp.boolValue ? 1 : 0, s_DestroyGameObjectOptions) == 1;
                        break;

                    case (int) CVRInteractableActionOperation.ActionType.LuaFunctionCall:
                        EditorGUILayout.HelpBox(L("LuaFunctionCallHelp").text, MessageType.None);
                        EditorGUILayout.PropertyField(gameObjectValProp, L("LuaBehaviour"));
                        EditorGUILayout.PropertyField(stringValProp, L("GlobalFunctionName"));
                        DrawWarningIfMissingRequiredComponentInGameObject(typeof(CVRLuaClientBehaviour));
                        break;
                    
                    default:
                        EditorGUILayout.HelpBox(L("OperationTypeNotImplemented").text, MessageType.Error);
                        break;
                }

                return;
                void DrawTargetsProp(GUIContent label = null)
                {
                    label ??= L("Targets");
                    // Offset rect by indent so lame af foldout icon doesn't overlap the left border...
                    Rect rect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(targetsProp));
                    EditorGUI.PropertyField(new Rect(rect.x + 15f, rect.y, rect.width - 16f, rect.height), targetsProp,
                        label);
                }
                void DrawWarningIfGUIDIsInvalid(string stringValue)
                {
                    if (string.IsNullOrEmpty(stringValue)) return;
                    if (!Guid.TryParse(stringValue, out _)) EditorGUILayout.HelpBox(L("InvalidGuidError").text, MessageType.Error);
                }
                void DrawWarningIfMissingRequiredComponentInGameObject(Type componentType)
                {
                    if (gameObjectValProp.objectReferenceValue is not GameObject gameObject) return;
                    if (!gameObject.TryGetComponent(componentType, out _))
                    {
                        EditorGUILayout.HelpBox(
                            string.Format(L("MissingRequiredComponentInGameObject").text, componentType.Name),
                            MessageType.Error);
                    }
                }
                void DrawWarningIfMissingRequiredComponentInTargets(Type componentType, bool onlyFirstObject = false)
                {
                    if (componentType == null) return;
                    if (targetsProp.arraySize == 0) return;
                    for (int i = 0; i < targetsProp.arraySize; i++)
                    {
                        SerializedProperty targetProp = targetsProp.GetArrayElementAtIndex(i);
                        if (targetProp.objectReferenceValue is not GameObject gameObject) continue;
                        if (onlyFirstObject && i > 0) return;
                        if (!gameObject.TryGetComponent(componentType, out _))
                        {
                            EditorGUILayout.HelpBox(
                                string.Format(L("MissingRequiredComponentInTarget").text, gameObject.name, componentType.Name),
                                MessageType.Error);
                        }
                    }
                }
                void DrawInfoIfMultipleComponentsMatchTarget(Type componentType)
                {
                    if (componentType == null) return;
                    EditorGUILayout.HelpBox(
                        string.Format(L("MultipleComponentsFoundInfo").text, componentType.Name),
                        MessageType.Info);
                }
                void DrawComponentSelection(SerializedProperty componentTypeProp, SerializedProperty gameObjectProp, string label)
                {
                    if (gameObjectProp.objectReferenceValue is not GameObject targetGameObject) return;
                    var components = targetGameObject.GetComponents<Component>();

                    var componentTypes = new List<Type>();
                    var componentNames = new List<string>();

                    foreach (Component component in components)
                    {
                        Type type = component.GetType();
                        if (!componentTypes.Contains(type) && type != typeof(Transform) && type != typeof(CVRInteractable))
                        {
                            componentTypes.Add(type);
                            componentNames.Add(type.Name);
                        }
                    }

                    int selectedIndex = componentTypes.FindIndex(t => t.AssemblyQualifiedName == componentTypeProp.stringValue);
                    selectedIndex = EditorGUILayout.Popup(label, selectedIndex, componentNames.ToArray());

                    componentTypeProp.stringValue = selectedIndex >= 0 && selectedIndex < componentTypes.Count
                        ? componentTypes[selectedIndex].AssemblyQualifiedName
                        : string.Empty;
                }
                void DrawPropertySelection(SerializedProperty propertyNameProp, SerializedProperty isPropertyProp, SerializedProperty componentTypeProp, CVRInteractableAction.ActionRegister actionRegister)
                {
                    Type componentType = Type.GetType(componentTypeProp.stringValue);
                    if (componentType == null) return;

                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    var fields = componentType.GetFields(flags);
                    var properties = componentType.GetProperties(flags);

                    List<string> memberNames = new List<string>();
                    List<bool> isPropertyList = new List<bool>();
                    Type expectedType = GetExpectedType(actionRegister);

                    foreach (var field in fields)
                    {
                        if (field.IsPublic && field.FieldType == expectedType)
                        {
                            memberNames.Add(field.Name);
                            isPropertyList.Add(false);
                        }
                    }

                    foreach (var prop in properties)
                    {
                        if (prop.CanWrite && prop.PropertyType == expectedType)
                        {
                            memberNames.Add(prop.Name);
                            isPropertyList.Add(true);
                        }
                    }

                    int selectedIndex = memberNames.FindIndex(memberName => memberName == propertyNameProp.stringValue);
                    selectedIndex = EditorGUILayout.Popup(L("Property").text, selectedIndex, memberNames.ToArray());

                    if (selectedIndex >= 0 && selectedIndex < memberNames.Count)
                    {
                        propertyNameProp.stringValue = memberNames[selectedIndex];
                        isPropertyProp.boolValue = isPropertyList[selectedIndex];
                    }
                    else
                    {
                        propertyNameProp.stringValue = "";
                    }
                }
                Type GetExpectedType(CVRInteractableAction.ActionRegister actionRegister)
                {
                    return actionRegister switch
                    {
                        // TODO: verify
                        CVRInteractableAction.ActionRegister.OnAPFBoolChange => typeof(bool),
                        CVRInteractableAction.ActionRegister.OnAPFFloatChange => typeof(float),
                        CVRInteractableAction.ActionRegister.OnAPFIntChange => typeof(int),
                        CVRInteractableAction.ActionRegister.OnAPFStringChange => typeof(string),
                        CVRInteractableAction.ActionRegister.OnAPFTrigger => typeof(bool),
                        _ => typeof(float) // SetPropertyByValue
                    };
                }
            }
            #endregion Operation Entry Drawing
        }
        
        #endregion Draw Operation Entries
        
        #endregion Draw Trigger Entries
    }
}