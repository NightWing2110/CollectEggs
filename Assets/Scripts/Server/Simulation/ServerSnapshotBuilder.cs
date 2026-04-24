using CollectEggs.Server.State;
using CollectEggs.Shared.Messages;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Server.Simulation
{
    public static class ServerSnapshotBuilder
    {
        public static GameStateSnapshotMessage Build(ServerGameState state)
        {
            var msg = new GameStateSnapshotMessage
            {
                RemainingTime = state.RemainingTime
            };
            foreach (var kv in state.Players)
            {
                var p = kv.Value;
                msg.Players.Add(new PlayerSnapshot
                {
                    PlayerId = p.PlayerId,
                    Position = p.Position,
                    Score = p.Score
                });
                msg.Scores.Add(new ScoreSnapshot { PlayerId = p.PlayerId, Score = p.Score });
            }

            foreach (var kv in state.Eggs)
            {
                var e = kv.Value;
                msg.Eggs.Add(new EggSnapshot
                {
                    EggId = e.EggId,
                    Position = e.Position,
                    Color = e.Color,
                    ScoreValue = e.ScoreValue,
                    IsActive = e.IsActive
                });
            }

            return msg;
        }
    }
}
