using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 風傾斜エフェクト - 風による横方向の力
    /// </summary>
    public class WindSlopeEffect : SpecialSlopeEffect
    {
        [Header("Wind Settings")]
        public Vector2 windDirection = Vector2.right;
        public float windForce = 10f;
        public float gustIntensity = 0.5f;
        public float gustFrequency = 2f;

        [Header("Visual Effects")]
        public ParticleSystem windEffect;
        public LineRenderer[] windLines;

        private float gustTimer = 0f;

        protected override void OnEffectStart(Rigidbody2D rb)
        {
            Debug.Log($"WindSlope: Object {rb.name} entered wind slope");
        }

        protected override void OnEffectEnd(Rigidbody2D rb)
        {
            Debug.Log($"WindSlope: Object {rb.name} exited wind slope");
        }

        protected override void ApplyEffect(Rigidbody2D rb)
        {
            // Calculate current wind force with gusts
            gustTimer += Time.fixedDeltaTime;
            float gustMultiplier = 1f + Mathf.Sin(gustTimer * gustFrequency) * gustIntensity;

            Vector2 currentWindForce = windDirection.normalized * windForce * gustMultiplier;
            rb.AddForce(currentWindForce, ForceMode2D.Force);

            // Update wind visual effects
            UpdateWindEffects(rb.transform.position);
        }

        private void UpdateWindEffects(Vector3 position)
        {
            if (windEffect != null)
            {
                windEffect.transform.position = position;
                if (!windEffect.isPlaying)
                {
                    windEffect.Play();
                }

                // Update wind particle direction
                var main = windEffect.main;
                main.startSpeed = windForce * 0.1f;
            }

            // Update wind lines
            if (windLines != null)
            {
                foreach (var line in windLines)
                {
                    if (line != null)
                    {
                        Vector3 start = position;
                        Vector3 end = position + (Vector3)windDirection * 3f;
                        line.SetPosition(0, start);
                        line.SetPosition(1, end);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!isActive) return;

            // Draw wind direction
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)windDirection.normalized * 3f;
            Gizmos.DrawLine(start, end);

            // Draw wind area
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawCube(transform.position, Vector3.one * 2f);
        }
    }
}