using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRHomeArch.DataCollection
{
    // Drives a full-view white fade overlay used between session phase transitions.
    // Attach to the FadeOverlay Canvas GameObject, which must be a child of Main Camera
    // so it follows head movement and covers both eyes in stereo rendering.
    //
    // Scene setup:
    //   XR Origin (XR Rig)
    //   └── Camera Offset
    //       └── Main Camera
    //           └── FadeOverlay          (Canvas — World Space, no Event Camera needed)
    //               └── FadeImage        (Image — stretched to fill, starting color alpha = 0)
    //
    //   FadeOverlay RectTransform:
    //     Local Position : (0, 0, 0.5)   — just past the near clip plane
    //     Width / Height : 2 / 2         — covers Quest 3 FOV (~110 deg) at 0.5 m distance
    //     Scale          : (1, 1, 1)
    //
    // SessionManager calls FadeIn() and FadeOut() as coroutines:
    //   yield return StartCoroutine(_transitionFader.FadeIn());
    //   TransitionTo(nextPhase);
    //   yield return StartCoroutine(_transitionFader.FadeOut());
    public class TransitionFader : MonoBehaviour
    {
        [Header("References")]
        // Assign the Image child of the FadeOverlay Canvas.
        // If left empty, the script will attempt to find it with GetComponentInChildren.
        [SerializeField] private Image _fadeImage;

        [Header("Durations")]
        // Time in seconds to fade from transparent to peak opacity.
        [SerializeField] private float _fadeInDuration = 0.5f;
        // Time in seconds to fade from peak opacity back to transparent.
        // Slightly longer than fade-in so the new environment is revealed gradually.
        [SerializeField] private float _fadeOutDuration = 0.8f;

        [Header("Color")]
        // Peak color at the midpoint of the transition.
        // Alpha 128 (0.5) is intentional — a semi-transparent white is gentler in VR
        // than a fully opaque blackout while still masking the scene swap.
        [SerializeField] private Color _peakColor = new Color(240f / 255f, 240f / 255f, 240f / 255f, 128f / 255f);

        private void Awake()
        {
            if (_fadeImage == null)
                _fadeImage = GetComponentInChildren<Image>();

            if (_fadeImage == null)
            {
                Debug.LogError("[TransitionFader] No Image component found. " +
                               "Add an Image child to the FadeOverlay Canvas and assign it.");
                return;
            }

            // Ensure the overlay starts fully transparent so it is invisible at runtime start.
            _fadeImage.color = new Color(_peakColor.r, _peakColor.g, _peakColor.b, 0f);
        }

        // Fades the overlay from transparent to peak opacity.
        // Yield this coroutine in SessionManager before calling TransitionTo().
        public IEnumerator FadeIn()
        {
            yield return Fade(fromAlpha: 0f, toAlpha: _peakColor.a, duration: _fadeInDuration);
        }

        // Fades the overlay from peak opacity back to transparent.
        // Yield this coroutine in SessionManager after calling TransitionTo().
        public IEnumerator FadeOut()
        {
            yield return Fade(fromAlpha: _peakColor.a, toAlpha: 0f, duration: _fadeOutDuration);
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            if (_fadeImage == null)
                yield break;

            float elapsed = 0f;
            Color color = _fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                _fadeImage.color = color;
                yield return null;
            }

            // Snap to target to avoid floating point drift
            color.a = toAlpha;
            _fadeImage.color = color;
        }
    }
}