using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    public class CheckpointManager : MonoBehaviour
    {
        private static CheckpointManager _instance;
        public static CheckpointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CheckpointManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CheckpointManager");
                        _instance = go.AddComponent<CheckpointManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Checkpoint Settings")]
        public Vector3 defaultCheckpointPosition = Vector3.zero;

        private Vector3 currentCheckpointPosition;
        private List<Vector3> checkpointHistory = new List<Vector3>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Set initial checkpoint
            currentCheckpointPosition = defaultCheckpointPosition;
            if (currentCheckpointPosition == Vector3.zero)
            {
                // Find player start position
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    currentCheckpointPosition = player.transform.position;
                }
            }
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpointHistory.Add(currentCheckpointPosition);
            currentCheckpointPosition = position;

            Debug.Log($"Checkpoint set at: {position}");
        }

        public Vector3 GetCurrentCheckpointPosition()
        {
            return currentCheckpointPosition;
        }

        public void ResetToDefaultCheckpoint()
        {
            currentCheckpointPosition = defaultCheckpointPosition;
            checkpointHistory.Clear();
        }
    }
}