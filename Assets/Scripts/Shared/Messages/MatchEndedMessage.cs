using System;
using System.Collections.Generic;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    [Serializable]
    public sealed class MatchEndedMessage : GameMessage
    {
        public List<ScoreSnapshot> scores;
        public List<string> winnerPlayerIds;

        public MatchEndedMessage()
        {
            scores = new List<ScoreSnapshot>();
            winnerPlayerIds = new List<string>();
        }
    }
}
