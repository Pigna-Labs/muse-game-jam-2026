using System;
using MuseGameJam.StateSystem;
using MuseGameJam.XR;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Overlay that shows the QR scanner above the main scene.
    ///
    /// Pushed onto the stack by the CAMERA button of the MainUI: the GameStateMachine
    /// pauses the main state, the OverlayState base hides the main UI, and this state
    /// activates the QrScanner GameObject already present in the scene.
    ///
    /// It closes (PopOverlay) when:
    ///  - the QR is read successfully  -> the URL is passed via OnUrlScanned
    ///  - the user presses "Close"      -> QrScannerController.OnClosed
    ///
    /// NB: the scanner is a scene object (not a prefab): here we activate/deactivate
    /// it, we do not instantiate it. Its internal auto-close callbacks are driven by
    /// this state, which owns the lifecycle.
    /// </summary>
    public class StateCamera : OverlayState
    {
        private readonly GameObject scannerObject;
        private QrScannerController scanner;

        /// <summary>Raised when the QR has been read, with the URL/text it contains.</summary>
        public event Action<string> OnUrlScanned;

        public StateCamera(GameObject scannerObject, GameObject mainUiObject = null)
            : base(mainUiObject)
        {
            this.scannerObject = scannerObject;
        }

        protected override void OnEnter()
        {
            if (scannerObject == null)
            {
                Debug.LogError("[StateCamera] scannerObject is null: assign the QrScanner in the MainUI.");
                GameStateMachine.Instance.PopOverlay();
                return;
            }

            scanner = scannerObject.GetComponent<QrScannerController>();

            // Activate the scanner (OnEnable -> StartScan runs on its own).
            scannerObject.SetActive(true);

            if (scanner != null)
            {
                scanner.OnQrDecoded += HandleQrDecoded;
                scanner.OnClosed    += HandleClosed;
            }
        }

        protected override void OnExit()
        {
            if (scanner != null)
            {
                scanner.OnQrDecoded -= HandleQrDecoded;
                scanner.OnClosed    -= HandleClosed;
                // Stop the camera without re-emitting OnClosed (avoids recursion with the pop).
                scanner.StopScan();
            }

            if (scannerObject != null)
            {
                scannerObject.SetActive(false);
            }
        }

        // QR read: forward the URL and close the overlay (back to the main state).
        private void HandleQrDecoded(string url)
        {
            Debug.Log($"[StateCamera] QR decodificato: '{url}' — invoco OnUrlScanned (subscriber count={OnUrlScanned?.GetInvocationList().Length ?? 0}).");
            OnUrlScanned?.Invoke(url);
            GameStateMachine.Instance.PopOverlay();
        }

        // The user pressed "Close" on the scanner: close the overlay.
        private void HandleClosed()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
