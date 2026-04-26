using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerSpawnPointProvider : ISpawnPointProvider
    {
        private readonly ServerConfig _config;
        private readonly IServerWorldQuery _worldQuery;

        public ServerSpawnPointProvider(ServerConfig config, IServerWorldQuery worldQuery)
        {
            _config = config;
            _worldQuery = worldQuery;
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
            candidates.AddRange(source);

            if (candidates.Count == 0)
            {
                AddRingPositions(result, playerCount, Vector3.zero, _config.botSpawnRingRadius);
                return result;
            }

            var center = candidates.Aggregate(Vector3.zero, (current, p) => current + p);
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

        public bool ResolvePlayerMove(
            Vector3 currentPosition,
            Vector2 input,
            float speed,
            float deltaTime,
            out Vector3 resolvedPosition)
        {
            if (input.sqrMagnitude > 1f)
                input.Normalize();
            var displacement = new Vector3(input.x, 0f, input.y) * Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime);
            var candidate = currentPosition + displacement;
            if (_worldQuery != null &&
                _worldQuery.GetBlockingOverlap(candidate, _worldQuery.MoveCheckRadius))
            {
                resolvedPosition = currentPosition;
                return false;
            }

            resolvedPosition = candidate;
            return true;
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

        public bool GetEggSpawnPosition(System.Random rng,
            IReadOnlyList<Vector3> playerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            out Vector3 position)
        {
            position = default;
            if (_worldQuery == null || rng == null || !_worldQuery.GetEggSpawnBounds(out var min, out var max))
                return false;
            const int maxAttempts = 96;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var p = SampleInBounds(min, max, rng);
                if (!_worldQuery.SnapEggSpawnToGround(p, out p))
                    continue;
                if (!IsSpawnValid(p, playerWorldPositions, occupiedEggWorldPositions, true))
                    continue;
                position = p;
                return true;
            }

            for (var attempt = 0; attempt < 48; attempt++)
            {
                var p = SampleInBounds(min, max, rng);
                if (!_worldQuery.SnapEggSpawnToGround(p, out p))
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
            return IsClearOfCollisions(p);
        }

        private bool HasMinDistanceFromPlayers(Vector3 p, IReadOnlyList<Vector3> playerWorldPositions)
        {
            if (playerWorldPositions == null || playerWorldPositions.Count == 0)
                return true;
            var minD = _worldQuery?.MinHorizontalDistanceFromPlayer ?? 2.5f;
            var minDistanceSq = minD * minD;
            return !(from pp in playerWorldPositions let dx = p.x - pp.x let dz = p.z - pp.z where dx * dx + dz * dz < minDistanceSq select dx).Any();
        }

        private bool HasMinDistanceFromOccupiedEggs(Vector3 p, IReadOnlyList<Vector3> occupiedEggWorldPositions)
        {
            if (occupiedEggWorldPositions == null || occupiedEggWorldPositions.Count == 0)
                return true;
            var r = _worldQuery?.EggSpawnCheckRadius ?? 0.45f;
            var minCenter = Mathf.Max(r * 2.1f, 0.35f);
            var minSq = minCenter * minCenter;
            return !(from e in occupiedEggWorldPositions let dx = p.x - e.x let dz = p.z - e.z where dx * dx + dz * dz < minSq select dx).Any();
        }

        private bool IsClearOfCollisions(Vector3 p)
        {
            if (_worldQuery == null)
                return true;
            var r = _worldQuery.EggSpawnCheckRadius;
            if (_worldQuery.GetBlockingOverlap(p, r))
                return false;
            return !_worldQuery.HasEggOverlap(p, r);
        }
    }
}
