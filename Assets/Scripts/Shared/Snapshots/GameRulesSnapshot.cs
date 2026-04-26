using System;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct GameRulesSnapshot
    {
        public float matchDuration;
        public float eggCollectRadius;
    }
}
