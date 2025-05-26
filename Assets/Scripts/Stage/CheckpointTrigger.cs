using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    public class CheckpointTrigger : MonoBehaviour
    {
        public Vector3 checkpointPosition;
        private bool triggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;

            if (other.CompareTag("Player"))
            {
                CheckpointManager.Instance.SetCheckpoint(checkpointPosition);
                triggered = true;

                // Visual/audio feedback
                PlayCheckpointEffect();
            }
        }

        private void PlayCheckpointEffect()
        {
            // Add particle effect or sound here
            Debug.Log($"Checkpoint activated at {checkpointPosition}");
        }
    }
}