using UnityEngine;

namespace CollectEggs.Server.State
{
    public sealed class ServerEggState
    {
        public string EggId;
        public Vector3 Position;
        public Color Color;
        public int ScoreValue;
        public bool IsActive;
        public string CollectedByPlayerId;
    }
}
