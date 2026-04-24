using System;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct GameRulesSnapshot
    {
        public float MatchDuration;
        public float PlayerMoveSpeed;
        public float EggCollectRadius;
        public int PlayerCount;
        public int InitialEggCount;
    }
}
