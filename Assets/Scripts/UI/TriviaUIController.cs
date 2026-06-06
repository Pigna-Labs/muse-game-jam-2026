using System;
using MuseGameJam.Trivia;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TriviaUIController : MonoBehaviour
    {
        private const int AnswerCount = 4;

        // How long the answer feedback (correct/wrong highlight) stays up before a
        // trivia session advances to the next question.
        private const long FeedbackMilliseconds = 900;

        [SerializeField] private TriviaQuestion question;

        private readonly Action[] answerHandlers = new Action[AnswerCount];
        private readonly Button[] answerButtons = new Button[AnswerCount];
        private UIDocument document;
        private Label questionLabel;
        private bool hasAnswered;

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
            question = RequireReference(nextQuestion, "TriviaUIController needs a TriviaQuestion asset assigned.");

            SetQuestionText(question.question);

            for (int i = 0; i < AnswerCount; i++)
            {
                SetAnswer(i, question.GetAnswer(i));
            }

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

            answerButtons[index].text = answer;
        }

        // Runs an action after the answer feedback has been visible for a beat, using the
        // document's scheduler so a trivia session can pace itself without a coroutine.
        public void ScheduleAfterFeedback(Action action)
        {
            document.rootVisualElement.schedule.Execute(action).StartingIn(FeedbackMilliseconds);
        }

        // Enables or disables answer taps while feedback or transitions are playing.
        public void SetInteractable(bool interactable)
        {
            foreach (Button button in answerButtons)
            {
                button.SetEnabled(interactable);
            }
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
            ShowAnswerFeedback(answerIndex, question.correctAnswerIndex);
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
