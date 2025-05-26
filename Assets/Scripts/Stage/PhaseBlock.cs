using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class PhaseBlock : MonoBehaviour
    {
        [Header("Phase Settings")]
        public float phaseInterval = 2f;
        public bool startsVisible = true;
        public float transitionDuration = 0.5f;

        private bool isVisible;
        private SpriteRenderer spriteRenderer;
        private Collider2D blockCollider;
        private Coroutine phaseCoroutine;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            blockCollider = GetComponent<Collider2D>();
            isVisible = startsVisible;
        }

        private void Start()
        {
            UpdatePhaseState();
            StartPhasing();
        }

        private void StartPhasing()
        {
            if (phaseCoroutine != null)
                StopCoroutine(phaseCoroutine);
            phaseCoroutine = StartCoroutine(PhaseCycle());
        }

        private IEnumerator PhaseCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(phaseInterval);

                isVisible = !isVisible;
                yield return StartCoroutine(TransitionPhase());
            }
        }

        private IEnumerator TransitionPhase()
        {
            float startAlpha = spriteRenderer.color.a;
            float targetAlpha = isVisible ? 1f : 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < transitionDuration)
            {
                float t = elapsedTime / transitionDuration;
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                spriteRenderer.color = color;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Color finalColor = spriteRenderer.color;
            finalColor.a = targetAlpha;
            spriteRenderer.color = finalColor;

            UpdatePhaseState();
        }

        private void UpdatePhaseState()
        {
            if (blockCollider != null)
                blockCollider.enabled = isVisible;
        }
    }
}