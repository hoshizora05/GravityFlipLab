using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        public Animator animator;
        public float runAnimationSpeed = 1.0f;
        public float fallAnimationSpeed = 0.5f;

        [Header("Animation Triggers")]
        public string runTrigger = "Run";
        public string fallTrigger = "Fall";
        public string gravityFlipTrigger = "GravityFlip";
        public string deathTrigger = "Death";

        private PlayerController playerController;
        private SpriteRenderer spriteRenderer;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;

            if (animator == null)
                animator = GetComponent<Animator>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void OnStateChanged(PlayerState newState)
        {
            if (animator == null) return;

            switch (newState)
            {
                case PlayerState.Running:
                    animator.SetTrigger(runTrigger);
                    animator.speed = runAnimationSpeed;
                    break;

                case PlayerState.Falling:
                    animator.SetTrigger(fallTrigger);
                    animator.speed = fallAnimationSpeed;
                    break;

                case PlayerState.GravityFlipping:
                    animator.SetTrigger(gravityFlipTrigger);
                    break;

                case PlayerState.Dead:
                    animator.SetTrigger(deathTrigger);
                    break;
            }
        }

        public void PlayDeathAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(deathTrigger);
            }
        }

        public void UpdateGravityDirection(GravityDirection direction)
        {
            if (spriteRenderer != null)
            {
                // Flip sprite based on gravity direction
                spriteRenderer.flipY = (direction == GravityDirection.Up);
            }
        }
    }
}