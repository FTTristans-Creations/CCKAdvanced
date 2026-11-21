using System;
using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    public class NetworkedObjectStep : IValidationStep
    {
        private readonly bool _checkForWorldSpawnables;
        
        public NetworkedObjectStep(bool checkForWorldSpawnables = false)
        {
            _checkForWorldSpawnables = checkForWorldSpawnables;
        }
        
        private readonly Dictionary<Type, HashSet<string>> _componentTypeToNetworkIds = new();
        private readonly Dictionary<Type, HashSet<string>> _conflictingIds = new();
        
        private readonly HashSet<Object> _noNetworkIds = new();
        private readonly HashSet<Object> _conflictingNetworkIds = new();
        private readonly HashSet<Object> _processedObjects = new();
        
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyNoNetworkId = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyConflictingNetworkIds = new();
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (!_processedObjects.Add(component))
                return; // Already processed
            
            switch (component)
            {
                case CVRInteractable interactable: ProcessCVRInteractable(interactable, asset); break;
                case CVRObjectSync objectSync: ProcessCVRObjectSync(objectSync, asset); break;
                case CombatSystem combatSystem: ProcessCombatSystem(combatSystem, asset); break;
                case ObjectHealth objectHealth: ProcessObjectHealth(objectHealth, asset); break;
                case GunController gunController: ProcessGunController(gunController, asset); break;
                case CVRVideoPlayer videoPlayer: ProcessCVRVideoPlayer(videoPlayer, asset); break;
                case CVRSpawnable spawnable: ProcessCVRSpawnable(spawnable, asset); break; // World spawnables are stupid
                case GameInstanceController gameInstanceController: ProcessGameInstanceController(gameInstanceController, asset); break;
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_noNetworkIds.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Some networked components are missing Network IDs.",
                    RootObjects = _noNetworkIds,
                    Hierarchy = _hierarchyNoNetworkId,
                    AutoFix = () =>
                    {
                        foreach (Object item in _noNetworkIds)
                        {
                            switch (item)
                            {
                                case CVRInteractable interactable: AutoFixCVRInteractable(interactable, false); break;
                                case CVRObjectSync objectSync: AutoFixCVRObjectSync(objectSync, false); break;
                                case CombatSystem combatSystem: AutoFixCombatSystem(combatSystem, false); break;
                                case ObjectHealth objectHealth: AutoFixObjectHealth(objectHealth, false); break;
                                case GunController gunController: AutoFixGunController(gunController, false); break;
                                case CVRVideoPlayer videoPlayer: AutoFixCVRVideoPlayer(videoPlayer, false); break;
                                case CVRSpawnable spawnable: AutoFixCVRSpawnable(spawnable, false); break;
                                case GameInstanceController gameInstanceController: AutoFixGameInstanceController(gameInstanceController, false); break;
                            }
                        }
                    },
                };
            }

            if (_conflictingNetworkIds.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Some networked components have conflicting Network IDs.",
                    RootObjects = _conflictingNetworkIds,
                    Hierarchy = _hierarchyConflictingNetworkIds,
                    AutoFix = () =>
                    {
                        foreach (Object item in _conflictingNetworkIds)
                        {
                            switch (item)
                            {
                                case CVRInteractable interactable: AutoFixCVRInteractable(interactable, true); break;
                                case CVRObjectSync objectSync: AutoFixCVRObjectSync(objectSync, true); break;
                                case CombatSystem combatSystem: AutoFixCombatSystem(combatSystem, true); break;
                                case ObjectHealth objectHealth: AutoFixObjectHealth(objectHealth, true); break;
                                case GunController gunController: AutoFixGunController(gunController, true); break;
                                case CVRVideoPlayer videoPlayer: AutoFixCVRVideoPlayer(videoPlayer, true); break;
                                case CVRSpawnable spawnable: AutoFixCVRSpawnable(spawnable, true); break;
                                case GameInstanceController gameInstanceController: AutoFixGameInstanceController(gameInstanceController, true); break;
                            }
                        }
                    },
                };
            }
        }

        #region CVRInteractable
        
        private void ProcessCVRInteractable(CVRInteractable interactable, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(CVRInteractableAction)))
                _componentTypeToNetworkIds[typeof(CVRInteractableAction)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(CVRInteractableAction)))
                _conflictingIds[typeof(CVRInteractableAction)] = new HashSet<string>();
            
            foreach (CVRInteractableAction interactableAction in interactable.actions)
            {
                if (interactableAction == null) continue;
                if (interactableAction.execType == CVRInteractableAction.ExecutionType.LocalNotNetworked)
                    continue; // Skip local-only actions
                
                if (string.IsNullOrWhiteSpace(interactableAction.guid))
                {
                    if (_noNetworkIds.Add(interactable))
                        ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, interactable, asset);
                }
                else if (!_componentTypeToNetworkIds[typeof(CVRInteractableAction)].Add(interactableAction.guid))
                {
                    Debug.Log($"Conflict found for CVRInteractableAction with GUID {interactableAction.guid} on GameObject {interactable.gameObject.name}");
                    _conflictingIds[typeof(CVRInteractableAction)].Add(interactableAction.guid);
                    if (_conflictingNetworkIds.Add(interactable))
                        ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, interactable, asset);
                }
            }
        }

        private void AutoFixCVRInteractable(CVRInteractable interactable, bool isConflictFix)
        {
            SerializedObject so = new(interactable);
            SerializedProperty actionsProperty = so.FindProperty("actions");
            
            Undo.RegisterCompleteObjectUndo(interactable, isConflictFix ? "Regenerate Conflicting Network IDs" : "Assign Network IDs to Interactable Actions");
            
            for (int i = 0; i < actionsProperty.arraySize; i++)
            {
                SerializedProperty actionProperty = actionsProperty.GetArrayElementAtIndex(i);
                SerializedProperty guidProperty = actionProperty.FindPropertyRelative("guid");
                
                bool shouldRegenerate;
                
                if (isConflictFix)
                {
                    shouldRegenerate = !string.IsNullOrWhiteSpace(guidProperty.stringValue) && 
                                     _conflictingIds.ContainsKey(typeof(CVRInteractableAction)) &&
                                     _conflictingIds[typeof(CVRInteractableAction)].Contains(guidProperty.stringValue);
                }
                else
                {
                    shouldRegenerate = string.IsNullOrWhiteSpace(guidProperty.stringValue);
                }
                
                if (shouldRegenerate)
                    guidProperty.stringValue = Guid.NewGuid().ToString();
            }
            
            so.ApplyModifiedProperties();
        }
        
        #endregion CVRInteractable

        #region CVRObjectSync
        
        private void ProcessCVRObjectSync(CVRObjectSync objectSync, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(CVRObjectSync)))
                _componentTypeToNetworkIds[typeof(CVRObjectSync)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(CVRObjectSync)))
                _conflictingIds[typeof(CVRObjectSync)] = new HashSet<string>();

            if (string.IsNullOrWhiteSpace(objectSync.guid))
            {
                if (_noNetworkIds.Add(objectSync))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, objectSync, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(CVRObjectSync)].Add(objectSync.guid))
            {
                _conflictingIds[typeof(CVRObjectSync)].Add(objectSync.guid);
                if (_conflictingNetworkIds.Add(objectSync))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, objectSync, asset);
            }
        }

        private void AutoFixCVRObjectSync(CVRObjectSync objectSync, bool isConflictFix)
        {
            SerializedObject so = new(objectSync);
            SerializedProperty prop = so.FindProperty("guid");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(CVRObjectSync)) &&
                                 _conflictingIds[typeof(CVRObjectSync)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(objectSync, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion CVRObjectSync

        #region CombatSystem
        
        private void ProcessCombatSystem(CombatSystem combatSystem, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(CombatSystem)))
                _componentTypeToNetworkIds[typeof(CombatSystem)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(CombatSystem)))
                _conflictingIds[typeof(CombatSystem)] = new HashSet<string>();
            
            if (string.IsNullOrEmpty(combatSystem.referenceID))
            {
                if (_noNetworkIds.Add(combatSystem))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, combatSystem, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(CombatSystem)].Add(combatSystem.referenceID))
            {
                _conflictingIds[typeof(CombatSystem)].Add(combatSystem.referenceID);
                if (_conflictingNetworkIds.Add(combatSystem))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, combatSystem, asset);
            }
        }

        private void AutoFixCombatSystem(CombatSystem combatSystem, bool isConflictFix)
        {
            SerializedObject so = new(combatSystem);
            SerializedProperty prop = so.FindProperty("referenceID");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(CombatSystem)) &&
                                 _conflictingIds[typeof(CombatSystem)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(combatSystem, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion CombatSystem

        #region ObjectHealth
        
        private void ProcessObjectHealth(ObjectHealth objectHealth, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(ObjectHealth)))
                _componentTypeToNetworkIds[typeof(ObjectHealth)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(ObjectHealth)))
                _conflictingIds[typeof(ObjectHealth)] = new HashSet<string>();
            
            if (string.IsNullOrWhiteSpace(objectHealth.referenceID))
            {
                if (_noNetworkIds.Add(objectHealth))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, objectHealth, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(ObjectHealth)].Add(objectHealth.referenceID))
            {
                _conflictingIds[typeof(ObjectHealth)].Add(objectHealth.referenceID);
                if (_conflictingNetworkIds.Add(objectHealth))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, objectHealth, asset);
            }
        }

        private void AutoFixObjectHealth(ObjectHealth objectHealth, bool isConflictFix)
        {
            SerializedObject so = new(objectHealth);
            SerializedProperty prop = so.FindProperty("referenceID");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(ObjectHealth)) &&
                                 _conflictingIds[typeof(ObjectHealth)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(objectHealth, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion ObjectHealth

        #region GunController
        
        private void ProcessGunController(GunController gunController, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(GunController)))
                _componentTypeToNetworkIds[typeof(GunController)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(GunController)))
                _conflictingIds[typeof(GunController)] = new HashSet<string>();
            
            if (string.IsNullOrWhiteSpace(gunController.referenceID))
            {
                if (_noNetworkIds.Add(gunController))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, gunController, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(GunController)].Add(gunController.referenceID))
            {
                _conflictingIds[typeof(GunController)].Add(gunController.referenceID);
                if (_conflictingNetworkIds.Add(gunController))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, gunController, asset);
            }
        }

        private void AutoFixGunController(GunController gunController, bool isConflictFix)
        {
            SerializedObject so = new(gunController);
            SerializedProperty prop = so.FindProperty("referenceID");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(GunController)) &&
                                 _conflictingIds[typeof(GunController)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(gunController, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion GunController

        #region CVRVideoPlayer
        
        private void ProcessCVRVideoPlayer(CVRVideoPlayer videoPlayer, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(CVRVideoPlayer)))
                _componentTypeToNetworkIds[typeof(CVRVideoPlayer)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(CVRVideoPlayer)))
                _conflictingIds[typeof(CVRVideoPlayer)] = new HashSet<string>();
            
            if (string.IsNullOrWhiteSpace(videoPlayer.playerId))
            {
                if (_noNetworkIds.Add(videoPlayer))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, videoPlayer, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(CVRVideoPlayer)].Add(videoPlayer.playerId))
            {
                _conflictingIds[typeof(CVRVideoPlayer)].Add(videoPlayer.playerId);
                if (_conflictingNetworkIds.Add(videoPlayer))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, videoPlayer, asset);
            }
        }

        private void AutoFixCVRVideoPlayer(CVRVideoPlayer videoPlayer, bool isConflictFix)
        {
            SerializedObject so = new(videoPlayer);
            SerializedProperty prop = so.FindProperty("playerId");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(CVRVideoPlayer)) &&
                                 _conflictingIds[typeof(CVRVideoPlayer)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(videoPlayer, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion CVRVideoPlayer

        #region CVRSpawnable
        
        private void ProcessCVRSpawnable(CVRSpawnable spawnable, Object asset)
        {
            if (!_checkForWorldSpawnables) return; // Does not matter unless checking for world spawnables
            
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(CVRSpawnable)))
                _componentTypeToNetworkIds[typeof(CVRSpawnable)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(CVRSpawnable)))
                _conflictingIds[typeof(CVRSpawnable)] = new HashSet<string>();
            
            if (string.IsNullOrWhiteSpace(spawnable.preGeneratedInstanceId))
            {
                if (_noNetworkIds.Add(spawnable))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, spawnable, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(CVRSpawnable)].Add(spawnable.preGeneratedInstanceId))
            {
                _conflictingIds[typeof(CVRSpawnable)].Add(spawnable.preGeneratedInstanceId);
                if (_conflictingNetworkIds.Add(spawnable))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, spawnable, asset);
            }
        }

        private void AutoFixCVRSpawnable(CVRSpawnable spawnable, bool isConflictFix)
        {
            SerializedObject so = new(spawnable);
            SerializedProperty prop = so.FindProperty("preGeneratedInstanceId");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(CVRSpawnable)) &&
                                 _conflictingIds[typeof(CVRSpawnable)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(spawnable, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion CVRSpawnable

        #region GameInstanceController
        
        private void ProcessGameInstanceController(GameInstanceController gameInstanceController, Object asset)
        {
            if (!_componentTypeToNetworkIds.ContainsKey(typeof(GameInstanceController)))
                _componentTypeToNetworkIds[typeof(GameInstanceController)] = new HashSet<string>();
            
            if (!_conflictingIds.ContainsKey(typeof(GameInstanceController)))
                _conflictingIds[typeof(GameInstanceController)] = new HashSet<string>();
            
            if (string.IsNullOrWhiteSpace(gameInstanceController.referenceID))
            {
                if (_noNetworkIds.Add(gameInstanceController))
                    ValidationUtils.AddToHierarchySet(_hierarchyNoNetworkId, gameInstanceController, asset);
            }
            else if (!_componentTypeToNetworkIds[typeof(GameInstanceController)].Add(gameInstanceController.referenceID))
            {
                _conflictingIds[typeof(GameInstanceController)].Add(gameInstanceController.referenceID);
                if (_conflictingNetworkIds.Add(gameInstanceController))
                    ValidationUtils.AddToHierarchySet(_hierarchyConflictingNetworkIds, gameInstanceController, asset);
            }
        }

        private void AutoFixGameInstanceController(GameInstanceController gameInstanceController, bool isConflictFix)
        {
            SerializedObject so = new(gameInstanceController);
            SerializedProperty prop = so.FindProperty("referenceID");
            if (prop == null) return;
            
            bool shouldRegenerate;
            
            if (isConflictFix)
            {
                shouldRegenerate = !string.IsNullOrWhiteSpace(prop.stringValue) && 
                                 _conflictingIds.ContainsKey(typeof(GameInstanceController)) &&
                                 _conflictingIds[typeof(GameInstanceController)].Contains(prop.stringValue);
            }
            else
            {
                shouldRegenerate = string.IsNullOrWhiteSpace(prop.stringValue);
            }
            
            if (shouldRegenerate)
            {
                Undo.RegisterCompleteObjectUndo(gameInstanceController, isConflictFix ? "Regenerate Conflicting Network ID" : "Assign Network ID");
                prop.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion GameInstanceController
    }
}