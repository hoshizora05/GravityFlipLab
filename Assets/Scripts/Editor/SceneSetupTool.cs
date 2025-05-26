#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GravityFlipLab.Editor
{
    public class SceneSetupTool : EditorWindow
    {
        private bool showAdvancedOptions = false;
        private bool createLightingSetup = true;
        private bool addSampleAudio = false;

        [MenuItem("Gravity Flip Lab/Setup Scenes")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupTool>("Scene Setup Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Gravity Flip Lab - Scene Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Advanced options toggle
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                createLightingSetup = EditorGUILayout.Toggle("Create Lighting Setup", createLightingSetup);
                addSampleAudio = EditorGUILayout.Toggle("Add Sample Audio Files", addSampleAudio);
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Create Title Scene", GUILayout.Height(30)))
            {
                CreateTitleScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Main Menu Scene", GUILayout.Height(30)))
            {
                CreateMainMenuScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Stage Select Scene", GUILayout.Height(30)))
            {
                CreateStageSelectScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Game Scene", GUILayout.Height(30)))
            {
                CreateGameScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Options Scene", GUILayout.Height(30)))
            {
                CreateOptionsScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Leaderboard Scene", GUILayout.Height(30)))
            {
                CreateLeaderboardScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Setup All Scenes", GUILayout.Height(30)))
            {
                CreateAllScenes();
                SetupBuildSettings();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Build Settings"))
            {
                SetupBuildSettings();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Validate Existing Scenes"))
            {
                ValidateAllScenes();
            }

            GUILayout.Space(10);

            GUILayout.Label("Project Structure:", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Folder Structure"))
            {
                CreateFolderStructure();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Fix Missing References"))
            {
                FixMissingReferences();
            }
        }

        private static void CreateAllScenes()
        {
            CreateTitleScene();
            CreateMainMenuScene();
            CreateStageSelectScene();
            CreateGameScene();
            CreateOptionsScene();
            CreateLeaderboardScene();
        }

        private static void CreateTitleScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Main Camera for Title Scene
            CreateTitleCamera();

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Title Screen UI
            CreateTitleScreenUI();

            // Create Audio Source
            CreateTitleAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/TitleScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Title Scene created successfully!");
        }

        private static void CreateMainMenuScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Main Camera for Main Menu Scene
            CreateMainMenuCamera();

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Main Menu UI
            CreateMainMenuUI();

            // Create Audio Source
            CreateMainMenuAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/MainMenuScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Main Menu Scene created successfully!");
        }

        private static void CreateStageSelectScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Main Camera
            CreateStandardUICamera();

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Stage Select UI
            CreateStageSelectUI();

            // Create Audio Source
            CreateStandardAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/StageSelectScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Stage Select Scene created successfully!");
        }

        private static void CreateGameScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Game UI
            CreateGameSceneUI();

            // Create Game World
            CreateGameWorld();

            // Create Audio Source
            CreateGameAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/GameScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Game Scene created successfully!");
        }

        private static void CreateOptionsScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Main Camera
            CreateStandardUICamera();

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Options UI
            CreateOptionsUI();

            // Create Audio Source
            CreateStandardAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/OptionsScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Options Scene created successfully!");
        }

        private static void CreateLeaderboardScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Create Main Camera
            CreateStandardUICamera();

            // Create Scene Initializer
            CreateSceneInitializer();

            // Create Leaderboard UI
            CreateLeaderboardUI();

            // Create Audio Source
            CreateStandardAudioSource();

            // Save scene
            string scenePath = "Assets/Scenes/LeaderboardScene.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(scenePath));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("Leaderboard Scene created successfully!");
        }

        private static void CreateSceneInitializer()
        {
            GameObject sceneInitializer = new GameObject("SceneInitializer");
            sceneInitializer.AddComponent<GravityFlipLab.UI.SceneInitializer>();
        }

        #region UI Creation Methods

        private static void CreateTitleScreenUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Event System
            CreateEventSystem();

            // Create Title Screen Manager
            GameObject titleManagerObj = new GameObject("TitleScreenManager");
            titleManagerObj.transform.SetParent(canvasObj.transform);
            var titleManager = titleManagerObj.AddComponent<GravityFlipLab.UI.TitleScreenManager>();

            // Create Background
            GameObject backgroundObj = CreateUIImage("Background", canvasObj.transform);
            SetupFullScreenImage(backgroundObj, new Color(0.1f, 0.1f, 0.2f, 1f));

            // Create Title Logo
            GameObject titleLogoObj = CreateUIImage("TitleLogo", canvasObj.transform);
            SetupCenteredImage(titleLogoObj, new Vector2(600, 200), new Vector3(0, 100, 0));

            // Create Menu Panel
            GameObject menuPanelObj = new GameObject("MenuPanel");
            menuPanelObj.transform.SetParent(canvasObj.transform);
            RectTransform menuRect = menuPanelObj.AddComponent<RectTransform>();
            SetupCenteredRect(menuRect, new Vector2(300, 300), new Vector3(0, -150, 0));

            VerticalLayoutGroup menuLayout = menuPanelObj.AddComponent<VerticalLayoutGroup>();
            menuLayout.spacing = 20;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlHeight = false;
            menuLayout.childControlWidth = false;

            // Create Buttons
            Button startButton = CreateButton("StartButton", menuPanelObj.transform, "START GAME");
            Button continueButton = CreateButton("ContinueButton", menuPanelObj.transform, "CONTINUE");
            Button optionsButton = CreateButton("OptionsButton", menuPanelObj.transform, "OPTIONS");
            Button exitButton = CreateButton("ExitButton", menuPanelObj.transform, "EXIT");

            // Create Fade Canvas Group
            GameObject fadeObj = CreateUIImage("FadeCanvasGroup", canvasObj.transform);
            SetupFullScreenImage(fadeObj, Color.black);
            CanvasGroup fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeObj.GetComponent<Image>().raycastTarget = false;

            // Setup Title Manager references
            SetupTitleManagerReferences(titleManager, startButton, continueButton, optionsButton, exitButton,
                titleLogoObj, menuPanelObj, fadeCanvasGroup);
        }

        private static void CreateMainMenuUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Event System
            CreateEventSystem();

            // Create Main Menu Scene Manager
            GameObject mainMenuManagerObj = new GameObject("MainMenuSceneManager");
            mainMenuManagerObj.transform.SetParent(canvasObj.transform);
            var mainMenuManager = mainMenuManagerObj.AddComponent<GravityFlipLab.UI.MainMenuSceneManager>();

            // Create Background
            GameObject backgroundObj = CreateUIImage("Background", canvasObj.transform);
            SetupFullScreenImage(backgroundObj, new Color(0.05f, 0.05f, 0.15f, 1f));

            // Create Title Section
            GameObject titleSectionObj = new GameObject("TitleSection");
            titleSectionObj.transform.SetParent(canvasObj.transform);
            RectTransform titleSectionRect = titleSectionObj.AddComponent<RectTransform>();
            titleSectionRect.anchorMin = new Vector2(0, 0.7f);
            titleSectionRect.anchorMax = new Vector2(1, 1);
            titleSectionRect.offsetMin = Vector2.zero;
            titleSectionRect.offsetMax = Vector2.zero;

            // Create Game Title
            TextMeshProUGUI gameTitleText = CreateText("GameTitle", titleSectionObj.transform, "GRAVITY FLIP LAB");
            RectTransform titleTextRect = gameTitleText.GetComponent<RectTransform>();
            SetupCenteredRect(titleTextRect, new Vector2(800, 100), Vector3.zero);
            gameTitleText.fontSize = 48;
            gameTitleText.fontStyle = FontStyles.Bold;
            gameTitleText.alignment = TextAlignmentOptions.Center;
            gameTitleText.color = Color.white;

            // Create Menu Panel
            GameObject menuPanelObj = new GameObject("MenuPanel");
            menuPanelObj.transform.SetParent(canvasObj.transform);
            RectTransform menuRect = menuPanelObj.AddComponent<RectTransform>();
            SetupCenteredRect(menuRect, new Vector2(400, 500), new Vector3(0, -50, 0));

            VerticalLayoutGroup menuLayout = menuPanelObj.AddComponent<VerticalLayoutGroup>();
            menuLayout.spacing = 15;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlHeight = false;
            menuLayout.childControlWidth = false;
            menuLayout.padding = new RectOffset(0, 0, 20, 20);

            // Create Buttons
            Button newGameButton = CreateButton("NewGameButton", menuPanelObj.transform, "NEW GAME");
            Button continueButton = CreateButton("ContinueButton", menuPanelObj.transform, "CONTINUE");
            Button stageSelectButton = CreateButton("StageSelectButton", menuPanelObj.transform, "STAGE SELECT");
            Button optionsButton = CreateButton("OptionsButton", menuPanelObj.transform, "OPTIONS");
            Button leaderboardButton = CreateButton("LeaderboardButton", menuPanelObj.transform, "LEADERBOARD");
            Button shopButton = CreateButton("ShopButton", menuPanelObj.transform, "SHOP");
            Button backToTitleButton = CreateButton("BackToTitleButton", menuPanelObj.transform, "BACK TO TITLE");
            Button exitButton = CreateButton("ExitButton", menuPanelObj.transform, "EXIT GAME");

            // Style buttons for main menu
            StyleMainMenuButton(newGameButton, new Color(0.2f, 0.8f, 0.2f, 1f));
            StyleMainMenuButton(continueButton, new Color(0.2f, 0.6f, 0.8f, 1f));
            StyleMainMenuButton(stageSelectButton, new Color(0.8f, 0.6f, 0.2f, 1f));
            StyleMainMenuButton(optionsButton, new Color(0.6f, 0.6f, 0.6f, 1f));
            StyleMainMenuButton(leaderboardButton, new Color(0.8f, 0.4f, 0.8f, 1f));
            StyleMainMenuButton(shopButton, new Color(0.8f, 0.8f, 0.2f, 1f));
            StyleMainMenuButton(backToTitleButton, new Color(0.5f, 0.5f, 0.5f, 1f));
            StyleMainMenuButton(exitButton, new Color(0.8f, 0.2f, 0.2f, 1f));

            // Create Progress Display Panel
            GameObject progressPanelObj = new GameObject("ProgressPanel");
            progressPanelObj.transform.SetParent(canvasObj.transform);
            RectTransform progressRect = progressPanelObj.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0, 0);
            progressRect.anchorMax = new Vector2(1, 0.2f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            // Add background to progress panel
            Image progressBg = progressPanelObj.AddComponent<Image>();
            progressBg.color = new Color(0, 0, 0, 0.3f);

            HorizontalLayoutGroup progressLayout = progressPanelObj.AddComponent<HorizontalLayoutGroup>();
            progressLayout.spacing = 50;
            progressLayout.childAlignment = TextAnchor.MiddleCenter;
            progressLayout.padding = new RectOffset(50, 50, 20, 20);

            // Create Progress Text
            TextMeshProUGUI playerProgressText = CreateText("PlayerProgressText", progressPanelObj.transform, "World 1 - Stage 1");
            playerProgressText.fontSize = 24;
            playerProgressText.alignment = TextAlignmentOptions.Center;

            // Create Energy Chips Text
            TextMeshProUGUI totalEnergyChipsText = CreateText("TotalEnergyChipsText", progressPanelObj.transform, "Total Energy Chips: 0");
            totalEnergyChipsText.fontSize = 24;
            totalEnergyChipsText.alignment = TextAlignmentOptions.Center;

            // Create Fade Canvas Group
            GameObject fadeObj = CreateUIImage("FadeCanvasGroup", canvasObj.transform);
            SetupFullScreenImage(fadeObj, Color.black);
            CanvasGroup fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeObj.GetComponent<Image>().raycastTarget = false;

            // Setup Main Menu Manager references
            SetupMainMenuManagerReferences(mainMenuManager, newGameButton, continueButton, stageSelectButton,
                optionsButton, leaderboardButton, shopButton, backToTitleButton, exitButton,
                menuPanelObj, fadeCanvasGroup, playerProgressText, totalEnergyChipsText);
        }

        private static void CreateStageSelectUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            SetupStandardCanvas(canvasObj);

            // Create Event System
            CreateEventSystem();

            // Create Stage Select Manager placeholder
            GameObject stageSelectManagerObj = new GameObject("StageSelectManager");
            stageSelectManagerObj.transform.SetParent(canvasObj.transform);
            // Note: StageSelectManager component would be added here when available

            // Create Background
            GameObject backgroundObj = CreateUIImage("Background", canvasObj.transform);
            SetupFullScreenImage(backgroundObj, new Color(0.1f, 0.1f, 0.2f, 1f));

            // Create Title
            TextMeshProUGUI titleText = CreateText("Title", canvasObj.transform, "SELECT STAGE");
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;

            // Create Back Button
            Button backButton = CreateButton("BackButton", canvasObj.transform, "BACK");
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0);
            backRect.anchorMax = new Vector2(0, 0);
            backRect.anchoredPosition = new Vector2(100, 50);
            backRect.sizeDelta = new Vector2(150, 50);
        }

        private static void CreateOptionsUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            SetupStandardCanvas(canvasObj);

            // Create Event System
            CreateEventSystem();

            // Create Options Manager placeholder
            GameObject optionsManagerObj = new GameObject("OptionsManager");
            optionsManagerObj.transform.SetParent(canvasObj.transform);
            // Note: OptionsManager component would be added here when available

            // Create Background
            GameObject backgroundObj = CreateUIImage("Background", canvasObj.transform);
            SetupFullScreenImage(backgroundObj, new Color(0.1f, 0.1f, 0.2f, 1f));

            // Create Title
            TextMeshProUGUI titleText = CreateText("Title", canvasObj.transform, "OPTIONS");
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;

            // Create Settings Panel
            GameObject settingsPanelObj = new GameObject("SettingsPanel");
            settingsPanelObj.transform.SetParent(canvasObj.transform);
            RectTransform settingsRect = settingsPanelObj.AddComponent<RectTransform>();
            SetupCenteredRect(settingsRect, new Vector2(600, 400), Vector3.zero);

            VerticalLayoutGroup settingsLayout = settingsPanelObj.AddComponent<VerticalLayoutGroup>();
            settingsLayout.spacing = 20;
            settingsLayout.padding = new RectOffset(20, 20, 20, 20);

            // Create sample settings
            CreateSliderSetting("Master Volume", settingsPanelObj.transform, 0.8f);
            CreateSliderSetting("BGM Volume", settingsPanelObj.transform, 0.8f);
            CreateSliderSetting("SE Volume", settingsPanelObj.transform, 1.0f);

            // Create Back Button
            Button backButton = CreateButton("BackButton", canvasObj.transform, "BACK");
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0);
            backRect.anchorMax = new Vector2(0, 0);
            backRect.anchoredPosition = new Vector2(100, 50);
            backRect.sizeDelta = new Vector2(150, 50);
        }

        private static void CreateLeaderboardUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            SetupStandardCanvas(canvasObj);

            // Create Event System
            CreateEventSystem();

            // Create Leaderboard Manager placeholder
            GameObject leaderboardManagerObj = new GameObject("LeaderboardManager");
            leaderboardManagerObj.transform.SetParent(canvasObj.transform);
            // Note: LeaderboardManager component would be added here when available

            // Create Background
            GameObject backgroundObj = CreateUIImage("Background", canvasObj.transform);
            SetupFullScreenImage(backgroundObj, new Color(0.1f, 0.1f, 0.2f, 1f));

            // Create Title
            TextMeshProUGUI titleText = CreateText("Title", canvasObj.transform, "LEADERBOARD");
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;

            // Create Leaderboard Panel
            GameObject leaderboardPanelObj = new GameObject("LeaderboardPanel");
            leaderboardPanelObj.transform.SetParent(canvasObj.transform);
            RectTransform leaderboardRect = leaderboardPanelObj.AddComponent<RectTransform>();
            SetupCenteredRect(leaderboardRect, new Vector2(800, 500), Vector3.zero);

            // Add scroll view for leaderboard entries
            GameObject scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(leaderboardPanelObj.transform);
            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scrollView = scrollViewObj.AddComponent<ScrollRect>();
            scrollViewObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);

            // Create Back Button
            Button backButton = CreateButton("BackButton", canvasObj.transform, "BACK");
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0);
            backRect.anchorMax = new Vector2(0, 0);
            backRect.anchoredPosition = new Vector2(100, 50);
            backRect.sizeDelta = new Vector2(150, 50);
        }

        private static void CreateGameSceneUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Event System if not exists
            CreateEventSystem();

            // Create Game Scene Manager
            GameObject gameManagerObj = new GameObject("GameSceneManager");
            gameManagerObj.transform.SetParent(canvasObj.transform);
            var gameManager = gameManagerObj.AddComponent<GravityFlipLab.UI.GameSceneManager>();

            // Create HUD
            GameObject hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform);
            RectTransform hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(20, -20);
            hudRect.sizeDelta = new Vector2(300, 200);

            VerticalLayoutGroup hudLayout = hudObj.AddComponent<VerticalLayoutGroup>();
            hudLayout.spacing = 10;
            hudLayout.childAlignment = TextAnchor.UpperLeft;

            // Create HUD texts
            TextMeshProUGUI timerText = CreateText("TimerText", hudObj.transform, "Time: 00:00");
            TextMeshProUGUI energyText = CreateText("EnergyChipText", hudObj.transform, "Energy Chips: 0");
            TextMeshProUGUI livesText = CreateText("LivesText", hudObj.transform, "Lives: 3");

            // Create Pause Button
            Button pauseButton = CreateButton("PauseButton", canvasObj.transform, "PAUSE");
            RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1, 1);
            pauseRect.anchorMax = new Vector2(1, 1);
            pauseRect.anchoredPosition = new Vector2(-50, -50);
            pauseRect.sizeDelta = new Vector2(80, 40);

            // Create Pause Menu
            GameObject pauseMenuObj = CreateUIPanel("PauseMenu", canvasObj.transform);
            SetupFullScreenPanel(pauseMenuObj, new Color(0, 0, 0, 0.8f));
            pauseMenuObj.SetActive(false);

            // Create pause menu content
            GameObject pauseContentObj = new GameObject("PauseContent");
            pauseContentObj.transform.SetParent(pauseMenuObj.transform);
            RectTransform pauseContentRect = pauseContentObj.AddComponent<RectTransform>();
            SetupCenteredRect(pauseContentRect, new Vector2(300, 400), Vector3.zero);

            VerticalLayoutGroup pauseLayout = pauseContentObj.AddComponent<VerticalLayoutGroup>();
            pauseLayout.spacing = 20;
            pauseLayout.childAlignment = TextAnchor.MiddleCenter;

            // Pause menu title
            CreateText("PauseTitle", pauseContentObj.transform, "PAUSED");

            // Pause menu buttons
            Button resumeButton = CreateButton("ResumeButton", pauseContentObj.transform, "RESUME");
            Button restartButton = CreateButton("RestartButton", pauseContentObj.transform, "RESTART");
            Button mainMenuButton = CreateButton("MainMenuButton", pauseContentObj.transform, "MAIN MENU");

            // Setup Game Manager references
            SetupGameManagerReferences(gameManager, canvasObj, pauseMenuObj, pauseButton,
                resumeButton, restartButton, mainMenuButton, timerText, energyText, livesText);
        }

        #endregion

        #region Camera Creation Methods

        private static void CreateTitleCamera()
        {
            // Create Main Camera for Title Scene
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();

            // Setup camera for UI rendering
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = false; // UI works better with perspective for title screen
            camera.fieldOfView = 60f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Dark blue background
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask = -1; // Render everything
            camera.depth = -1; // Main camera depth

            // Add audio listener
            cameraObj.AddComponent<AudioListener>();

            // Tag as MainCamera
            cameraObj.tag = "MainCamera";

            Debug.Log("Title Scene Camera created");
        }

        private static void CreateMainMenuCamera()
        {
            // Create Main Camera for Main Menu Scene
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();

            // Setup camera for UI rendering
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.15f, 1f); // Dark blue background
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask = -1;
            camera.depth = -1;

            // Add audio listener
            cameraObj.AddComponent<AudioListener>();

            // Tag as MainCamera
            cameraObj.tag = "MainCamera";

            Debug.Log("Main Menu Scene Camera created");
        }

        private static void CreateStandardUICamera()
        {
            // Create Main Camera for standard UI scenes
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();

            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask = -1;
            camera.depth = -1;

            // Add audio listener
            cameraObj.AddComponent<AudioListener>();

            // Tag as MainCamera
            cameraObj.tag = "MainCamera";
        }

        #endregion

        #region Audio Creation Methods

        private static void CreateTitleAudioSource()
        {
            GameObject audioObj = new GameObject("TitleAudioSource");
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0.7f;
        }

        private static void CreateMainMenuAudioSource()
        {
            GameObject audioObj = new GameObject("MainMenuAudioSource");
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0.8f;
        }

        private static void CreateGameAudioSource()
        {
            GameObject audioObj = new GameObject("GameAudioSource");
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0.8f;
        }

        private static void CreateStandardAudioSource()
        {
            GameObject audioObj = new GameObject("AudioSource");
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0.7f;
        }

        #endregion

        #region Game World Creation

        private static void CreateGameWorld()
        {
            GameObject gameWorldObj = new GameObject("GameWorld");

            // Create Player placeholder
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.SetParent(gameWorldObj.transform);
            playerObj.tag = "Player";

            // Create Stage placeholder
            GameObject stageObj = new GameObject("Stage");
            stageObj.transform.SetParent(gameWorldObj.transform);

            // Setup Main Camera (should already exist from DefaultGameObjects)
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 0, -10);
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 10;
                mainCamera.backgroundColor = Color.black;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                // Create camera if it doesn't exist
                GameObject cameraObj = new GameObject("Main Camera");
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 10;
                camera.backgroundColor = Color.black;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.tag = "MainCamera";

                // Add audio listener if none exists
                if (FindFirstObjectByType<AudioListener>() == null)
                {
                    cameraObj.AddComponent<AudioListener>();
                }
            }
        }

        #endregion

        #region UI Helper Methods

        private static void CreateEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        private static void SetupStandardCanvas(GameObject canvasObj)
        {
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private static GameObject CreateUIImage(string name, Transform parent)
        {
            GameObject imageObj = new GameObject(name);
            imageObj.transform.SetParent(parent);
            imageObj.AddComponent<RectTransform>();
            imageObj.AddComponent<Image>();
            return imageObj;
        }

        private static GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent);
            panelObj.AddComponent<RectTransform>();
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            return panelObj;
        }

        private static Button CreateButton(string name, Transform parent, string text)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(250, 50);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white;

            Button button = buttonObj.AddComponent<Button>();

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 18;
            buttonText.color = Color.black;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            button.targetGraphic = buttonImage;

            return button;
        }

        private static void StyleMainMenuButton(Button button, Color color)
        {
            if (button == null) return;

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color;
            }

            // Setup color transitions
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = Color.Lerp(color, Color.gray, 0.5f);
            button.colors = colors;

            // Style text
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = Color.white;
                buttonText.fontStyle = FontStyles.Bold;
                buttonText.fontSize = 20;
            }
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(300, 30);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;

            return textComponent;
        }

        private static void CreateSliderSetting(string labelText, Transform parent, float defaultValue)
        {
            GameObject settingObj = new GameObject($"{labelText}Setting");
            settingObj.transform.SetParent(parent);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(500, 40);

            HorizontalLayoutGroup layout = settingObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Create label
            TextMeshProUGUI label = CreateText($"{labelText}Label", settingObj.transform, labelText);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);

            // Create slider
            GameObject sliderObj = new GameObject($"{labelText}Slider");
            sliderObj.transform.SetParent(settingObj.transform);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(200, 30);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.value = defaultValue;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Create slider background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = Color.gray;

            // Create slider handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.targetGraphic = handleImage;
            slider.handleRect = handleRect;

            // Create value text
            TextMeshProUGUI valueText = CreateText($"{labelText}Value", settingObj.transform, $"{defaultValue:F1}");
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(50, 30);
        }

        private static void SetupFullScreenImage(GameObject imageObj, Color color)
        {
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = imageObj.GetComponent<Image>();
            image.color = color;
        }

        private static void SetupFullScreenPanel(GameObject panelObj, Color color)
        {
            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObj.GetComponent<Image>();
            image.color = color;
        }

        private static void SetupCenteredImage(GameObject imageObj, Vector2 size, Vector3 position)
        {
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetupCenteredRect(RectTransform rect, Vector2 size, Vector3 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        #endregion

        #region Reference Setup Methods

        private static void SetupTitleManagerReferences(GravityFlipLab.UI.TitleScreenManager titleManager,
            Button startButton, Button continueButton, Button optionsButton, Button exitButton,
            GameObject titleLogo, GameObject menuPanel, CanvasGroup fadeCanvasGroup)
        {
            SerializedObject serializedObject = new SerializedObject(titleManager);

            serializedObject.FindProperty("startButton").objectReferenceValue = startButton;
            serializedObject.FindProperty("continueButton").objectReferenceValue = continueButton;
            serializedObject.FindProperty("optionsButton").objectReferenceValue = optionsButton;
            serializedObject.FindProperty("exitButton").objectReferenceValue = exitButton;
            serializedObject.FindProperty("titleLogo").objectReferenceValue = titleLogo;
            serializedObject.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            serializedObject.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCanvasGroup;

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetupMainMenuManagerReferences(GravityFlipLab.UI.MainMenuSceneManager mainMenuManager,
            Button newGameButton, Button continueButton, Button stageSelectButton, Button optionsButton,
            Button leaderboardButton, Button shopButton, Button backToTitleButton, Button exitButton,
            GameObject menuPanel, CanvasGroup fadeCanvasGroup,
            TextMeshProUGUI playerProgressText, TextMeshProUGUI totalEnergyChipsText)
        {
            SerializedObject serializedObject = new SerializedObject(mainMenuManager);

            serializedObject.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            serializedObject.FindProperty("continueButton").objectReferenceValue = continueButton;
            serializedObject.FindProperty("stageSelectButton").objectReferenceValue = stageSelectButton;
            serializedObject.FindProperty("optionsButton").objectReferenceValue = optionsButton;
            serializedObject.FindProperty("leaderboardButton").objectReferenceValue = leaderboardButton;
            serializedObject.FindProperty("shopButton").objectReferenceValue = shopButton;
            serializedObject.FindProperty("backToTitleButton").objectReferenceValue = backToTitleButton;
            serializedObject.FindProperty("exitButton").objectReferenceValue = exitButton;
            serializedObject.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            serializedObject.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCanvasGroup;
            serializedObject.FindProperty("playerProgressText").objectReferenceValue = playerProgressText;
            serializedObject.FindProperty("totalEnergyChipsText").objectReferenceValue = totalEnergyChipsText;

            // Setup audio source
            AudioSource audioSource = FindFirstObjectByType<AudioSource>();
            if (audioSource != null)
            {
                serializedObject.FindProperty("audioSource").objectReferenceValue = audioSource;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetupGameManagerReferences(GravityFlipLab.UI.GameSceneManager gameManager,
            GameObject gameUICanvas, GameObject pauseMenu, Button pauseButton,
            Button resumeButton, Button restartButton, Button mainMenuButton,
            TextMeshProUGUI timerText, TextMeshProUGUI energyText, TextMeshProUGUI livesText)
        {
            SerializedObject serializedObject = new SerializedObject(gameManager);

            serializedObject.FindProperty("gameUICanvas").objectReferenceValue = gameUICanvas;
            serializedObject.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
            serializedObject.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            serializedObject.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            serializedObject.FindProperty("restartButton").objectReferenceValue = restartButton;
            serializedObject.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
            serializedObject.FindProperty("timerText").objectReferenceValue = timerText;
            serializedObject.FindProperty("energyChipText").objectReferenceValue = energyText;
            serializedObject.FindProperty("livesText").objectReferenceValue = livesText;

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Build and Validation

        private static void SetupBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/StageSelectScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/OptionsScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/LeaderboardScene.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("Build settings updated with all scenes!");
        }

        private static void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Scripts",
                "Assets/Scripts/UI",
                "Assets/Scripts/Core",
                "Assets/Scripts/Player",
                "Assets/Scripts/Stage",
                "Assets/Scripts/Physics",
                "Assets/Scenes",
                "Assets/Resources",
                "Assets/Resources/Audio",
                "Assets/Resources/Audio/BGM",
                "Assets/Resources/Audio/SE",
                "Assets/Resources/UI",
                "Assets/Resources/Fonts",
                "Assets/Prefabs",
                "Assets/Prefabs/UI",
                "Assets/Textures",
                "Assets/Materials",
                "Assets/Animations"
            };

            foreach (string folder in folders)
            {
                EnsureDirectoryExists(folder);
            }

            Debug.Log("Folder structure created successfully!");
        }

        private static void FixMissingReferences()
        {
            Debug.Log("=== Fixing Missing References ===");

            // Title Scene の修正
            FixSceneReferences("Assets/Scenes/TitleScene.unity", FixTitleSceneReferences);

            // Main Menu Scene の修正
            FixSceneReferences("Assets/Scenes/MainMenuScene.unity", FixMainMenuSceneReferences);

            // Stage Select Scene の修正
            FixSceneReferences("Assets/Scenes/StageSelectScene.unity", FixStageSelectSceneReferences);

            // Game Scene の修正
            FixSceneReferences("Assets/Scenes/GameScene.unity", FixGameSceneReferences);

            // Options Scene の修正
            FixSceneReferences("Assets/Scenes/OptionsScene.unity", FixOptionsSceneReferences);

            // Leaderboard Scene の修正
            FixSceneReferences("Assets/Scenes/LeaderboardScene.unity", FixLeaderboardSceneReferences);

            Debug.Log("=== Reference fixing completed ===");
        }

        private static void FixSceneReferences(string scenePath, System.Action fixAction)
        {
            if (File.Exists(scenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);

                fixAction?.Invoke();

                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"✓ {Path.GetFileNameWithoutExtension(scenePath)} references fixed");
            }
        }

        private static void FixTitleSceneReferences()
        {
            var titleManager = FindFirstObjectByType<GravityFlipLab.UI.TitleScreenManager>();
            if (titleManager == null) return;

            SerializedObject serializedObject = new SerializedObject(titleManager);

            // Find UI elements and auto-assign
            var startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
            var continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();
            var optionsButton = GameObject.Find("OptionsButton")?.GetComponent<Button>();
            var exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();
            var titleLogo = GameObject.Find("TitleLogo");
            var menuPanel = GameObject.Find("MenuPanel");
            var fadeCanvasGroup = GameObject.Find("FadeCanvasGroup")?.GetComponent<CanvasGroup>();
            var audioSource = FindFirstObjectByType<AudioSource>();

            if (startButton != null)
                serializedObject.FindProperty("startButton").objectReferenceValue = startButton;
            if (continueButton != null)
                serializedObject.FindProperty("continueButton").objectReferenceValue = continueButton;
            if (optionsButton != null)
                serializedObject.FindProperty("optionsButton").objectReferenceValue = optionsButton;
            if (exitButton != null)
                serializedObject.FindProperty("exitButton").objectReferenceValue = exitButton;
            if (titleLogo != null)
                serializedObject.FindProperty("titleLogo").objectReferenceValue = titleLogo;
            if (menuPanel != null)
                serializedObject.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            if (fadeCanvasGroup != null)
                serializedObject.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCanvasGroup;
            if (audioSource != null)
                serializedObject.FindProperty("audioSource").objectReferenceValue = audioSource;

            serializedObject.ApplyModifiedProperties();
        }

        private static void FixMainMenuSceneReferences()
        {
            var mainMenuManager = FindFirstObjectByType<GravityFlipLab.UI.MainMenuSceneManager>();
            if (mainMenuManager == null) return;

            SerializedObject serializedObject = new SerializedObject(mainMenuManager);

            // Find UI elements and auto-assign
            var newGameButton = GameObject.Find("NewGameButton")?.GetComponent<Button>();
            var continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();
            var stageSelectButton = GameObject.Find("StageSelectButton")?.GetComponent<Button>();
            var optionsButton = GameObject.Find("OptionsButton")?.GetComponent<Button>();
            var leaderboardButton = GameObject.Find("LeaderboardButton")?.GetComponent<Button>();
            var shopButton = GameObject.Find("ShopButton")?.GetComponent<Button>();
            var backToTitleButton = GameObject.Find("BackToTitleButton")?.GetComponent<Button>();
            var exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();
            var menuPanel = GameObject.Find("MenuPanel");
            var fadeCanvasGroup = GameObject.Find("FadeCanvasGroup")?.GetComponent<CanvasGroup>();
            var playerProgressText = GameObject.Find("PlayerProgressText")?.GetComponent<TextMeshProUGUI>();
            var totalEnergyChipsText = GameObject.Find("TotalEnergyChipsText")?.GetComponent<TextMeshProUGUI>();
            var audioSource = FindFirstObjectByType<AudioSource>();

            if (newGameButton != null)
                serializedObject.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            if (continueButton != null)
                serializedObject.FindProperty("continueButton").objectReferenceValue = continueButton;
            if (stageSelectButton != null)
                serializedObject.FindProperty("stageSelectButton").objectReferenceValue = stageSelectButton;
            if (optionsButton != null)
                serializedObject.FindProperty("optionsButton").objectReferenceValue = optionsButton;
            if (leaderboardButton != null)
                serializedObject.FindProperty("leaderboardButton").objectReferenceValue = leaderboardButton;
            if (shopButton != null)
                serializedObject.FindProperty("shopButton").objectReferenceValue = shopButton;
            if (backToTitleButton != null)
                serializedObject.FindProperty("backToTitleButton").objectReferenceValue = backToTitleButton;
            if (exitButton != null)
                serializedObject.FindProperty("exitButton").objectReferenceValue = exitButton;
            if (menuPanel != null)
                serializedObject.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            if (fadeCanvasGroup != null)
                serializedObject.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCanvasGroup;
            if (playerProgressText != null)
                serializedObject.FindProperty("playerProgressText").objectReferenceValue = playerProgressText;
            if (totalEnergyChipsText != null)
                serializedObject.FindProperty("totalEnergyChipsText").objectReferenceValue = totalEnergyChipsText;
            if (audioSource != null)
                serializedObject.FindProperty("audioSource").objectReferenceValue = audioSource;

            serializedObject.ApplyModifiedProperties();
        }

        private static void FixStageSelectSceneReferences()
        {
            // Stage Select Scene specific reference fixing would go here
            // when StageSelectManager is implemented
        }

        private static void FixGameSceneReferences()
        {
            var gameManager = FindFirstObjectByType<GravityFlipLab.UI.GameSceneManager>();
            if (gameManager == null) return;

            SerializedObject serializedObject = new SerializedObject(gameManager);

            // Find UI elements and auto-assign
            var canvas = FindFirstObjectByType<Canvas>();
            var pauseMenu = GameObject.Find("PauseMenu");
            var pauseButton = GameObject.Find("PauseButton")?.GetComponent<Button>();
            var resumeButton = GameObject.Find("ResumeButton")?.GetComponent<Button>();
            var restartButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
            var mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
            var timerText = GameObject.Find("TimerText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var energyText = GameObject.Find("EnergyChipText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var livesText = GameObject.Find("LivesText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var audioSource = FindFirstObjectByType<AudioSource>();

            if (canvas != null)
                serializedObject.FindProperty("gameUICanvas").objectReferenceValue = canvas.gameObject;
            if (pauseMenu != null)
                serializedObject.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
            if (pauseButton != null)
                serializedObject.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            if (resumeButton != null)
                serializedObject.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            if (restartButton != null)
                serializedObject.FindProperty("restartButton").objectReferenceValue = restartButton;
            if (mainMenuButton != null)
                serializedObject.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
            if (timerText != null)
                serializedObject.FindProperty("timerText").objectReferenceValue = timerText;
            if (energyText != null)
                serializedObject.FindProperty("energyChipText").objectReferenceValue = energyText;
            if (livesText != null)
                serializedObject.FindProperty("livesText").objectReferenceValue = livesText;
            if (audioSource != null)
                serializedObject.FindProperty("gameAudioSource").objectReferenceValue = audioSource;

            serializedObject.ApplyModifiedProperties();
        }

        private static void FixOptionsSceneReferences()
        {
            // Options Scene specific reference fixing would go here
            // when OptionsManager is implemented
        }

        private static void FixLeaderboardSceneReferences()
        {
            // Leaderboard Scene specific reference fixing would go here
            // when LeaderboardManager is implemented
        }

        private static void ValidateAllScenes()
        {
            Debug.Log("=== Starting Scene Validation ===");

            ValidateTitleScene();
            ValidateMainMenuScene();
            ValidateStageSelectScene();
            ValidateGameScene();
            ValidateOptionsScene();
            ValidateLeaderboardScene();
            ValidateBuildSettings();

            Debug.Log("=== Scene Validation Completed ===");
        }

        private static void ValidateTitleScene()
        {
            Debug.Log("Validating Title Scene...");
            ValidateScene("Assets/Scenes/TitleScene.unity", "TitleScreenManager");
        }

        private static void ValidateMainMenuScene()
        {
            Debug.Log("Validating Main Menu Scene...");
            ValidateScene("Assets/Scenes/MainMenuScene.unity", "MainMenuSceneManager");
        }

        private static void ValidateStageSelectScene()
        {
            Debug.Log("Validating Stage Select Scene...");
            ValidateScene("Assets/Scenes/StageSelectScene.unity", "StageSelectManager");
        }

        private static void ValidateGameScene()
        {
            Debug.Log("Validating Game Scene...");
            ValidateScene("Assets/Scenes/GameScene.unity", "GameSceneManager");
        }

        private static void ValidateOptionsScene()
        {
            Debug.Log("Validating Options Scene...");
            ValidateScene("Assets/Scenes/OptionsScene.unity", "OptionsManager");
        }

        private static void ValidateLeaderboardScene()
        {
            Debug.Log("Validating Leaderboard Scene...");
            ValidateScene("Assets/Scenes/LeaderboardScene.unity", "LeaderboardManager");
        }

        private static void ValidateScene(string scenePath, string managerName)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"Scene not found at: {scenePath}");
                return;
            }

            // シーンを開いて検証
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);

            // Main Camera の存在確認
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"Main Camera not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
            }
            else
            {
                Debug.Log($"✓ Main Camera found in {Path.GetFileNameWithoutExtension(scenePath)}");
            }

            // Audio Listener の存在確認
            var audioListener = FindFirstObjectByType<AudioListener>();
            if (audioListener == null)
            {
                Debug.LogError($"Audio Listener not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
            }
            else
            {
                Debug.Log($"✓ Audio Listener found in {Path.GetFileNameWithoutExtension(scenePath)}");
            }

            // EventSystem の存在確認
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogError($"EventSystem not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
            }
            else
            {
                Debug.Log($"✓ EventSystem found in {Path.GetFileNameWithoutExtension(scenePath)}");
            }

            // Canvas の存在確認
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"Canvas not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
            }
            else
            {
                Debug.Log($"✓ Canvas found in {Path.GetFileNameWithoutExtension(scenePath)}");
            }

            // Manager の存在確認（利用可能な場合のみ）
            if (managerName == "TitleScreenManager")
            {
                var titleManager = FindFirstObjectByType<GravityFlipLab.UI.TitleScreenManager>();
                if (titleManager == null)
                {
                    Debug.LogError($"TitleScreenManager not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
                }
                else
                {
                    Debug.Log($"✓ TitleScreenManager found in {Path.GetFileNameWithoutExtension(scenePath)}");
                }
            }
            else if (managerName == "MainMenuSceneManager")
            {
                var mainMenuManager = FindFirstObjectByType<GravityFlipLab.UI.MainMenuSceneManager>();
                if (mainMenuManager == null)
                {
                    Debug.LogError($"MainMenuSceneManager not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
                }
                else
                {
                    Debug.Log($"✓ MainMenuSceneManager found in {Path.GetFileNameWithoutExtension(scenePath)}");
                }
            }
            else if (managerName == "GameSceneManager")
            {
                var gameManager = FindFirstObjectByType<GravityFlipLab.UI.GameSceneManager>();
                if (gameManager == null)
                {
                    Debug.LogError($"GameSceneManager not found in {Path.GetFileNameWithoutExtension(scenePath)}!");
                }
                else
                {
                    Debug.Log($"✓ GameSceneManager found in {Path.GetFileNameWithoutExtension(scenePath)}");
                }
            }
            else
            {
                Debug.Log($"Note: {managerName} validation skipped (component not yet implemented)");
            }

            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }

        private static void ValidateBuildSettings()
        {
            Debug.Log("Validating Build Settings...");

            var scenes = EditorBuildSettings.scenes;

            string[] expectedScenes = {
                "TitleScene", "MainMenuScene", "StageSelectScene",
                "GameScene", "OptionsScene", "LeaderboardScene"
            };

            foreach (string expectedScene in expectedScenes)
            {
                bool found = scenes.Any(scene => scene.path.Contains(expectedScene));
                if (!found)
                {
                    Debug.LogError($"{expectedScene} not found in Build Settings!");
                }
                else
                {
                    Debug.Log($"✓ {expectedScene} in Build Settings");
                }
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }

    #region Scene Validation Tool

    public class SceneValidator : EditorWindow
    {
        [MenuItem("Gravity Flip Lab/Validate Scenes")]
        public static void ShowWindow()
        {
            GetWindow<SceneValidator>("Scene Validator");
        }

        private Vector2 scrollPosition;
        private string validationResults = "";

        private void OnGUI()
        {
            GUILayout.Label("Scene Validation Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Validate All Scenes"))
            {
                ValidateAllScenes();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Validate Individual Scenes"))
            {
                ShowIndividualValidationOptions();
            }

            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Validation Results:", EditorStyles.boldLabel);
            GUILayout.TextArea(validationResults, GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();
        }

        private void ValidateAllScenes()
        {
            validationResults = "=== Scene Validation Started ===\n";

            ValidateSceneInternal("Title Scene", "Assets/Scenes/TitleScene.unity", "TitleScreenManager");
            ValidateSceneInternal("Main Menu Scene", "Assets/Scenes/MainMenuScene.unity", "MainMenuSceneManager");
            ValidateSceneInternal("Stage Select Scene", "Assets/Scenes/StageSelectScene.unity", "StageSelectManager");
            ValidateSceneInternal("Game Scene", "Assets/Scenes/GameScene.unity", "GameSceneManager");
            ValidateSceneInternal("Options Scene", "Assets/Scenes/OptionsScene.unity", "OptionsManager");
            ValidateSceneInternal("Leaderboard Scene", "Assets/Scenes/LeaderboardScene.unity", "LeaderboardManager");

            ValidateBuildSettingsInternal();

            validationResults += "\n=== Scene Validation Completed ===";
        }

        private void ShowIndividualValidationOptions()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Title Scene"), false, () => ValidateIndividualScene("Title Scene", "Assets/Scenes/TitleScene.unity", "TitleScreenManager"));
            menu.AddItem(new GUIContent("Main Menu Scene"), false, () => ValidateIndividualScene("Main Menu Scene", "Assets/Scenes/MainMenuScene.unity", "MainMenuSceneManager"));
            menu.AddItem(new GUIContent("Stage Select Scene"), false, () => ValidateIndividualScene("Stage Select Scene", "Assets/Scenes/StageSelectScene.unity", "StageSelectManager"));
            menu.AddItem(new GUIContent("Game Scene"), false, () => ValidateIndividualScene("Game Scene", "Assets/Scenes/GameScene.unity", "GameSceneManager"));
            menu.AddItem(new GUIContent("Options Scene"), false, () => ValidateIndividualScene("Options Scene", "Assets/Scenes/OptionsScene.unity", "OptionsManager"));
            menu.AddItem(new GUIContent("Leaderboard Scene"), false, () => ValidateIndividualScene("Leaderboard Scene", "Assets/Scenes/LeaderboardScene.unity", "LeaderboardManager"));
            menu.ShowAsContext();
        }

        private void ValidateIndividualScene(string sceneName, string scenePath, string managerName)
        {
            validationResults = $"=== Validating {sceneName} ===\n";
            ValidateSceneInternal(sceneName, scenePath, managerName);
            validationResults += $"=== {sceneName} Validation Completed ===";
        }

        private void ValidateSceneInternal(string sceneName, string scenePath, string managerName)
        {
            validationResults += $"\nValidating {sceneName}...\n";

            if (!File.Exists(scenePath))
            {
                validationResults += $"❌ Scene not found at: {scenePath}\n";
                return;
            }

            // シーンを開いて検証
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);

            // Main Camera の存在確認
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                validationResults += $"❌ Main Camera not found in {sceneName}!\n";
            }
            else
            {
                validationResults += $"✓ Main Camera found in {sceneName}\n";
            }

            // Audio Listener の存在確認
            var audioListener = FindFirstObjectByType<AudioListener>();
            if (audioListener == null)
            {
                validationResults += $"❌ Audio Listener not found in {sceneName}!\n";
            }
            else
            {
                validationResults += $"✓ Audio Listener found in {sceneName}\n";
            }

            // EventSystem の存在確認
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                validationResults += $"❌ EventSystem not found in {sceneName}!\n";
            }
            else
            {
                validationResults += $"✓ EventSystem found in {sceneName}\n";
            }

            // Canvas の存在確認
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                validationResults += $"❌ Canvas not found in {sceneName}!\n";
            }
            else
            {
                validationResults += $"✓ Canvas found in {sceneName}\n";
            }

            // Manager の存在確認
            ValidateManagerComponent(sceneName, managerName);

            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }

        private void ValidateManagerComponent(string sceneName, string managerName)
        {
            switch (managerName)
            {
                case "TitleScreenManager":
                    var titleManager = FindFirstObjectByType<GravityFlipLab.UI.TitleScreenManager>();
                    if (titleManager == null)
                    {
                        validationResults += $"❌ TitleScreenManager not found in {sceneName}!\n";
                    }
                    else
                    {
                        validationResults += $"✓ TitleScreenManager found in {sceneName}\n";
                    }
                    break;

                case "MainMenuSceneManager":
                    var mainMenuManager = FindFirstObjectByType<GravityFlipLab.UI.MainMenuSceneManager>();
                    if (mainMenuManager == null)
                    {
                        validationResults += $"❌ MainMenuSceneManager not found in {sceneName}!\n";
                    }
                    else
                    {
                        validationResults += $"✓ MainMenuSceneManager found in {sceneName}\n";
                    }
                    break;

                case "GameSceneManager":
                    var gameManager = FindFirstObjectByType<GravityFlipLab.UI.GameSceneManager>();
                    if (gameManager == null)
                    {
                        validationResults += $"❌ GameSceneManager not found in {sceneName}!\n";
                    }
                    else
                    {
                        validationResults += $"✓ GameSceneManager found in {sceneName}\n";
                    }
                    break;

                default:
                    validationResults += $"⚠️ {managerName} validation skipped (component not yet implemented)\n";
                    break;
            }
        }

        private void ValidateBuildSettingsInternal()
        {
            validationResults += "\nValidating Build Settings...\n";

            var scenes = EditorBuildSettings.scenes;

            string[] expectedScenes = {
                "TitleScene", "MainMenuScene", "StageSelectScene",
                "GameScene", "OptionsScene", "LeaderboardScene"
            };

            foreach (string expectedScene in expectedScenes)
            {
                bool found = scenes.Any(scene => scene.path.Contains(expectedScene));
                if (!found)
                {
                    validationResults += $"❌ {expectedScene} not found in Build Settings!\n";
                }
                else
                {
                    validationResults += $"✓ {expectedScene} in Build Settings\n";
                }
            }
        }
    }

    #endregion

    #region Project Setup Assistant

    [InitializeOnLoad]
    public class ProjectSetupAssistant
    {
        private const string SETUP_COMPLETE_KEY = "GravityFlipLab_SetupComplete";

        static ProjectSetupAssistant()
        {
            EditorApplication.delayCall += CheckFirstTimeSetup;
        }

        private static void CheckFirstTimeSetup()
        {
            if (!EditorPrefs.GetBool(SETUP_COMPLETE_KEY, false))
            {
                ShowFirstTimeSetupDialog();
            }
        }

        private static void ShowFirstTimeSetupDialog()
        {
            bool setupNow = EditorUtility.DisplayDialog(
                "Gravity Flip Lab Setup",
                "Welcome to Gravity Flip Lab!\n\n" +
                "It looks like this is your first time opening this project.\n" +
                "Would you like to automatically set up the basic scenes and folder structure?",
                "Yes, Set Up Now",
                "Not Now"
            );

            if (setupNow)
            {
                PerformFirstTimeSetup();
                EditorPrefs.SetBool(SETUP_COMPLETE_KEY, true);
            }
        }

        private static void PerformFirstTimeSetup()
        {
            Debug.Log("Performing first-time project setup...");

            // Create folder structure
            CreateBasicFolders();

            // Create basic scenes
            if (EditorUtility.DisplayDialog("Create Scenes",
                "Create all scenes now?", "Yes", "No"))
            {
                SceneSetupTool.ShowWindow();
            }

            // Setup TextMeshPro
            SetupTextMeshPro();

            Debug.Log("First-time setup completed!");
        }

        private static void CreateBasicFolders()
        {
            string[] folders = {
                "Assets/Scripts/Core",
                "Assets/Scripts/UI",
                "Assets/Scenes",
                "Assets/Resources/Audio/BGM",
                "Assets/Resources/Audio/SE",
                "Assets/Prefabs"
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            AssetDatabase.Refresh();
        }

        private static void SetupTextMeshPro()
        {
            // TextMeshProの設定（必要に応じて）
            try
            {
                // TextMeshPro Essentialsをインポート
                AssetDatabase.importPackageCompleted += OnPackageImported;
                AssetDatabase.ImportPackage("Assets/TextMesh Pro/Resources/TMP Essential Resources.unitypackage", false);
            }
            catch (System.Exception)
            {
                // TextMeshProが見つからない場合は無視
                Debug.Log("TextMeshPro not found, skipping setup");
            }
        }

        private static void OnPackageImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
            Debug.Log("TextMeshPro Essential Resources imported");
        }

        [MenuItem("Gravity Flip Lab/Reset First Time Setup")]
        public static void ResetFirstTimeSetup()
        {
            EditorPrefs.DeleteKey(SETUP_COMPLETE_KEY);
            Debug.Log("First time setup flag reset. Setup dialog will appear on next project load.");
        }
    }

    #endregion

    #region Quick Actions

    public class QuickActions
    {
        [MenuItem("Gravity Flip Lab/Quick Actions/Open Title Scene")]
        public static void OpenTitleScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Open Main Menu Scene")]
        public static void OpenMainMenuScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Open Stage Select Scene")]
        public static void OpenStageSelectScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/StageSelectScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Open Game Scene")]
        public static void OpenGameScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Open Options Scene")]
        public static void OpenOptionsScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OptionsScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Open Leaderboard Scene")]
        public static void OpenLeaderboardScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/LeaderboardScene.unity");
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Play From Title Scene")]
        public static void PlayFromTitleScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Play From Main Menu")]
        public static void PlayFromMainMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Build and Run")]
        public static void BuildAndRun()
        {
            // ビルド設定が正しいか確認
            if (EditorBuildSettings.scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build Error",
                    "No scenes in build settings!\nPlease use 'Setup Build Settings' first.", "OK");
                return;
            }

            // 現在のシーンを保存
            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // ビルドと実行
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
            buildOptions.locationPathName = "Builds/GravityFlipLab.exe";
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.options = BuildOptions.AutoRunPlayer;

            // ビルドディレクトリを作成
            string buildDir = Path.GetDirectoryName(buildOptions.locationPathName);
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            BuildPipeline.BuildPlayer(buildOptions);
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Clear Console")]
        public static void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

        [MenuItem("Gravity Flip Lab/Quick Actions/Delete PlayerPrefs")]
        public static void DeletePlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("Delete PlayerPrefs",
                "Are you sure you want to delete all PlayerPrefs?", "Yes", "No"))
            {
                PlayerPrefs.DeleteAll();
                Debug.Log("PlayerPrefs deleted");
            }
        }
    }

    #endregion

    #region Build Preprocessor

    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Starting Gravity Flip Lab build preprocessing...");

            // ビルド前の検証
            ValidateBuildConfiguration();
            OptimizeForBuild();

            Debug.Log("Build preprocessing completed");
        }

        private void ValidateBuildConfiguration()
        {
            // シーンの存在確認
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!File.Exists(scene.path))
                {
                    throw new System.Exception($"Scene not found: {scene.path}");
                }
            }

            // 必要なリソースの確認
            string[] requiredPaths = {
                "Assets/Scripts",
                "Assets/Scenes"
            };

            foreach (string path in requiredPaths)
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogWarning($"Required directory not found: {path}");
                }
            }
        }

        private void OptimizeForBuild()
        {
            // ビルド最適化の実行
            Debug.Log("Optimizing project for build...");

            // 未使用アセットの削除警告
            string[] unusedAssets = AssetDatabase.FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !IsAssetUsed(path))
                .ToArray();

            if (unusedAssets.Length > 0)
            {
                Debug.LogWarning($"Found {unusedAssets.Length} potentially unused assets");
            }
        }

        private bool IsAssetUsed(string assetPath)
        {
            // 簡単な使用判定（実際にはより詳細な分析が必要）
            return assetPath.Contains("Scripts") ||
                   assetPath.Contains("Scenes") ||
                   assetPath.EndsWith(".cs");
        }
    }

    #endregion
}
#endif