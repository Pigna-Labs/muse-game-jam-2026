using System;
using UnityEngine;

[Serializable]
public class TriviaQuestion
{
        [TextArea] public string question;
        public string[] answers;        // e.g. 4 options
        public int correctIndex;        // index into answers[] that is correct

        public bool IsCorrect(int picked) => picked == correctIndex;
}
