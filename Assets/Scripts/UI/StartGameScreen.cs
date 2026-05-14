using CollectEggs.Core;
using UnityEngine;

namespace UI
{
    public sealed class StartGameScreen : MonoBehaviour
    {
        private const float ButtonWidth = 180f;
        private const float ButtonHeight = 44f;

        private GameBootstrapper _bootstrapper;
        private GUIStyle _buttonStyle;

        public void Initialize(GameBootstrapper bootstrapper) => _bootstrapper = bootstrapper;

        private void OnGUI()
        {
            if (_bootstrapper == null || !_bootstrapper.CanStartMatch)
                return;
            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            var width = Mathf.Min(ButtonWidth, Screen.width - 32f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - ButtonHeight) * 0.5f, width, ButtonHeight);
            if (GUI.Button(rect, "Start Game", _buttonStyle))
                _bootstrapper.StartLocalMatch();
        }
    }
}
