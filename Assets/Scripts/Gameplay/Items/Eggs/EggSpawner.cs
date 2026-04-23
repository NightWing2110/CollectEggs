using CollectEggs.Gameplay.Timer;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    [DefaultExecutionOrder(10)]
    public class EggSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject eggPrefab;

        [SerializeField]
        private Vector3 spawnBoundsMin = new(-8f, 0.35f, -8f);

        [SerializeField]
        private Vector3 spawnBoundsMax = new(8f, 0.35f, 8f);

        [SerializeField]
        private float overlapCheckRadius = 0.45f;

        [SerializeField]
        private LayerMask groundLayer;

        [SerializeField]
        private LayerMask blockingLayer;

        [SerializeField]
        private float groundProbeHeight = 2f;

        [SerializeField]
        private float groundProbeDistance = 4f;

        [SerializeField]
        private float minHorizontalDistanceFromPlayer = 2.5f;

        [SerializeField]
        private int maxSpawnAttempts = 48;

        [SerializeField]
        private int initialEggCount = 20;
        // public int initialEggCount = 1;

        private int _spawnedEggIndex;
        private Transform _spawnRoot;
        private readonly List<Transform> _players = new();
        private MatchTimer _matchTimer;

        public Vector3 SpawnBoundsMin => spawnBoundsMin;
        public Vector3 SpawnBoundsMax => spawnBoundsMax;

        public void Initialize(Transform spawnRoot, IReadOnlyList<Transform> players, MatchTimer matchTimer)
        {
            _spawnRoot = spawnRoot;
            _players.Clear();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player != null)
                        _players.Add(player);
                }
            }
            _matchTimer = matchTimer;
        }

        public void SpawnInitialEggs()
        {
            for (var i = 0; i < initialEggCount; i++)
                SpawnEggs();
        }

        public void SpawnEggs()
        {
            if (_matchTimer != null && !_matchTimer.IsRunning) return;
            if (eggPrefab == null) return;
            for (var attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                var p = SamplePosition();
                if (!IsSpawnValid(p))
                    continue;
                var egg = Instantiate(eggPrefab, p, Quaternion.identity, _spawnRoot);
                _spawnedEggIndex++;
                egg.name = $"Egg_{_spawnedEggIndex:000}";
                var eggEntity = egg.GetComponent<EggEntity>();
                if (eggEntity != null)
                    eggEntity.Configure($"egg-{_spawnedEggIndex:000}");
                return;
            }
        }

        private Vector3 SamplePosition()
        {
            var x = Random.Range(spawnBoundsMin.x, spawnBoundsMax.x);
            var y = Random.Range(spawnBoundsMin.y, spawnBoundsMax.y);
            var z = Random.Range(spawnBoundsMin.z, spawnBoundsMax.z);
            return new Vector3(x, y, z);
        }

        private bool IsSpawnValid(Vector3 p)
        {
            return HasMinDistanceFromPlayers(p) && IsOnGround(p) && IsClearOfCollisions(p);
        }

        private bool HasMinDistanceFromPlayers(Vector3 p)
        {
            if (_players.Count == 0)
                return true;
            var minDistanceSq = minHorizontalDistanceFromPlayer * minHorizontalDistanceFromPlayer;
            foreach (var player in _players)
            {
                if (player == null)
                    continue;
                var pp = player.position;
                var dx = p.x - pp.x;
                var dz = p.z - pp.z;
                if (dx * dx + dz * dz < minDistanceSq)
                    return false;
            }
            return true;
        }

        private bool IsClearOfCollisions(Vector3 p)
        {
            if (blockingLayer.value != 0 && Physics.CheckSphere(p, overlapCheckRadius, blockingLayer, QueryTriggerInteraction.Ignore))
                return false;
            var hits = Physics.OverlapSphere(p, overlapCheckRadius, -1, QueryTriggerInteraction.Ignore);
            foreach (var collider in hits)
            {
                if (collider == null)
                    continue;
                if (collider.CompareTag("Egg"))
                    return false;
            }
            return true;
        }

        private bool IsOnGround(Vector3 p)
        {
            if (groundLayer.value == 0)
                return true;
            var origin = p + Vector3.up * groundProbeHeight;
            return Physics.Raycast(origin, Vector3.down, groundProbeDistance, groundLayer, QueryTriggerInteraction.Ignore);
        }
    }
}
