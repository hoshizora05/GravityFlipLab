using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class WindTunnel : LocalGravityZone
    {
        [Header("Wind Settings")]
        public Vector2 windDirection = Vector2.right;
        public float windStrength = 5f;
        public float turbulence = 0f;
        public float gustFrequency = 2f;

        [Header("Area")]
        public Vector2 tunnelSize = new Vector2(10f, 5f);
        //public bool useColliderBounds = true;

        private List<Rigidbody2D> affectedObjects = new List<Rigidbody2D>();
        private float gustTimer = 0f;

        private void Update()
        {
            gustTimer += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            ApplyWindForce();
        }

        private void ApplyWindForce()
        {
            foreach (var rb in affectedObjects)
            {
                if (rb == null) continue;

                Vector2 windForce = CalculateWindForce();
                rb.AddForce(windForce, ForceMode2D.Force);
            }
        }

        private Vector2 CalculateWindForce()
        {
            Vector2 baseWind = windDirection.normalized * windStrength;

            // Add turbulence and gusts
            if (turbulence > 0f)
            {
                float turbulenceX = Mathf.PerlinNoise(Time.time * gustFrequency, 0f) * 2f - 1f;
                float turbulenceY = Mathf.PerlinNoise(0f, Time.time * gustFrequency) * 2f - 1f;
                Vector2 turbulenceForce = new Vector2(turbulenceX, turbulenceY) * turbulence;

                baseWind += turbulenceForce;
            }

            return baseWind;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && !affectedObjects.Contains(rb))
            {
                affectedObjects.Add(rb);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                affectedObjects.Remove(rb);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, tunnelSize);

            // Draw wind direction
            Gizmos.color = Color.blue;
            Vector3 windDir = windDirection.normalized;
            Gizmos.DrawLine(transform.position, transform.position + windDir * 3f);
        }
    }
}