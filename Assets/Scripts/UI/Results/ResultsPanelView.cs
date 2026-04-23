using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CollectEggs.UI.Results
{
    public class ResultsPanelView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform overlayRoot;

        [SerializeField]
        private RectTransform panelRoot;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private RectTransform resultsListRoot;

        [SerializeField]
        private ResultsRowView rowTemplate;

        private readonly List<ResultsRowView> _rows = new();
        private TMP_FontAsset _fontAsset;

        public void Hide()
        {
            EnsureBuilt();
            if (overlayRoot != null)
                overlayRoot.gameObject.SetActive(false);
        }

        public void Show(IReadOnlyList<MatchResultEntry> results)
        {
            EnsureBuilt();
            if (overlayRoot == null || panelRoot == null || rowTemplate == null)
                return;
            overlayRoot.gameObject.SetActive(true);
            var count = results != null ? results.Count : 0;
            EnsureRowPool(count);
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var active = i < count;
                row.gameObject.SetActive(active);
                if (active)
                    row.Bind(i + 1, results[i]);
            }
        }

        private void EnsureBuilt()
        {
            if (overlayRoot != null && panelRoot != null && titleText != null && resultsListRoot != null && rowTemplate != null)
                return;

            _fontAsset = _fontAsset != null ? _fontAsset : TMP_Settings.defaultFontAsset;
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                canvas = CreateCanvas();

            overlayRoot = CreateOverlay(canvas.transform);
            panelRoot = CreatePanel(overlayRoot);
            titleText = CreateTitle(panelRoot, _fontAsset);
            resultsListRoot = CreateListRoot(panelRoot);
            ResultsRowView.CreateHeader(resultsListRoot, _fontAsset);
            rowTemplate = ResultsRowView.CreateTemplate(resultsListRoot, _fontAsset);
            overlayRoot.gameObject.SetActive(false);
        }

        private void EnsureRowPool(int needed)
        {
            while (_rows.Count < needed)
            {
                var row = Instantiate(rowTemplate, resultsListRoot);
                row.name = $"ResultRow_{_rows.Count + 1:00}";
                row.gameObject.SetActive(false);
                _rows.Add(row);
            }
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform CreateOverlay(Transform parent)
        {
            var overlayGo = new GameObject("ResultsOverlay", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(parent, false);
            var overlay = (RectTransform)overlayGo.transform;
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            var overlayImage = overlayGo.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
            return overlay;
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            var panelGo = new GameObject("ResultsPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGo.transform.SetParent(parent, false);
            var rt = (RectTransform)panelGo.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 90f);
            rt.sizeDelta = new Vector2(560f, 0f);
            var image = panelGo.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.985f);
            var outline = panelGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.2f);
            outline.effectDistance = new Vector2(1f, -1f);
            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 20);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = panelGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        private static TMP_Text CreateTitle(Transform parent, TMP_FontAsset fontAsset)
        {
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleGo.transform.SetParent(parent, false);
            var title = titleGo.GetComponent<TextMeshProUGUI>();
            title.font = fontAsset;
            title.text = "Result";
            title.fontSize = 42f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.black;
            var rt = (RectTransform)titleGo.transform;
            rt.sizeDelta = new Vector2(0f, 44f);
            var layout = titleGo.GetComponent<LayoutElement>();
            layout.minHeight = 50f;
            return title;
        }

        private static RectTransform CreateListRoot(Transform parent)
        {
            var listGo = new GameObject("ResultsList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(parent, false);
            var list = (RectTransform)listGo.transform;
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = listGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return list;
        }
    }
}
