using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class StageStreaming : MonoBehaviour
    {
        [Header("Streaming Settings")]
        public float segmentWidth = 1024f;
        public int maxActiveSegments = 5;
        public float cullingDistance = 2048f;

        private Transform playerTransform;
        private Queue<GameObject> segmentPool = new Queue<GameObject>();
        private List<GameObject> activeSegments = new List<GameObject>();
        private Dictionary<float, GameObject> segmentsByPosition = new Dictionary<float, GameObject>();

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        private void Update()
        {
            if (playerTransform != null)
            {
                UpdateSegmentStreaming();
            }
        }

        private void UpdateSegmentStreaming()
        {
            float playerX = playerTransform.position.x;

            // Calculate which segments should be active
            List<float> requiredPositions = new List<float>();
            for (int i = -1; i <= maxActiveSegments - 2; i++)
            {
                float segmentPosition = Mathf.Floor(playerX / segmentWidth) * segmentWidth + (i * segmentWidth);
                requiredPositions.Add(segmentPosition);
            }

            // Activate needed segments
            foreach (float position in requiredPositions)
            {
                if (!segmentsByPosition.ContainsKey(position))
                {
                    LoadSegmentAtPosition(position);
                }
            }

            // Deactivate distant segments
            List<float> positionsToRemove = new List<float>();
            foreach (var kvp in segmentsByPosition)
            {
                float distance = Mathf.Abs(kvp.Key - playerX);
                if (distance > cullingDistance)
                {
                    positionsToRemove.Add(kvp.Key);
                }
            }

            foreach (float position in positionsToRemove)
            {
                UnloadSegmentAtPosition(position);
            }
        }

        private void LoadSegmentAtPosition(float xPosition)
        {
            GameObject segment = GetSegmentFromPool();
            segment.transform.position = new Vector3(xPosition, 0, 0);
            segment.SetActive(true);

            segmentsByPosition[xPosition] = segment;
            activeSegments.Add(segment);
        }

        private void UnloadSegmentAtPosition(float xPosition)
        {
            if (segmentsByPosition.ContainsKey(xPosition))
            {
                GameObject segment = segmentsByPosition[xPosition];
                segmentsByPosition.Remove(xPosition);
                activeSegments.Remove(segment);

                ReturnSegmentToPool(segment);
            }
        }

        private GameObject GetSegmentFromPool()
        {
            if (segmentPool.Count > 0)
            {
                return segmentPool.Dequeue();
            }
            else
            {
                // Create new segment - this should be replaced with actual prefab instantiation
                return new GameObject("StageSegment");
            }
        }

        private void ReturnSegmentToPool(GameObject segment)
        {
            segment.SetActive(false);
            segmentPool.Enqueue(segment);
        }
    }
}