using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ABI.CCK.Components;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class SpawnpointOOBRootStep : IValidationStep
    {
        private const float MaxPlayableBounds = 50_000f;
        
        private readonly HashSet<Object> _oobSpawns = new();
        private readonly HashSet<Object> _belowRespawnSpawns = new();
        private readonly HashSet<Object> _nullSpawns = new();
        
        private CVRWorld _world;

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not CVRWorld world) return;
            _world = world;

            float respawnHeight = world.respawnHeightY;
            GameObject[] spawnPoints = world.spawns is { Length: > 0 }
                ? world.spawns
                : new[] { world.gameObject };

            foreach (GameObject spawn in spawnPoints)
            {
                if (!spawn)
                {
                    _nullSpawns.Add(world);
                    continue;
                }

                Vector3 pos = spawn.transform.position;
                if (!IsWithinMaxBounds(pos))
                    _oobSpawns.Add(spawn);

                if (pos.y < respawnHeight)
                    _belowRespawnSpawns.Add(spawn);
            }

            return;
            static bool IsWithinMaxBounds(Vector3 pos) =>
                !float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z) &&
                Vector3.Distance(Vector3.zero, pos) <= MaxPlayableBounds;
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_nullSpawns.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.WORLD_NULL_SPAWNPOINT"),
                    RootObjects = _nullSpawns,
                    AutoFix = () =>
                    {
                        if (!_world || _world.spawns == null) return;
                        Undo.RecordObject(_world, "Remove null spawnpoints");
                        _world.spawns = _world.spawns.Where(sp => sp).ToArray();
                    }
                };
            }

            if (_oobSpawns.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.WORLD_SPAWNPOINT_OUTSIDE_MAX_BOUNDS")
                        .Replace("{MAX_PLAYABLE_BOUNDS}", MaxPlayableBounds.ToString(CultureInfo.InvariantCulture)),
                    RootObjects = _oobSpawns
                };
            }

            if (_belowRespawnSpawns.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.WORLD_SPAWNPOINT_BELOW_RESPAWN")
                        .Replace("{RESPAWN_HEIGHT}", _world.respawnHeightY.ToString(CultureInfo.InvariantCulture)),
                    RootObjects = _belowRespawnSpawns
                };
            }
        }
    }
}