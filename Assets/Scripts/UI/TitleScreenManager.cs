using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GravityFlipLab.UI
{
    public class TitleScreenManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;

        [Header("Visual Elements")]
        [SerializeField] private GameObject titleLogo;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip titleBGM;

        [Header("Animation Settings")]
        [SerializeField] private float logoAnimationDuration = 2f;
        [SerializeField] private float buttonAnimationDelay = 0.5f;
        [SerializeField] private float fadeTransitionDuration = 1f;

        [Header("Input Settings")]
        [SerializeField] private KeyCode startGameKey = KeyCode.Space;
        [SerializeField] private KeyCode exitKey = KeyCode.Escape;

        private bool isTransitioning = false;
        private bool inputEnabled = false;
        private int currentSelectedButton = 0;
        private Button[] menuButtons;

        // Events
        public static event System.Action OnTitleScreenLoaded;
        public static event System.Action OnGameStart;

        private void Awake()
        {
            InitializeComponents();
            SetupMenuButtons();
        }

        private void Start()
        {
            StartCoroutine(InitializeTitleScreen());
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
            audioSource.volume = 0.7f;

            // Initialize fade canvas group
            if (fadeCanvasGroup == null)
            {
                GameObject fadeObject = new GameObject("FadeCanvasGroup");
                fadeObject.transform.SetParent(transform);
                fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();

                // Setup fade panel
                Image fadeImage = fadeObject.AddComponent<Image>();
                fadeImage.color = Color.black;
                fadeImage.raycastTarget = false;

                RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
                fadeRect.anchorMin = Vector2.zero;
                fadeRect.anchorMax = Vector2.one;
                fadeRect.offsetMin = Vector2.zero;
                fadeRect.offsetMax = Vector2.zero;
            }
        }

        private void SetupMenuButtons()
        {
            menuButtons = new Button[] { startButton, continueButton, optionsButton, exitButton };

            // Setup button events
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
                AddButtonHoverEffects(startButton);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                AddButtonHoverEffects(continueButton);

                // Enable/disable continue button based on save data
                bool hasSaveData = SaveManager.Instance.HasSaveData();
                continueButton.interactable = hasSaveData;
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.AddListener(OnOptionsButtonClicked);
                AddButtonHoverEffects(optionsButton);
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

        private IEnumerator InitializeTitleScreen()
        {
            GameManager.Instance.ChangeGameState(GameState.Title);

            // Start with black screen
            fadeCanvasGroup.alpha = 1f;

            // Hide menu initially
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Play title BGM
            if (titleBGM != null && audioSource != null)
            {
                audioSource.clip = titleBGM;
                audioSource.Play();
            }

            // Wait a moment
            yield return new WaitForSeconds(0.5f);

            // Fade in from black
            yield return StartCoroutine(FadeIn());

            // Animate logo
            if (titleLogo != null)
            {
                yield return StartCoroutine(AnimateLogo());
            }

            // Show menu
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                yield return StartCoroutine(AnimateMenuButtons());
            }

            // Enable input
            inputEnabled = true;

            // Fire event
            OnTitleScreenLoaded?.Invoke();
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTransitionDuration);
                fadeCanvasGroup.alpha = alpha;
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
        }

        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeTransitionDuration);
                fadeCanvasGroup.alpha = alpha;
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        private IEnumerator AnimateLogo()
        {
            if (titleLogo == null) yield break;

            Vector3 originalScale = titleLogo.transform.localScale;
            titleLogo.transform.localScale = Vector3.zero;

            float elapsedTime = 0f;

            while (elapsedTime < logoAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / logoAnimationDuration;

                // Elastic scale animation
                float scale = Mathf.Lerp(0f, 1f, EaseOutElastic(t));
                titleLogo.transform.localScale = originalScale * scale;

                yield return null;
            }

            titleLogo.transform.localScale = originalScale;
        }

        private IEnumerator AnimateMenuButtons()
        {
            foreach (Button button in menuButtons)
            {
                if (button != null)
                {
                    StartCoroutine(AnimateButtonEntry(button));
                    yield return new WaitForSeconds(buttonAnimationDelay);
                }
            }
        }

        private IEnumerator AnimateButtonEntry(Button button)
        {
            Vector3 originalPosition = button.transform.localPosition;
            Vector3 startPosition = originalPosition + Vector3.left * 500f;

            button.transform.localPosition = startPosition;

            float elapsedTime = 0f;
            float duration = 0.5f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                Vector3 currentPosition = Vector3.Lerp(startPosition, originalPosition, EaseOutCubic(t));
                button.transform.localPosition = currentPosition;

                yield return null;
            }

            button.transform.localPosition = originalPosition;
        }

        private void HandleKeyboardInput()
        {
            // Start game with space
            if (Input.GetKeyDown(startGameKey))
            {
                OnStartButtonClicked();
            }

            // Exit with escape
            if (Input.GetKeyDown(exitKey))
            {
                OnExitButtonClicked();
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
            // Handle controller input (A button, D-pad, etc.)
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
                OnExitButtonClicked();
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

        private void OnStartButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            StartCoroutine(TransitionToMainMenu());
        }

        private void OnContinueButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            StartCoroutine(TransitionToMainMenu());
        }

        private void OnOptionsButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();

            // Transition to options screen
            StartCoroutine(TransitionToOptions());
        }

        private void OnExitButtonClicked()
        {
            if (isTransitioning) return;

            OnButtonClick();
            StartCoroutine(ExitGame());
        }

        #endregion

        #region Scene Transitions

        private IEnumerator TransitionToMainMenu()
        {
            isTransitioning = true;
            inputEnabled = false;

            // Fire game start event
            OnGameStart?.Invoke();

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Fade to black
            yield return StartCoroutine(FadeOut());

            // Load main menu scene
            GameManager.Instance.ChangeGameState(GameState.Loading);
            SceneTransitionManager.Instance.LoadScene(SceneType.MainMenu);
        }

        private IEnumerator TransitionToOptions()
        {
            isTransitioning = true;
            inputEnabled = false;

            // Fade to black
            yield return StartCoroutine(FadeOut());

            // Load options scene
            SceneTransitionManager.Instance.LoadScene(SceneType.Options);
        }

        private IEnumerator ExitGame()
        {
            isTransitioning = true;
            inputEnabled = false;

            // Fade out audio
            yield return StartCoroutine(FadeOutAudio());

            // Fade to black
            yield return StartCoroutine(FadeOut());

            // Exit application
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

        private IEnumerator FadeOutAudio()
        {
            if (audioSource == null || !audioSource.isPlaying) yield break;

            float startVolume = audioSource.volume;
            float elapsedTime = 0f;
            float duration = 1f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / duration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
        }

        #endregion

        #region Utility Methods

        private float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            return t == 0f ? 0f : t == 1f ? 1f :
                Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up button events
            if (startButton != null) startButton.onClick.RemoveAllListeners();
            if (continueButton != null) continueButton.onClick.RemoveAllListeners();
            if (optionsButton != null) optionsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}