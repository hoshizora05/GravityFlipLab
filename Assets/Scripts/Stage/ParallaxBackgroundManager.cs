using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class ParallaxBackgroundManager : MonoBehaviour
    {
        [Header("Parallax Settings")]
        public Transform cameraTransform;
        public ParallaxLayer[] parallaxLayers;

        [Header("Performance")]
        public bool enableParallax = true;
        public float updateInterval = 0.016f; // 60fps update

        private Vector3 lastCameraPosition;
        private float lastUpdateTime;

        [System.Serializable]
        public class ParallaxLayer
        {
            public string layerName;
            public SpriteRenderer spriteRenderer;
            public float parallaxFactor = 0.5f;
            public Vector2 textureSize = Vector2.one;
            public bool enableVerticalParallax = false;
            public bool enableLoop = true;
        }

        private void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            lastCameraPosition = cameraTransform.position;
            InitializeParallaxLayers();
        }

        private void Update()
        {
            if (!enableParallax || Time.time - lastUpdateTime < updateInterval) return;

            UpdateParallax();
            lastUpdateTime = Time.time;
        }

        private void InitializeParallaxLayers()
        {
            foreach (var layer in parallaxLayers)
            {
                if (layer.spriteRenderer != null)
                {
                    // Setup material for UV scrolling
                    Material material = layer.spriteRenderer.material;
                    if (material.mainTexture != null)
                    {
                        layer.textureSize = new Vector2(
                            material.mainTexture.width,
                            material.mainTexture.height
                        );
                    }
                }
            }
        }

        private void UpdateParallax()
        {
            Vector3 cameraMovement = cameraTransform.position - lastCameraPosition;

            foreach (var layer in parallaxLayers)
            {
                if (layer.spriteRenderer == null) continue;

                // Calculate parallax movement
                Vector2 parallaxMovement = new Vector2(
                    cameraMovement.x * layer.parallaxFactor,
                    layer.enableVerticalParallax ? cameraMovement.y * layer.parallaxFactor : 0f
                );

                if (layer.enableLoop)
                {
                    // UV scrolling for seamless loop
                    Material material = layer.spriteRenderer.material;
                    Vector2 currentOffset = material.mainTextureOffset;

                    Vector2 newOffset = currentOffset + new Vector2(
                        parallaxMovement.x / layer.textureSize.x,
                        parallaxMovement.y / layer.textureSize.y
                    );

                    // Wrap UV coordinates
                    newOffset.x = newOffset.x % 1f;
                    newOffset.y = newOffset.y % 1f;

                    material.mainTextureOffset = newOffset;
                }
                else
                {
                    // Direct position movement
                    layer.spriteRenderer.transform.position += new Vector3(
                        parallaxMovement.x, parallaxMovement.y, 0f
                    );
                }
            }

            lastCameraPosition = cameraTransform.position;
        }

        public void SetParallaxFactor(int layerIndex, float factor)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                parallaxLayers[layerIndex].parallaxFactor = factor;
            }
        }

        public void SetParallaxEnabled(bool enabled)
        {
            enableParallax = enabled;
        }
    }
}