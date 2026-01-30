using UnityEngine;

[CreateAssetMenu(fileName = "SatisfactionConfig", menuName = "Configs/Satisfaction Config")]
public class SatisfactionConfig : ScriptableObject
{
    [Header("Initial Values")]
    [Tooltip("Starting and maximum satisfaction value")]
    public int OverAllSatisfaction = 100;

    [Header("Passive Reduction")]
    [Tooltip("Satisfaction lost per second (normal state)")]
    public float PassiveReduction = 1f;
    
    [Tooltip("Satisfaction lost per second while waiting for answer")]
    public float PassiveWaitingForAnswerReduction = 2f;

    [Header("Answer Rewards/Penalties")]
    [Tooltip("Satisfaction gained for correct answer")]
    public float QuestionAnswerCorrectlyPoints = 10f;
    
    [Tooltip("Satisfaction lost for incorrect answer (use negative value)")]
    public float QuestionAnswerIncorrectlyPoints = -5f;

    private static SatisfactionConfig _instance;

    public static SatisfactionConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<SatisfactionConfig>("Configs/SatisfactionConfig");
            
            if (_instance == null)
                Debug.LogError("SatisfactionConfig not found in Resources/Configs/! Please create one.");
            
            return _instance;
        }
    }
}
