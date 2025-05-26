using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class PistonCrusher : BaseObstacle
    {
        [Header("Piston Settings")]
        public float crushDistance = 3f;
        public float crushSpeed = 5f;
        public float waitTime = 2f;
        public bool crushOnStart = true;
        public Transform pistonHead;

        private Vector3 startPosition;
        private Vector3 crushPosition;
        private bool isCrushing = false;
        private Coroutine crushCoroutine;

        private void Awake()
        {
            if (pistonHead == null)
                pistonHead = transform;

            startPosition = pistonHead.localPosition;
            crushPosition = startPosition + Vector3.down * crushDistance;
        }

        protected override void OnObstacleStart()
        {
            if (crushOnStart)
            {
                StartCrushCycle();
            }
        }

        private void StartCrushCycle()
        {
            if (crushCoroutine != null)
                StopCoroutine(crushCoroutine);
            crushCoroutine = StartCoroutine(CrushCycle());
        }

        private IEnumerator CrushCycle()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(waitTime);

                // Crush down
                isCrushing = true;
                yield return StartCoroutine(MovePiston(crushPosition));

                // Wait briefly
                yield return new WaitForSeconds(0.5f);

                // Return up
                yield return StartCoroutine(MovePiston(startPosition));
                isCrushing = false;
            }
        }

        private IEnumerator MovePiston(Vector3 targetPosition)
        {
            Vector3 currentPosition = pistonHead.localPosition;
            float distance = Vector3.Distance(currentPosition, targetPosition);
            float duration = distance / crushSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                pistonHead.localPosition = Vector3.Lerp(currentPosition, targetPosition, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            pistonHead.localPosition = targetPosition;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCrushing)
            {
                DealDamage(other.gameObject);
            }
        }
    }

}