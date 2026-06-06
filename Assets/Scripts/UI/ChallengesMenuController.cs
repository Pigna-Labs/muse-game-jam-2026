using System;
using System.Collections.Generic;
using MuseGameJam.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Controller for the Challenges menu UI (Challenges.uxml).
    ///
    /// The menu is a full-screen bottom sheet: it slides up from the bottom on open
    /// and slides back down on close. Sliding is a USS 'translate' transition; this
    /// controller only toggles the open class and times the close so the overlay is
    /// not destroyed before the slide-down finishes.
    ///
    /// Stays decoupled from the state system: it only exposes CloseRequested once the
    /// close animation is done. The StateChallengesMenu owns the lifecycle (PopOverlay).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChallengesMenuController : MonoBehaviour
    {
        private const string OpenClass = "challenges-panel--open";

        // USS classes for the generated challenge cards (see Challenges.uss).
        private const string CardClass = "challenge-card";
        private const string CardNameClass = "challenge-card__name";
        private const string CardBodyClass = "challenge-card__body";
        private const string ObjectivesClass = "challenge-card__objectives";
        private const string ObjectivesFadedClass = "challenge-card__objectives--faded";
        private const string ObjectiveClass = "challenge-objective";
        private const string ObjectiveCheckClass = "challenge-objective__check";
        private const string ObjectiveCheckDoneClass = "challenge-objective__check--done";
        private const string ObjectiveCheckDebugClass = "challenge-objective__check--debug";
        private const string ObjectiveLabelClass = "challenge-objective__label";
        private const string BeginClass = "challenge-card__begin";

        // Completion animation classes (see Challenges.uss).
        private const string CardCompletedClass = "challenge-card--completed";
        private const string CardFadingClass = "challenge-card--fading";

        // Must match the transition-duration of .challenges-panel in Challenges.uss.
        private const long SlideMilliseconds = 320;

        // How long the card stays green before it fades, and how long the fade lasts.
        // Must match the .challenge-card transition durations in Challenges.uss.
        private const long CardGreenHoldMilliseconds = 450;
        private const long CardFadeMilliseconds = 450;

        private UIDocument document;
        private VisualElement panel;
        private ScrollView list;
        private Button closeButton;
        private bool closing;
        private ChallengeManager manager;

        // The card element drawn for each active challenge, so a completed one can be found and animated out.
        private readonly Dictionary<ChallengeSO, VisualElement> cards = new();

        // Raised when the user requests to close the challenges menu (after the slide-down).
        public event Action CloseRequested;

        // Raised when the player presses "Begin trivia" on a ready challenge card.
        // The owning state launches the trivia session (keeps this controller decoupled).
        public event Action<ChallengeSO> TriviaRequested;

        // Binds to the UXML elements and starts the slide-in when the UIDocument becomes active.
        private void OnEnable()
        {
            closing = false;
            BindElements();

            manager = ChallengeManager.Instance;
            if (manager != null)
            {
                manager.ProgressChanged += PopulateChallenges;
                manager.ChallengeCompleted += HandleChallengeCompleted;
            }

            PopulateChallenges();
            PlayOpenAnimation();
        }

        // Detaches callbacks so re-enabling the object does not double-register them.
        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.clicked -= RequestClose;
                closeButton = null;
            }

            if (manager != null)
            {
                manager.ProgressChanged -= PopulateChallenges;
                manager.ChallengeCompleted -= HandleChallengeCompleted;
                manager = null;
            }
        }

        // A challenge's trivia finished: turn its card green, then fade it out and remove it.
        // If the card is no longer drawn (menu was reopened), there is nothing to animate.
        private void HandleChallengeCompleted(ChallengeSO completed)
        {
            if (cards.TryGetValue(completed, out VisualElement card))
            {
                cards.Remove(completed);
                AnimateCardRemoval(card);
            }
        }

        // Plays the completion animation: add the green class, hold, fade, then detach.
        private void AnimateCardRemoval(VisualElement card)
        {
            card.AddToClassList(CardCompletedClass);
            card.schedule.Execute(() => card.AddToClassList(CardFadingClass)).StartingIn(CardGreenHoldMilliseconds);
            card.schedule.Execute(card.RemoveFromHierarchy).StartingIn(CardGreenHoldMilliseconds + CardFadeMilliseconds);
        }

        // Finds the named elements in Challenges.uxml and wires the close button.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "Challenges UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            panel = RequireElement<VisualElement>(root, "challenges-panel");
            list = RequireElement<ScrollView>(root, "challenges-list");
            closeButton = RequireElement<Button>(root, "challenges-close");

            closeButton.clicked -= RequestClose;
            closeButton.clicked += RequestClose;
        }

        // Fills the list with one card per active (not yet completed) challenge.
        private void PopulateChallenges()
        {
            list.Clear();
            cards.Clear();

            if (manager == null)
            {
                Debug.LogWarning("ChallengesMenuController: no ChallengeManager in scene; the list stays empty.");
                return;
            }

            foreach (ChallengeSO challenge in manager.ActiveChallenges)
            {
                if (challenge != null)
                {
                    VisualElement card = CreateCard(challenge);
                    cards[challenge] = card;
                    list.Add(card);
                }
            }
        }

        // Builds one challenge card: the name, an objective row per Info, and — once every
        // Info is scanned — a "Begin trivia" button overlaid on the faded objectives.
        private VisualElement CreateCard(ChallengeSO challenge)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList(CardClass);

            Label name = new Label(challenge.DisplayName);
            name.AddToClassList(CardNameClass);
            card.Add(name);

            VisualElement body = new VisualElement();
            body.AddToClassList(CardBodyClass);
            card.Add(body);

            VisualElement objectives = new VisualElement();
            objectives.AddToClassList(ObjectivesClass);
            body.Add(objectives);

            foreach (InfoSO info in challenge.Infos)
            {
                if (info != null)
                {
                    objectives.Add(CreateObjective(info, manager.IsScanned(challenge, info)));
                }
            }

            bool ready = manager.IsReadyForTrivia(challenge);
            if (ready)
            {
                objectives.AddToClassList(ObjectivesFadedClass);

                Button begin = new Button(() => TriviaRequested?.Invoke(challenge)) { text = "Begin trivia" };
                begin.AddToClassList(BeginClass);
                body.Add(begin);
            }

            return card;
        }

        // One objective row: a check box (tinted when scanned) and the Info's name.
        // When debug tap-to-complete is on, an un-scanned check box can be tapped to mark
        // its Info scanned without the camera.
        private VisualElement CreateObjective(InfoSO info, bool done)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(ObjectiveClass);

            VisualElement check = new VisualElement();
            check.AddToClassList(ObjectiveCheckClass);
            if (done)
            {
                check.AddToClassList(ObjectiveCheckDoneClass);
            }
            else if (manager != null && manager.DebugTapToComplete)
            {
                check.AddToClassList(ObjectiveCheckDebugClass);
                check.RegisterCallback<PointerDownEvent>(_ => manager.DebugMarkScanned(info));
            }
            row.Add(check);

            Label caption = new Label(info.DisplayName);
            caption.AddToClassList(ObjectiveLabelClass);
            row.Add(caption);

            return row;
        }

        // Adds the open class once the panel has its first (closed) layout, so the
        // translate transition animates from off-screen up into place.
        private void PlayOpenAnimation()
        {
            panel.RemoveFromClassList(OpenClass);
            panel.RegisterCallback<GeometryChangedEvent>(OnPanelFirstLayout);
        }

        private void OnPanelFirstLayout(GeometryChangedEvent evt)
        {
            panel.UnregisterCallback<GeometryChangedEvent>(OnPanelFirstLayout);
            panel.AddToClassList(OpenClass);
        }

        // Slides the sheet back down, then asks the owning state to close once it is off-screen.
        // Public so the state can route mobile back through the same animated close.
        public void RequestClose()
        {
            if (closing)
            {
                return;
            }

            closing = true;
            panel.RemoveFromClassList(OpenClass);
            panel.schedule.Execute(() => CloseRequested?.Invoke()).StartingIn(SlideMilliseconds);
        }

        // Returns a required Unity reference or throws, so missing Inspector references are visible in the console.
        private T RequireReference<T>(T reference, string message) where T : class
        {
            if (reference == null)
            {
                throw new MissingReferenceException(message);
            }

            return reference;
        }

        // Finds a required UXML element by name and throws when the asset is out of sync with this controller.
        private T RequireElement<T>(VisualElement root, string elementName) where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element == null)
            {
                throw new MissingReferenceException($"Challenges.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
