using System;
using CollectEggs.Shared.Data;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct PlayerSpawnData
    {
        public string playerId;
        public string displayName;
        public PlayerType playerType;
        public bool isLocalClientPlayer;
        public Vector3 spawnPosition;
        public float moveSpeed;
    }
}
