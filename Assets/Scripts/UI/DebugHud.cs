using System;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using UnityEngine;

namespace CollectEggs.Gameplay.UI
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

        public void Initialize(MatchTimer timer, ScoreService score, string localPlayerId, Action restart)
        {
            _timer = timer;
            _score = score;
            _localPlayerId = localPlayerId;
            _restart = restart;
        }

        private void OnGUI()
        {
            if (_timer == null || _score == null)
                return;
            if (_left == null)
                BuildStyles();
            GUI.Label(new Rect(16f, 16f, 320f, 32f), $"Score: {_score.GetScore(_localPlayerId)}", _left);
            GUI.Label(new Rect(Screen.width - 220f, 16f, 204f, 32f), $"Time: {Mathf.CeilToInt(_timer.RemainingSeconds)}", _right);
            if (_timer.IsRunning)
                return;
            GUI.Label(new Rect(0f, Screen.height * 0.45f, Screen.width, 48f), "Match Ended", _center);
            if (GUI.Button(new Rect((Screen.width - 180f) * 0.5f, Screen.height * 0.55f, 180f, 44f), "Restart", _button))
                _restart?.Invoke();
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
