using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GravityFlipLab
{
    /// <summary>
    /// フェードイン・フェードアウト機能を提供するシンプルなマネージャー
    /// </summary>
    public class FadeTransitionManager : MonoBehaviour
    {
        private static FadeTransitionManager _instance;
        public static FadeTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FadeTransitionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FadeTransitionManager");
                        _instance = go.AddComponent<FadeTransitionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Fade Settings")]
        public float fadeDuration = 1f;
        public Color fadeColor = Color.black;

        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isFading = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFadeCanvas();

                // SceneTransitionManagerのイベントに登録
                SceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
                SceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // イベントの登録解除
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.OnSceneLoadStart -= OnSceneLoadStart;
                SceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;
            }
        }

        private void OnSceneLoadStart(string sceneName)
        {
            // シーン読み込み開始時は既にフェードアウト済みなので何もしない
        }

        private void OnSceneLoadComplete(string sceneName)
        {
            // シーン読み込み完了時にフェードインを開始
            StartCoroutine(DelayedFadeIn());
        }

        private IEnumerator DelayedFadeIn()
        {
            // 少し待ってからフェードイン（シーンの初期化を待つ）
            yield return new WaitForSeconds(0.2f);
            FadeIn();
        }

        private void InitializeFadeCanvas()
        {
            // フェード用のCanvasを作成
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);

            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // 最前面に表示

            // CanvasScalerを追加
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // フェード用のImageを作成
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(fadeCanvas.transform, false);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

            // 画面全体を覆うように設定
            RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // 初期状態では非表示
            fadeCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// フェードアウト（透明→不透明）
        /// </summary>
        public void FadeOut(float duration = -1f, System.Action onComplete = null)
        {
            if (isFading) return;

            float actualDuration = duration > 0 ? duration : fadeDuration;
            StartCoroutine(FadeCoroutine(0f, 1f, actualDuration, onComplete));
        }

        /// <summary>
        /// フェードイン（不透明→透明）
        /// </summary>
        public void FadeIn(float duration = -1f, System.Action onComplete = null)
        {
            if (isFading) return;

            float actualDuration = duration > 0 ? duration : fadeDuration;
            StartCoroutine(FadeCoroutine(1f, 0f, actualDuration, onComplete));
        }

        /// <summary>
        /// フェードアウト後にシーン遷移
        /// </summary>
        public void FadeOutAndLoadScene(SceneType sceneType, float duration = -1f)
        {
            if (isFading) return;

            float actualDuration = duration > 0 ? duration : fadeDuration;
            FadeOut(actualDuration, () => {
                SceneTransitionManager.Instance.LoadScene(sceneType);
            });
        }

        /// <summary>
        /// フェードアウト後にシーン遷移（シーン名指定）
        /// </summary>
        public void FadeOutAndLoadScene(string sceneName, float duration = -1f)
        {
            if (isFading) return;

            float actualDuration = duration > 0 ? duration : fadeDuration;
            FadeOut(actualDuration, () => {
                SceneTransitionManager.Instance.LoadScene(sceneName);
            });
        }

        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
        {
            isFading = true;
            fadeCanvas.gameObject.SetActive(true);

            float elapsedTime = 0f;
            Color currentColor = fadeImage.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Time.timeScaleに影響されない
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

                currentColor.a = alpha;
                fadeImage.color = currentColor;

                yield return null;
            }

            // 最終値を確実に設定
            currentColor.a = endAlpha;
            fadeImage.color = currentColor;

            // フェードイン完了時はCanvasを非表示
            if (endAlpha <= 0f)
            {
                fadeCanvas.gameObject.SetActive(false);
            }

            isFading = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 現在フェード中かどうか
        /// </summary>
        public bool IsFading => isFading;

        /// <summary>
        /// フェード色を変更
        /// </summary>
        public void SetFadeColor(Color color)
        {
            fadeColor = color;
            if (fadeImage != null)
            {
                Color currentColor = fadeImage.color;
                currentColor.r = color.r;
                currentColor.g = color.g;
                currentColor.b = color.b;
                fadeImage.color = currentColor;
            }
        }

        /// <summary>
        /// 即座にフェードアウト状態にする
        /// </summary>
        public void SetFadeOut()
        {
            if (fadeImage != null)
            {
                fadeCanvas.gameObject.SetActive(true);
                Color color = fadeImage.color;
                color.a = 1f;
                fadeImage.color = color;
            }
        }

        /// <summary>
        /// 即座にフェードイン状態にする
        /// </summary>
        public void SetFadeIn()
        {
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
                fadeCanvas.gameObject.SetActive(false);
            }
        }
    }
}