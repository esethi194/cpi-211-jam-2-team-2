// Assets/Scripts/Player Scripts/Camera/PlayerReporter.cs
using System.Collections;
using UnityEngine;

namespace Contrast
{
    public class PlayerReporter : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public CanvasGroup flashOverlay;     // full-screen white overlay (alpha 0 at rest)

        [Header("Capture")]
        public KeyCode captureKey = KeyCode.Mouse0;
        public float maxRayDistance = 40f;
        public LayerMask rayMask = ~0;       // set in Inspector (exclude Player/UI layers)

        [Header("Timing")]
        public float flashHoldSecondsMin = 0.12f;  // minimum white time
        public float flashFadeSeconds = 0.25f;     // fade back duration
        public float resolveBufferSeconds = 0.05f; // pad to ensure resolve finishes under flash
        public float shotCooldown = 1.2f;          // camera recharge

        [Header("SFX (optional)")]
        public AudioSource audioSource;
        public AudioClip shutterSfx;
        public AudioClip successSfx;
        public AudioClip failSfx;

        bool _busy;
        float _nextShotTime;

        void Reset()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (!playerCamera) playerCamera = Camera.main;
        }

        void Update()
        {
            if (_busy) return;
            if (Time.time < _nextShotTime) return;

            if (Input.GetKeyDown(captureKey))
                StartCoroutine(CaptureSequence());
        }

        IEnumerator CaptureSequence()
        {
            _busy = true;

            // ---------- 1) Raycast & resolve (during white) ----------
            bool success = false;
            float resolveTimeHint = 0f;

            if (playerCamera)
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                // Use only the AnomalyTarget layer to avoid desks/walls blocking the shot
                // In the Inspector, assign this to only include the "AnomalyTarget" layer.
                LayerMask targetMask = rayMask;

                // A small radius gives a camera “cone” – tweak 0.15–0.35 to taste
                const float castRadius = 0.25f;

                var hits = Physics.SphereCastAll(
                    ray, castRadius, maxRayDistance, targetMask, QueryTriggerInteraction.Collide);

                // Choose the nearest active anomaly we hit
                IAnomaly chosen = null;
                float best = float.MaxValue;

                foreach (var h in hits)
                {
                    var movableTarget = h.transform.GetComponentInParent<MovableTarget>();
                    var owner = (movableTarget != null) ? movableTarget.currentOwner : null;
                    if (owner != null && !owner.IsResolved && h.distance < best)
                    {
                        chosen = owner;
                        best = h.distance;
                    }
                }

                if (chosen != null)
                {
                    if (chosen is MovedObjectAnomaly move)
                        resolveTimeHint = Mathf.Max(resolveTimeHint, move.moveBackDuration);

                    chosen.ForceResolve();   // resolve while the flash is white
                    success = true;
                }
            }

            // ---------- 2) Flash white immediately ----------
            if (audioSource && shutterSfx) audioSource.PlayOneShot(shutterSfx);
            if (flashOverlay) flashOverlay.alpha = 1f;

            float hold = Mathf.Max(flashHoldSecondsMin, resolveTimeHint + resolveBufferSeconds);
            if (hold > 0f) yield return new WaitForSeconds(hold);

            // Optional success/fail SFX while still white
            if (audioSource)
            {
                if (success && successSfx) audioSource.PlayOneShot(successSfx);
                else if (!success && failSfx) audioSource.PlayOneShot(failSfx);
            }

            // ---------- 3) Fade back to gameplay ----------
            if (flashOverlay && flashFadeSeconds > 0f)
            {
                float t = 0f;
                while (t < flashFadeSeconds)
                {
                    t += Time.deltaTime;
                    flashOverlay.alpha = Mathf.Lerp(1f, 0f, t / flashFadeSeconds);
                    yield return null;
                }
                flashOverlay.alpha = 0f;
            }
            else if (flashOverlay)
            {
                flashOverlay.alpha = 0f;
            }

            _nextShotTime = Time.time + shotCooldown;
            _busy = false;
        }
    }
}
