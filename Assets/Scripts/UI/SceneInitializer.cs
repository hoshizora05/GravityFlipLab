using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GravityFlipLab.UI
{
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private float initializationDelay = 0.1f;
        [SerializeField] private bool manageFadeTransitions = true;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                StartCoroutine(InitializeScene());
            }
        }

        private IEnumerator InitializeScene()
        {
            yield return new WaitForSeconds(initializationDelay);

            // Ensure core managers exist
            EnsureCoreManagersExist();

            // フェード管理が有効な場合、画面を黒くしてから初期化
            if (manageFadeTransitions && FadeTransitionManager.Instance != null)
            {
                FadeTransitionManager.Instance.SetFadeOut();
            }

            // Scene-specific initialization
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            switch (sceneName)
            {
                case "TitleScene":
                case "MainMenuScene": // MainMenuも追加
                    InitializeTitleScene();
                    break;
                case "GameScene":
                    InitializeGameScene();
                    break;
                case "OptionsScene":
                    InitializeOptionsScene();
                    break;
                case "StageSelect":
                    InitializeStageSelectScene();
                    break;
                case "Leaderboard":
                    InitializeLeaderboardScene();
                    break;
                case "Shop":
                    InitializeShopScene();
                    break;
            }

            // 初期化完了後、少し待ってからフェードイン
            if (manageFadeTransitions && FadeTransitionManager.Instance != null)
            {
                yield return new WaitForSeconds(0.3f); // シーンの初期化を待つ
                FadeTransitionManager.Instance.FadeIn();
            }
        }

        private void EnsureCoreManagersExist()
        {
            // Ensure GameManager exists
            if (GameManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManagerObj.AddComponent<GameManager>();
                DontDestroyOnLoad(gameManagerObj);
            }

            // Ensure SaveManager exists
            if (SaveManager.Instance == null)
            {
                GameObject saveManagerObj = new GameObject("SaveManager");
                saveManagerObj.AddComponent<SaveManager>();
                DontDestroyOnLoad(saveManagerObj);
            }

            // Ensure SceneTransitionManager exists
            if (SceneTransitionManager.Instance == null)
            {
                GameObject sceneManagerObj = new GameObject("SceneTransitionManager");
                sceneManagerObj.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(sceneManagerObj);
            }

            // Ensure FadeTransitionManager exists
            if (FadeTransitionManager.Instance == null)
            {
                GameObject fadeManagerObj = new GameObject("FadeTransitionManager");
                fadeManagerObj.AddComponent<FadeTransitionManager>();
                DontDestroyOnLoad(fadeManagerObj);
            }
        }

        private void InitializeTitleScene()
        {
            Debug.Log("Initializing Title/MainMenu Scene");
            GameManager.Instance.ChangeGameState(GameState.MainMenu);
        }

        private void InitializeGameScene()
        {
            Debug.Log("Initializing Game Scene");
            GameManager.Instance.ChangeGameState(GameState.Gameplay);
        }

        private void InitializeOptionsScene()
        {
            Debug.Log("Initializing Options Scene");
            GameManager.Instance.ChangeGameState(GameState.Options);
        }

        private void InitializeStageSelectScene()
        {
            Debug.Log("Initializing Stage Select Scene");
            GameManager.Instance.ChangeGameState(GameState.StageSelect);
        }

        private void InitializeLeaderboardScene()
        {
            Debug.Log("Initializing Leaderboard Scene");
            GameManager.Instance.ChangeGameState(GameState.Leaderboard);
        }

        private void InitializeShopScene()
        {
            Debug.Log("Initializing Shop Scene");
            GameManager.Instance.ChangeGameState(GameState.Shop);
        }
    }
}