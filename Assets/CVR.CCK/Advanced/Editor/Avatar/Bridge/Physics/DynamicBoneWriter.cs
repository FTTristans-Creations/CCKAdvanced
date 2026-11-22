#if VRC_SDK_VRCSDK3 && !UDON && CVR_CCK_EXISTS && AVATARBRIDGE_DYNBONE
using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace AvatarBridge
{
    // Writes VRCPhysBone chains as DynamicBone components using the same layout and
    // raw value mapping as the pre-AvatarBridge converter: the component sits on the
    // PhysBone's own GameObject, each collider is twinned onto the VRCPhysBoneCollider's
    // own GameObject, and every PhysBone on the avatar is converted.
    public static class DynamicBoneWriter
    {
        const string Category = "PhysBones -> DynamicBone";

        public static void Write(BridgeContext ctx, PhysBoneChainData data,
            Dictionary<VRCPhysBoneCollider, DynamicBoneColliderBase> colliderCache)
        {
            var pb = data.Source;
            var db = data.SourceGameObject.AddComponent<DynamicBone>();
            // Stacked systems leave extras disabled; without this every variant
            // simulates at once and they fight over the same bones.
            db.enabled = data.ComponentEnabled && data.InitiallyActive;
            db.m_Root = data.Root;
            db.m_Elasticity = data.Pull;
            db.m_Damping = data.Spring;
            // Synthesized chains (HelperRigCleanup) carry no source component; their
            // Stiffness was already resolved by the caller.
            db.m_Stiffness = pb != null ? pb.stiffness : data.Stiffness;
            db.m_Inert = data.Immobile;
            db.m_Radius = data.Radius;
            db.m_Gravity = new Vector3(0f, -data.Gravity, 0f);
            if (pb != null && pb.ignoreTransforms != null && pb.ignoreTransforms.Count > 0)
            {
                db.m_Exclusions = new List<Transform>(pb.ignoreTransforms);
            }
            if (data.Colliders.Count > 0)
            {
                db.m_Colliders = new List<DynamicBoneColliderBase>();
                foreach (var pbCollider in data.Colliders)
                {
                    var collider = GetOrCreateCollider(ctx, pbCollider, colliderCache);
                    if (collider != null && !db.m_Colliders.Contains(collider))
                    {
                        db.m_Colliders.Add(collider);
                    }
                }
            }

            // The component lives on the source object itself, so the animator pass
            // retargets PhysBone toggles straight onto it through this entry.
            ctx.ConvertedPhysicsChains.Add(new BridgeContext.ConvertedPhysicsChain
            {
                Source = data.SourceGameObject,
                Host = data.SourceGameObject,
                Physics = db,
                Root = data.Root
            });

            ctx.Report.Converted(Category, data.Root.name,
                $"DynamicBone. Source PhysBone was pull {data.Pull:0.##}, spring {data.Spring:0.##}, " +
                $"stiffness {(pb != null ? pb.stiffness : data.Stiffness):0.##}, gravity {data.Gravity:0.##}, " +
                $"immobile {data.Immobile:0.##}, radius {data.Radius:0.###}.");
        }

        // Twinned onto the collider's own GameObject: DeleteConverted removes the
        // VRCPhysBoneCollider component afterwards and this survives it.
        internal static DynamicBoneColliderBase GetOrCreateCollider(BridgeContext ctx,
            VRCPhysBoneCollider pbCollider, Dictionary<VRCPhysBoneCollider, DynamicBoneColliderBase> cache)
        {
            if (pbCollider == null)
            {
                return null;
            }
            if (cache.TryGetValue(pbCollider, out var cached))
            {
                return cached;
            }

            var collider = pbCollider.GetComponent<DynamicBoneCollider>();
            if (collider == null)
            {
                collider = pbCollider.gameObject.AddComponent<DynamicBoneCollider>();
            }

            collider.m_Radius = pbCollider.radius;
            collider.m_Height = pbCollider.height;
            collider.m_Center = pbCollider.position;

            int direction = 1; // Y axis
            if (pbCollider.shapeType.ToString().Equals("Capsule", StringComparison.OrdinalIgnoreCase))
            {
                Vector3 dirVec = pbCollider.rotation * Vector3.up;
                if (Mathf.Abs(dirVec.x) > 0.8f) direction = 0;
                else if (Mathf.Abs(dirVec.z) > 0.8f) direction = 2;
            }
            collider.m_Direction = (DynamicBoneColliderBase.Direction)direction;
            collider.m_Bound = pbCollider.insideBounds
                ? DynamicBoneColliderBase.Bound.Inside
                : DynamicBoneColliderBase.Bound.Outside;

            PhysBoneConverter.RecordColliderHost(ctx, pbCollider, pbCollider.gameObject);
            cache[pbCollider] = collider;
            return collider;
        }
    }
}
#endif
