using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Scoring
{
    public class ScoreService : MonoBehaviour
    {
        private readonly Dictionary<string, int> _scores = new();
        public event Action<string, int> ScoreChanged;

        public void ResetScores() => _scores.Clear();

        public void EnsurePlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;
            if (!_scores.TryAdd(playerId, 0))
                return;
            ScoreChanged?.Invoke(playerId, 0);
        }

        public void SetScore(string playerId, int score)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;
            _scores[playerId] = Math.Max(0, score);
            ScoreChanged?.Invoke(playerId, _scores[playerId]);
        }

        public int GetScore(string playerId) => string.IsNullOrWhiteSpace(playerId) ? 0 : _scores.GetValueOrDefault(playerId, 0);
    }
}
