using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Stage;

namespace GravityFlipLab.Player
{
    // PlayerMovementの改善版 (地形対応)
    public partial class PlayerMovement : MonoBehaviour
    {
        [Header("Tilemap Ground Detection")]
        public bool useTilemapGroundDetection = true;

        private GroundDetector groundDetector;
        private TilemapGroundManager tilemapManager;

        private void InitializeTilemapDetection()
        {
            if (useTilemapGroundDetection)
            {
                groundDetector = GetComponent<GroundDetector>();
                if (groundDetector == null)
                {
                    groundDetector = gameObject.AddComponent<GroundDetector>();
                }

                tilemapManager = FindObjectOfType<TilemapGroundManager>();
            }
        }

        private void CheckGroundedImproved()
        {
            if (useTilemapGroundDetection && groundDetector != null)
            {
                isGrounded = groundDetector.IsGrounded();
            }
            else
            {
                // 従来の地面判定
                Vector2 rayDirection = (playerController.gravityDirection == GravityDirection.Down) ?
                    Vector2.down : Vector2.up;

                groundHit = Physics2D.Raycast(groundCheckPoint.position, rayDirection,
                    groundCheckDistance, groundLayerMask);

                isGrounded = groundHit.collider != null;
            }
        }

        private void ApplyAutoRunImproved()
        {
            Vector2 velocity = rb2d.linearVelocity;
            velocity.x = autoRunSpeed;

            // 傾斜面での速度調整
            if (isGrounded && useTilemapGroundDetection && groundDetector != null)
            {
                if (groundDetector.IsOnSlope())
                {
                    Vector3 slopeDirection = groundDetector.GetSlopeDirection();
                    velocity.x *= slopeSpeedMultiplier;

                    // 傾斜に応じたY軸速度調整
                    if (slopeDirection.y > 0.1f) // 上り坂
                    {
                        velocity.y += slopeDirection.y * autoRunSpeed * 0.3f;
                    }
                }
            }
            else if (isGrounded && groundHit.normal != Vector2.up)
            {
                // 従来の傾斜処理
                float slopeAngle = Vector2.Angle(groundHit.normal, Vector2.up);
                if (slopeAngle > 5f)
                {
                    velocity.x *= slopeSpeedMultiplier;
                }
            }

            rb2d.linearVelocity = velocity;
        }

        // FixedUpdateで改善された判定を使用
        private void FixedUpdateImproved()
        {
            if (!playerController.isAlive) return;

            CheckGroundedImproved();
            ApplyAutoRunImproved();
            ApplyGravity();
            LimitFallSpeed();
        }
    }
}