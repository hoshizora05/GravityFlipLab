using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 重力傾斜エフェクト - 重力の影響を強化
    /// </summary>
    public class GravitySlopeEffect : SpecialSlopeEffect
    {
        [Header("Gravity Settings")]
        public float gravityMultiplier = 2.0f;
        public bool affectLocalGravity = true;
        public Vector2 additionalGravity = Vector2.zero;

        [Header("Visual Effects")]
        public ParticleSystem gravityEffect;
        public Color gravityFieldColor = Color.cyan;

        private Dictionary<Rigidbody2D, float> originalGravityScale = new Dictionary<Rigidbody2D, float>();

        protected override void OnEffectStart(Rigidbody2D rb)
        {
            originalGravityScale[rb] = rb.gravityScale;
            rb.gravityScale *= gravityMultiplier;

            Debug.Log($"GravitySlope: Object {rb.name} entered gravity slope");
        }

        protected override void OnEffectEnd(Rigidbody2D rb)
        {
            if (originalGravityScale.ContainsKey(rb))
            {
                rb.gravityScale = originalGravityScale[rb];
                originalGravityScale.Remove(rb);
            }

            Debug.Log($"GravitySlope: Object {rb.name} exited gravity slope");
        }

        protected override void ApplyEffect(Rigidbody2D rb)
        {
            if (additionalGravity != Vector2.zero)
            {
                rb.AddForce(additionalGravity * rb.mass, ForceMode2D.Force);
            }

            // Play gravity field effects
            if (gravityEffect != null && !gravityEffect.isPlaying)
            {
                gravityEffect.transform.position = rb.transform.position;
                gravityEffect.Play();
            }
        }

        private void OnDrawGizmos()
        {
            if (!isActive) return;

            Gizmos.color = gravityFieldColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}