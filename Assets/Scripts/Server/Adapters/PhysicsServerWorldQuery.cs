using System.Linq;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Server.Simulation;
using UnityEngine;

namespace CollectEggs.Server.Adapters
{
    public sealed class PhysicsServerWorldQuery : IServerWorldQuery
    {
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private readonly EggSpawner _eggSpawner;

        public PhysicsServerWorldQuery(EggSpawner eggSpawner) => _eggSpawner = eggSpawner;

        public float MoveCheckRadius => _eggSpawner != null ? _eggSpawner.OverlapCheckRadius : 0.45f;
        public float EggSpawnCheckRadius => _eggSpawner != null ? _eggSpawner.OverlapCheckRadius : 0.45f;
        public float MinHorizontalDistanceFromPlayer => _eggSpawner != null ? _eggSpawner.MinHorizontalDistanceFromPlayer : 2.5f;

        public bool GetEggSpawnBounds(out Vector3 min, out Vector3 max)
        {
            if (_eggSpawner == null)
            {
                min = default;
                max = default;
                return false;
            }

            min = _eggSpawner.SpawnBoundsMin;
            max = _eggSpawner.SpawnBoundsMax;
            return true;
        }

        public bool SnapEggSpawnToGround(Vector3 position, out Vector3 snappedPosition)
        {
            snappedPosition = position;
            if (_eggSpawner == null)
                return false;
            var ground = _eggSpawner.GroundLayer;
            if (ground.value == 0)
                return true;
            var origin = position + Vector3.up * _eggSpawner.GroundProbeHeight;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, _eggSpawner.GroundProbeDistance, ground.value, QueryTriggerInteraction.Ignore))
                return false;
            snappedPosition = new Vector3(position.x, hit.point.y + _eggSpawner.EggCenterOffsetAboveGround, position.z);
            return true;
        }

        public bool GetBlockingOverlap(Vector3 position, float radius)
        {
            if (_eggSpawner == null)
                return false;
            var blocking = _eggSpawner.BlockingLayer;
            if (blocking.value == 0)
                return false;
            var count = Physics.OverlapSphereNonAlloc(position, radius, OverlapBuffer, blocking, QueryTriggerInteraction.Ignore);
            return count > 0;
        }

        public bool HasEggOverlap(Vector3 position, float radius)
        {
            var hits = Physics.OverlapSphere(position, radius, -1, QueryTriggerInteraction.Ignore);
            return hits.Any(col => col != null && col.CompareTag("Egg"));
        }
    }
}
