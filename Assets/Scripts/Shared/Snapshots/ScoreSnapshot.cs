using System;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class ScoreSnapshot
    {
        public string playerId;
        public int score;
    }
}
