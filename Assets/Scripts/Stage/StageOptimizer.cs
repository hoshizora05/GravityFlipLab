using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class StageOptimizer : MonoBehaviour
    {
        [Header("Optimization Settings")]
        public bool enableObjectPooling = true;
        public bool enableCulling = true;
        public float cullingDistance = 30f;
        public int maxActiveEffects = 10;

        private Camera mainCamera;
        private List<ParticleSystem> activeEffects = new List<ParticleSystem>();
        private Queue<ParticleSystem> effectPool = new Queue<ParticleSystem>();

        private void Start()
        {
            mainCamera = Camera.main;
            InitializeOptimization();
        }

        private void InitializeOptimization()
        {
            // Setup object pooling for effects
            if (enableObjectPooling)
            {
                CreateEffectPool();
            }
        }

        private void CreateEffectPool()
        {
            // Pre-create particle effects for pooling
            // This would be implemented based on specific effect prefabs
        }

        private void Update()
        {
            if (enableCulling)
            {
                PerformCulling();
            }

            ManageEffectPool();
        }

        private void PerformCulling()
        {
            Vector3 cameraPosition = mainCamera.transform.position;

            // Cull distant obstacles
            BaseObstacle[] obstacles = FindObjectsByType<BaseObstacle>(FindObjectsSortMode.InstanceID);
            foreach (var obstacle in obstacles)
            {
                float distance = Vector3.Distance(obstacle.transform.position, cameraPosition);
                bool shouldBeActive = distance <= cullingDistance;

                if (obstacle.gameObject.activeSelf != shouldBeActive)
                {
                    obstacle.gameObject.SetActive(shouldBeActive);
                }
            }
        }

        private void ManageEffectPool()
        {
            // Limit active particle effects
            while (activeEffects.Count > maxActiveEffects)
            {
                ParticleSystem oldestEffect = activeEffects[0];
                activeEffects.RemoveAt(0);

                if (oldestEffect != null)
                {
                    oldestEffect.Stop();
                    effectPool.Enqueue(oldestEffect);
                }
            }
        }

        public ParticleSystem GetPooledEffect()
        {
            if (effectPool.Count > 0)
            {
                ParticleSystem effect = effectPool.Dequeue();
                activeEffects.Add(effect);
                return effect;
            }
            return null;
        }
    }
}