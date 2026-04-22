using System;
using CollectEggs.Core;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using UnityEngine;

namespace CollectEggs.UI
{
    public class DebugHud : MonoBehaviour
    {
        private MatchTimer _timer;
        private ScoreService _score;
        private string _localPlayerId;
        private Action _restart;
        private GUIStyle _left;
        private GUIStyle _right;
        private GUIStyle _center;
        private GUIStyle _button;
        private bool _isMatchEnded;
        private string _winnerText;

        public void Initialize(MatchTimer timer, ScoreService score, string localPlayerId, Action restart)
        {
            UnsubscribeFromGameSignals();
            _timer = timer;
            _score = score;
            _localPlayerId = localPlayerId;
            _restart = restart;
            _isMatchEnded = _timer != null && !_timer.IsRunning;
            _winnerText = "Match Ended";
            SubscribeToGameSignals();
        }

        private void OnGUI()
        {
            if (_timer == null || _score == null)
                return;
            if (_left == null)
                BuildStyles();
            GUI.Label(new Rect(16f, 16f, 320f, 32f), $"Score: {_score.GetScore(_localPlayerId)}", _left);
            GUI.Label(new Rect(Screen.width - 220f, 16f, 204f, 32f), $"Time: {Mathf.CeilToInt(_timer.RemainingSeconds)}", _right);
            if (!_isMatchEnded)
                return;
            GUI.Label(new Rect(0f, Screen.height * 0.45f, Screen.width, 48f), _winnerText, _center);
            if (GUI.Button(new Rect((Screen.width - 180f) * 0.5f, Screen.height * 0.55f, 180f, 44f), "Restart", _button))
                _restart?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameSignals();
        }

        private void SubscribeToGameSignals()
        {
            if (GameManager.Instance == null)
                return;
            GameManager.Instance.MatchStarted += HandleMatchStarted;
            GameManager.Instance.MatchEnded += HandleMatchEnded;
        }

        private void UnsubscribeFromGameSignals()
        {
            if (GameManager.Instance == null)
                return;
            GameManager.Instance.MatchStarted -= HandleMatchStarted;
            GameManager.Instance.MatchEnded -= HandleMatchEnded;
        }

        private void HandleMatchStarted()
        {
            _isMatchEnded = false;
            _winnerText = "Match Ended";
        }

        private void HandleMatchEnded(string winnerId, int winnerScore)
        {
            _isMatchEnded = true;
            _winnerText = string.IsNullOrWhiteSpace(winnerId)
                ? "Match Ended"
                : $"Winner: {winnerId} ({winnerScore})";
        }

        private void BuildStyles()
        {
            _left = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _right = new GUIStyle(_left) { alignment = TextAnchor.UpperRight };
            _center = new GUIStyle(_left) { alignment = TextAnchor.MiddleCenter, fontSize = 30 };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        }
    }
}
