using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Scoring
{
    public class ScoreService : MonoBehaviour
    {
        private readonly Dictionary<string, int> _scores = new();
        public event Action<string, int> ScoreChanged;

        public void ResetScores()
        {
            _scores.Clear();
        }

        public void EnsurePlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;
            if (_scores.ContainsKey(playerId))
                return;
            _scores[playerId] = 0;
            ScoreChanged?.Invoke(playerId, 0);
        }

        public void AddScore(string playerId, int amount)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;
            if (amount <= 0)
                return;
            if (!_scores.ContainsKey(playerId))
                _scores[playerId] = 0;
            _scores[playerId] += amount;
            ScoreChanged?.Invoke(playerId, _scores[playerId]);
        }

        public int GetScore(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return 0;
            return _scores.TryGetValue(playerId, out var score) ? score : 0;
        }

        public bool TryGetWinner(out string winnerId, out int winnerScore)
        {
            winnerId = string.Empty;
            winnerScore = 0;
            var hasEntry = false;
            foreach (var pair in _scores)
            {
                if (hasEntry && pair.Value <= winnerScore)
                    continue;
                hasEntry = true;
                winnerId = pair.Key;
                winnerScore = pair.Value;
            }

            return hasEntry;
        }
    }
}
