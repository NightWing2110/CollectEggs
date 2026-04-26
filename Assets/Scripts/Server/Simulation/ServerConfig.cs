using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    [Serializable]
    public sealed class ServerConfig
    {
        private static readonly Vector3[] DefaultPlayerSpawnPoints =
        {
            new(0f, 0f, 0f),
            new(8.5f, 0f, -4f),
            new(-9f, 0f, 0f),
            new(-5f, 0f, 7f),
            new(7f, 0f, 7f)
        };

        public float matchDurationSeconds = 60f;
        public float playerMoveSpeed = 6f;
        public float eggCollectRadius = 0.75f;
        public float eggCollectValidationSlack = 0.35f;
        public int eggScoreValue = 1;
        public const int PlayerCount = 5;
        public int initialEggCount = 20;
        public float botSpawnRingRadius = 6f;
        public float snapshotIntervalMinSeconds = 0.1f; //0.1f
        public float snapshotIntervalMaxSeconds = 0.5f; //0.5f
        public float simulatedTransportLatencyMinSeconds = 0.3f; //0.3
        public float simulatedTransportLatencyMaxSeconds = 0.5f; //0.5
        public List<Vector3> playerSpawnPoints = new();


        public void Normalize()
        {
            playerSpawnPoints ??= new List<Vector3>();
            if (playerSpawnPoints.Count == 0)
            {
                foreach (var p in DefaultPlayerSpawnPoints)
                    playerSpawnPoints.Add(p);
            }

            matchDurationSeconds = Mathf.Max(1f, matchDurationSeconds);
            playerMoveSpeed = Mathf.Max(0f, playerMoveSpeed);
            eggCollectRadius = Mathf.Max(0.01f, eggCollectRadius);
            eggCollectValidationSlack = Mathf.Max(0f, eggCollectValidationSlack);
            eggScoreValue = Mathf.Max(0, eggScoreValue);
            initialEggCount = Mathf.Max(1, initialEggCount);
            botSpawnRingRadius = Mathf.Max(0.1f, botSpawnRingRadius);

            var snapA = snapshotIntervalMinSeconds;
            var snapB = snapshotIntervalMaxSeconds;
            snapshotIntervalMinSeconds = Mathf.Min(snapA, snapB);
            snapshotIntervalMaxSeconds = Mathf.Max(snapA, snapB);
            snapshotIntervalMinSeconds = Mathf.Max(0f, snapshotIntervalMinSeconds);
            snapshotIntervalMaxSeconds = Mathf.Max(snapshotIntervalMinSeconds, snapshotIntervalMaxSeconds);

            simulatedTransportLatencyMinSeconds = Mathf.Max(0f, simulatedTransportLatencyMinSeconds);
            simulatedTransportLatencyMaxSeconds = Mathf.Max(simulatedTransportLatencyMinSeconds, simulatedTransportLatencyMaxSeconds);
        }
    }
}
