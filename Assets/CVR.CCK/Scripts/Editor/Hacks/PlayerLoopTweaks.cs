using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace CVR.CCKEditor.Hacks
{
    // Fixes ConstraintManagerUpdate and ParticleSystemBeginUpdateAll systems not being executed in the correct order.
    // Required for FinalIK, DynBone, Magica Cloth, ect to run prior to Constraints and Particle Systems.
    public static class PlayerLoopTweaks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            try { TweakPlayerLoop(); }
            catch (Exception e) { Debug.LogError($"Failed to tweak PlayerLoop: {e.Message}"); }
        }

        private static void TweakPlayerLoop()
        {
            // get the current player loop
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            
            int preLateUpdateIndex = -1, postLateUpdateIndex = -1;
            for (int i = 0; i < currentPlayerLoop.subSystemList.Length; i++)
            {
                Type curType = currentPlayerLoop.subSystemList[i].type;
                if (curType == typeof(PreLateUpdate))
                    preLateUpdateIndex = i;
                else if (curType == typeof(PostLateUpdate))
                    postLateUpdateIndex = i;
                
                if (preLateUpdateIndex != -1 
                    && postLateUpdateIndex != -1) 
                    break; // found both systems
            }

            if (preLateUpdateIndex == -1
                || postLateUpdateIndex == -1)
                throw new Exception("Failed to find PreLateUpdate and PostLateUpdate systems");

            // get PreLateUpdate and PostLateUpdate systems
            PlayerLoopSystem preLateUpdateSystem = currentPlayerLoop.subSystemList[preLateUpdateIndex];
            PlayerLoopSystem postLateUpdateSystem = currentPlayerLoop.subSystemList[postLateUpdateIndex];

            // find ConstraintManagerUpdate and ParticleSystemBeginUpdateAll
            var preLateUpdateList = new List<PlayerLoopSystem>(preLateUpdateSystem.subSystemList);
            var postLateUpdateList = new List<PlayerLoopSystem>(postLateUpdateSystem.subSystemList);
            PlayerLoopSystem constraintManagerUpdate = preLateUpdateList.FirstOrDefault(x 
                => x.type == typeof(PreLateUpdate.ConstraintManagerUpdate));
            PlayerLoopSystem particleSystemBeginUpdate = preLateUpdateList.FirstOrDefault(x 
                => x.type == typeof(PreLateUpdate.ParticleSystemBeginUpdateAll));
            
            // move ConstraintManagerUpdate and ParticleSystemBeginUpdateAll to PostLateUpdate
            postLateUpdateList.Insert(0, constraintManagerUpdate);
            postLateUpdateList.Insert(1, particleSystemBeginUpdate);
            
            // remove ConstraintManagerUpdate and ParticleSystemBeginUpdateAll from PreLateUpdate
            preLateUpdateList.Remove(constraintManagerUpdate);
            preLateUpdateList.Remove(particleSystemBeginUpdate);

            // update PreLateUpdate and PostLateUpdate systems
            preLateUpdateSystem.subSystemList = preLateUpdateList.ToArray();
            postLateUpdateSystem.subSystemList = postLateUpdateList.ToArray();
            currentPlayerLoop.subSystemList[preLateUpdateIndex] = preLateUpdateSystem;
            currentPlayerLoop.subSystemList[postLateUpdateIndex] = postLateUpdateSystem;
            
            // apply the updated player loop
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);
        }
    }
}
