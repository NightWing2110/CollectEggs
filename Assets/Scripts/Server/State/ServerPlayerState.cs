using CollectEggs.Shared.Data;
using UnityEngine;

namespace CollectEggs.Server.State
{
    public sealed class ServerPlayerState
    {
        public string PlayerId;
        public string DisplayName;
        public PlayerType PlayerType;
        public bool IsLocalPlayer;
        public Vector3 Position;
        public float MoveSpeed;
        public int Score;
    }
}
