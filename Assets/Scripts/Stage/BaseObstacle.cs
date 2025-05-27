using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public abstract class BaseObstacle : MonoBehaviour
    {
        [Header("Base Obstacle Settings")]
        public ObstacleType obstacleType;
        public bool isActive = true;
        public float damageAmount = 1f;
        public LayerMask targetLayers = 1;

        [Header("Visual Effects")]
        public ParticleSystem activationEffect;
        public ParticleSystem damageEffect;

        [Header("Audio")]
        public AudioClip activationSound;
        public AudioClip damageSound;

        protected ObstacleData obstacleData;
        protected bool initialized = false;

        // Events
        public System.Action<BaseObstacle> OnObstacleTriggered;
        public System.Action<BaseObstacle, GameObject> OnTargetDamaged;

        public virtual void Initialize(ObstacleData data)
        {
            obstacleData = data;
            obstacleType = data.type;
            ApplyParameters(data.parameters);
            initialized = true;
        }

        public virtual void StartObstacle()
        {
            if (!initialized) return;
            isActive = true;
            OnObstacleStart();
        }

        public virtual void StopObstacle()
        {
            isActive = false;
            OnObstacleStop();
        }

        protected virtual void ApplyParameters(Dictionary<string, object> parameters)
        {
            // Override in derived classes to handle specific parameters
        }

        protected virtual void OnObstacleStart()
        {
            // Override in derived classes for start behavior
        }

        protected virtual void OnObstacleStop()
        {
            // Override in derived classes for stop behavior
        }

        protected virtual void TriggerObstacle()
        {
            if (!isActive) return;

            OnObstacleTriggered?.Invoke(this);
            PlayActivationEffect();
        }

        protected virtual void DealDamage(GameObject target)
        {
            if (!isActive) return;

            // Check if target is on the correct layer
            if (!IsTargetValid(target)) return;

            // Apply damage to player
            var playerController = target.GetComponent<GravityFlipLab.Player.PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage();
                OnTargetDamaged?.Invoke(this, target);
                PlayDamageEffect();
            }
        }

        public bool IsTargetValid(GameObject target)
        {
            return (targetLayers.value & (1 << target.layer)) != 0;
        }

        protected void PlayActivationEffect()
        {
            if (activationEffect != null)
                activationEffect.Play();

            if (activationSound != null)
            {
                // AudioManager.Instance.PlaySE(activationSound);
            }
        }

        protected void PlayDamageEffect()
        {
            if (damageEffect != null)
                damageEffect.Play();

            if (damageSound != null)
            {
                // AudioManager.Instance.PlaySE(damageSound);
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = isActive ? Color.red : Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}