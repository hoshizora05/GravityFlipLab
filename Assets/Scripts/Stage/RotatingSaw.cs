using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class RotatingSaw : BaseObstacle
    {
        [Header("Saw Settings")]
        public float rotationSpeed = 360f;
        public bool moveInPath = false;
        public Transform[] pathPoints;
        public float moveSpeed = 2f;

        private int currentPathIndex = 0;
        private Coroutine moveCoroutine;

        protected override void OnObstacleStart()
        {
            if (moveInPath && pathPoints.Length > 1)
            {
                StartMovement();
            }
        }

        private void Update()
        {
            if (isActive)
            {
                // Always rotate
                transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
        }

        private void StartMovement()
        {
            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        private IEnumerator MoveAlongPath()
        {
            while (isActive && pathPoints.Length > 1)
            {
                Vector3 targetPosition = pathPoints[currentPathIndex].position;

                while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                currentPathIndex = (currentPathIndex + 1) % pathPoints.Length;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            DealDamage(other.gameObject);
        }
    }
}