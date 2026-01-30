using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogSystem
{
    public class AnswerHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogSystemManager _dialogManager;
        [SerializeField] private DialogHandler _dialogHandler;
        [SerializeField] private GameObject _dialogPanel;
        [SerializeField] private TMP_Text _questionText;
        [SerializeField] private Button[] _answerButtons;

        private void OnEnable()
        {
            if (_dialogManager != null)
            {
                _dialogHandler.OnDisplayEnded += ShowQuestion;
                _dialogManager.OnAllQuestionsDone += HideDialog;
            }

            if (_answerButtons != null)
            {
                for (int i = 0; i < _answerButtons.Length; i++)
                {
                    int index = i;
                    _answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
                }
            }
        }

        private void OnDisable()
        {
            if (_dialogManager != null)
            {
                _dialogHandler.OnDisplayEnded -= ShowQuestion;
                _dialogManager.OnAllQuestionsDone -= HideDialog;
            }

            if (_answerButtons != null)
            {
                for (int i = 0; i < _answerButtons.Length; i++)
                    _answerButtons[i].onClick.RemoveAllListeners();
            }
        }

        private void Start()
        {
            HideDialog();
        }

        private void ShowQuestion(DialogData dialog, bool isShowingAnswer)
        {
            if (dialog == null) return;
            
            if (isShowingAnswer) return;

            if (_dialogPanel != null)
                _dialogPanel.SetActive(true);

            if (_questionText != null)
                _questionText.text = dialog.Question;

            int answerCount = dialog.Answers != null ? dialog.Answers.Length : 0;
            for (int i = 0; i < _answerButtons?.Length; i++)
            {
                bool visible = i < answerCount;
                _answerButtons[i].gameObject.SetActive(visible);
                if (visible)
                {
                    var label = _answerButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                        label.text = dialog.Answers[i];
                }
            }
        }

        private void HideDialog()
        {
            if (_dialogPanel != null)
                _dialogPanel.SetActive(false);
        }

        private void OnAnswerClicked(int answerIndex)
        {
            if (_dialogManager != null && _dialogManager.State == DialogSystemManager.DialogState.WaitingForAnswer)
            {
                _dialogManager.SubmitAnswer(answerIndex);
                HideDialog();
            }
        }
    }
}
