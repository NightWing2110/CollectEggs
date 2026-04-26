using System.Linq;
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
                remainingTime = state.RemainingTime
            };
            foreach (var p in state.Players.Select(kv => kv.Value))
            {
                msg.players.Add(new PlayerSnapshot
                {
                    playerId = p.PlayerId,
                    position = p.Position,
                    lastProcessedInputSequence = p.LastProcessedInputSequence
                });
                msg.scores.Add(new ScoreSnapshot { playerId = p.PlayerId, score = p.Score });
            }

            foreach (var e in state.Eggs.Select(kv => kv.Value))
            {
                msg.eggs.Add(new EggSnapshot
                {
                    eggId = e.EggId,
                    position = e.Position,
                    color = e.Color,
                    scoreValue = e.ScoreValue,
                    isActive = e.IsActive,
                    collectedByPlayerId = e.CollectedByPlayerId
                });
            }

            return msg;
        }

        public static MatchEndedMessage BuildMatchEnded(ServerGameState state)
        {
            var msg = new MatchEndedMessage
            {
                ServerTime = state.ServerTime
            };
            var highestScore = 0;
            foreach (var p in state.Players.Select(kv => kv.Value))
            {
                msg.scores.Add(new ScoreSnapshot { playerId = p.PlayerId, score = p.Score });
                if (p.Score > highestScore)
                    highestScore = p.Score;
            }

            foreach (var p in state.Players.Select(kv => kv.Value).Where(p => p.Score == highestScore))
                msg.winnerPlayerIds.Add(p.PlayerId);

            return msg;
        }
    }
}
