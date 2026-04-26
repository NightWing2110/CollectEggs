using System;
using CollectEggs.Core;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.UI.Results;
using UnityEngine;

namespace UI
{
    public class MatchHud : MonoBehaviour
    {
        private const float RestartButtonWidth = 148f;
        private const float RestartButtonHeight = 38f;
        private const float RestartButtonGapBelowResults = 5f;

        private MatchTimer _timer;
        private ScoreService _score;
        private ResultsPanelView _resultsPanel;
        private string _localPlayerId;
        private Action _onRestartRequested;
        private GUIStyle _scoreLabelStyle;
        private GUIStyle _timeLabelStyle;
        private GUIStyle _matchEndLabelStyle;
        private GUIStyle _restartButtonStyle;
        private bool _isMatchEnded;
        private string _matchEndSummaryText;
        private bool _showMatchEndSummary = true;

        public void Initialize(MatchTimer timer, ScoreService score, ResultsPanelView resultsPanel, string localPlayerId, Action restart)
        {
            UnsubscribeFromGameSignals();
            _timer = timer;
            _score = score;
            _resultsPanel = resultsPanel;
            _localPlayerId = localPlayerId;
            _onRestartRequested = restart;
            _isMatchEnded = _timer != null && !_timer.IsRunning;
            _matchEndSummaryText = "Match Ended";
            _showMatchEndSummary = true;
            SubscribeToGameSignals();
        }

        public void SetMatchEndSummaryVisible(bool visible) => _showMatchEndSummary = visible;

        private void OnGUI()
        {
            if (_timer == null || _score == null)
                return;
            if (_scoreLabelStyle == null)
                BuildStyles();
            GUI.Label(new Rect(16f, 16f, 320f, 32f), $"Score: {_score.GetScore(_localPlayerId)}", _scoreLabelStyle);
            GUI.Label(new Rect(Screen.width - 220f, 16f, 204f, 32f), $"Time: {Mathf.CeilToInt(_timer.RemainingSeconds)}", _timeLabelStyle);
            if (!_isMatchEnded)
                return;
            if (_showMatchEndSummary)
                GUI.Label(new Rect(0f, Screen.height * 0.45f, Screen.width, 48f), _matchEndSummaryText, _matchEndLabelStyle);
            var buttonRect = ResolveRestartButtonRect();
            if (GUI.Button(buttonRect, "Play Again", _restartButtonStyle))
                _onRestartRequested?.Invoke();
        }

        private Rect ResolveRestartButtonRect()
        {
            var buttonX = (Screen.width - RestartButtonWidth) * 0.5f;
            if (_resultsPanel == null || _resultsPanel.PanelRoot == null)
                return new Rect(buttonX, Screen.height * 0.65f, RestartButtonWidth, RestartButtonHeight);
            var corners = new Vector3[4];
            var panelRoot = _resultsPanel.PanelRoot;
            panelRoot.GetWorldCorners(corners);
            var canvas = panelRoot.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var screenBottom = RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
            var panelBottomY = Screen.height - screenBottom;
            return new Rect(
                buttonX,
                panelBottomY + RestartButtonGapBelowResults,
                RestartButtonWidth,
                RestartButtonHeight);
        }

        private void OnDestroy() => UnsubscribeFromGameSignals();

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
            _matchEndSummaryText = "Match Ended";
        }

        private void HandleMatchEnded(string winnerId, int winnerScore)
        {
            _isMatchEnded = true;
            _matchEndSummaryText = string.IsNullOrWhiteSpace(winnerId)
                ? "Match Ended"
                : $"Winner: {winnerId} ({winnerScore})";
        }

        private void BuildStyles()
        {
            _scoreLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _timeLabelStyle = new GUIStyle(_scoreLabelStyle) { alignment = TextAnchor.UpperRight };
            _matchEndLabelStyle = new GUIStyle(_scoreLabelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 30 };
            _restartButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
        }
    }
}
