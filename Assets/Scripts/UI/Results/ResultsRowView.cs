using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CollectEggs.UI.Results
{
    public class ResultsRowView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text rankText;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text eggCountText;

        public void Bind(int rank, MatchResultEntry entry)
        {
            if (rankText != null)
                rankText.text = $"#{rank}";
            if (nameText != null)
                nameText.text = entry.DisplayName;
            if (eggCountText != null)
                eggCountText.text = entry.EggCount.ToString();
        }

        public static ResultsRowView CreateTemplate(Transform parent, TMP_FontAsset fontAsset)
        {
            var rowGo = new GameObject("RowTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);
            var bg = rowGo.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.82f);
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 12f;
            var rowLayout = rowGo.GetComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            var view = rowGo.AddComponent<ResultsRowView>();
            view.rankText = CreateText("Rank", rowGo.transform, fontAsset, TextAlignmentOptions.Left, 74f);
            view.nameText = CreateText("Name", rowGo.transform, fontAsset, TextAlignmentOptions.Left, 248f);
            view.eggCountText = CreateText("Eggs", rowGo.transform, fontAsset, TextAlignmentOptions.Right, 90f);
            rowGo.SetActive(false);
            return view;
        }

        public static GameObject CreateHeader(Transform parent, TMP_FontAsset fontAsset)
        {
            var headerGo = new GameObject("HeaderRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerGo.transform.SetParent(parent, false);
            var image = headerGo.GetComponent<Image>();
            image.color = new Color(0.93f, 0.93f, 0.93f, 1f);
            var layout = headerGo.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(14, 14, 9, 9);
            layout.spacing = 12f;
            var headerLayout = headerGo.GetComponent<LayoutElement>();
            headerLayout.minHeight = 46f;
            CreateText("HeaderRank", headerGo.transform, fontAsset, TextAlignmentOptions.Left, 74f, "Rank", true);
            CreateText("HeaderName", headerGo.transform, fontAsset, TextAlignmentOptions.Left, 248f, "Player", true);
            CreateText("HeaderEggs", headerGo.transform, fontAsset, TextAlignmentOptions.Right, 90f, "Point", true);
            return headerGo;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            TextAlignmentOptions align,
            float width,
            string initial = "",
            bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, 30f);
            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
            text.fontSize = 28f;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = align;
            text.color = Color.black;
            text.text = initial;
            return text;
        }
    }
}
