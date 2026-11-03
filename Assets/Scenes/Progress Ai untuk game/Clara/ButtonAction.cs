
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ButtonAction : MonoBehaviour
{
    public enum ActionType
    {
        LoadSceneByName,
        LoadSceneByIndex,
        QuitGame
    }

    [Header("Aksi Tombol")]
    public ActionType actionType = ActionType.LoadSceneByName;

    [Header("Load Scene by Name")]
    public string sceneName = "NamaScene";

    [Header("Load Scene by Index")]
    public int sceneIndex = 0;

    // Method ini yang di-hook ke Button.onClick
    public void Execute()
    {
        switch (actionType)
        {
            case ActionType.LoadSceneByName:
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("[ButtonAction] Scene name masih kosong!");
                    return;
                }
                SceneManager.LoadScene(sceneName);
                break;

            case ActionType.LoadSceneByIndex:
                if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
                {
                    Debug.LogWarning($"[ButtonAction] Scene index {sceneIndex} tidak valid. Cek Build Settings.");
                    return;
                }
                SceneManager.LoadScene(sceneIndex);
                break;

            case ActionType.QuitGame:
                QuitGame();
                break;
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        // Supaya tombol juga berfungsi saat testing di Editor
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}