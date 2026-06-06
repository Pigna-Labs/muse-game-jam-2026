using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Debug
{
    /// <summary>
    /// On-screen log viewer for Android builds: shows the last N log messages
    /// in a scrollable list so you can see Debug.Log output without ADB/logcat.
    ///
    /// SETUP:
    ///   1. Attach this script to any persistent GameObject in the scene.
    ///   2. In the Inspector, set Enabled = true while debugging, false for release.
    ///
    /// Tap the panel to hide/show it temporarily. The panel stays on top of everything
    /// (OnGUI draws after UI Toolkit, so it is always visible).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [Tooltip("Disabilita per il build di release.")]
        [SerializeField] private bool enabled = true;

        [Tooltip("Numero massimo di messaggi mostrati.")]
        [SerializeField] private int maxMessages = 50;

        [Tooltip("Dimensione del font a schermo (adattata a DPI mobili, ~14 va bene).")]
        [SerializeField] private int fontSize = 28;

        [Tooltip("Altezza del pannello come % dello schermo (0..1).")]
        [SerializeField, Range(0.2f, 1f)] private float panelHeight = 0.45f;

        private readonly List<string> _messages = new List<string>();
        private Vector2 _scroll;
        private bool _visible = true;
        private GUIStyle _style;
        private GUIStyle _toggleStyle;

        private void OnEnable()
        {
            if (!enabled) return;
            Application.logMessageReceived += OnLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string logString, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error   => "<color=red>[E]</color> ",
                LogType.Warning => "<color=yellow>[W]</color> ",
                _               => "[I] ",
            };
            _messages.Add(prefix + logString);
            if (_messages.Count > maxMessages)
                _messages.RemoveAt(0);

            // Auto-scroll to bottom on new message.
            _scroll.y = float.MaxValue;
        }

        private void OnGUI()
        {
            if (!enabled) return;

            // Build styles once (can't do it in Awake/Start — no GUI context yet).
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    wordWrap = true,
                    richText = true,
                    normal = { textColor = Color.white },
                };
                _toggleStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = fontSize,
                    normal = { textColor = Color.white },
                };
            }

            float sw = Screen.width;
            float sh = Screen.height;
            float panH = sh * panelHeight;

            // Toggle button in top-right corner.
            Rect toggleRect = new Rect(sw - 160, 0, 160, fontSize + 16);
            if (GUI.Button(toggleRect, _visible ? "LOG ▲" : "LOG ▼", _toggleStyle))
                _visible = !_visible;

            if (!_visible) return;

            // Semi-transparent dark panel.
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, sw, panH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Scrollable text area.
            Rect scrollArea = new Rect(0, 0, sw, panH);
            float lineH = fontSize + 4;
            float contentH = Mathf.Max(_messages.Count * lineH, panH);
            _scroll = GUI.BeginScrollView(scrollArea, _scroll, new Rect(0, 0, sw - 20, contentH));

            for (int i = 0; i < _messages.Count; i++)
                GUI.Label(new Rect(4, i * lineH, sw - 24, lineH), _messages[i], _style);

            GUI.EndScrollView();
        }
    }
}
