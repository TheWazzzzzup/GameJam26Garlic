using UnityEngine;

[CreateAssetMenu(fileName = "New Dialog Data", menuName = "Dialog Data/New Dialog Data")]
public class DialogData : ScriptableObject
{
    [TextArea(5, 20)] public string Dialog;
    public string Question;
    public string[] Answers;
    public int RightAnswer;
    public string RightAnswerReply;
    public string WrongAnswerReply;
}
