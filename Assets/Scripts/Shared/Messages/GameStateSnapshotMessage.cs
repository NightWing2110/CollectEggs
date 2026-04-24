using System;
using System.Collections.Generic;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    [Serializable]
    public sealed class GameStateSnapshotMessage : GameMessage
    {
        public float RemainingTime;
        public List<PlayerSnapshot> Players;
        public List<EggSnapshot> Eggs;
        public List<ScoreSnapshot> Scores;

        public GameStateSnapshotMessage()
        {
            Players = new List<PlayerSnapshot>();
            Eggs = new List<EggSnapshot>();
            Scores = new List<ScoreSnapshot>();
        }
    }
}
