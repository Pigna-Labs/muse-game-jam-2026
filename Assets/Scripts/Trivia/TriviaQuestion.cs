using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Trivia
{
    [CreateAssetMenu(fileName = "TriviaQuestion", menuName = "Muse Game Jam/Trivia Question")]
    public class TriviaQuestion : ScriptableObject
    {
        private const int RequiredAnswerCount = 4;

        [SerializeField, TextArea] private string question = string.Empty;
        [SerializeField] private string[] answers = new string[RequiredAnswerCount];
        [SerializeField, Range(0, RequiredAnswerCount - 1)] private int correctAnswerIndex = 0;

        public string Question => question;
        public IReadOnlyList<string> Answers => answers;
        public int CorrectAnswerIndex => correctAnswerIndex;

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

        // Keeps authored question assets in the expected four-answer shape.
        private void OnValidate()
        {
            if (answers != null && answers.Length == RequiredAnswerCount)
            {
                return;
            }

            string[] resizedAnswers = new string[RequiredAnswerCount];

            for (int i = 0; i < resizedAnswers.Length; i++)
            {
                resizedAnswers[i] = answers != null && i < answers.Length
                    ? answers[i]
                    : string.Empty;
            }

            answers = resizedAnswers;
        }
    }
}
