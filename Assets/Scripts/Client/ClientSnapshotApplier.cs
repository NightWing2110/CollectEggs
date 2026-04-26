using System.Collections.Generic;
using System.Linq;
using CollectEggs.Bots;
using CollectEggs.Client.View;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Shared.Messages;
using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Client
{
    public sealed class ClientSnapshotApplier
    {
        private readonly Dictionary<string, int> _snapshotScores = new();

        public void ApplySnapshot(
            GameStateSnapshotMessage message,
            MatchTimer matchTimer,
            EggViewManager eggViewManager,
            PlayerSpawner playerSpawner,
            EggCollectRequestController eggCollectRequests,
            IReadOnlyDictionary<string, PlayerEntity> playersById,
            PlayerEntity localPlayerEntity)
        {
            if (message == null || matchTimer == null)
                return;
            matchTimer.SetRemainingSecondsFromNetwork(message.remainingTime);
            ApplyScores(message.scores, eggCollectRequests);
            ApplyEggs(message, eggViewManager, playerSpawner, eggCollectRequests);
            ApplyPlayerPositions(message, playersById, localPlayerEntity);
        }

        public void ApplyFinalScores(MatchEndedMessage message, EggCollectRequestController eggCollectRequests)
        {
            if (message == null)
                return;
            ApplyScores(message.scores, eggCollectRequests);
        }

        private void ApplyScores(IEnumerable<ScoreSnapshot> scores, EggCollectRequestController eggCollectRequests)
        {
            _snapshotScores.Clear();
            if (scores == null)
                return;
            foreach (var score in scores.Where(score => score != null && !string.IsNullOrWhiteSpace(score.playerId)))
            {
                _snapshotScores[score.playerId] = score.score;
                eggCollectRequests?.ApplyServerScore(score.playerId, score.score);
            }
        }

        private void ApplyEggs(
            GameStateSnapshotMessage message,
            EggViewManager eggViewManager,
            PlayerSpawner playerSpawner,
            EggCollectRequestController eggCollectRequests)
        {
            if (message.eggs == null || eggViewManager == null || playerSpawner == null)
                return;
            foreach (var egg in message.eggs.Where(egg => egg != null && !string.IsNullOrWhiteSpace(egg.eggId)))
            {
                if (egg.isActive)
                {
                    eggViewManager.SpawnFromSnapshot(egg, playerSpawner.EggsRoot);
                    continue;
                }

                eggViewManager.RemoveFromServerData(egg.eggId);
                if (!string.IsNullOrWhiteSpace(egg.collectedByPlayerId))
                    eggCollectRequests?.ApplyServerCollected(
                        egg.collectedByPlayerId,
                        egg.eggId,
                        ResolveSnapshotScore(egg.collectedByPlayerId));
            }
        }

        private int ResolveSnapshotScore(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) &&
                   _snapshotScores.TryGetValue(playerId, out var score)
                ? score
                : 0;
        }

        private static void ApplyPlayerPositions(
            GameStateSnapshotMessage message,
            IReadOnlyDictionary<string, PlayerEntity> playersById,
            PlayerEntity localPlayerEntity)
        {
            if (message.players == null || playersById == null)
                return;
            foreach (var snapshot in message.players.Where(snapshot => snapshot != null && !string.IsNullOrWhiteSpace(snapshot.playerId)))
            {
                if (!playersById.TryGetValue(snapshot.playerId, out var entity) || entity == null)
                    continue;
                if (entity.GetComponent<BotController>() != null)
                    continue;
                if (localPlayerEntity != null && entity == localPlayerEntity)
                {
                    var controller = entity.Controller != null ? entity.Controller : entity.GetComponent<PlayerController>();
                    if (controller != null)
                        controller.ReconcileFromServer(snapshot.position, snapshot.lastProcessedInputSequence);
                    else
                        entity.transform.position = snapshot.position;
                }
                else
                    entity.transform.position = snapshot.position;
            }
        }
    }
}
