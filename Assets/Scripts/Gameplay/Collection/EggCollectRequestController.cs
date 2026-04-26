using System;
using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Networking.Transport;
using CollectEggs.Shared.Messages;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    public sealed class EggCollectRequestController : MonoBehaviour
    {
        [SerializeField] private MatchTimer matchTimer;
        [SerializeField] private ScoreService scoreService;
        [SerializeField] private float requestRetryDelaySeconds = 0.5f;

        private readonly Dictionary<string, float> _pendingEggRetryAt = new();
        private readonly HashSet<string> _confirmedCollectedEggIds = new();
        private IGameTransport _transport;

        public static EggCollectRequestController Active { get; private set; }
        public event Action<string, string, int> EggCollectionConfirmed;
        public bool IsMatchRunning => matchTimer != null && matchTimer.IsRunning;

        private void Awake() => Active = this;

        public void BeginMatch(
            IReadOnlyList<PlayerEntity> players,
            IGameTransport transport)
        {
            _transport = transport;
            _pendingEggRetryAt.Clear();
            _confirmedCollectedEggIds.Clear();
            scoreService.ResetScores();
            if (players == null)
                return;
            foreach (var player in players)
            {
                if (player == null)
                    continue;
                scoreService.EnsurePlayer(player.PlayerId);
            }
        }

        public bool RequestEggCollection(PlayerEntity collector, GameObject egg)
        {
            if (!CanRequestCollect(collector, egg, out var eggId))
                return false;
            _pendingEggRetryAt[eggId] = Time.time + Mathf.Max(0.05f, requestRetryDelaySeconds);
            _transport.SendToServer(new EggCollectRequestMessage
            {
                PlayerId = collector.PlayerId,
                EggId = eggId,
                PlayerPosition = collector.transform.position
            });
            return true;
        }

        public void ApplyServerScore(string playerId, int score) => scoreService.SetScore(playerId, score);

        public void ApplyServerCollected(string collectorPlayerId, string eggId, int newScore)
        {
            if (string.IsNullOrWhiteSpace(collectorPlayerId) || string.IsNullOrWhiteSpace(eggId))
                return;
            if (!_confirmedCollectedEggIds.Add(eggId))
                return;
            _pendingEggRetryAt.Remove(eggId);
            scoreService.SetScore(collectorPlayerId, newScore);
            EggCollectionConfirmed?.Invoke(collectorPlayerId, eggId, newScore);
            Debug.Log($"Score[{collectorPlayerId}]: {newScore}");
        }

        private bool CanRequestCollect(PlayerEntity collector, GameObject egg, out string eggId)
        {
            eggId = string.Empty;
            if (!IsMatchRunning || collector == null || egg == null || _transport == null)
                return false;
            if (string.IsNullOrWhiteSpace(collector.PlayerId))
                return false;
            var eggEntity = egg.GetComponent<EggEntity>();
            if (eggEntity == null || string.IsNullOrWhiteSpace(eggEntity.EggId))
                return false;
            eggId = eggEntity.EggId;
            if (!_pendingEggRetryAt.TryGetValue(eggId, out var retryAt))
                return true;
            if (Time.time < retryAt)
                return false;
            _pendingEggRetryAt.Remove(eggId);
            return true;
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }
    }
}
