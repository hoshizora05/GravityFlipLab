using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GravityFlipLab.UI
{
    public class MainMenuSceneManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button stageSelectButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button backToTitleButton;
        [SerializeField] private Button exitButton;

        [Header("Visual Elements")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TMPro.TextMeshProUGUI playerProgressText;
        [SerializeField] private TMPro.TextMeshProUGUI totalEnergyChipsText;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip menuBGM;

        [Header("Animation Settings")]
        [SerializeField] private float buttonAnimationDelay = 0.1f;
        [SerializeField] private float menuAnimationDuration = 0.8f;

        [Header("Input Settings")]
        [SerializeField] private KeyCode backKey = KeyCode.Escape;

        private bool isTransitioning = false;
        private bool inputEnabled = false;
        private int currentSelectedButton = 0;
        private Button[] menuButtons;

        // Events
        public static event System.Action OnMainMenuLoaded;
        public static event System.Action OnMainMenuExit;

        private void Awake()
        {
            InitializeComponents();
            SetupMenuButtons();
        }

        private void Start()
        {
            StartCoroutine(InitializeMainMenu());
        }

        private void Update()
        {
            if (!inputEnabled || isTransitioning) return;

            HandleKeyboardInput();
            HandleControllerInput();
        }

        private void InitializeComponents()
        {
            // Ensure audio source exists
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Setup audio source
            audioSource.loop = true;
            audioSource.volume = 0.8f;
        }

        private void SetupMenuButtons()
        {
            // Create menu buttons array (excluding null buttons)
            List<Button> buttonList = new List<Button>();

            if (newGameButton != null) buttonList.Add(newGameButton);
            if (continueButton != null) buttonList.Add(continueButton);
            if (stageSelectButton != null) buttonList.Add(stageSelectButton);
            if (optionsButton != null) buttonList.Add(optionsButton);
            if (leaderboardButton != null) buttonList.Add(leaderboardButton);
            if (shopButton != null) buttonList.Add(shopButton);
            if (backToTitleButton != null) buttonList.Add(backToTitleButton);
            if (exitButton != null) buttonList.Add(exitButton);

            menuButtons = buttonList.ToArray();

            // Setup button events
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameButtonClicked);
                AddButtonHoverEffects(newGameButton);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                AddButtonHoverEffects(continueButton);

                // Enable/disable continue button based on save data
                bool hasSaveData = SaveManager.Instance.HasSaveData();
                continueButton.interactable = hasSaveData;
            }

            if (stageSelectButton != null)
            {
                stageSelectButton.onClick.AddListener(OnStageSelectButtonClicked);
                AddButtonHoverEffects(stageSelectButton);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.AddListener(OnOptionsButtonClicked);
                AddButtonHoverEffects(optionsButton);
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
                AddButtonHoverEffects(leaderboardButton);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopButtonClicked);
                AddButtonHoverEffects(shopButton);
            }

            if (backToTitleButton != null)
            {
                backToTitleButton.onClick.AddListener(OnBackToTitleButtonClicked);
                AddButtonHoverEffects(backToTitleButton);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitButtonClicked);
                AddButtonHoverEffects(exitButton);
            }

            // Set initial button selection
            if (menuButtons.Length > 0 && menuButtons[0] != null)
            {
                menuButtons[0].Select();
            }
        }

        private void AddButtonHoverEffects(Button button)
        {
            if (button == null) return;

            var trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }

            // Hover enter
            var hoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((data) => OnButtonHover());
            trigger.triggers.Add(hoverEntry);

            // Click
            var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            clickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => OnButtonClick());
            trigger.triggers.Add(clickEntry);
        }

        private IEnumerator InitializeMainMenu()
        {
            GameManager.Instance.ChangeGameState(GameState.MainMenu);

            // Hide menu initially
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Play menu BGM
            if (menuBGM != null && audioSource != null)
            {
                audioSource.clip = menuBGM;
                audioSource.Play();
            }

            // Wait a moment for fade system to handle the transition
            yield return new WaitForSeconds(0.5f);

            // Show and animate menu
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                yield return StartCoroutine(AnimateMenuEntrance());
            }

            // Update player progress display
            UpdatePlayerProgressDisplay();

            // Enable input
            inputEnabled = true;

            // Fire event
            OnMainMenuLoaded?.Invoke();
        }

        private IEnumerator AnimateMenuEntrance()
        {
            // Animate each button sliding in from the left
            foreach (Button button in menuButtons)
            {
                if (button != null)
                {
                    StartCoroutine(AnimateButtonSlideIn(button));
                    yield return new WaitForSeconds(buttonAnimationDelay);
                }
            }
        }

        private IEnumerator AnimateButtonSlideIn(Button button)
        {
            Vector3 originalPosition = button.transform.localPosition;
            Vector3 startPosition = originalPosition + Vector3.left * 800f;

            button.transform.localPosition = startPosition;

            float elapsedTime = 0f;

            while (elapsedTime < menuAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / menuAnimationDuration;

                Vector3 currentPosition = Vector3.Lerp(startPosition, originalPosition, EaseOutBack(t));
                button.transform.localPosition = currentPosition;

                yield return null;
            }

            button.transform.localPosition = originalPosition;
        }

        private void UpdatePlayerProgressDisplay()
        {
            PlayerProgress progress = GameManager.Instance.playerProgress;

            if (playerProgressText != null)
            {
                if (progress != null)
                {
                    playerProgressText.text = $"World {progress.currentWorld} - Stage {progress.currentStage}";
                }
                else
                {
                    playerProgressText.text = "New Game";
                }
            }

            if (totalEnergyChipsText != null)
            {
                if (progress != null)
                {
                    totalEnergyChipsText.text = $"Total Energy Chips: {progress.totalEnergyChips}";
                }
                else
                {
                    totalEnergyChipsText.text = "Total Energy Chips: 0";
                }
            }
        }

        private void HandleKeyboardInput()
        {
            // Back to title with escape
            if (Input.GetKeyDown(backKey))
            {
                OnBackToTitleButtonClicked();
            }

            // Navigate menu with arrow keys
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                NavigateMenu(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                NavigateMenu(1);
            }

            // Select with enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (currentSelectedButton >= 0 && currentSelectedButton < menuButtons.Length)
                {
                    Button selectedButton = menuButtons[currentSelectedButton];
                    if (selectedButton != null && selectedButton.interactable)
                    {
                        selectedButton.onClick.Invoke();
                    }
                }
            }
        }

        private void HandleControllerInput()
        {
            // Handle controller input
            if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Submit"))
            {
                if (currentSelectedButton >= 0 && currentSelectedButton < menuButtons.Length)
                {
                    Button selectedButton = menuButtons[currentSelectedButton];
                    if (selectedButton != null && selectedButton.interactable)
                    {
                        selectedButton.onClick.Invoke();
                    }
                }
            }

            if (Input.GetButtonDown("Cancel"))
            {
                OnBackToTitleButtonClicked();
            }

            // Navigation
            float vertical = Input.GetAxis("Vertical");
            if (Mathf.Abs(vertical) > 0.5f)
            {
                NavigateMenu(vertical > 0 ? -1 : 1);
            }
        }

        private void NavigateMenu(int direction)
        {
            if (menuButtons.Length == 0) return;

            int newSelection = currentSelectedButton + direction;

            // Wrap around
            if (newSelection < 0)
                newSelection = menuButtons.Length - 1;
            else if (newSelection >= menuButtons.Length)
                newSelection = 0;

            // Skip non-interactable buttons
            int attempts = 0;
            while (attempts < menuButtons.Length)
            {
                if (menuButtons[newSelection] != null && menuButtons[newSelection].interactable)
                {
                    currentSelectedButton = newSelection;
                    menuButtons[currentSelectedButton].Select();
                    OnButtonHover();
                    break;
                }

                newSelection += direction;
                if (newSelection < 0)
                    newSelection = menuButtons.Length - 1;
                else if (newSelection >= menuButtons.Length)
                    newSelection = 0;

                attempts++;
            }
        }

        #region Button Event Handlers

        private void OnNewGameButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            StartCoroutine(StartNewGame());
        }

        private void OnContinueButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            StartCoroutine(ContinueGame());
        }

        private void OnStageSelectButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            TransitionToStageSelect();
        }

        private void OnOptionsButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            TransitionToOptions();
        }

        private void OnLeaderboardButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            TransitionToLeaderboard();
        }

        private void OnShopButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            TransitionToShop();
        }

        private void OnBackToTitleButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            TransitionToTitle();
        }

        private void OnExitButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            ExitGame();
        }

        #endregion

        #region Scene Transitions (Simplified)

        private IEnumerator StartNewGame()
        {
            isTransitioning = true;
            inputEnabled = false;

            // Create new game progress
            GameManager.Instance.playerProgress = SaveManager.Instance.CreateNewProgress();
            GameManager.Instance.SetCurrentStage(1, 1);

            // Use FadeTransitionManager for transition
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Gameplay);

            yield break;
        }

        private IEnumerator ContinueGame()
        {
            isTransitioning = true;
            inputEnabled = false;

            // Load existing progress
            GameManager.Instance.playerProgress = SaveManager.Instance.LoadProgress();
            int world = GameManager.Instance.playerProgress.currentWorld;
            int stage = GameManager.Instance.playerProgress.currentStage;
            GameManager.Instance.SetCurrentStage(world, stage);

            // Use FadeTransitionManager for transition
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Gameplay);

            yield break;
        }

        private void TransitionToStageSelect()
        {
            isTransitioning = true;
            inputEnabled = false;
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.StageSelect);
        }

        private void TransitionToOptions()
        {
            isTransitioning = true;
            inputEnabled = false;
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Options);
        }

        private void TransitionToLeaderboard()
        {
            isTransitioning = true;
            inputEnabled = false;
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Leaderboard);
        }

        private void TransitionToShop()
        {
            isTransitioning = true;
            inputEnabled = false;
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Shop);
        }

        private void TransitionToTitle()
        {
            isTransitioning = true;
            inputEnabled = false;
            OnMainMenuExit?.Invoke();
            FadeTransitionManager.Instance.FadeOutAndLoadScene(SceneType.Title);
        }

        private void ExitGame()
        {
            isTransitioning = true;
            inputEnabled = false;

            FadeTransitionManager.Instance.FadeOut(1f, () => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        #endregion

        #region Audio Methods

        private void OnButtonHover()
        {
            if (buttonHoverSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonHoverSound, 0.5f);
            }
        }

        private void OnButtonClick()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, 0.7f);
            }
        }

        #endregion

        #region Utility Methods

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up button events
            if (newGameButton != null) newGameButton.onClick.RemoveAllListeners();
            if (continueButton != null) continueButton.onClick.RemoveAllListeners();
            if (stageSelectButton != null) stageSelectButton.onClick.RemoveAllListeners();
            if (optionsButton != null) optionsButton.onClick.RemoveAllListeners();
            if (leaderboardButton != null) leaderboardButton.onClick.RemoveAllListeners();
            if (shopButton != null) shopButton.onClick.RemoveAllListeners();
            if (backToTitleButton != null) backToTitleButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}