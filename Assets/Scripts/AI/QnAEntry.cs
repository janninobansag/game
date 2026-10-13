// Serializable Q&A data shared by White Lady and Tikbalang encounters.
using UnityEngine;

[System.Serializable]
public class QnAEntry
{
    [TextArea(2, 4)]
    public string question;
    public string[] options = new string[4];
    public int correctAnswerIndex;

    public QnAEntry()
    {
        options = new string[4];
        correctAnswerIndex = 0;
    }
}