using System.Collections.Generic;
using MuseGameJam.Gameplay;
using MuseGameJam.StateSystem;
using MuseGameJam.Trivia;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Drives a full trivia session for one challenge: one question per Info, asked in
    /// order. A correct answer advances to the next Info; a wrong answer re-asks the
    /// same Info with a different question from its pool (looping until correct). When
    /// the last Info is answered correctly the challenge is completed (reward granted)
    /// and the overlay closes.
    ///
    /// Reuses a single TriviaUIController instance, re-feeding it questions via
    /// SetQuestion — it is not nested per-question on the state stack.
    /// </summary>
    public class StateTriviaSession : GameState
    {
        private readonly GameObject triviaUiPrefab;
        private readonly Transform parent;
        private readonly ChallengeSO challenge;
        private readonly ChallengeManager manager;

        private readonly List<InfoSO> sequence = new();
        private GameObject triviaUiInstance;
        private TriviaUIController triviaUi;
        private int currentInfoIndex;
        private TriviaQuestion currentQuestion;
        private TriviaQuestion lastAsked;

        public StateTriviaSession(
            GameObject triviaUiPrefab,
            Transform parent,
            ChallengeSO challenge,
            ChallengeManager manager)
        {
            this.triviaUiPrefab = triviaUiPrefab;
            this.parent = parent;
            this.challenge = challenge;
            this.manager = manager;
        }

        // Instantiates the trivia UI and presents the first Info's question.
        public override void Enter()
        {
            triviaUiInstance = Object.Instantiate(triviaUiPrefab, parent);
            triviaUi = triviaUiInstance.GetComponent<TriviaUIController>();

            if (triviaUi == null)
            {
                throw new MissingComponentException("Trivia overlay prefab needs a TriviaUIController component.");
            }

            BuildSequence();

            if (sequence.Count == 0)
            {
                // No answerable Info: nothing to ask — complete immediately.
                Debug.LogWarning($"StateTriviaSession: challenge '{challenge?.DisplayName}' has no Infos with questions.");
                Finish();
                return;
            }

            triviaUi.AnswerSelected += HandleAnswerSelected;

            currentInfoIndex = 0;
            PresentForCurrentInfo();
        }

        // Closes the session overlay when the mobile back action reaches this state.
        public override bool HandleBack()
        {
            GameStateMachine.Instance.PopOverlay();
            return true;
        }

        // Removes the trivia UI prefab instance when the overlay closes.
        public override void Exit()
        {
            if (triviaUi != null)
            {
                triviaUi.AnswerSelected -= HandleAnswerSelected;
            }

            Object.Destroy(triviaUiInstance);
        }

        // Collects the Infos that actually have a question pool to draw from.
        private void BuildSequence()
        {
            sequence.Clear();

            if (challenge == null)
            {
                return;
            }

            foreach (InfoSO info in challenge.Infos)
            {
                if (info != null && info.TriviaQuestions.Count > 0)
                {
                    sequence.Add(info);
                }
            }
        }

        // Shows a question for the current Info: a random one that differs from the last
        // one asked for this Info (so a retry serves something new when the pool allows).
        private void PresentForCurrentInfo()
        {
            InfoSO info = sequence[currentInfoIndex];
            currentQuestion = PickQuestion(info.TriviaQuestions, lastAsked);
            lastAsked = currentQuestion;
            triviaUi.SetQuestion(currentQuestion);
        }

        // Picks a random question from the pool, avoiding 'exclude' when an alternative exists.
        private static TriviaQuestion PickQuestion(IReadOnlyList<TriviaQuestion> pool, TriviaQuestion exclude)
        {
            if (pool.Count == 1)
            {
                return pool[0];
            }

            TriviaQuestion picked;
            do
            {
                picked = pool[Random.Range(0, pool.Count)];
            }
            while (picked == exclude);

            return picked;
        }

        // The UI has shown feedback; advance after a beat so the player sees it.
        private void HandleAnswerSelected(int answerIndex)
        {
            bool correct = answerIndex == currentQuestion.correctAnswerIndex;
            triviaUi.ScheduleAfterFeedback(() => Advance(correct));
        }

        private void Advance(bool correct)
        {
            if (!correct)
            {
                // Same Info, different question — keep going until it is answered right.
                PresentForCurrentInfo();
                return;
            }

            currentInfoIndex++;

            if (currentInfoIndex >= sequence.Count)
            {
                Finish();
                return;
            }

            // New Info: clear the per-Info "don't repeat" memory.
            lastAsked = null;
            PresentForCurrentInfo();
        }

        // All Infos answered: grant the reward and close the session.
        private void Finish()
        {
            manager?.CompleteChallenge(challenge);
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
