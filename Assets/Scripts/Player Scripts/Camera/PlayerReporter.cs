// Assets/Scripts/Player Scripts/Camera/PlayerReporter.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contrast
{
    public class PlayerReporter : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        [Tooltip("Full-screen white overlay (CanvasGroup). Alpha should start at 0.")]
        public CanvasGroup flashOverlay;

        [Header("Capture")]
        public KeyCode captureKey = KeyCode.Mouse0;
        public float maxRayDistance = 40f;

        [Tooltip("Set this to ONLY include your 'AnomalyTarget' layer for best results.")]
        public LayerMask rayMask = ~0;

        [Tooltip("Use a cone-like SphereCast instead of a single RayCast for forgiving aim.")]
        public bool useSphereCast = true;

        [Tooltip("Radius of the SphereCast cone (when useSphereCast=true).")]
        public float sphereCastRadius = 0.25f;

        [Header("Flash & Timing")]
        [Tooltip("Minimum time (seconds) the screen stays fully white.")]
        public float flashHoldSecondsMin = 0.12f;

        [Tooltip("Extra padding to ensure the resolve finishes before fade.")]
        public float resolveBufferSeconds = 0.05f;

        [Tooltip("Fade duration (seconds) from white back to clear.")]
        public float flashFadeSeconds = 0.25f;

        [Header("Cooldown")]
        [Tooltip("Seconds between photos.")]
        public float shotCooldownSeconds = 15f;

        [Tooltip("If true, only start the cooldown when a shot correctly resolves an anomaly.")]
        public bool cooldownOnlyOnSuccess = false;

        [Header("Optional UI / SFX")]
        [Tooltip("Radial Image (Filled/Radial360) that fills up while recharging.")]
        public Image cooldownFill;
        public AudioSource audioSource;
        public AudioClip shutterSfx;
        public AudioClip successSfx;
        public AudioClip failSfx;
        public AudioClip readySfx;

        // --- runtime ---
        bool _busy;
        float _nextShotTime;
        bool _readyPingPlayed;

        void Reset()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (!playerCamera) playerCamera = Camera.main;
        }

        void Update()
        {
            UpdateCooldownUI();

            if (_busy) return;
            if (Time.time < _nextShotTime) return;

            if (Input.GetKeyDown(captureKey))
                StartCoroutine(CaptureSequence());
        }

        void UpdateCooldownUI()
        {
            float remaining = Mathf.Max(0f, _nextShotTime - Time.time);

            if (cooldownFill)
            {
                float denom = Mathf.Max(0.0001f, shotCooldownSeconds);
                // 0 = just shot (empty), 1 = ready (full)
                cooldownFill.fillAmount = 1f - Mathf.Clamp01(remaining / denom);
            }

            if (remaining <= 0f && !_readyPingPlayed)
            {
                if (audioSource && readySfx) audioSource.PlayOneShot(readySfx);
                _readyPingPlayed = true;
            }
        }

        IEnumerator CaptureSequence()
        {
            _busy = true;
            _readyPingPlayed = false; // reset for this cycle

            bool success = false;
            float resolveTimeHint = 0f;

            // -------- 1) Aim & choose target --------
            IAnomaly chosenAnomaly = null;

            if (playerCamera)
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                if (useSphereCast)
                {
                    var hits = Physics.SphereCastAll(
                        ray, sphereCastRadius, maxRayDistance, rayMask, QueryTriggerInteraction.Collide);

                    float nearest = float.MaxValue;
                    foreach (var hit in hits)
                    {
                        var movableTarget = hit.transform.GetComponentInParent<MovableTarget>();
                        var owner = (movableTarget != null) ? movableTarget.currentOwner : null;
                        if (owner != null && !owner.IsResolved && hit.distance < nearest)
                        {
                            nearest = hit.distance;
                            chosenAnomaly = owner;
                        }
                    }
                }
                else
                {
                    if (Physics.Raycast(ray, out var hit, maxRayDistance, rayMask, QueryTriggerInteraction.Collide))
                    {
                        var movableTarget = hit.transform.GetComponentInParent<MovableTarget>();
                        var owner = (movableTarget != null) ? movableTarget.currentOwner : null;
                        if (owner != null && !owner.IsResolved)
                            chosenAnomaly = owner;
                    }
                }
            }

            // -------- 2) Flash white immediately --------
            if (audioSource && shutterSfx) audioSource.PlayOneShot(shutterSfx);
            if (flashOverlay) flashOverlay.alpha = 1f;

            // Resolve while the flash is up
            if (chosenAnomaly != null)
            {
                if (chosenAnomaly is MovedObjectAnomaly move)
                    resolveTimeHint = Mathf.Max(resolveTimeHint, move.moveBackDuration);

                chosenAnomaly.ForceResolve();
                success = true;
            }

            float hold = Mathf.Max(flashHoldSecondsMin, resolveTimeHint + resolveBufferSeconds);
            if (hold > 0f) yield return new WaitForSeconds(hold);

            // Optional success/fail SFX while still white
            if (audioSource)
            {
                if (success && successSfx) audioSource.PlayOneShot(successSfx);
                else if (!success && failSfx) audioSource.PlayOneShot(failSfx);
            }

            // -------- 3) Fade back to gameplay --------
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

            // -------- 4) Start cooldown --------
            if (!cooldownOnlyOnSuccess || success)
                _nextShotTime = Time.time + shotCooldownSeconds;

            _busy = false;
        }
    }
}
