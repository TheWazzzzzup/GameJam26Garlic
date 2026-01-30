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

        public event Action<DialogData> OnQuestionShown;
        public event Action OnAllQuestionsDone;
        public event Action<bool> OnQuestionDone; 

        private DialogState _state;
        private HashSet<int> _askedDialogIndices;
        private int _currentDialogIndex;
        private float _nextQuestionTime;
        private DialogSystemConfig _config;
        
        public DialogData CurrentDialog => _config?.Dialogs != null && _currentDialogIndex >= 0 && _currentDialogIndex < _config.Dialogs.Length ? _config.Dialogs[_currentDialogIndex] : null;
        public DialogState State => _state;

        private void Awake()
        {
            _config = DialogSystemConfig.Instance;
            if (_config == null)
            {
                Debug.LogError("DialogSystemConfig not found! Please create one in Resources/Configs/");
                enabled = false;
                return;
            }

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
            if (_config == null) return;
            _nextQuestionTime = Random.Range(_config.MinAskQuestionTimeRange, _config.MaxAskQuestionTimeRange);
        }

        private void TryShowNextQuestion()
        {
            if (_config == null || _config.Dialogs == null || _config.Dialogs.Length == 0)
            {
                _state = DialogState.AllQuestionsDone;
                OnAllQuestionsDone?.Invoke();
                return;
            }

            var unaskedIndices = new List<int>();
            for (int i = 0; i < _config.Dialogs.Length; i++)
            {
                if (_config.Dialogs[i] != null && !_askedDialogIndices.Contains(i))
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
            OnQuestionShown?.Invoke(_config.Dialogs[_currentDialogIndex]);
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
