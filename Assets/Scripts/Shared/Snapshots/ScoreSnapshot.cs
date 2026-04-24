using System;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class ScoreSnapshot
    {
        public string PlayerId;
        public int Score;
    }
}
