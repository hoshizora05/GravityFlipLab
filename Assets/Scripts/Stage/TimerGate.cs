using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class TimerGate : BaseObstacle
    {
        [Header("Gate Settings")]
        public float openDuration = 5f;
        public bool startsOpen = false;
        public Transform gateVisual;
        public Collider2D gateCollider;

        private bool isOpen;
        private Coroutine timerCoroutine;

        private void Start()
        {
            isOpen = startsOpen;
            UpdateGateState();
        }

        public void ActivateGate()
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);

            timerCoroutine = StartCoroutine(GateTimer());
        }

        private IEnumerator GateTimer()
        {
            OpenGate();
            yield return new WaitForSeconds(openDuration);
            CloseGate();
        }

        private void OpenGate()
        {
            isOpen = true;
            UpdateGateState();
            PlayActivationEffect();
        }

        private void CloseGate()
        {
            isOpen = false;
            UpdateGateState();
        }

        private void UpdateGateState()
        {
            if (gateCollider != null)
                gateCollider.enabled = !isOpen;

            if (gateVisual != null)
            {
                gateVisual.gameObject.SetActive(!isOpen);
            }
        }
    }
}