using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using ZXing;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace MuseJam.XR
{
    /// <summary>
    /// QR scanner per il museo: apre la camera del telefono, mostra il feed a
    /// schermo intero con un mirino ("Inquadra il QR"); quando legge un QR chiude
    /// la camera e mostra il link letto. UI interamente in UI Toolkit.
    ///
    /// FEED in UI Toolkit: copiamo ogni frame del WebCamTexture in una RenderTexture
    /// (Graphics.Blit) e la mettiamo come background-image del #camera-view.
    /// Rotazione/mirror/fullscreen li gestiamo ruotando e dimensionando il
    /// #camera-view a runtime (vedi ApplyFeedOrientation).
    ///
    /// Camera: WebCamTexture (no AR). Decodifica: ZXing (DLL in Assets/Plugins/).
    ///
    /// SETUP IN EDITOR (lo fa l'umano):
    ///   1. GameObject con UIDocument: Panel Settings + Source Asset = QrScanner.uxml.
    ///   2. Questo script sullo stesso GameObject.
    ///   3. DLL ZXing in Assets/Plugins/.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class QrScannerController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private bool preferRearCamera = true;
        [Tooltip("640x480 basta per leggere un QR ed è molto più leggero di 1280x720.")]
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFps = 30;
        [Tooltip("Ogni quanti secondi provare a decodificare (non ogni frame).")]
        [SerializeField] private float decodeInterval = 0.4f;

        /// <summary>Invocato quando un QR viene letto (di norma un link).</summary>
        public event Action<string> OnQrDecoded;

        // --- UI Toolkit ---
        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _cameraView;
        private VisualElement _overlay;

        // --- Camera / decode ---
        private WebCamTexture _webcam;
        private RenderTexture _displayRt;
        private bool _isScanning;
        private float _nextDecodeTime;
        private Color32[] _pixelBuffer;

        private readonly BarcodeReader _reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        public bool IsScanning => _isScanning;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc != null ? _doc.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogWarning("[QrScanner] rootVisualElement non pronto: controlla UIDocument/Source Asset.");
                return;
            }

            _cameraView  = _root.Q<VisualElement>("camera-view");
            _overlay     = _root.Q<VisualElement>("overlay");

            // Il pulsante "Scansiona" non esiste più: lo scanner si apre dall'esterno
            // (MainUI attiva il GameObject) e parte subito. "Chiudi" chiude l'oggetto.
            _root.Q<Button>("close-button")?.RegisterCallback<ClickEvent>(_ => Close());

            SetVisible(_overlay, false);

            // Ri-orienta il feed quando il layout cambia (rotazione device, primo layout).
            _cameraView?.RegisterCallback<GeometryChangedEvent>(_ => ApplyFeedOrientation());

            // Attivando il GameObject (lo fa il MainUI) parte subito la scansione.
            StartScan();
        }

        // ---- API pubblica ----------------------------------------------------

        public void StartScan()
        {
            if (_isScanning) return;
            StartCoroutine(StartScanRoutine());
        }

        public void StopScan()
        {
            if (!_isScanning) return;
            _isScanning = false;

            if (_webcam != null) { _webcam.Stop(); _webcam = null; }
            if (_cameraView != null) _cameraView.style.backgroundImage = StyleKeyword.None;
            SetVisible(_overlay, false);
            ReleaseRenderTexture();
        }

        /// <summary>
        /// Chiude lo scanner: ferma la camera e DISATTIVA il GameObject (così il
        /// MainUI torna visibile). Lo chiama il pulsante "Chiudi" e lo scan riuscito.
        /// </summary>
        public void Close()
        {
            StopScan();
            OnClosed?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>Invocato quando lo scanner si chiude (per Chiudi o scan riuscito).</summary>
        public event Action OnClosed;

        // ---- Avvio camera ----------------------------------------------------

        private IEnumerator StartScanRoutine()
        {
            yield return RequestCameraPermission();
            if (!HasCameraPermission())
            {
                Debug.LogWarning("[QrScanner] Permesso camera negato.");
                yield break;
            }

            string deviceName = PickCameraDevice();
            if (string.IsNullOrEmpty(deviceName))
            {
                Debug.LogWarning("[QrScanner] Nessuna camera trovata.");
                yield break;
            }

            _webcam = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
            _webcam.Play();

            float timeout = Time.realtimeSinceStartup + 3f;
            while (_webcam.width <= 16 && Time.realtimeSinceStartup < timeout)
                yield return null;

            CreateRenderTexture(_webcam.width, _webcam.height);
            if (_cameraView != null && _displayRt != null)
                _cameraView.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_displayRt));

            ApplyFeedOrientation();

            _isScanning = true;
            _nextDecodeTime = 0f;
            SetVisible(_overlay, true);
        }

        /// <summary>
        /// Ruota e dimensiona il #camera-view perché il feed copra TUTTO lo schermo
        /// anche con la rotazione mobile.
        ///
        /// Idea: parto dalle dimensioni del parent (lo schermo). Se il feed è ruotato
        /// di 90°/270°, il #camera-view va dimensionato a lati INVERTITI (w = altezza
        /// schermo, h = larghezza schermo) e centrato, così DOPO la rotazione coincide
        /// col viewport. Poi una scala "cover" elimina eventuali bande.
        /// </summary>
        private void ApplyFeedOrientation()
        {
            if (_cameraView == null || _webcam == null || _root == null) return;

            float screenW = _root.resolvedStyle.width;
            float screenH = _root.resolvedStyle.height;
            if (screenW <= 0 || screenH <= 0) return; // layout non ancora pronto

            int angle = _webcam.videoRotationAngle;       // 0/90/180/270 orario
            bool quarter = (angle == 90 || angle == 270);

            // Dimensioni del camera-view: invertite se ruotato di un quarto.
            float vw = quarter ? screenH : screenW;
            float vh = quarter ? screenW : screenH;

            var s = _cameraView.style;
            s.position = Position.Absolute;
            s.width  = vw;
            s.height = vh;
            // Centra l'elemento (left/top al centro, poi translate -50%).
            s.left = (screenW - vw) / 2f;
            s.top  = (screenH - vh) / 2f;

            s.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)));
            s.rotate = new StyleRotate(new Rotate(new Angle(angle, AngleUnit.Degree)));

            // "Cover": scala per riempire lo schermo (elimina bande dovute al rapporto).
            float cover = quarter ? Mathf.Max(screenW / vh, screenH / vw)
                                  : 1f;
            float sy = _webcam.videoVerticallyMirrored ? -cover : cover;
            s.scale = new StyleScale(new Scale(new Vector3(cover, sy, 1f)));
        }

        private string PickCameraDevice()
        {
            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0) return null;
            if (preferRearCamera)
                foreach (var d in devices)
                    if (!d.isFrontFacing) return d.name;
            return devices[0].name;
        }

        // ---- Loop: feed -> RT + decode --------------------------------------

        private void Update()
        {
            if (!_isScanning || _webcam == null || !_webcam.didUpdateThisFrame) return;

            if (_displayRt != null) Graphics.Blit(_webcam, _displayRt);

            if (Time.realtimeSinceStartup < _nextDecodeTime) return;
            _nextDecodeTime = Time.realtimeSinceStartup + decodeInterval;
            TryDecodeFrame();
        }

        private void TryDecodeFrame()
        {
            try
            {
                int w = _webcam.width, h = _webcam.height;
                if (_pixelBuffer == null || _pixelBuffer.Length != w * h)
                    _pixelBuffer = new Color32[w * h];
                _webcam.GetPixels32(_pixelBuffer);

                var result = _reader.Decode(_pixelBuffer, w, h);
                if (result != null && !string.IsNullOrEmpty(result.Text))
                    HandleDecoded(result.Text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QrScanner] Decode error: {e.Message}");
            }
        }

        private void HandleDecoded(string text)
        {
            Debug.Log($"[QrScanner] QR letto: {text}");

            // 1. Passa l'URL letto a chi ascolta (un altro script userà OnQrDecoded).
            OnQrDecoded?.Invoke(text);

            // 2. Chiude lo scanner e torna alla UI normale (come premere "Chiudi").
            Close();
        }

        // ---- RenderTexture ---------------------------------------------------

        private void CreateRenderTexture(int w, int h)
        {
            ReleaseRenderTexture();
            if (w <= 0 || h <= 0) { w = requestedWidth; h = requestedHeight; }
            _displayRt = new RenderTexture(w, h, 0, RenderTextureFormat.Default);
            _displayRt.Create();
        }

        private void ReleaseRenderTexture()
        {
            if (_displayRt == null) return;
            _displayRt.Release();
            Destroy(_displayRt);
            _displayRt = null;
        }

        // ---- Permessi --------------------------------------------------------

        private IEnumerator RequestCameraPermission()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float timeout = Time.realtimeSinceStartup + 30f;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera)
                       && Time.realtimeSinceStartup < timeout)
                    yield return null;
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
        }

        private bool HasCameraPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            return Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
        }

        // ---- Util ------------------------------------------------------------

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (el == null) return;
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDisable() => StopScan();
    }
}
