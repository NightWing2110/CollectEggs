using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    public static class EggCollectProximity
    {
        public static bool RequestCollectTargetEgg(
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
            return RequestCollectWithinRadius(collector, targetEgg, collectorCenterXZ, totalRadius);
        }

        public static void RequestCollectFirstEggInRadius(PlayerEntity collector,
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
                if (RequestCollectWithinRadius(collector, egg, collectorCenterXZ, totalRadius)) return;
            }
        }

        public static bool RequestCollectFromEggCollider(PlayerEntity collector, Collider collider)
        {
            if (collider == null || !collider.CompareTag("Egg"))
                return false;
            return CanCollect(collector) && EggCollectRequestController.Active.RequestEggCollection(collector, collider.gameObject);
        }

        private static bool CanCollect(PlayerEntity collector)
        {
            return collector != null &&
                   EggCollectRequestController.Active != null &&
                   EggCollectRequestController.Active.IsMatchRunning;
        }

        private static bool RequestCollectWithinRadius(
            PlayerEntity collector,
            EggEntity egg,
            Vector2 collectorCenterXZ,
            float radius)
        {
            var eggPos = egg.transform.position;
            var dx = eggPos.x - collectorCenterXZ.x;
            var dz = eggPos.z - collectorCenterXZ.y;
            return !(dx * dx + dz * dz > radius * radius) && EggCollectRequestController.Active.RequestEggCollection(collector, egg.gameObject);
        }
    }
}
