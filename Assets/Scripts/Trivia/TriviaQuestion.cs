using UnityEngine;

namespace MuseGameJam.Trivia
{
    [CreateAssetMenu(fileName = "TriviaQuestion", menuName = "Muse Game Jam/Trivia Question")]
    public class TriviaQuestion : ScriptableObject
    {
        private const int RequiredAnswerCount = 4;

        [TextArea] public string question = string.Empty;
        public string[] answers = new string[RequiredAnswerCount];
        [Range(0, RequiredAnswerCount - 1)] public int correctAnswerIndex = 0;

        // Returns fallback text for missing answers so the UI always has four buttons.
        public string GetAnswer(int index)
        {
            if (index < 0 || index >= RequiredAnswerCount)
            {
                Debug.LogError($"Trivia answer index {index} is outside the 0-{RequiredAnswerCount - 1} range.", this);
                return string.Empty;
            }

            if (answers == null || index >= answers.Length || string.IsNullOrWhiteSpace(answers[index]))
            {
                return $"Answer {index + 1}";
            }

            return answers[index];
        }
    }
}
