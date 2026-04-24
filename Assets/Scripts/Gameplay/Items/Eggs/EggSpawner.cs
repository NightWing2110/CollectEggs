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
        private float eggCenterOffsetAboveGround = 0.5f;

        public GameObject EggPrefab => eggPrefab;
        public Vector3 SpawnBoundsMin => spawnBoundsMin;
        public Vector3 SpawnBoundsMax => spawnBoundsMax;
        public float OverlapCheckRadius => overlapCheckRadius;
        public LayerMask GroundLayer => groundLayer;
        public LayerMask BlockingLayer => blockingLayer;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public float MinHorizontalDistanceFromPlayer => minHorizontalDistanceFromPlayer;

        public float EggCenterOffsetAboveGround => Mathf.Max(0f, eggCenterOffsetAboveGround);
    }
}
