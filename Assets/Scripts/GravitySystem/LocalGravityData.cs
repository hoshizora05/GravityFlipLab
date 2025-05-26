using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public enum LocalGravityType
    {
        Directional,    // Fixed direction gravity
        Radial,         // Gravity towards/away from center
        Orbital,        // Circular gravity around center
        Custom          // Custom gravity function
    }

    [System.Serializable]
    public class LocalGravityData
    {
        public LocalGravityType gravityType = LocalGravityType.Directional;
        public Vector2 direction = Vector2.down;
        public float strength = 9.81f;
        public bool overrideGlobal = true;
        public float transitionDistance = 1f;
        public AnimationCurve strengthCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
}