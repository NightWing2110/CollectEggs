using UnityEngine;

namespace CollectEggs.Server.State
{
    public sealed class ServerPlayerState
    {
        public string PlayerId;
        public bool IsLocalClientPlayer;
        public Vector3 Position;
        public float MoveSpeed;
        public int Score;
        public int LastProcessedInputSequence;
    }
}
