using System;
using System.Collections.Generic;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    [Serializable]
    public sealed class GameStateSnapshotMessage : GameMessage
    {
        public float remainingTime;
        public List<PlayerSnapshot> players;
        public List<EggSnapshot> eggs;
        public List<ScoreSnapshot> scores;

        public GameStateSnapshotMessage()
        {
            players = new List<PlayerSnapshot>();
            eggs = new List<EggSnapshot>();
            scores = new List<ScoreSnapshot>();
        }
    }
}
