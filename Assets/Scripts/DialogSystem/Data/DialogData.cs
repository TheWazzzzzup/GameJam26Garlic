using UnityEngine;

[CreateAssetMenu(fileName = "New Dialog Data", menuName = "Dialog Data/New Dialog Data")]
public class DialogData : ScriptableObject
{
    [TextArea(5, 20)] public string Dialog;
    [SerializeField,TextArea(5, 20)] public string RightAnswerReply;
    [SerializeField,TextArea(5, 20)] public string WrongAnswerReply;
    
    [SerializeField] public string Question;
    [SerializeField] public string[] Answers;
    [SerializeField] public int RightAnswer;
}
