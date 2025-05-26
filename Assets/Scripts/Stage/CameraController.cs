using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Transform target; // Player
        public float followSpeed = 2f;
        public float lookAheadDistance = 3f;
        public Vector2 offset = Vector2.zero;

        [Header("Boundaries")]
        public float leftBoundary = 0f;
        public float rightBoundary = 100f;
        public float topBoundary = 10f;
        public float bottomBoundary = -10f;

        [Header("Smooth Follow")]
        public bool enableSmoothFollow = true;
        public float horizontalDamping = 0.1f;
        public float verticalDamping = 0.2f;

        private Vector3 velocity = Vector3.zero;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            FollowTarget();
        }

        private void FollowTarget()
        {
            Vector3 targetPosition = target.position + (Vector3)offset;

            // Add look-ahead based on player movement
            Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
            if (playerRb != null && playerRb.linearVelocity.x > 0.1f)
            {
                targetPosition.x += lookAheadDistance;
            }

            Vector3 desiredPosition = new Vector3(
                targetPosition.x,
                targetPosition.y,
                transform.position.z
            );

            // Apply boundaries
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, leftBoundary, rightBoundary);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, bottomBoundary, topBoundary);

            if (enableSmoothFollow)
            {
                // Smooth follow with different damping for X and Y
                Vector3 smoothPosition = new Vector3(
                    Mathf.SmoothDamp(transform.position.x, desiredPosition.x, ref velocity.x, horizontalDamping),
                    Mathf.SmoothDamp(transform.position.y, desiredPosition.y, ref velocity.y, verticalDamping),
                    transform.position.z
                );
                transform.position = smoothPosition;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            }
        }

        public void SetBoundaries(float left, float right, float top, float bottom)
        {
            leftBoundary = left;
            rightBoundary = right;
            topBoundary = top;
            bottomBoundary = bottom;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}