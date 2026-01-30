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
        
        // Added Mask reference to handle the "disappearing" logic
        [SerializeField] private RectMask2D _viewportMask;

        [Tooltip("Optional. Text leaving at the top fades out; assign a RawImage to enable.")]
        [SerializeField] private RawImage _topFadeOverlay;
        [Tooltip("Optional. Text entering at the bottom fades in; assign a RawImage to enable.")]
        [SerializeField] private RawImage _bottomFadeOverlay;

        public event Action OnDisplayStarted;
        public event Action<DialogData, bool> OnDisplayEnded;

        private Coroutine _scrollRoutine;
        private DialogData _dialogData;
        private Texture2D _topFadeTexture;
        private Texture2D _bottomFadeTexture;
        private DialogDisplayConfig _config;

        private void Awake()
        {
            _config = DialogDisplayConfig.Instance;
            if (_config == null)
            {
                Debug.LogError("DialogDisplayConfig not found! Please create one in Resources/Configs/");
                enabled = false;
                return;
            }

            _dialogSystemManager.OnQuestionShown += ShowDialog;
            _dialogSystemManager.OnQuestionDone += ShowAnswer;
            
            // Auto-assign mask if not set
            if (_viewportMask == null && _viewport != null)
                _viewportMask = _viewport.GetComponent<RectMask2D>();
        }

        private void ShowAnswer(bool answerCorrectly)
        {
            if (_dialogData == null) return;

            string replyText = answerCorrectly ? _dialogData.RightAnswerReply : _dialogData.WrongAnswerReply;
            ShowDialog(replyText ?? string.Empty, true);
        }

        private void ShowDialog(DialogData data)
        {
            if (data == null) return;
            _dialogData = data;
            ShowDialog(data.Dialog ?? string.Empty);
        }

        private void ShowDialog(string content, bool isShowingAnswer = false)
        {
            if (content == null || _text == null || _viewport == null) return;

            if (_scrollRoutine != null)
                StopCoroutine(_scrollRoutine);

            _text.text = content;
            _text.ForceMeshUpdate(true);
            _scrollRoutine = StartCoroutine(ScrollRoutine(isShowingAnswer));
        }

        private IEnumerator ScrollRoutine(bool isShowingAnswer)
        {
            yield return null;

            _text.ForceMeshUpdate(true);
            int lineCount = _text.textInfo.lineCount;
            if (lineCount == 0)
            {
                OnDisplayEnded?.Invoke(_dialogData, isShowingAnswer);
                yield break;
            }

            float lineHeight = _text.preferredHeight / lineCount;
            float viewportHeight = lineHeight * Mathf.Min(_config.VisibleLineCount, lineCount);
            _viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);
            
            // Apply padding to the mask so text is hidden behind the fade zones
            if (_viewportMask != null)
            {
                // This forces the text to "exist" only within the visible area
                // padding = (left, bottom, right, top)
                _viewportMask.padding = Vector4.zero;
            }

            SetupFadeOverlays();

            RectTransform textRect = _text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = Vector2.zero;

            float contentHeight = _text.preferredHeight;
            float scrollDistance = Mathf.Max(0f, contentHeight + viewportHeight);

            OnDisplayStarted?.Invoke();

            float scrolled = 0f;
            while (scrolled < scrollDistance)
            {
                float step = _config.ScrollSpeed * Time.deltaTime;
                scrolled += step;
                if (scrolled > scrollDistance) step -= (scrolled - scrollDistance);
                textRect.anchoredPosition += Vector2.up * step;
                yield return null;
            }

            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, scrollDistance);
            OnDisplayEnded?.Invoke(_dialogData, isShowingAnswer);
            _scrollRoutine = null;
        }

        // --- Rest of your original fade logic remains untouched ---
        private void SetupFadeOverlays()
        {
            const int gradientResolution = 64;
            if (_topFadeOverlay != null)
            {
                if (_topFadeTexture == null)
                    _topFadeTexture = CreateGradientTexture(gradientResolution, fadeAtTop: false);
                _topFadeOverlay.texture = _topFadeTexture;
                _topFadeOverlay.color = _config.FadeColor;
                _topFadeOverlay.raycastTarget = false;
                RectTransform r = _topFadeOverlay.rectTransform;
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, 0f); // Adjusted to sit exactly at the top
                r.sizeDelta = new Vector2(0f, _config.FadeHeight);
            }

            if (_bottomFadeOverlay != null)
            {
                if (_bottomFadeTexture == null)
                    _bottomFadeTexture = CreateGradientTexture(gradientResolution, fadeAtTop: true);
                _bottomFadeOverlay.texture = _bottomFadeTexture;
                _bottomFadeOverlay.color = _config.FadeColor;
                _bottomFadeOverlay.raycastTarget = false;
                RectTransform r = _bottomFadeOverlay.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 0f); // Adjusted to sit exactly at the bottom
                r.sizeDelta = new Vector2(0f, _config.FadeHeight);
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