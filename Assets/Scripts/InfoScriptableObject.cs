using UnityEngine;
using System.Collections.Generic;
using MuseGameJam.Trivia;

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/Unlockable")]
public class InfoScriptableObject : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private string qrValue;          // unique id — what the QR scan returns
        [SerializeField] private List<TriviaQuestion> triviaQuestions = new();
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private bool unlocked;

        public string DisplayName => displayName;
        public string QrValue => qrValue;
        public GameObject ModelPrefab => modelPrefab;
        public bool Unlocked => unlocked;
        public IReadOnlyList<TriviaQuestion> TriviaQuestions => triviaQuestions;
}
