using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("Visual Effects")]
        public ParticleSystem gravityFlipEffect;
        public ParticleSystem deathEffect;
        public ParticleSystem trailEffect;

        [Header("Skin Settings")]
        public SpriteRenderer bodyRenderer;
        public SpriteRenderer visorRenderer;
        public Color[] skinColors = new Color[8];
        public int currentSkinIndex = 0;

        [Header("Invincibility")]
        public float flashInterval = 0.1f;

        private PlayerController playerController;
        private Coroutine invincibilityCoroutine;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;

            // Subscribe to events
            PlayerController.OnGravityFlip += OnGravityFlip;

            ApplySkin(currentSkinIndex);
        }

        private void OnDestroy()
        {
            PlayerController.OnGravityFlip -= OnGravityFlip;
        }

        private void OnGravityFlip(GravityDirection direction)
        {
            PlayGravityFlipEffect();
        }

        public void PlayGravityFlipEffect()
        {
            if (gravityFlipEffect != null)
            {
                gravityFlipEffect.Play();
            }
        }

        public void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                deathEffect.Play();
            }
        }

        public void SetInvincibleVisuals(bool invincible)
        {
            if (invincible)
            {
                if (invincibilityCoroutine != null)
                    StopCoroutine(invincibilityCoroutine);
                invincibilityCoroutine = StartCoroutine(InvincibilityFlashCoroutine());
            }
            else
            {
                if (invincibilityCoroutine != null)
                {
                    StopCoroutine(invincibilityCoroutine);
                    invincibilityCoroutine = null;
                }
                ResetVisuals();
            }
        }

        private IEnumerator InvincibilityFlashCoroutine()
        {
            while (playerController.stats.isInvincible)
            {
                SetRenderersAlpha(0.3f);
                yield return new WaitForSeconds(flashInterval);
                SetRenderersAlpha(1.0f);
                yield return new WaitForSeconds(flashInterval);
            }
        }

        private void SetRenderersAlpha(float alpha)
        {
            if (bodyRenderer != null)
            {
                Color color = bodyRenderer.color;
                color.a = alpha;
                bodyRenderer.color = color;
            }

            if (visorRenderer != null)
            {
                Color color = visorRenderer.color;
                color.a = alpha;
                visorRenderer.color = color;
            }
        }

        public void ResetVisuals()
        {
            SetRenderersAlpha(1.0f);
        }

        public void ApplySkin(int skinIndex)
        {
            if (skinIndex >= 0 && skinIndex < skinColors.Length)
            {
                currentSkinIndex = skinIndex;

                if (bodyRenderer != null)
                {
                    bodyRenderer.color = skinColors[skinIndex];
                }

                // Update visor glow effect based on skin
                UpdateVisorGlow();
            }
        }

        private void UpdateVisorGlow()
        {
            if (visorRenderer != null)
            {
                // Create a brighter version of the skin color for visor
                Color glowColor = skinColors[currentSkinIndex] * 1.5f;
                glowColor.a = 1.0f;
                visorRenderer.color = glowColor;
            }
        }

        public void SetTrailEffect(bool enabled)
        {
            if (trailEffect != null)
            {
                if (enabled)
                    trailEffect.Play();
                else
                    trailEffect.Stop();
            }
        }
    }
}