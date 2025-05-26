using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class HoverDrone : BaseObstacle
    {
        [Header("Drone Settings")]
        public float patrolSpeed = 2f;
        public float detectionRange = 5f;
        public float beamChargeTime = 1f;
        public float beamDuration = 2f;
        public LineRenderer beamRenderer;
        public Transform[] patrolPoints;

        [Header("AI Behavior")]
        public bool followPlayer = false;
        public float followSpeed = 3f;
        public float attackCooldown = 3f;

        private int currentPatrolIndex = 0;
        private bool isCharging = false;
        private bool isFiring = false;
        private GameObject targetPlayer;
        private Coroutine aiCoroutine;
        private float lastAttackTime;

        private void Start()
        {
            targetPlayer = GameObject.FindGameObjectWithTag("Player");
            if (beamRenderer == null)
                beamRenderer = GetComponent<LineRenderer>();
        }

        protected override void OnObstacleStart()
        {
            if (aiCoroutine != null)
                StopCoroutine(aiCoroutine);
            aiCoroutine = StartCoroutine(AIBehavior());
        }

        private IEnumerator AIBehavior()
        {
            while (isActive)
            {
                if (targetPlayer != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

                    if (distanceToPlayer <= detectionRange && Time.time - lastAttackTime >= attackCooldown)
                    {
                        yield return StartCoroutine(AttackSequence());
                    }
                    else if (followPlayer)
                    {
                        yield return StartCoroutine(FollowPlayer());
                    }
                    else
                    {
                        yield return StartCoroutine(PatrolBehavior());
                    }
                }
                else
                {
                    yield return StartCoroutine(PatrolBehavior());
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator AttackSequence()
        {
            isCharging = true;

            // Charge beam
            yield return new WaitForSeconds(beamChargeTime);

            // Fire beam
            isFiring = true;
            if (beamRenderer != null)
            {
                beamRenderer.enabled = true;
                beamRenderer.SetPosition(0, transform.position);
                beamRenderer.SetPosition(1, targetPlayer.transform.position);
            }

            // Check if player is hit
            RaycastHit2D hit = Physics2D.Raycast(transform.position,
                (targetPlayer.transform.position - transform.position).normalized,
                detectionRange, targetLayers);

            if (hit.collider != null && hit.collider.gameObject == targetPlayer)
            {
                DealDamage(targetPlayer);
            }

            yield return new WaitForSeconds(beamDuration);

            // End attack
            if (beamRenderer != null)
                beamRenderer.enabled = false;

            isCharging = false;
            isFiring = false;
            lastAttackTime = Time.time;
        }

        private IEnumerator FollowPlayer()
        {
            Vector3 targetPosition = targetPlayer.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.deltaTime);
            yield return null;
        }

        private IEnumerator PatrolBehavior()
        {
            if (patrolPoints.Length == 0) yield break;

            Vector3 targetPosition = patrolPoints[currentPatrolIndex].position;

            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, patrolSpeed * Time.deltaTime);
            }
            else
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }

            yield return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (patrolPoints != null)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);

                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                    }
                }
            }
        }
    }
}