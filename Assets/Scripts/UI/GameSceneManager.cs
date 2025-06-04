using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GravityFlipLab.UI
{
    public class GameSceneManager : MonoBehaviour
    {
        [Header("Game UI References")]
        [SerializeField] private GameObject gameUICanvas;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("HUD Elements")]
        [SerializeField] private TMPro.TextMeshProUGUI timerText;
        [SerializeField] private TMPro.TextMeshProUGUI energyChipText;
        [SerializeField] private TMPro.TextMeshProUGUI livesText;

        [Header("Audio")]
        [SerializeField] private AudioSource gameAudioSource;
        [SerializeField] private AudioClip gameplayBGM;

        private bool isPaused = false;
        private float gameStartTime;
        private int collectedEnergyChips = 0;
        private bool sceneInitialized = false;

        // Events
        public static event System.Action OnGameSceneLoaded;
        public static event System.Action OnGamePaused;
        public static event System.Action OnGameResumed;

        private void Awake()
        {
            Debug.Log("GameSceneManager: Awake called");
            InitializeGameScene();
        }

        private void Start()
        {
            Debug.Log("GameSceneManager: Start called");
            if (!sceneInitialized)
            {
                StartCoroutine(LoadGameScene());
            }
        }

        private void OnEnable()
        {
            Debug.Log("GameSceneManager: OnEnable called");

            // シーン再有効化時の処理
            if (!sceneInitialized)
            {
                StartCoroutine(LoadGameScene());
            }
        }

        private void Update()
        {
            if (GameManager.Instance.currentState == GameState.Gameplay)
            {
                UpdateHUD();
                HandlePauseInput();
            }
        }

        private void InitializeGameScene()
        {
            Debug.Log("GameSceneManager: Initializing game scene");

            // Ensure audio source exists
            if (gameAudioSource == null)
            {
                gameAudioSource = gameObject.AddComponent<AudioSource>();
            }

            gameAudioSource.loop = true;
            gameAudioSource.volume = 0.8f;

            // Setup pause menu
            if (pauseMenu != null)
                pauseMenu.SetActive(false);

            // Setup button events
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private IEnumerator LoadGameScene()
        {
            if (sceneInitialized)
            {
                Debug.Log("GameSceneManager: Scene already initialized, skipping");
                yield break;
            }

            Debug.Log("GameSceneManager: Starting game scene load process");

            // Wait for GameManager to be ready
            yield return new WaitUntil(() => GameManager.Instance != null);

            // Wait for StageManager to be ready and initialized
            yield return new WaitUntil(() => Stage.StageManager.Instance != null);

            // Wait additional frame for StageManager initialization
            yield return new WaitForSeconds(0.2f);

            // Start gameplay music
            if (gameplayBGM != null && gameAudioSource != null)
            {
                gameAudioSource.clip = gameplayBGM;
                gameAudioSource.Play();
                Debug.Log("GameSceneManager: Gameplay music started");
            }

            // Initialize game state
            GameManager.Instance.ChangeGameState(GameState.Gameplay);
            gameStartTime = Time.time;

            // Initialize HUD
            UpdateHUD();

            // Register for StageManager events
            Stage.StageManager.OnStageLoaded += OnStageLoaded;
            Stage.StageManager.OnStageCompleted += OnStageCompleted;

            sceneInitialized = true;

            // Fire event
            OnGameSceneLoaded?.Invoke();

            Debug.Log("GameSceneManager: Game scene loaded successfully");
        }

        /// <summary>
        /// StageManager からステージロード完了通知を受け取る
        /// </summary>
        private void OnStageLoaded()
        {
            Debug.Log("GameSceneManager: Stage loaded notification received");

            // ステージロード完了後の処理
            gameStartTime = Time.time;
            UpdateHUD();

            // プレイヤーの状態確認
            ValidatePlayerState();
        }

        /// <summary>
        /// StageManager からステージクリア通知を受け取る
        /// </summary>
        private void OnStageCompleted()
        {
            Debug.Log("GameSceneManager: Stage completed notification received");

            // ステージクリア時の処理
            // 必要に応じてリザルト画面への遷移等
        }

        /// <summary>
        /// プレイヤーの状態を検証
        /// </summary>
        private void ValidatePlayerState()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("GameSceneManager: Player not found after stage load!");

                // StageManagerに再初期化を要求
                if (Stage.StageManager.Instance != null)
                {
                    Debug.Log("GameSceneManager: Requesting StageManager reinitialization");
                    Stage.StageManager.Instance.ForceReinitialize();
                }
            }
            else
            {
                Debug.Log("GameSceneManager: Player validation passed");
            }
        }

        /// <summary>
        /// 外部からの強制再初期化メソッド
        /// </summary>
        public void ForceReinitialize()
        {
            Debug.Log("GameSceneManager: Force reinitialization requested");

            sceneInitialized = false;
            StartCoroutine(LoadGameScene());
        }

        private void UpdateHUD()
        {
            // Update timer
            if (timerText != null)
            {
                float elapsed = Time.time - gameStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // Update energy chips
            if (energyChipText != null)
            {
                energyChipText.text = $"Energy Chips: {GameManager.Instance.sessionEnergyChips}";
            }

            // Update lives (if player controller is available)
            if (livesText != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var playerController = player.GetComponent<GravityFlipLab.Player.PlayerController>();
                    if (playerController != null)
                    {
                        livesText.text = $"Lives: {playerController.stats.livesRemaining}";
                    }
                }
            }
        }

        private void HandlePauseInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        public void PauseGame()
        {
            if (isPaused) return;

            isPaused = true;
            GameManager.Instance.PauseGame();

            if (pauseMenu != null)
                pauseMenu.SetActive(true);

            OnGamePaused?.Invoke();

            Debug.Log("GameSceneManager: Game paused");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            GameManager.Instance.ResumeGame();

            if (pauseMenu != null)
                pauseMenu.SetActive(false);

            OnGameResumed?.Invoke();

            Debug.Log("GameSceneManager: Game resumed");
        }

        public void RestartGame()
        {
            Debug.Log("GameSceneManager: Restart game requested");
            StartCoroutine(RestartGameCoroutine());
        }

        private IEnumerator RestartGameCoroutine()
        {
            // Resume if paused
            if (isPaused)
                ResumeGame();

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Reset scene initialization flag
            sceneInitialized = false;

            // Clear StageManager and force reload
            if (Stage.StageManager.Instance != null)
            {
                Stage.StageManager.Instance.ClearStage();
                Stage.StageManager.Instance.ForceReinitialize();
            }

            // Restart the stage
            GameManager.Instance.RestartStage();

            Debug.Log("GameSceneManager: Game restart completed");
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("GameSceneManager: Return to main menu requested");
            StartCoroutine(ReturnToMainMenuCoroutine());
        }

        private IEnumerator ReturnToMainMenuCoroutine()
        {
            // Resume if paused
            if (isPaused)
                ResumeGame();

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Clean up scene
            CleanupScene();

            // Return to main menu
            GameManager.Instance.ReturnToMainMenu();

            Debug.Log("GameSceneManager: Returning to main menu");
        }

        /// <summary>
        /// シーンのクリーンアップ処理
        /// </summary>
        private void CleanupScene()
        {
            Debug.Log("GameSceneManager: Cleaning up scene");

            // イベントの登録解除
            if (Stage.StageManager.Instance != null)
            {
                Stage.StageManager.OnStageLoaded -= OnStageLoaded;
                Stage.StageManager.OnStageCompleted -= OnStageCompleted;
            }

            // StageManagerのクリア
            if (Stage.StageManager.Instance != null)
            {
                Stage.StageManager.Instance.ClearStage();
            }

            sceneInitialized = false;
        }

        private IEnumerator FadeOutAudio()
        {
            if (gameAudioSource == null || !gameAudioSource.isPlaying) yield break;

            float startVolume = gameAudioSource.volume;
            float elapsedTime = 0f;
            float duration = 1f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                gameAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / duration);
                yield return null;
            }

            gameAudioSource.Stop();
            gameAudioSource.volume = startVolume;
        }

        private void OnDestroy()
        {
            Debug.Log("GameSceneManager: OnDestroy called");

            // Clean up button events
            if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();

            // Clean up StageManager events
            CleanupScene();
        }
    }
}