using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// ゴールトリガー - プレイヤーがゴールに到達したときの処理
    /// </summary>
    public class GoalTrigger : MonoBehaviour
    {
        [Header("Goal Settings")]
        public float rotationSpeed = 90f;
        public float bobHeight = 0.3f;
        public float bobSpeed = 1.5f;
        public float glowIntensity = 2f;

        [Header("Effects")]
        public ParticleSystem goalEffect;
        public AudioClip goalReachedSound;
        public Color goalColor = Color.yellow;

        [Header("Visual Components")]
        public SpriteRenderer goalRenderer;
        public Light goalLight;

        private Vector3 startPosition;
        private bool isActivated = false;
        private float activationTime = 0f;

        private void Start()
        {
            startPosition = transform.position;
            SetupVisualEffects();
        }

        private void Update()
        {
            if (isActivated) return;

            // ゴールのアニメーション
            AnimateGoal();
        }

        private void SetupVisualEffects()
        {
            // SpriteRendererの設定
            if (goalRenderer != null)
            {
                goalRenderer.color = goalColor;
            }

            // ライトの設定
            if (goalLight != null)
            {
                goalLight.color = goalColor;
                goalLight.intensity = glowIntensity;
                goalLight.range = 5f;
            }

            // パーティクルエフェクトの設定
            if (goalEffect != null)
            {
                var main = goalEffect.main;
                main.startColor = goalColor;
                goalEffect.Play();
            }
        }

        private void AnimateGoal()
        {
            // 回転アニメーション
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            // 上下のボブアニメーション
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 newPosition = startPosition;
            newPosition.y += bobOffset;
            transform.position = newPosition;

            // ライトの明滅効果
            if (goalLight != null)
            {
                float pulseIntensity = glowIntensity + Mathf.Sin(Time.time * 3f) * 0.5f;
                goalLight.intensity = pulseIntensity;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;

            if (other.CompareTag("Player"))
            {
                ActivateGoal();
            }
        }

        private void ActivateGoal()
        {
            if (isActivated) return;

            isActivated = true;
            activationTime = Time.time;

            // エフェクトの再生
            PlayGoalEffects();

            // ステージクリア処理
            CompleteStage();
        }

        private void PlayGoalEffects()
        {
            // パーティクルエフェクトの強化
            if (goalEffect != null)
            {
                var emission = goalEffect.emission;
                emission.rateOverTime = 50f; // エフェクトを強化
            }

            // 音響効果
            if (goalReachedSound != null)
            {
                // AudioManager.Instance.PlaySE(goalReachedSound);
                AudioSource.PlayClipAtPoint(goalReachedSound, transform.position);
            }

            // ライトエフェクト
            if (goalLight != null)
            {
                StartCoroutine(GoalLightSequence());
            }

            // スプライトエフェクト
            if (goalRenderer != null)
            {
                StartCoroutine(GoalSpriteSequence());
            }
        }

        private IEnumerator GoalLightSequence()
        {
            float originalIntensity = goalLight.intensity;
            float targetIntensity = originalIntensity * 3f;

            // ライトを明るくする
            float elapsedTime = 0f;
            float duration = 0.5f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                goalLight.intensity = Mathf.Lerp(originalIntensity, targetIntensity, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // しばらく明るいままにする
            yield return new WaitForSeconds(1f);

            // ゆっくりと元に戻す
            elapsedTime = 0f;
            duration = 2f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                goalLight.intensity = Mathf.Lerp(targetIntensity, originalIntensity, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator GoalSpriteSequence()
        {
            Color originalColor = goalRenderer.color;
            Color brightColor = Color.white;

            // スプライトを明るくする
            float elapsedTime = 0f;
            float duration = 0.3f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                goalRenderer.color = Color.Lerp(originalColor, brightColor, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 点滅効果
            for (int i = 0; i < 3; i++)
            {
                goalRenderer.color = originalColor;
                yield return new WaitForSeconds(0.1f);
                goalRenderer.color = brightColor;
                yield return new WaitForSeconds(0.1f);
            }

            // 元の色に戻す
            goalRenderer.color = originalColor;
        }

        private void CompleteStage()
        {
            Debug.Log($"Goal reached at {transform.position}");

            // フェードアウト後にメインメニューへ遷移
            StartCoroutine(CompleteStageSequence());
        }

        private IEnumerator CompleteStageSequence()
        {
            // プレイヤーの動きを停止
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }

            // ステージクリア処理を実行
            var playerStageAdapter = FindObjectOfType<PlayerStageAdapter>();
            if (playerStageAdapter != null)
            {
                playerStageAdapter.ReachGoal();
            }
            else if (Stage.StageManager.Instance != null)
            {
                Stage.StageManager.Instance.CompleteStage();
            }

            // 少し待ってからフェードアウト開始
            yield return new WaitForSeconds(1.5f);

            // フェードアウトしてメインメニューへ遷移
            if (FadeTransitionManager.Instance != null)
            {
                FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.MainMenu, 1f);
            }
            else
            {
                // フェードマネージャーがない場合は直接遷移
                SceneTransitionManager.Instance.LoadScene(SceneType.MainMenu);
            }
        }

        // デバッグ用の強制ゴール処理
        [ContextMenu("Force Goal Activation")]
        public void ForceActivateGoal()
        {
            ActivateGoal();
        }

        // ゴールの位置を設定
        public void SetGoalPosition(Vector3 position)
        {
            transform.position = position;
            startPosition = position;
        }

        // ゴールの色を変更
        public void SetGoalColor(Color color)
        {
            goalColor = color;
            SetupVisualEffects();
        }

        private void OnDrawGizmos()
        {
            // ゴールの範囲を可視化
            Gizmos.color = isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // トリガーエリアを表示
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.color = Color.cyan;
                if (collider is BoxCollider2D boxCollider)
                {
                    Gizmos.DrawWireCube(transform.position, boxCollider.size);
                }
                else if (collider is CircleCollider2D circleCollider)
                {
                    Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 詳細な情報表示
            if (isActivated)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
    }
}