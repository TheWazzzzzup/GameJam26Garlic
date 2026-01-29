using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogSystem
{
    public class DialogHandler : MonoBehaviour
    {
        [SerializeField] private DialogSystemManager _dialogSystemManager;
        [Header("Display")]
        [SerializeField] private TMP_Text _text;
        [SerializeField] private RectTransform _viewport;
        [Tooltip("Optional. Text leaving at the top fades out; assign a RawImage to enable.")]
        [SerializeField] private RawImage _topFadeOverlay;
        [Tooltip("Optional. Text entering at the bottom fades in; assign a RawImage to enable.")]
        [SerializeField] private RawImage _bottomFadeOverlay;

        [Header("Configuration")]
        [Tooltip("Number of lines visible in the viewport at once (e.g. 2–3).")]
        [SerializeField] [Min(1)] private int _visibleLineCount = 3;
        [Tooltip("How fast the text scrolls up (units per second).")]
        [SerializeField] [Min(0.1f)] private float _scrollSpeed = 80f;
        [Tooltip("Height of the fade zone at top and bottom (in pixels).")]
        [SerializeField] [Min(1)] private float _fadeHeight = 48f;
        [Tooltip("Color used for the fade overlay (e.g. match your dialog background).")]
        [SerializeField] private Color _fadeColor = Color.black;

        public event Action OnDisplayStarted;
        public event Action<DialogData> OnDisplayEnded;

        private Coroutine _scrollRoutine;
        private DialogData _dialogData;
        private Texture2D _topFadeTexture;
        private Texture2D _bottomFadeTexture;

        private void Awake()
        {
            _dialogSystemManager.OnQuestionShown += ShowDialog;
        }
        
        private void ShowDialog(DialogData data)
        {
            if (data == null) return;
            _dialogData = data;
            ShowDialog(data.Dialog ?? string.Empty);
        }

        private void ShowDialog(string content)
        {
            if (content == null || _text == null || _viewport == null) return;

            if (_scrollRoutine != null)
                StopCoroutine(_scrollRoutine);

            _text.text = content;
            _text.ForceMeshUpdate(true);
            _scrollRoutine = StartCoroutine(ScrollRoutine());
        }

        private IEnumerator ScrollRoutine()
        {
            yield return null;

            _text.ForceMeshUpdate(true);
            int lineCount = _text.textInfo.lineCount;
            if (lineCount == 0)
            {
                OnDisplayEnded?.Invoke(_dialogData);
                yield break;
            }
            float lineHeight = _text.preferredHeight / lineCount;
            float viewportHeight = lineHeight * Mathf.Min(_visibleLineCount, lineCount);
            _viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);
            SetupFadeOverlays();

            RectTransform textRect = _text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = Vector2.zero;

            float contentHeight = _text.preferredHeight;
            float scrollDistance = Mathf.Max(0f, contentHeight + viewportHeight);

            OnDisplayStarted?.Invoke();

            if (scrollDistance <= 0f)
            {
                OnDisplayEnded?.Invoke(_dialogData);
                yield break;
            }

            float scrolled = 0f;
            while (scrolled < scrollDistance)
            {
                float step = _scrollSpeed * Time.deltaTime;
                scrolled += step;
                if (scrolled > scrollDistance) step -= (scrolled - scrollDistance);
                textRect.anchoredPosition += Vector2.up * step;
                yield return null;
            }

            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, scrollDistance);
            OnDisplayEnded?.Invoke(_dialogData);
            _scrollRoutine = null;
        }

        private void SetupFadeOverlays()
        {
            const int gradientResolution = 64;

            if (_topFadeOverlay != null)
            {
                if (_topFadeTexture == null)
                    _topFadeTexture = CreateGradientTexture(gradientResolution, fadeAtTop: false);
                _topFadeOverlay.texture = _topFadeTexture;
                _topFadeOverlay.color = _fadeColor;
                _topFadeOverlay.raycastTarget = false;
                RectTransform r = _topFadeOverlay.rectTransform;
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, _fadeHeight);
                r.sizeDelta = new Vector2(0f, _fadeHeight);
            }

            if (_bottomFadeOverlay != null)
            {
                if (_bottomFadeTexture == null)
                    _bottomFadeTexture = CreateGradientTexture(gradientResolution, fadeAtTop: true);
                _bottomFadeOverlay.texture = _bottomFadeTexture;
                _bottomFadeOverlay.color = _fadeColor;
                _bottomFadeOverlay.raycastTarget = false;
                RectTransform r = _bottomFadeOverlay.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, -_fadeHeight);
                r.sizeDelta = new Vector2(0f, _fadeHeight);
            }
        }

        private static Texture2D CreateGradientTexture(int height, bool fadeAtTop)
        {
            var tex = new Texture2D(1, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                float t = (y + 0.5f) / height;
                byte alpha = (byte)(fadeAtTop ? Mathf.RoundToInt((1f - t) * 255f) : Mathf.RoundToInt(t * 255f));
                tex.SetPixel(0, y, new Color32(255, 255, 255, alpha));
            }
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (_topFadeTexture != null) Destroy(_topFadeTexture);
            if (_bottomFadeTexture != null) Destroy(_bottomFadeTexture);
        }
    }
}
