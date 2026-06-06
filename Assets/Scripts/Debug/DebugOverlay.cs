using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.DevTools
{
    /// <summary>
    /// On-screen log viewer — si crea da solo a runtime, nessun GameObject da aggiungere.
    /// Mostra tutti i Debug.Log/Warning/Error in un pannello scrollabile.
    /// Il pulsante "LOG ▼" in alto a destra lo nasconde/mostra.
    /// Per disabilitare nel build di release: commenta [RuntimeInitializeOnLoadMethod] oppure
    /// usa un #if DEVELOPMENT_BUILD / #if UNITY_EDITOR guard.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private const int MaxMessages = 50;
        private const int FontSize    = 28;
        private const float PanelHeightFraction = 0.45f;

        private readonly List<string> _messages = new List<string>();
        private Vector2 _scroll;
        private bool _visible = true;
        private GUIStyle _labelStyle;
        private GUIStyle _btnStyle;

        // Crea il GameObject e ci attacca questo script automaticamente all'avvio di ogni scena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            var go = new GameObject("[DebugOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugOverlay>();
        }

        private void OnEnable()
        {
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
            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);
            _scroll.y = float.MaxValue;
        }

        private void OnGUI()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = FontSize,
                    wordWrap  = true,
                    richText  = true,
                    normal    = { textColor = Color.white },
                };
                _btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = FontSize,
                    normal   = { textColor = Color.white },
                };
            }

            float sw   = Screen.width;
            float sh   = Screen.height;
            float panH = sh * PanelHeightFraction;

            // Toggle button — top-right so it doesn't cover the gameplay area.
            Rect toggleRect = new Rect(sw - 200, 0, 200, FontSize + 20);
            if (GUI.Button(toggleRect, _visible ? "LOG ▲" : "LOG ▼", _btnStyle))
                _visible = !_visible;

            if (!_visible) return;

            // Dark background.
            GUI.color = new Color(0f, 0f, 0f, 0.80f);
            GUI.DrawTexture(new Rect(0, 0, sw, panH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Larghezza disponibile per il testo (lasciamo spazio alla scrollbar).
            float textWidth = sw - 28;

            // Altezza reale di ogni riga col word-wrap attivo: gli URL lunghi vanno a capo
            // invece di essere tagliati a destra. CalcHeight tiene conto del wrap.
            float contentH = 0f;
            for (int i = 0; i < _messages.Count; i++)
                contentH += _labelStyle.CalcHeight(new GUIContent(_messages[i]), textWidth) + 2f;
            contentH = Mathf.Max(contentH, panH);

            _scroll = GUI.BeginScrollView(
                new Rect(0, 0, sw, panH),
                _scroll,
                new Rect(0, 0, sw - 20, contentH));

            float y = 0f;
            for (int i = 0; i < _messages.Count; i++)
            {
                float h = _labelStyle.CalcHeight(new GUIContent(_messages[i]), textWidth);
                GUI.Label(new Rect(4, y, textWidth, h), _messages[i], _labelStyle);
                y += h + 2f;
            }

            GUI.EndScrollView();
        }
    }
}
