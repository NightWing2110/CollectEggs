using System.Globalization;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.Simulation;
using UnityEngine;

namespace UI
{
    public sealed class NetworkSimulationDebugPanel : MonoBehaviour
    {
        private const float ButtonWidth = 140f;
        private const float ButtonHeight = 36f;
        private const float PanelWidth = 360f;
        private const float PanelHeight = 248f;
        private const float Padding = 16f;
        private const float DefaultSnapshotMinSeconds = 0.1f;
        private const float DefaultSnapshotMaxSeconds = 0.5f;
        private const float DefaultLatencyMinSeconds = 0.3f;
        private const float DefaultLatencyMaxSeconds = 0.5f;
        private const float ResetSeconds = 0f;
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        private ServerConfig _config;
        private LatencyProfile _latencyProfile;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private Rect _panelRect;
        private bool _isOpen;
        private bool _panelPositionInitialized;
        private string _snapshotMin;
        private string _snapshotMax;
        private string _latencyMin;
        private string _latencyMax;
        private string _status;

        public void Initialize(ServerConfig config, LatencyProfile latencyProfile)
        {
            _config = config;
            _latencyProfile = latencyProfile;
            LoadDefaultFields();
        }

        private void OnGUI()
        {
            if (_config == null || _latencyProfile == null)
                return;

            EnsureStyles();
            EnsurePanelPosition();

            var buttonRect = new Rect(Padding, Screen.height - ButtonHeight - Padding, ButtonWidth, ButtonHeight);
            if (GUI.Button(buttonRect, "Net Debug", _buttonStyle))
                _isOpen = !_isOpen;

            if (_isOpen)
                _panelRect = GUI.Window(GetInstanceID(), _panelRect, DrawPanel, "Network Simulation");
        }

        private void DrawPanel(int windowId)
        {
            GUILayout.Space(8f);
            DrawField("Snapshot Min (s)", ref _snapshotMin);
            DrawField("Snapshot Max (s)", ref _snapshotMax);
            DrawField("Latency Min (s)", ref _latencyMin);
            DrawField("Latency Max (s)", ref _latencyMax);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Height(32f)))
                ApplyValues();
            if (GUILayout.Button("Defaults", GUILayout.Height(32f)))
                ApplyResetValues();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label(_status, _labelStyle);

            GUI.DragWindow(new Rect(0f, 0f, PanelWidth, 24f));
        }

        private void DrawField(string label, ref string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(150f));
            value = GUILayout.TextField(value, GUILayout.Width(160f));
            GUILayout.EndHorizontal();
        }

        private void ApplyValues()
        {
            if (!TryParse(_snapshotMin, out var snapshotMin) ||
                !TryParse(_snapshotMax, out var snapshotMax) ||
                !TryParse(_latencyMin, out var latencyMin) ||
                !TryParse(_latencyMax, out var latencyMax))
            {
                _status = "Use numeric values like 0.25";
                return;
            }

            _config.snapshotIntervalMinSeconds = snapshotMin;
            _config.snapshotIntervalMaxSeconds = snapshotMax;
            _config.simulatedTransportLatencyMinSeconds = latencyMin;
            _config.simulatedTransportLatencyMaxSeconds = latencyMax;
            _config.Normalize();
            _latencyProfile.SetDelays(
                _config.simulatedTransportLatencyMinSeconds,
                _config.simulatedTransportLatencyMaxSeconds);
            RefreshFieldsFromConfig();
            _status = "Applied runtime simulation settings";
        }

        private void LoadDefaultFields()
        {
            _snapshotMin = Format(DefaultSnapshotMinSeconds);
            _snapshotMax = Format(DefaultSnapshotMaxSeconds);
            _latencyMin = Format(DefaultLatencyMinSeconds);
            _latencyMax = Format(DefaultLatencyMaxSeconds);
            _status = string.Empty;
        }

        private void ApplyResetValues()
        {
            _config.snapshotIntervalMinSeconds = ResetSeconds;
            _config.snapshotIntervalMaxSeconds = ResetSeconds;
            _config.simulatedTransportLatencyMinSeconds = ResetSeconds;
            _config.simulatedTransportLatencyMaxSeconds = ResetSeconds;
            _config.Normalize();
            _latencyProfile.SetDelays(ResetSeconds, ResetSeconds);
            _status = "Reset runtime simulation settings";
        }

        private void RefreshFieldsFromConfig()
        {
            if (_config == null)
                return;

            _snapshotMin = Format(_config.snapshotIntervalMinSeconds);
            _snapshotMax = Format(_config.snapshotIntervalMaxSeconds);
            _latencyMin = Format(_config.simulatedTransportLatencyMinSeconds);
            _latencyMax = Format(_config.simulatedTransportLatencyMaxSeconds);
            _status = string.Empty;
        }

        private void EnsureStyles()
        {
            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
        }

        private void EnsurePanelPosition()
        {
            if (_panelPositionInitialized)
                return;

            var y = Screen.height - PanelHeight - ButtonHeight - Padding * 2f;
            _panelRect = new Rect(Padding, Mathf.Max(Padding, y), PanelWidth, PanelHeight);
            _panelPositionInitialized = true;
        }

        private static bool TryParse(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, Culture, out result);

        private static string Format(float value) => value.ToString("0.###", Culture);
    }
}
