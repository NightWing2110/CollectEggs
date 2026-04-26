using System;
using System.Collections.Generic;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    [Serializable]
    public sealed class MatchStartedMessage : GameMessage
    {
        public GameRulesSnapshot rules;
        public List<PlayerSpawnData> players;
        public List<EggSpawnData> eggs;

        public MatchStartedMessage()
        {
            players = new List<PlayerSpawnData>();
            eggs = new List<EggSpawnData>();
        }
    }
}
