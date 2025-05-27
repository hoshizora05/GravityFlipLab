using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    // プレイヤー用の地面判定改善コンポーネント
    public class GroundDetector : MonoBehaviour
    {
        [Header("Ground Detection Settings")]
        public LayerMask groundLayerMask = 1;
        public float detectionDistance = 0.6f;
        public int raycastCount = 3;
        public bool useTilemapDetection = true;

        [Header("Debug")]
        public bool showDebugRays = false;

        private TilemapGroundManager tilemapManager;
        private RaycastHit2D[] raycastResults = new RaycastHit2D[10];

        private void Start()
        {
            tilemapManager = FindFirstObjectByType<TilemapGroundManager>();
        }

        public bool IsGrounded()
        {
            if (useTilemapDetection && tilemapManager != null)
            {
                return CheckGroundedWithTilemap();
            }
            else
            {
                return CheckGroundedWithPhysics();
            }
        }

        private bool CheckGroundedWithTilemap()
        {
            Vector3 position = transform.position;
            Vector3 bottomLeft = position + Vector3.left * 0.4f;
            Vector3 bottomRight = position + Vector3.right * 0.4f;
            Vector3 bottomCenter = position;

            bool leftGrounded = tilemapManager.IsGroundAtPosition(bottomLeft + Vector3.down * detectionDistance);
            bool rightGrounded = tilemapManager.IsGroundAtPosition(bottomRight + Vector3.down * detectionDistance);
            bool centerGrounded = tilemapManager.IsGroundAtPosition(bottomCenter + Vector3.down * detectionDistance);

            return leftGrounded || rightGrounded || centerGrounded;
        }

        private bool CheckGroundedWithPhysics()
        {
            Vector3 position = transform.position;
            float raySpacing = 0.8f / (raycastCount - 1);

            for (int i = 0; i < raycastCount; i++)
            {
                Vector3 rayOrigin = position + Vector3.left * 0.4f + Vector3.right * (raySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, detectionDistance, groundLayerMask);

                if (showDebugRays)
                {
                    Debug.DrawRay(rayOrigin, Vector2.down * detectionDistance, hit.collider != null ? Color.green : Color.red);
                }

                if (hit.collider != null)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetGroundDistance()
        {
            Vector3 position = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, detectionDistance * 2f, groundLayerMask);

            return hit.collider != null ? hit.distance : float.MaxValue;
        }

        public Vector3 GetGroundNormal()
        {
            Vector3 position = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, detectionDistance * 2f, groundLayerMask);

            return hit.collider != null ? hit.normal : Vector3.up;
        }

        // 傾斜面での速度調整用
        public Vector3 GetSlopeDirection()
        {
            Vector3 groundNormal = GetGroundNormal();
            return Vector3.Cross(groundNormal, Vector3.forward).normalized;
        }

        public bool IsOnSlope()
        {
            Vector3 groundNormal = GetGroundNormal();
            float angle = Vector3.Angle(groundNormal, Vector3.up);
            return angle > 5f && angle < 60f; // 5度〜60度の傾斜
        }

        private void OnDrawGizmosSelected()
        {
            if (showDebugRays)
            {
                Gizmos.color = Color.yellow;
                Vector3 position = transform.position;
                float raySpacing = 0.8f / (raycastCount - 1);

                for (int i = 0; i < raycastCount; i++)
                {
                    Vector3 rayOrigin = position + Vector3.left * 0.4f + Vector3.right * (raySpacing * i);
                    Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * detectionDistance);
                }
            }
        }
    }
}