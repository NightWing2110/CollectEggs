using CollectEggs.Core;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    public static class EggCollectProximity
    {
        public static bool CollectTargetEgg(
            PlayerEntity collector,
            EggEntity targetEgg,
            Vector2 collectorCenterXZ,
            float collectRadius,
            float collectRadiusSlack,
            float extraRadius)
        {
            if (!CanCollect(collector))
                return false;
            if (targetEgg == null || !targetEgg.gameObject.activeInHierarchy)
                return false;
            var totalRadius = collectRadius + collectRadiusSlack + extraRadius;
            return CollectEggWithinRadius(collector, targetEgg, collectorCenterXZ, totalRadius);
        }

        public static void CollectAnyNearbyEgg(PlayerEntity collector,
            Vector2 collectorCenterXZ,
            float collectRadius,
            float collectRadiusSlack)
        {
            if (!CanCollect(collector)) return;
            var totalRadius = collectRadius + collectRadiusSlack;
            var eggs = EggEntity.Active;
            foreach (var egg in eggs)
            {
                if (egg == null || !egg.gameObject.activeInHierarchy)
                    continue;
                if (CollectEggWithinRadius(collector, egg, collectorCenterXZ, totalRadius)) return;
            }
        }

        public static bool CollectFromCollider(PlayerEntity collector, Collider collider)
        {
            if (collider == null || !collider.CompareTag("Egg"))
                return false;
            if (!CanCollect(collector))
                return false;
            return GameManager.Instance.CollectEgg(collector, collider.gameObject);
        }

        private static bool CanCollect(PlayerEntity collector)
        {
            return collector != null && GameManager.Instance != null && GameManager.Instance.IsMatchRunning;
        }

        private static bool CollectEggWithinRadius(
            PlayerEntity collector,
            EggEntity egg,
            Vector2 collectorCenterXZ,
            float radius)
        {
            var eggPos = egg.transform.position;
            var dx = eggPos.x - collectorCenterXZ.x;
            var dz = eggPos.z - collectorCenterXZ.y;
            if (dx * dx + dz * dz > radius * radius)
                return false;
            return GameManager.Instance.CollectEgg(collector, egg.gameObject);
        }
    }
}
