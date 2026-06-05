using System;
using MuseGameJam.Trivia;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TriviaUiController : MonoBehaviour
    {
        private const int AnswerCount = 4;

        [SerializeField] private TriviaQuestion question;

        private readonly Action[] answerHandlers = new Action[AnswerCount];
        private readonly Button[] answerButtons = new Button[AnswerCount];
        private UIDocument document;
        private Label questionLabel;
        private bool hasAnswered;
        private int correctAnswerIndex;

        public event Action<int> AnswerSelected;

        // Prepares reusable answer callbacks before the authored UI document is bound.
        private void Awake()
        {
            CreateAnswerHandlers();
        }

        // Binds to the authored UXML elements when the UIDocument becomes active.
        private void OnEnable()
        {
            BindElements();
            SetQuestion(question);
        }

        // Removes button callbacks so re-enabling the object does not double-register taps.
        private void OnDisable()
        {
            UnbindAnswerButtons();
        }

        // Updates the visible question and answers from trivia gameplay code.
        public void SetQuestion(TriviaQuestion nextQuestion)
        {
            question = RequireReference(nextQuestion, "TriviaUiController needs a TriviaQuestion asset assigned.");

            SetQuestionText(question.Question);

            for (int i = 0; i < AnswerCount; i++)
            {
                SetAnswer(i, question.GetAnswer(i));
            }

            SetCorrectAnswer(question.CorrectAnswerIndex);

            ClearAnswerFeedback();
            hasAnswered = false;
            SetInteractable(true);
        }

        // Updates one answer button when only a single option changes.
        public void SetAnswer(int index, string answer)
        {
            if (index < 0 || index >= AnswerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Trivia answer index must be between 0 and {AnswerCount - 1}.");
            }

            answerButtons[index].text = string.IsNullOrWhiteSpace(answer) ? $"Answer {index + 1}" : answer;
        }

        // Enables or disables answer taps while feedback or transitions are playing.
        public void SetInteractable(bool interactable)
        {
            foreach (Button button in answerButtons)
            {
                button.SetEnabled(interactable);
            }
        }

        // Sets which of the four authored answer buttons should count as correct.
        public void SetCorrectAnswer(int index)
        {
            if (index < 0 || index >= AnswerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Trivia correct answer index must be between 0 and {AnswerCount - 1}.");
            }

            correctAnswerIndex = index;
        }

        // Highlights the chosen answer and, when provided, the correct one.
        public void ShowAnswerFeedback(int selectedIndex, int correctIndex)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button button = answerButtons[i];

                button.RemoveFromClassList("answer-selected");
                button.RemoveFromClassList("answer-correct");
                button.RemoveFromClassList("answer-wrong");

                if (i == correctIndex)
                {
                    button.AddToClassList("answer-correct");
                }

                if (i == selectedIndex && selectedIndex != correctIndex)
                {
                    button.AddToClassList("answer-wrong");
                }
                else if (i == selectedIndex)
                {
                    button.AddToClassList("answer-selected");
                }
            }
        }

        // Removes answer feedback before showing the next question.
        public void ClearAnswerFeedback()
        {
            foreach (Button button in answerButtons)
            {
                button.RemoveFromClassList("answer-selected");
                button.RemoveFromClassList("answer-correct");
                button.RemoveFromClassList("answer-wrong");
            }
        }

        // Finds the named elements from Trivia.uxml and wires answer button callbacks.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "Trivia UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            questionLabel = RequireElement<Label>(root, "trivia-question");

            for (int i = 0; i < AnswerCount; i++)
            {
                Button answerButton = RequireElement<Button>(root, $"trivia-answer-{i + 1}");
                answerButtons[i] = answerButton;

                answerButton.clicked -= answerHandlers[i];
                answerButton.clicked += answerHandlers[i];
            }
        }

        // Raises the selected answer index for the trivia rules controller.
        private void HandleAnswerClicked(int answerIndex)
        {
            if (hasAnswered)
            {
                return;
            }

            hasAnswered = true;
            ShowAnswerFeedback(answerIndex, correctAnswerIndex);
            AnswerSelected?.Invoke(answerIndex);
        }

        // Updates only the visible question label.
        private void SetQuestionText(string text)
        {
            questionLabel.text = string.IsNullOrWhiteSpace(text) ? "Question text goes here." : text;
        }

        // Creates stable callbacks so they can be unsubscribed when the UI disables.
        private void CreateAnswerHandlers()
        {
            for (int i = 0; i < AnswerCount; i++)
            {
                int answerIndex = i;
                answerHandlers[i] = () => HandleAnswerClicked(answerIndex);
            }
        }

        // Detaches callbacks from the authored answer buttons.
        private void UnbindAnswerButtons()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                {
                    continue;
                }

                answerButtons[i].clicked -= answerHandlers[i];
                answerButtons[i] = null;
            }
        }

        // Returns a required Unity object or throws so missing Inspector references are visible in the console and terminal.
        private T RequireReference<T>(T reference, string message) where T : class
        {
            if (reference == null)
            {
                throw new MissingReferenceException(message);
            }

            return reference;
        }

        // Finds a required authored UXML element by name and throws when the asset is out of sync with this controller.
        private T RequireElement<T>(VisualElement root, string elementName) where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element == null)
            {
                throw new MissingReferenceException($"Trivia.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
