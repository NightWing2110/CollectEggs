using UnityEngine;
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

        private void Awake() => ApplyLayout();

        public void Bind(MatchResultEntry entry)
        {
            ApplyLayout();
            if (rankText != null)
                rankText.text = $"#{entry.Rank}";
            if (nameText != null)
                nameText.text = entry.DisplayName;
            if (eggCountText != null)
                eggCountText.text = entry.EggCount.ToString();
        }

        private void ApplyLayout()
        {
            ConfigureText(rankText, new Vector2(-210f, 0f), new Vector2(74f, 30f), TextAlignmentOptions.Left);
            ConfigureText(nameText, new Vector2(-20f, 0f), new Vector2(248f, 30f), TextAlignmentOptions.Left);
            ConfigureText(eggCountText, new Vector2(210f, 0f), new Vector2(90f, 30f), TextAlignmentOptions.Right);
        }

        private static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            if (text == null)
                return;
            text.fontSize = 28f;
            text.color = Color.black;
            text.alignment = alignment;
            var rect = text.transform as RectTransform;
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
