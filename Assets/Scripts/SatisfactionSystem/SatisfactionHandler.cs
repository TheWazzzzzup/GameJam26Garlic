using System;
using UnityEngine;
using DialogSystem;

namespace DefaultNamespace.SatisfactionSystem
{
    public class SatisfactionHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogSystemManager _dialogSystemManager;

        public event Action<float> OnSatisfactionChanged;
        public event Action OnSatisfactionDepleted;

        private static float _currentSatisfaction;
        private SatisfactionConfig _config;

        public static float CurrentSatisfaction => _currentSatisfaction;
        public float MaxSatisfaction => _config != null ? _config.OverAllSatisfaction : 0f;

        private void Awake()
        {
            _config = SatisfactionConfig.Instance;
            if (_config == null)
            {
                Debug.LogError("SatisfactionConfig not found! Please create one in Resources/Configs/");
                enabled = false;
                return;
            }

            Data.MaxSatisfaction = MaxSatisfaction;
            Data.CurrentSatisfaction = _currentSatisfaction;
            _currentSatisfaction = _config.OverAllSatisfaction;
            _dialogSystemManager.OnQuestionDone += OnQuestionDone;
        }

        private void OnQuestionDone(bool obj)
        {
            if (obj)
            {
                ApplyCorrectAnswerBonus();
            }
            else
            {
                ApplyIncorrectAnswerPenalty();
            }
        }

        private void Update()
        {
            if (_config == null || _dialogSystemManager == null)
                return;

            float reductionRate = _dialogSystemManager.State == DialogSystemManager.DialogState.WaitingForAnswer
                ? _config.PassiveWaitingForAnswerReduction
                : _config.PassiveReduction;

            ModifySatisfaction(-reductionRate * Time.deltaTime);
        }

        public void AddSatisfaction(float amount)
        {
            ModifySatisfaction(amount);
        }

        public void RemoveSatisfaction(float amount)
        {
            ModifySatisfaction(-amount);
        }

        public void ApplyCorrectAnswerBonus()
        {
            if (_config != null)
                ModifySatisfaction(_config.QuestionAnswerCorrectlyPoints);
        }

        public void ApplyIncorrectAnswerPenalty()
        {
            if (_config != null)
                ModifySatisfaction(_config.QuestionAnswerIncorrectlyPoints);
        }

        private void ModifySatisfaction(float delta)
        {
            float previousSatisfaction = _currentSatisfaction;
            _currentSatisfaction = Mathf.Clamp(_currentSatisfaction + delta, 0f, _config != null ? _config.OverAllSatisfaction : float.MaxValue);
            Data.CurrentSatisfaction = _currentSatisfaction;
            if (!Mathf.Approximately(previousSatisfaction, _currentSatisfaction))
                OnSatisfactionChanged?.Invoke(_currentSatisfaction);

            if (_currentSatisfaction <= 0f && previousSatisfaction > 0f)
                OnSatisfactionDepleted?.Invoke();
        }
    }
}
