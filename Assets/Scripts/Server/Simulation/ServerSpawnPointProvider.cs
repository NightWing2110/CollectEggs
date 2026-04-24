using System;
using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerSpawnPointProvider : ISpawnPointProvider
    {
        private readonly ServerMatchConfig _config;
        private readonly EggSpawner _boundsSource;

        public ServerSpawnPointProvider(ServerMatchConfig config, EggSpawner boundsSource)
        {
            _config = config;
            _boundsSource = boundsSource;
        }

        public IReadOnlyList<Vector3> CreatePlayerSpawnPositions(int playerCount, System.Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            var result = new List<Vector3>(Mathf.Max(0, playerCount));
            if (playerCount <= 0)
                return result;

            var source = _config.playerSpawnPoints ?? new List<Vector3>();
            var candidates = new List<Vector3>(source.Count);
            foreach (var p in source)
                candidates.Add(p);

            if (candidates.Count == 0)
            {
                AddRingPositions(result, playerCount, Vector3.zero, _config.botSpawnRingRadius);
                return result;
            }

            var center = Vector3.zero;
            foreach (var p in candidates)
                center += p;
            center /= candidates.Count;

            Shuffle(candidates, rng);

            var fromList = Mathf.Min(playerCount, candidates.Count);
            for (var i = 0; i < fromList; i++)
                result.Add(candidates[i]);

            var remaining = playerCount - fromList;
            if (remaining > 0)
                AddRingPositions(result, remaining, center, _config.botSpawnRingRadius);

            return result;
        }

        private static void Shuffle(List<Vector3> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void AddRingPositions(List<Vector3> result, int count, Vector3 center, float radius)
        {
            var r = Mathf.Max(0.1f, radius);
            var n = Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / n;
                result.Add(center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
            }
        }

        public bool GetEggSpawnPosition(
            int eggIndex,
            System.Random rng,
            IReadOnlyList<Vector3> playerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            out Vector3 position)
        {
            position = default;
            if (_boundsSource == null || rng == null)
                return false;
            var min = _boundsSource.SpawnBoundsMin;
            var max = _boundsSource.SpawnBoundsMax;
            const int maxAttempts = 96;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var p = SampleInBounds(min, max, rng);
                if (!SnapPositionToGround(ref p))
                    continue;
                if (!IsSpawnValid(p, playerWorldPositions, occupiedEggWorldPositions, true))
                    continue;
                position = p;
                return true;
            }

            for (var attempt = 0; attempt < 48; attempt++)
            {
                var p = SampleInBounds(min, max, rng);
                if (!SnapPositionToGround(ref p))
                    continue;
                if (!IsSpawnValid(p, playerWorldPositions, occupiedEggWorldPositions, false))
                    continue;
                position = p;
                return true;
            }

            return false;
        }

        private static float LerpRng(float a, float b, System.Random rng)
        {
            var t = (float)rng.NextDouble();
            return a + (b - a) * t;
        }

        private static Vector3 SampleInBounds(Vector3 min, Vector3 max, System.Random rng)
        {
            return new Vector3(LerpRng(min.x, max.x, rng), LerpRng(min.y, max.y, rng), LerpRng(min.z, max.z, rng));
        }

        private bool SnapPositionToGround(ref Vector3 p)
        {
            var ground = _boundsSource.GroundLayer;
            if (ground.value == 0)
                return true;
            var origin = p + Vector3.up * _boundsSource.GroundProbeHeight;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, _boundsSource.GroundProbeDistance, ground.value, QueryTriggerInteraction.Ignore))
                return false;
            var y = hit.point.y + _boundsSource.EggCenterOffsetAboveGround;
            p = new Vector3(p.x, y, p.z);
            return true;
        }

        private bool IsSpawnValid(
            Vector3 p,
            IReadOnlyList<Vector3> playerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            bool requireEggSeparation)
        {
            if (!HasMinDistanceFromPlayers(p, playerWorldPositions))
                return false;
            if (requireEggSeparation && !HasMinDistanceFromOccupiedEggs(p, occupiedEggWorldPositions))
                return false;
            if (!IsClearOfCollisions(p))
                return false;
            return true;
        }

        private bool HasMinDistanceFromPlayers(Vector3 p, IReadOnlyList<Vector3> playerWorldPositions)
        {
            if (playerWorldPositions == null || playerWorldPositions.Count == 0)
                return true;
            var minD = _boundsSource != null ? _boundsSource.MinHorizontalDistanceFromPlayer : 2.5f;
            var minDistanceSq = minD * minD;
            foreach (var pp in playerWorldPositions)
            {
                var dx = p.x - pp.x;
                var dz = p.z - pp.z;
                if (dx * dx + dz * dz < minDistanceSq)
                    return false;
            }

            return true;
        }

        private bool HasMinDistanceFromOccupiedEggs(Vector3 p, IReadOnlyList<Vector3> occupiedEggWorldPositions)
        {
            if (occupiedEggWorldPositions == null || occupiedEggWorldPositions.Count == 0)
                return true;
            var r = _boundsSource.OverlapCheckRadius;
            var minCenter = Mathf.Max(r * 2.1f, 0.35f);
            var minSq = minCenter * minCenter;
            foreach (var e in occupiedEggWorldPositions)
            {
                var dx = p.x - e.x;
                var dz = p.z - e.z;
                if (dx * dx + dz * dz < minSq)
                    return false;
            }

            return true;
        }

        private bool IsClearOfCollisions(Vector3 p)
        {
            var r = _boundsSource.OverlapCheckRadius;
            var blocking = _boundsSource.BlockingLayer;
            if (blocking.value != 0 && Physics.CheckSphere(p, r, blocking, QueryTriggerInteraction.Ignore))
                return false;
            var hits = Physics.OverlapSphere(p, r, -1, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                if (col != null && col.CompareTag("Egg"))
                    return false;
            }

            return true;
        }
    }
}
