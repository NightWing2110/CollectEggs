using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CollectEggs.UI.Results
{
    public class ResultsPanelView : MonoBehaviour
    {
        private const float RowHeight = 44f;
        private const float RowSpacing = 52f;
        private const float MaxPanelWidth = 560f;
        private const float MaxPanelHeight = 360f;
        private const float ScreenMargin = 48f;
        private const float PanelPadding = 40f;

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

        public RectTransform PanelRoot => panelRoot;

        private void Awake()
        {
            ApplySceneLayout();
        }

        public void Hide()
        {
            ApplySceneLayout();
            if (overlayRoot != null)
                overlayRoot.gameObject.SetActive(false);
        }

        public void Show(IReadOnlyList<MatchResultEntry> results)
        {
            ApplySceneLayout();
            if (!HasRequiredReferences())
            {
                Debug.LogError("ResultsPanelView references are not fully assigned.");
                return;
            }
            overlayRoot.gameObject.SetActive(true);
            var count = results?.Count ?? 0;
            EnsureRowPool(count);
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var active = i < count;
                row.gameObject.SetActive(active);
                if (!active) continue;
                var rt = row.transform as RectTransform;
                if (rt != null)
                {
                    var listHeight = ResolveHeight(resultsListRoot, 260f);
                    var startY = listHeight * 0.5f - RowHeight * 0.5f - 8f;
                    rt.anchoredPosition = new Vector2(0f, startY - i * RowSpacing);
                }
                if (results != null) row.Bind(results[i]);
            }
        }

        private void ApplySceneLayout()
        {
            Stretch(overlayRoot);
            var overlaySize = ResolveOverlaySize();
            var panelWidth = Mathf.Min(MaxPanelWidth, Mathf.Max(280f, overlaySize.x - ScreenMargin * 2f));
            var panelHeight = Mathf.Min(MaxPanelHeight, Mathf.Max(260f, overlaySize.y - ScreenMargin * 2f));
            var contentWidth = Mathf.Max(240f, panelWidth - PanelPadding);
            var listHeight = Mathf.Max(180f, panelHeight - 100f);

            if (panelRoot != null)
            {
                panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
                panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                panelRoot.pivot = new Vector2(0.5f, 0.5f);
                panelRoot.anchoredPosition = Vector2.zero;
                panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);
            }

            if (titleText != null)
            {
                titleText.text = "Result";
                titleText.fontSize = 42f;
                titleText.color = Color.black;
                titleText.alignment = TextAlignmentOptions.Center;
                var titleRect = titleText.transform as RectTransform;
                if (titleRect != null)
                {
                    titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                    titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                    titleRect.pivot = new Vector2(0.5f, 0.5f);
                    titleRect.anchoredPosition = new Vector2(0f, panelHeight * 0.5f - 40f);
                    titleRect.sizeDelta = new Vector2(contentWidth, 50f);
                }
            }

            if (resultsListRoot != null)
            {
                resultsListRoot.anchorMin = new Vector2(0.5f, 0.5f);
                resultsListRoot.anchorMax = new Vector2(0.5f, 0.5f);
                resultsListRoot.pivot = new Vector2(0.5f, 0.5f);
                resultsListRoot.anchoredPosition = new Vector2(0f, -10f);
                resultsListRoot.sizeDelta = new Vector2(contentWidth, listHeight);
            }

            if (rowTemplate == null) return;
            var templateRect = rowTemplate.transform as RectTransform;
            if (templateRect == null) return;
            templateRect.anchorMin = new Vector2(0.5f, 0.5f);
            templateRect.anchorMax = new Vector2(0.5f, 0.5f);
            templateRect.pivot = new Vector2(0.5f, 0.5f);
            templateRect.sizeDelta = new Vector2(contentWidth, RowHeight);
        }

        private Vector2 ResolveOverlaySize()
        {
            if (overlayRoot != null)
            {
                var rect = overlayRoot.rect;
                if (rect.width > 0f && rect.height > 0f)
                    return rect.size;
            }

            return new Vector2(Screen.width, Screen.height);
        }

        private static float ResolveHeight(RectTransform rect, float fallback)
        {
            if (rect == null)
                return fallback;
            return rect.rect.height > 0f ? rect.rect.height : Mathf.Max(fallback, rect.sizeDelta.y);
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private bool HasRequiredReferences()
        {
            return overlayRoot != null &&
                   panelRoot != null &&
                   titleText != null &&
                   resultsListRoot != null &&
                   rowTemplate != null;
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
    }
}
