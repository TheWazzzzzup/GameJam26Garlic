using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DialogSystem
{
    public class DialogSystemManager : MonoBehaviour
    {
        public enum DialogState
        {
            WaitingForNextQuestion,
            WaitingForAnswer,
            AllQuestionsDone
        }

        [Header("System configuration")]
        [SerializeField] private float _minAskQuestionTimeRange = 5f;
        [SerializeField] private float _maxAskQuestionTimeRange = 15f;
        [SerializeField] private DialogData[] dialogs;

        public event Action<DialogData> OnQuestionShown;
        public event Action OnAllQuestionsDone;
        public event Action<bool> OnQuestionDone; 

        private DialogState _state;
        private HashSet<int> _askedDialogIndices;
        private int _currentDialogIndex;
        private float _nextQuestionTime;
        
        public DialogData CurrentDialog => dialogs != null && _currentDialogIndex >= 0 && _currentDialogIndex < dialogs.Length ? dialogs[_currentDialogIndex] : null;
        public DialogState State => _state;

        private void Awake()
        {
            _askedDialogIndices = new HashSet<int>();
            _currentDialogIndex = -1;
            _state = DialogState.WaitingForNextQuestion;
            ScheduleNextQuestion();
        }

        private void Update()
        {
            if (_state != DialogState.WaitingForNextQuestion)
                return;

            _nextQuestionTime -= Time.deltaTime;
            if (_nextQuestionTime <= 0f)
                TryShowNextQuestion();
        }

        private void ScheduleNextQuestion()
        {
            _nextQuestionTime = Random.Range(_minAskQuestionTimeRange, _maxAskQuestionTimeRange);
        }

        private void TryShowNextQuestion()
        {
            if (dialogs == null || dialogs.Length == 0)
            {
                _state = DialogState.AllQuestionsDone;
                OnAllQuestionsDone?.Invoke();
                return;
            }

            var unaskedIndices = new List<int>();
            for (int i = 0; i < dialogs.Length; i++)
            {
                if (dialogs[i] != null && !_askedDialogIndices.Contains(i))
                    unaskedIndices.Add(i);
            }

            if (unaskedIndices.Count == 0)
            {
                _state = DialogState.AllQuestionsDone;
                OnAllQuestionsDone?.Invoke();
                return;
            }

            _currentDialogIndex = unaskedIndices[Random.Range(0, unaskedIndices.Count)];
            _askedDialogIndices.Add(_currentDialogIndex);
            _state = DialogState.WaitingForAnswer;
            OnQuestionShown?.Invoke(dialogs[_currentDialogIndex]);
        }

        public void SubmitAnswer(int answerIndex)
        {
            if (_state != DialogState.WaitingForAnswer)
                return;

            if (CurrentDialog.RightAnswer == answerIndex)
            {
                OnQuestionDone?.Invoke(true);
            }
            else
            {
                OnQuestionDone?.Invoke(false);
            }
            
            _state = DialogState.WaitingForNextQuestion;
            ScheduleNextQuestion();
        }
    }
}
