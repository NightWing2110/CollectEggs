using TMPro;
using UnityEngine;

namespace CollectEggs.Gameplay.Players.View
{
    public class PlayerNameView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text label;

        [SerializeField]
        private TextMesh fallbackLabel;

        [SerializeField]
        private Vector3 localOffset = new(0f, 2.1f, 0f);

        [SerializeField]
        private bool faceCamera = true;

        private Camera _targetCamera;

        public void Initialize(string displayName, Camera targetCamera)
        {
            _targetCamera = targetCamera;
            EnsureLabel();
            var finalName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
            if (label != null)
                label.text = finalName;
            else if (fallbackLabel != null)
                fallbackLabel.text = finalName;
            var labelTransform = LabelTransform();
            if (labelTransform != null)
                labelTransform.localPosition = localOffset;
            if (faceCamera)
                AlignToCamera();
        }

        private void LateUpdate()
        {
            if (!faceCamera || _targetCamera == null || LabelTransform() == null)
                return;
            AlignToCamera();
        }

        private void EnsureLabel()
        {
            if (label != null)
                return;
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = localOffset;
            label = labelGo.AddComponent<TextMeshPro>();
            if (TryConfigureTmpLabel(label))
                return;
            Destroy(label);
            label = null;
            fallbackLabel = labelGo.AddComponent<TextMesh>();
            fallbackLabel.anchor = TextAnchor.MiddleCenter;
            fallbackLabel.alignment = TextAlignment.Center;
            fallbackLabel.fontSize = 48;
            fallbackLabel.characterSize = 0.06f;
            fallbackLabel.color = Color.black;
        }

        private void AlignToCamera()
        {
            var labelTransform = LabelTransform();
            if (labelTransform == null)
                return;
            var camForward = _targetCamera.transform.forward;
            var planarForward = new Vector3(camForward.x, 0f, camForward.z);
            if (planarForward.sqrMagnitude <= 0.0001f)
                planarForward = Vector3.forward;
            labelTransform.forward = planarForward.normalized;
        }

        private bool TryConfigureTmpLabel(TMP_Text tmpLabel)
        {
            if (tmpLabel == null)
                return false;
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
                return false;
            tmpLabel.font = defaultFont;
            tmpLabel.alignment = TextAlignmentOptions.Center;
            tmpLabel.fontSize = 7f;
            tmpLabel.fontStyle = FontStyles.Bold;
            tmpLabel.enableWordWrapping = false;
            tmpLabel.color = Color.black;
            tmpLabel.outlineColor = new Color(1f, 1f, 1f, 0.85f);
            tmpLabel.outlineWidth = 0.12f;
            return true;
        }

        private Transform LabelTransform()
        {
            if (label != null)
                return label.transform;
            if (fallbackLabel != null)
                return fallbackLabel.transform;
            return null;
        }
    }
}
