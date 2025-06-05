using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 特殊傾斜エフェクトの基底クラス
    /// </summary>
    public abstract class SpecialSlopeEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        public bool isActive = true;
        public LayerMask affectedLayers = 1;

        protected SlopeObject slopeObject;
        protected List<Rigidbody2D> affectedObjects = new List<Rigidbody2D>();

        protected virtual void Awake()
        {
            slopeObject = GetComponent<SlopeObject>();
            if (slopeObject == null)
            {
                Debug.LogError($"SpecialSlopeEffect requires SlopeObject component on {gameObject.name}");
                enabled = false;
            }
        }

        protected virtual void Start()
        {
            if (slopeObject != null)
            {
                slopeObject.OnObjectEnterSlope += OnObjectEnteredSlope;
                slopeObject.OnObjectExitSlope += OnObjectExitedSlope;
            }
        }

        protected virtual void OnDestroy()
        {
            if (slopeObject != null)
            {
                slopeObject.OnObjectEnterSlope -= OnObjectEnteredSlope;
                slopeObject.OnObjectExitSlope -= OnObjectExitedSlope;
            }
        }

        protected virtual void OnObjectEnteredSlope(GameObject obj)
        {
            if (!isActive || !IsAffectedLayer(obj.layer)) return;

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null && !affectedObjects.Contains(rb))
            {
                affectedObjects.Add(rb);
                OnEffectStart(rb);
            }
        }

        protected virtual void OnObjectExitedSlope(GameObject obj)
        {
            if (!isActive) return;

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null && affectedObjects.Contains(rb))
            {
                affectedObjects.Remove(rb);
                OnEffectEnd(rb);
            }
        }

        protected bool IsAffectedLayer(int layer)
        {
            return (affectedLayers.value & (1 << layer)) != 0;
        }

        protected abstract void OnEffectStart(Rigidbody2D rb);
        protected abstract void OnEffectEnd(Rigidbody2D rb);

        protected virtual void FixedUpdate()
        {
            if (!isActive) return;

            foreach (var rb in affectedObjects)
            {
                if (rb != null)
                {
                    ApplyEffect(rb);
                }
            }
        }

        protected abstract void ApplyEffect(Rigidbody2D rb);
    }
}