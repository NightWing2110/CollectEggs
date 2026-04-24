using System;
using CollectEggs.Shared.Data;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct PlayerSpawnData
    {
        public string PlayerId;
        public string DisplayName;
        public PlayerType PlayerType;
        public bool IsLocalPlayer;
        public Vector3 SpawnPosition;
        public float MoveSpeed;
    }
}
