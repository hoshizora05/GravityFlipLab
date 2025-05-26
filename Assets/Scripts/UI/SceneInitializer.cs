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

            // Scene-specific initialization
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            switch (sceneName)
            {
                case "TitleScene":
                    InitializeTitleScene();
                    break;
                case "GameScene":
                    InitializeGameScene();
                    break;
                case "OptionsScene":
                    InitializeOptionsScene();
                    break;
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
        }

        private void InitializeTitleScene()
        {
            Debug.Log("Initializing Title Scene");
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
    }
}