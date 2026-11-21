using System;
using System.Collections.Generic;
using CVR.CCK;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Interactable")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-interactable")]
    [Serializable]
    [CVRComponent(ComponentStatus.None)]
    public class CVRInteractable : MonoBehaviour, IStateMachineCallbackReceiver
    {
        #region Serialized Properties
        
        public const int LATEST_VERSION = 1;
        
        /// <summary>
        /// Version of the interactable.
        /// Version 0: Migrate to Version 1 if not on Unity UI.
        /// Version 1. After Unity UI interaction was fixed.
        /// </summary>
        public int version;
        
        public string tooltip;
        public List<CVRInteractableAction> actions = new();

        public UnityEvent onEnterSeat;
        public UnityEvent onExitSeat;
        
        #endregion Serialized Properties
        
        #region Public Methods

        public void CustomTrigger() { }
        public void CustomTriggerAtIndex(int actionIndex) { }
        public void CancelAllDelays() { }
        public void CancelDelayAtIndex(int actionIndex) { }

        #endregion Public Methods

        private void Reset()
        {
            // Start at latest version
            version = LATEST_VERSION;
        }
        
    #if UNITY_EDITOR
    
        private List<CVRInteractableAction.ActionRegister> _usedTypes = new();
        public IReadOnlyList<CVRInteractableAction.ActionRegister > GetUsedActionTypes() 
            => _usedTypes;

        private void FindUsedActionTypes()
        {
            _usedTypes.Clear();
            foreach (CVRInteractableAction action in actions)
            {
                if (!_usedTypes.Contains(action.actionType))
                    _usedTypes.Add(action.actionType);
            }
        }
        
        public void InvokeActionType(CVRInteractableAction.ActionRegister type)
        {
            foreach (CVRInteractableAction action in actions)
            {
                if (action.actionType == type)
                {
                    InvokeActionOperations(action);
                }
            }
        }
        
        private void InvokeActionOperations(CVRInteractableAction action)
        {
            foreach (CVRInteractableActionOperation operation in action.operations)
            {
                switch (operation.type)
                {
                    case CVRInteractableActionOperation.ActionType.SetGameObjectActive:
                        switch (operation.floatVal)
                        {
                            case 0f:
                                foreach (GameObject target in operation.targets)
                                    if (target) target.SetActive(true);
                                break;
                            case 1f:
                                foreach (GameObject target in operation.targets) 
                                    if (target) target.SetActive(false);
                                break;
                            case 2f:
                                foreach (GameObject target in operation.targets) 
                                    if (target) target.SetActive(!target.activeSelf);
                                break;
                        }
                        break;
                    case CVRInteractableActionOperation.ActionType.SetComponentActive:
                        break;
                    case CVRInteractableActionOperation.ActionType.SitAtPosition:
                        break;
                    case CVRInteractableActionOperation.ActionType.TeleportPlayer:
                        break;
                    case CVRInteractableActionOperation.ActionType.SpawnObject:
                        break;
                    case CVRInteractableActionOperation.ActionType.TeleportObject:
                        break;
                    case CVRInteractableActionOperation.ActionType.DeleteGameObject:
                        break;
                    case CVRInteractableActionOperation.ActionType.PlayAudio:
                        break;
                    case CVRInteractableActionOperation.ActionType.StopAudio:
                        break;
                    case CVRInteractableActionOperation.ActionType.MethodCall:
                        operation.customEvent?.Invoke();
                        break;
                    case CVRInteractableActionOperation.ActionType.LuaFunctionCall:
                    case CVRInteractableActionOperation.ActionType.SetSpawnableSyncValue:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorFloatValue:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorBoolValue:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorIntValue:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorTrigger:
                    case CVRInteractableActionOperation.ActionType.ToggleAnimatorBoolValue:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorFloatRandom:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorBoolRandom:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorIntRandom:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorFloatByVar:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorIntByVar:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorBoolByAPF:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorIntByAPF:
                    case CVRInteractableActionOperation.ActionType.SetAnimatorFloatByAPF:
                    case CVRInteractableActionOperation.ActionType.SetVariableBufferByAPF:
                    case CVRInteractableActionOperation.ActionType.UpdateAPFTrigger:
                    case CVRInteractableActionOperation.ActionType.UpdateAPFBool:
                    case CVRInteractableActionOperation.ActionType.UpdateAPFInt:
                    case CVRInteractableActionOperation.ActionType.UpdateAPFFloat:
                    case CVRInteractableActionOperation.ActionType.UpdateAPFString:
                    case CVRInteractableActionOperation.ActionType.SetPropertyByApf:
                    case CVRInteractableActionOperation.ActionType.SetPropertyByValue:
                    case CVRInteractableActionOperation.ActionType.VariableBufferArithmetic:
                    case CVRInteractableActionOperation.ActionType.DisplayWorldDetailPage:
                    case CVRInteractableActionOperation.ActionType.DisplayAvatarDetailPage:
                    case CVRInteractableActionOperation.ActionType.DisplaySpawnableDetailPage:
                    case CVRInteractableActionOperation.ActionType.DisplayGroupDetailPage:
                        Debug.Log($"The action '{action.actionType}' with operation '{operation.type}' cannot be tested in the editor.", this);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void OnValidate()
        {
            VersionValidation();
            FindUsedActionTypes();
            
            // if the action is SitAtPosition, ensure there is at least one gameobject target
            foreach (CVRInteractableAction action in actions)
            {
                foreach (CVRInteractableActionOperation operation in action.operations)
                {
                    switch (operation.type)
                    {
                        // floatVal maps to s_ObjectStates
                        case CVRInteractableActionOperation.ActionType.SetGameObjectActive:
                        case CVRInteractableActionOperation.ActionType.SetComponentActive:
                            operation.floatVal = Mathf.Clamp(operation.floatVal, 0, 2);
                            break;
                        case CVRInteractableActionOperation.ActionType.SitAtPosition:
                            operation.targets ??= new List<GameObject>();
                            if (operation.targets.Count == 0) operation.targets.Add(null);
                            break;
                    }
                }
            }
        }

#endif

        #region Versioning
        
        public bool IsLegacyUiSetup { get; private set; }
        
        private void VersionValidation()
        {
            // Version handling

            #region Version 0 to 1 Migration

            if (version == 0)
            {
                if (IsLegacyUiSetup) 
                    return; // Already checked and set, no need to do it again.
                
                bool isLegacyUiSetup = false;
                
                // Legacy mitigation: Check if we are on a RectTransform with no collider.
                // This is to see if we are on a UI Element that *used* to get OnInteract events.
                if (TryGetComponent(out RectTransform _) && !TryGetComponent(out Collider _))
                {
                    foreach (CVRInteractableAction action in actions)
                    {
                        if (action.actionType is CVRInteractableAction.ActionRegister.OnInteractDown
                            or CVRInteractableAction.ActionRegister.OnInteractUp
                            or CVRInteractableAction.ActionRegister.OnPointerEnter
                            or CVRInteractableAction.ActionRegister.OnPointerExit)
                            isLegacyUiSetup = true;
                    }
                }

                if (isLegacyUiSetup)
                {
                    IsLegacyUiSetup = true;
                    return; // We cannot upgrade this interactable, it needs user attention.
                }
                
                // Migrate to version 1
                version = 1;
            }

            #endregion Version 0 to 1 Migration
        }

        public void AutoFixLegacyUiSetup()
        {
#if UNITY_EDITOR
            
            // Remap the Interact actions to the new Unity UI specific ones
            // Migrate the older interact actions to use the new OnUnityUIPointerDown/Up/Enter/Exit

            if (version != 0)
                return;
            
            if (!TryGetComponent(out RectTransform _) 
                || TryGetComponent(out Collider _)) 
                return;
            
            int fixedActions = 0;
            bool interactDown = false;
            bool interactUp = false;
            bool pointerEnter = false;
            bool pointerExit = false;
            
            // Pre-calculate which actions would be fixed
            foreach (CVRInteractableAction action in actions)
            {
                switch (action.actionType)
                {
                    case CVRInteractableAction.ActionRegister.OnInteractDown:
                        interactDown = true;
                        fixedActions++;
                        break;
                    case CVRInteractableAction.ActionRegister.OnInteractUp:
                        interactUp = true;
                        fixedActions++;
                        break;
                    case CVRInteractableAction.ActionRegister.OnPointerEnter:
                        pointerEnter = true;
                        fixedActions++;
                        break;
                    case CVRInteractableAction.ActionRegister.OnPointerExit:
                        pointerExit = true;
                        fixedActions++;
                        break;
                }
            }

            if (fixedActions > 0)
            {
                string message = "The following actions can be remapped:\n";
                if (interactDown) message += "- OnInteractDown -> OnUnityUIPointerDown\n";
                if (interactUp) message += "- OnInteractUp -> OnUnityUIPointerUp\n";
                if (pointerEnter) message += "- OnPointerEnter -> OnUnityUIPointerEnter\n";
                if (pointerExit) message += "- OnPointerExit -> OnUnityUIPointerExit\n";
                message += "Do you want to apply these changes? Please review the actions to ensure they are set up correctly.";

                if (!EditorUtility.DisplayDialog("CCK :: CVRInteractable Legacy Auto Fix", message, "OK", "Cancel"))
                    return; // User cancelled the fix, do not apply changes
            }

            foreach (CVRInteractableAction action in actions)
            {
                action.actionType = action.actionType switch
                {
                    CVRInteractableAction.ActionRegister.OnInteractDown => CVRInteractableAction.ActionRegister.OnUnityUIPointerDown,
                    CVRInteractableAction.ActionRegister.OnInteractUp => CVRInteractableAction.ActionRegister.OnUnityUIPointerUp,
                    CVRInteractableAction.ActionRegister.OnPointerEnter => CVRInteractableAction.ActionRegister.OnUnityUIPointerEnter,
                    CVRInteractableAction.ActionRegister.OnPointerExit => CVRInteractableAction.ActionRegister.OnUnityUIPointerExit,
                    _ => action.actionType
                };
            }
            
            // Set the version to 1, so we don't try to fix this again
            version = 1;

            EditorUtility.SetDirty(this);
            
            Debug.Log($"CCK :: Fixed {fixedActions} legacy UI actions on {gameObject.name}.", gameObject);
#endif
        }
        
        #endregion Versioning
        
        #region Gizmo Drawing
        
        private void OnDrawGizmos()
        {
            foreach (CVRInteractableAction action in actions)
            {
                foreach (CVRInteractableActionOperation operation in action.operations)
                {
                    if (operation.type == CVRInteractableActionOperation.ActionType.TeleportPlayer)
                    {
                        if(operation.gameObjectVal == null) continue;
                        
                        Transform target = operation.gameObjectVal.transform;
                        Vector3 position = target.position;
                        
                        // connection line
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(transform.position, position);
                        
                        // alignment is set to none
                        if (operation.floatVal == 3)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawWireSphere(position, 0.1f);
                            continue;
                        }
                        
                        // relative teleport sets forward direction
                        Vector3 targetDirection;
                        if (operation.boolVal)
                        {
                            Gizmos.color = Color.cyan;
                            targetDirection = transform.InverseTransformDirection(transform.forward);
                            targetDirection = target.TransformDirection(targetDirection);
                        }
                        else
                        {
                            Gizmos.color = Color.blue;
                            targetDirection = target.forward;
                        }
                        
                        if (operation.floatVal == 0) targetDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up).normalized;
                        
                        DrawArrow(position, targetDirection, 1);
                        
                        // up direction, when gravity, aligns to sampled gravity on teleport
                        Gizmos.color = operation.floatVal == 2 ? Color.magenta : Color.green;
                        
                        switch (operation.floatVal)
                        {
                            case 0: // World Up
                                DrawArrow(position, Vector3.up, 1);
                                break;
                            case 1: // Target Up
                            case 2: // Gravity
                                DrawArrow(position, target.up, 1);
                                break;
                        }
                    }
                    
                    if (operation.type == CVRInteractableActionOperation.ActionType.SitAtPosition)
                    {
                        if (operation.targets == null
                            || operation.targets.Count == 0
                            || operation.targets[0] == null) 
                            continue;
                        
                        // Exit position
                        Gizmos.color = Color.blue;
                        
                        Transform exitTransform = operation.targets[0].transform;
                        Vector3 exitPosition = exitTransform.position;
                        
                        Gizmos.DrawLine(transform.position, exitPosition);
                        DrawArrow(exitPosition,
                            exitTransform.forward, 0.5f);
                        
                        Gizmos.color = Color.yellow;
                        DrawArrow(exitPosition,
                            exitTransform.up, 0.5f);
                        
                        if (operation.gameObjectVal == null) 
                            continue;
                        
                        Gizmos.color = Color.blue;

                        // Sitting position
                        Transform position = operation.gameObjectVal.transform;
                        Gizmos.DrawLine(transform.position, position.position);
                        
                        // Draws sitting cube person
                        Gizmos.matrix = Matrix4x4.TRS(position.position, position.rotation, Vector3.one);
                        Gizmos.DrawWireCube(new Vector3(+0.12f, -0.2f, 0.05f), new Vector3(0.1f, 0.4f, 0.1f));
                        Gizmos.DrawWireCube(new Vector3(-0.12f, -0.2f, 0.05f), new Vector3(0.1f, 0.4f, 0.1f));
                        Gizmos.DrawWireCube(new Vector3(+0.12f, 0.05f, -0.2f), new Vector3(0.1f, 0.1f, 0.6f));
                        Gizmos.DrawWireCube(new Vector3(-0.12f, 0.05f, -0.2f), new Vector3(0.1f, 0.1f, 0.6f));
                        Gizmos.DrawWireCube(new Vector3(0f, 0.4f, -0.4f), new Vector3(0.34f, 0.6f, 0.2f));
                        Gizmos.DrawWireCube(new Vector3(0f, 0.8f, -0.4f), new Vector3(0.2f, 0.2f, 0.2f));
                        Gizmos.DrawWireCube(new Vector3(+0.22f, 0.4f, -0.4f), new Vector3(0.1f, 0.4f, 0.1f));
                        Gizmos.DrawWireCube(new Vector3(-0.22f, 0.4f, -0.4f), new Vector3(0.1f, 0.4f, 0.1f));
                        Gizmos.DrawWireCube(new Vector3(+0.22f, 0.25f, -0.2f), new Vector3(0.1f, 0.1f, 0.3f));
                        Gizmos.DrawWireCube(new Vector3(-0.22f, 0.25f, -0.2f), new Vector3(0.1f, 0.1f, 0.3f));
                    }
                }
            }
        }

        private void DrawArrow(Vector3 position, Vector3 direction, float size)
        {
            Vector3 a1 = RotatePointAroundPivot(position + new Vector3(0, 0.1f * size, 0), position, direction);
            Vector3 a2 = RotatePointAroundPivot(position + new Vector3(0.1f * size, 0, 0), position, direction);
            Vector3 a3 = RotatePointAroundPivot(position + new Vector3(0, -0.1f * size, 0), position, direction);
            Vector3 a4 = RotatePointAroundPivot(position + new Vector3(-0.1f * size, 0, 0), position, direction);
            
            Vector3 b1 = RotatePointAroundPivot(position + new Vector3(0, 0.1f * size, 0.3f * size), position, direction);
            Vector3 b2 = RotatePointAroundPivot(position + new Vector3(0.1f * size, 0, 0.3f * size), position, direction);
            Vector3 b3 = RotatePointAroundPivot(position + new Vector3(0, -0.1f * size, 0.3f * size), position, direction);
            Vector3 b4 = RotatePointAroundPivot(position + new Vector3(-0.1f * size, 0, 0.3f * size), position, direction);
            
            Vector3 c1 = RotatePointAroundPivot(position + new Vector3(0, 0.2f * size, 0.3f * size), position, direction);
            Vector3 c2 = RotatePointAroundPivot(position + new Vector3(0.2f * size, 0, 0.3f * size), position, direction);
            Vector3 c3 = RotatePointAroundPivot(position + new Vector3(0, -0.2f * size, 0.3f * size), position, direction);
            Vector3 c4 = RotatePointAroundPivot(position + new Vector3(-0.2f * size, 0, 0.3f * size), position, direction);
            
            Vector3 d = RotatePointAroundPivot(position + new Vector3(0, 0, 0.5f * size), position, direction);
            
            Gizmos.DrawLine(position, a1);
            Gizmos.DrawLine(position, a2);
            Gizmos.DrawLine(position, a3);
            Gizmos.DrawLine(position, a4);
            
            Gizmos.DrawLine(a1, b1);
            Gizmos.DrawLine(a2, b2);
            Gizmos.DrawLine(a3, b3);
            Gizmos.DrawLine(a4, b4);
            
            Gizmos.DrawLine(b1, c1);
            Gizmos.DrawLine(b2, c2);
            Gizmos.DrawLine(b3, c3);
            Gizmos.DrawLine(b4, c4);
            
            Gizmos.DrawLine(c1, d);
            Gizmos.DrawLine(c2, d);
            Gizmos.DrawLine(c3, d);
            Gizmos.DrawLine(c4, d);
        }
        
        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 direction)
        {
            Vector3 dir = point - pivot; // get point direction relative to pivot
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            dir = rotation * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }
        
        #endregion
    }
}
