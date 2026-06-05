using UnityEngine;
using System.Collections.Generic;
using MuseGameJam.Trivia;

[CreateAssetMenu(fileName = "Info", menuName = "Scriptable Objects/Info")]
public class InfoSO : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private string qrValue;          // unique id — what the QR scan returns
        [SerializeField] private List<TriviaQuestion> triviaQuestions = new();
        [SerializeField] private Sprite image;
        [SerializeField] private bool unlocked;

        public string DisplayName => displayName;
        public string QrValue => qrValue;
        public Sprite Image => image;
        public bool Unlocked => unlocked;
        public IReadOnlyList<TriviaQuestion> TriviaQuestions => triviaQuestions;
}
