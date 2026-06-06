using UnityEngine;
using System.Collections.Generic;
using MuseGameJam.Trivia;

[CreateAssetMenu(fileName = "Info", menuName = "Scriptable Objects/Info")]
public class InfoSO : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private string qrValue;          // unique id — what the QR scan returns
        [TextArea(4, 12)]
        [SerializeField] private string description;       // markdown shown in the Info detail menu
        [SerializeField] private List<TriviaQuestion> triviaQuestions = new();
        [SerializeField] private Sprite image;
        [SerializeField] private bool unlocked;

        public string DisplayName => displayName;
        public string QrValue => qrValue;
        public string Description => description;
        public Sprite Image => image;
        public bool Unlocked => unlocked;
        public IReadOnlyList<TriviaQuestion> TriviaQuestions => triviaQuestions;

        // Marks this info as unlocked. Returns true only the first time (locked -> unlocked),
        // so callers can react to a *newly* unlocked info (animation, save, etc.).
        public bool Unlock()
        {
                if (unlocked) return false;
                unlocked = true;
                return true;
        }
}
