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

        // Events
        public static event System.Action OnGameSceneLoaded;
        public static event System.Action OnGamePaused;
        public static event System.Action OnGameResumed;

        private void Awake()
        {
            InitializeGameScene();
        }

        private void Start()
        {
            StartCoroutine(LoadGameScene());
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
            // Wait for systems to initialize
            yield return new WaitForSeconds(0.1f);

            // Start gameplay music
            if (gameplayBGM != null && gameAudioSource != null)
            {
                gameAudioSource.clip = gameplayBGM;
                gameAudioSource.Play();
            }

            // Initialize game state
            GameManager.Instance.ChangeGameState(GameState.Gameplay);
            gameStartTime = Time.time;

            // Initialize HUD
            UpdateHUD();

            // Fire event
            OnGameSceneLoaded?.Invoke();
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
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            GameManager.Instance.ResumeGame();

            if (pauseMenu != null)
                pauseMenu.SetActive(false);

            OnGameResumed?.Invoke();
        }

        public void RestartGame()
        {
            StartCoroutine(RestartGameCoroutine());
        }

        private IEnumerator RestartGameCoroutine()
        {
            // Resume if paused
            if (isPaused)
                ResumeGame();

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Restart the stage
            GameManager.Instance.RestartStage();
        }

        public void ReturnToMainMenu()
        {
            StartCoroutine(ReturnToMainMenuCoroutine());
        }

        private IEnumerator ReturnToMainMenuCoroutine()
        {
            // Resume if paused
            if (isPaused)
                ResumeGame();

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Return to main menu
            GameManager.Instance.ReturnToMainMenu();
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
            // Clean up button events
            if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
        }
    }
}