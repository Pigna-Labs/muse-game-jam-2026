using System;
using MuseGameJam.StateSystem;
using MuseGameJam.XR;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Overlay che mostra il QR scanner sopra la scena principale.
    ///
    /// Spinto sullo stack dal pulsante CAMERA del MainUI: mette in pausa la main
    /// (lo fa la GameStateMachine chiamando Pause sullo stato sotto) e attiva il
    /// GameObject del QrScanner già presente in scena.
    ///
    /// Si chiude (PopOverlay) quando:
    ///  - il QR viene letto correttamente  -> l'URL è passato via OnUrlScanned
    ///  - l'utente preme "Chiudi"          -> QrScannerController.OnClosed
    ///
    /// NB: lo scanner è un oggetto in scena (non un prefab): qui lo attiviamo/
    /// disattiviamo, non lo istanziamo. Disattiviamo i suoi callback interni di
    /// auto-chiusura del GameObject perché il ciclo di vita lo governa lo stato.
    /// </summary>
    public class CameraState : GameState
    {
        private readonly GameObject scannerObject;
        private QrScannerController scanner;

        /// <summary>Emesso quando il QR è stato letto, con l'URL/testo contenuto.</summary>
        public event Action<string> OnUrlScanned;

        public CameraState(GameObject scannerObject)
        {
            this.scannerObject = scannerObject;
        }

        public override void Enter()
        {
            if (scannerObject == null)
            {
                Debug.LogError("[CameraState] scannerObject nullo: assegna il QrScanner nel MainUI.");
                GameStateMachine.Instance.PopOverlay();
                return;
            }

            scanner = scannerObject.GetComponent<QrScannerController>();

            // Attiva lo scanner (OnEnable -> StartScan parte da solo).
            scannerObject.SetActive(true);

            if (scanner != null)
            {
                scanner.OnQrDecoded += HandleQrDecoded;
                scanner.OnClosed    += HandleClosed;
            }
        }

        public override void Exit()
        {
            if (scanner != null)
            {
                scanner.OnQrDecoded -= HandleQrDecoded;
                scanner.OnClosed    -= HandleClosed;
                // Ferma la camera senza ri-emettere OnClosed (evita ricorsione col pop).
                scanner.StopScan();
            }

            if (scannerObject != null)
                scannerObject.SetActive(false);
        }

        // Back mobile = chiudi l'overlay camera.
        public override bool HandleBack()
        {
            GameStateMachine.Instance.PopOverlay();
            return true;
        }

        // QR letto: inoltra l'URL e chiudi l'overlay (torna alla main).
        private void HandleQrDecoded(string url)
        {
            OnUrlScanned?.Invoke(url);
            GameStateMachine.Instance.PopOverlay();
        }

        // L'utente ha premuto "Chiudi" sullo scanner: chiudi l'overlay.
        private void HandleClosed()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
