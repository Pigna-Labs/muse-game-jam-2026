using System;
using System.Collections.Generic;
using MuseGameJam.States;
using MuseGameJam.StateSystem;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
    /// <summary>
    /// Scene singleton that owns the runtime state of the challenges feature.
    ///
    /// Tracks, per challenge, which Infos have been scanned; launches the trivia
    /// session when a challenge is ready; and on completion grants the challenge's
    /// reward by adding it to a runtime unlock registry. All state is session-only
    /// (rebuilt in Awake) — nothing is persisted.
    ///
    /// The unlock registry is the runtime fallback to UnlockableSO.Unlocked: an entry
    /// counts as unlocked if it ships unlocked OR was unlocked this session.
    /// </summary>
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private List<ChallengeSO> challenges = new();

        [Header("Trivia")]
        // Same assets StateMainGame uses to open trivia. Assigned in the Inspector.
        [SerializeField] private GameObject triviaUiPrefab;
        [SerializeField] private Transform overlayRoot;

        [Header("Debug")]
        // When on, the Challenges screen lets you tap an objective's checkbox to mark it
        // scanned without using the camera — for testing the flow without printed QRs.
        [SerializeField] private bool debugTapToComplete;

        public static ChallengeManager Instance { get; private set; }

        // Whether the Challenges screen should make objective checkboxes tappable.
        public bool DebugTapToComplete => debugTapToComplete;

        // Per challenge: the set of its Infos that have been scanned.
        private readonly Dictionary<ChallengeSO, HashSet<InfoSO>> scanned = new();
        // Challenges whose trivia has been completed (hidden from the screen).
        private readonly HashSet<ChallengeSO> completed = new();
        // Entries unlocked this session (fallback to UnlockableSO.Unlocked).
        private readonly HashSet<UnlockableSO> unlockRegistry = new();

        // Raised whenever scan progress changes, so an open Challenges screen can refresh.
        public event Action ProgressChanged;
        // Raised when a challenge's trivia is completed and its reward granted.
        public event Action<ChallengeSO> ChallengeCompleted;

        // Builds the session state singleton before any UI or scan asks for it.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            scanned.Clear();
            foreach (ChallengeSO challenge in challenges)
            {
                if (challenge != null && !scanned.ContainsKey(challenge))
                {
                    scanned[challenge] = new HashSet<InfoSO>();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Challenges still to be completed (drawn on the Challenges screen).
        public IEnumerable<ChallengeSO> ActiveChallenges
        {
            get
            {
                foreach (ChallengeSO challenge in challenges)
                {
                    if (challenge != null && !completed.Contains(challenge))
                    {
                        yield return challenge;
                    }
                }
            }
        }

        // True once the given Info of the given challenge has been scanned.
        public bool IsScanned(ChallengeSO challenge, InfoSO info)
        {
            return challenge != null
                && scanned.TryGetValue(challenge, out HashSet<InfoSO> infos)
                && infos.Contains(info);
        }

        // True once every Info of the challenge has been scanned (trivia can begin).
        public bool IsReadyForTrivia(ChallengeSO challenge)
        {
            if (challenge == null || challenge.Infos.Count == 0)
            {
                return false;
            }

            foreach (InfoSO info in challenge.Infos)
            {
                if (info == null || !IsScanned(challenge, info))
                {
                    return false;
                }
            }

            return true;
        }

        // A QR was decoded: mark the matching Info as scanned in every challenge that
        // contains it. Returns true if at least one challenge progressed.
        public bool RegisterScan(string qrValue)
        {
            if (string.IsNullOrEmpty(qrValue))
            {
                return false;
            }

            bool matched = false;

            foreach (ChallengeSO challenge in challenges)
            {
                if (challenge == null || completed.Contains(challenge))
                {
                    continue;
                }

                HashSet<InfoSO> infos = scanned[challenge];
                foreach (InfoSO info in challenge.Infos)
                {
                    if (info != null && info.QrValue == qrValue && infos.Add(info))
                    {
                        matched = true;
                    }
                }
            }

            if (matched)
            {
                ProgressChanged?.Invoke();
            }

            return matched;
        }

        // Debug: mark an Info as scanned directly (same effect as scanning its QR), in
        // every active challenge that contains it. Used by the tap-to-complete checkbox.
        public void DebugMarkScanned(InfoSO info)
        {
            if (info == null)
            {
                return;
            }

            bool matched = false;

            foreach (ChallengeSO challenge in challenges)
            {
                if (challenge == null || completed.Contains(challenge))
                {
                    continue;
                }

                foreach (InfoSO challengeInfo in challenge.Infos)
                {
                    if (challengeInfo == info && scanned[challenge].Add(info))
                    {
                        matched = true;
                    }
                }
            }

            if (matched)
            {
                ProgressChanged?.Invoke();
            }
        }

        // Opens the trivia session for a ready challenge as an overlay above the main game.
        public void StartTrivia(ChallengeSO challenge)
        {
            if (challenge == null)
            {
                return;
            }

            if (GameStateMachine.Instance == null)
            {
                Debug.LogError("ChallengeManager needs a GameStateMachine before it can start trivia.");
                return;
            }

            if (triviaUiPrefab == null)
            {
                Debug.LogWarning("ChallengeManager: triviaUiPrefab not assigned in Inspector.");
                return;
            }

            GameStateMachine.Instance.PushState(
                new StateTriviaSession(triviaUiPrefab, overlayRoot, challenge, this));
        }

        // The trivia session finished successfully: grant the reward and hide the challenge.
        public void CompleteChallenge(ChallengeSO challenge)
        {
            if (challenge == null || !completed.Add(challenge))
            {
                return;
            }

            if (challenge.Reward != null)
            {
                unlockRegistry.Add(challenge.Reward);
            }

            // Only ChallengeCompleted (not ProgressChanged): the screen plays a removal
            // animation for the finished card instead of rebuilding the list immediately.
            ChallengeCompleted?.Invoke(challenge);
        }

        // An entry counts as unlocked if it ships unlocked or was unlocked this session.
        public bool IsUnlocked(UnlockableSO unlockable)
        {
            return unlockable != null && (unlockable.Unlocked || unlockRegistry.Contains(unlockable));
        }
    }
}
