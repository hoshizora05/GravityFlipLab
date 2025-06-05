using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 傾斜エフェクトのファクトリークラス
    /// </summary>
    public static class SlopeEffectFactory
    {
        public static SpecialSlopeEffect CreateEffect(SlopeType type, GameObject targetObject)
        {
            switch (type)
            {
                case SlopeType.SpringSlope:
                    return targetObject.AddComponent<SpringSlopeEffect>();
                case SlopeType.IceSlope:
                    return targetObject.AddComponent<IceSlopeEffect>();
                case SlopeType.RoughSlope:
                    return targetObject.AddComponent<RoughSlopeEffect>();
                case SlopeType.GravitySlope:
                    return targetObject.AddComponent<GravitySlopeEffect>();
                case SlopeType.WindSlope:
                    return targetObject.AddComponent<WindSlopeEffect>();
                default:
                    return null;
            }
        }

        public static void ConfigureEffect(SpecialSlopeEffect effect, SlopeData data)
        {
            if (effect == null || data == null) return;

            switch (effect)
            {
                case SpringSlopeEffect spring:
                    spring.bounceForce = data.GetParameter("bounceForce", 15f);
                    spring.accelerationMultiplier = data.GetParameter("accelerationMultiplier", 1.5f);
                    break;

                case IceSlopeEffect ice:
                    ice.friction = data.GetParameter("friction", 0.1f);
                    ice.slideAcceleration = data.GetParameter("slideAcceleration", 1.5f);
                    break;

                case RoughSlopeEffect rough:
                    rough.friction = data.GetParameter("friction", 2.0f);
                    rough.decelerationFactor = data.GetParameter("decelerationFactor", 0.8f);
                    break;

                case GravitySlopeEffect gravity:
                    gravity.gravityMultiplier = data.GetParameter("gravityMultiplier", 2.0f);
                    gravity.additionalGravity = data.GetParameter("additionalGravity", Vector2.zero);
                    break;

                case WindSlopeEffect wind:
                    wind.windDirection = data.GetParameter("windDirection", Vector2.right);
                    wind.windForce = data.GetParameter("windForce", 10f);
                    break;
            }
        }
    }
}