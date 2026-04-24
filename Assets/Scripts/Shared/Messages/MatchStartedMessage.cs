using System;
using System.Collections.Generic;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    [Serializable]
    public sealed class MatchStartedMessage : GameMessage
    {
        public GameRulesSnapshot Rules;
        public List<PlayerSpawnData> Players;
        public List<EggSpawnData> Eggs;

        public MatchStartedMessage()
        {
            Players = new List<PlayerSpawnData>();
            Eggs = new List<EggSpawnData>();
        }
    }
}
