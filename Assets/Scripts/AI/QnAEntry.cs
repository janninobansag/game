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

    // Shuffle an encounter copy so the Inspector question bank stays unchanged.
    public QnAEntry CreateShuffledCopy()
    {
        QnAEntry copy = new QnAEntry
        {
            question = question,
            options = options == null ? null : (string[])options.Clone(),
            correctAnswerIndex = correctAnswerIndex
        };

        // The shared Q&A panel displays four answer slots (A through D).
        int count = copy.options == null ? 0 : Mathf.Min(4, copy.options.Length);
        for (int i = count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            string option = copy.options[i];
            copy.options[i] = copy.options[other];
            copy.options[other] = option;

            if (copy.correctAnswerIndex == i)
                copy.correctAnswerIndex = other;
            else if (copy.correctAnswerIndex == other)
                copy.correctAnswerIndex = i;
        }

        return copy;
    }
}
