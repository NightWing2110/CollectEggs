using System.Collections.Generic;
using System.Linq;
using CollectEggs.Bots;
using CollectEggs.Core;
using CollectEggs.Gameplay;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CollectEggs.EditorTools
{
    public sealed class BotPathDebugEditorWindow : EditorWindow
    {
        private const int PathCount = 5;

        private static readonly Color[] PathColors =
        {
            new Color(0.95f, 0.25f, 0.2f, 1f),
            new Color(0.25f, 0.95f, 0.35f, 1f),
            new Color(0.25f, 0.45f, 1f, 1f),
            new Color(1f, 0.92f, 0.2f, 1f),
            new Color(0.95f, 0.35f, 1f, 1f)
        };

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Cull = Shader.PropertyToID("_Cull");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int ZTest = Shader.PropertyToID("_ZTest");

        private bool _drawInGame;
        private bool _liveRefreshInPlayMode;
        private float _lineHeightOffset = 0.06f;

        private readonly List<List<Vector3>> _pathLists = new(8);
        private readonly List<(BotController bot, List<List<Vector3>> paths)> _cache = new(16);
        private readonly List<Vector3> _drawScratch = new(128);
        private Material _lineMaterial;

        private double _lastAutoRefreshTime;

        [MenuItem("CollectEggs/Debug/Bot Path Overlay…")]
        private static void Open()
        {
            GetWindow<BotPathDebugEditorWindow>("Bot paths");
        }

        private void OnEnable()
        {
            EnsureLineMaterial();
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            Camera.onPostRender += OnCameraPostRender;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Camera.onPostRender -= OnCameraPostRender;
            EditorApplication.update -= OnEditorUpdate;
            _cache.Clear();
        }

        private void OnEditorUpdate()
        {
            if (!_drawInGame || !_liveRefreshInPlayMode || !Application.isPlaying)
                return;
            if (EditorApplication.timeSinceStartup - _lastAutoRefreshTime < 0.22)
                return;
            _lastAutoRefreshTime = EditorApplication.timeSinceStartup;
            RefreshCache();
            Repaint();
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsMatchRunning)
                EditorGUILayout.HelpBox("Trận chưa chạy — bot có thể chưa có target, đường có thể trống.", MessageType.Info);

            EditorGUILayout.Space(4f);
            var prevDraw = _drawInGame;
            _drawInGame = EditorGUILayout.ToggleLeft("Vẽ trong Game View", _drawInGame);
            if (_drawInGame && !prevDraw)
                RefreshCache();

            using (new EditorGUI.DisabledScope(!_drawInGame))
            {
                _liveRefreshInPlayMode = EditorGUILayout.ToggleLeft("Tự làm mới khi Play (Game View)", _liveRefreshInPlayMode);
                _lineHeightOffset = EditorGUILayout.FloatField("Lệch cao độ Y", _lineHeightOffset);
                EditorGUILayout.Space(6f);
                if (GUILayout.Button("Làm mới đường (click)", GUILayout.Height(28)))
                    RefreshCache();
            }

            if (!_drawInGame && prevDraw)
            {
                _cache.Clear();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Bot trong cache: {_cache.Count}", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("Đường chỉ hiện trong Game View khi chạy Play (Editor), không vào build.", MessageType.None);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            DrawForGameCamera(camera);
        }

        private void OnCameraPostRender(Camera camera)
        {
            DrawForGameCamera(camera);
        }

        private void DrawForGameCamera(Camera camera)
        {
            if (!_drawInGame || !Application.isPlaying || camera == null || camera.cameraType != CameraType.Game)
                return;
            if (GameManager.Instance != null && !GameManager.Instance.IsMatchRunning)
                return;
            EnsureLineMaterial();
            if (_lineMaterial == null)
                return;

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            foreach (var (bot, paths) in _cache)
            {
                if (bot == null)
                    continue;
                var y = bot.transform.position.y + _lineHeightOffset;
                for (var i = 0; i < paths.Count && i < PathColors.Length; i++)
                {
                    var pts = paths[i];
                    if (pts == null || pts.Count < 2)
                        continue;
                    CopyPathElevated(pts, y, _drawScratch);
                    GL.Color(PathColors[i]);
                    for (var p = 1; p < _drawScratch.Count; p++)
                    {
                        GL.Vertex(_drawScratch[p - 1]);
                        GL.Vertex(_drawScratch[p]);
                    }
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void CopyPathElevated(List<Vector3> source, float y, List<Vector3> dest)
        {
            dest.Clear();
            foreach (var t in source)
            {
                var v = t;
                v.y = y;
                dest.Add(v);
            }
        }

        private void RefreshCache()
        {
            _cache.Clear();
            var grid = Object.FindFirstObjectByType<GridMap>();
            if (grid == null)
                return;

            var bots = Object.FindObjectsByType<BotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot == null || !bot.gameObject.activeInHierarchy)
                    continue;

                var egg = bot.CurrentTargetEgg ?? PickAnyEgg();
                if (egg == null || !egg.gameObject.activeInHierarchy)
                    continue;

                var useExactEnd = bot.UsesExactApproachGoal;
                var endWorld = useExactEnd ? bot.DebugPathEndWorld : egg.transform.position;
                var pathCount = AStarPathfinder.FindUpToKCandidatePaths(grid, bot.transform.position, endWorld, _pathLists, PathCount, 5, useExactEnd);
                if (pathCount <= 0)
                    continue;

                var entryPaths = new List<List<Vector3>>(pathCount);
                for (var i = 0; i < pathCount; i++)
                {
                    var src = _pathLists[i];
                    var copy = new List<Vector3>(src.Count);
                    copy.AddRange(src);
                    entryPaths.Add(copy);
                }

                _cache.Add((bot, entryPaths));
            }
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null)
                return;
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                return;
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt(Cull, (int)CullMode.Off);
            _lineMaterial.SetInt(ZWrite, 0);
            _lineMaterial.SetInt(ZTest, (int)CompareFunction.LessEqual);
        }

        private static EggEntity PickAnyEgg()
        {
            var eggs = EggEntity.Active;
            return eggs.FirstOrDefault(e => e != null && e.gameObject.activeInHierarchy);
        }
    }
}
