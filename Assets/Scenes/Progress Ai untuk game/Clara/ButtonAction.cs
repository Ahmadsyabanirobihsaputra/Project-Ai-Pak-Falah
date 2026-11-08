
using System.Collections.Generic;

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
        LoadSceneRandom,
        QuitGame
    }

    [System.Serializable]
    public class WeightedScene
    {
        public string sceneName;
        [Tooltip("Semakin besar, semakin besar peluang terpilih. 0 = tidak pernah dipilih (kecuali semua 0).")]
        public float weight = 1f;
    }

    [Header("Aksi Tombol")]
    public ActionType actionType = ActionType.LoadSceneByName;

    [Header("Load Scene by Name")]
    public string sceneName = "NamaScene";

    [Header("Load Scene by Index")]
    public int sceneIndex = 0;

    [Header("Load Scene Random")]
    [Tooltip("Aktifkan untuk memilih scene acak dari daftar. Jika OFF, hanya memuat fallbackSceneName.")]
    public bool enableRandomScene = false;

    [Tooltip("Scene tunggal yang dimuat saat random OFF atau saat daftar random kosong.")]
    public string fallbackSceneName = "NamaScene";

    [Tooltip("Daftar scene dengan bobot (weight). Semakin besar weight, semakin besar peluang terpilih.")]
    public List<WeightedScene> randomScenes = new List<WeightedScene>();

    // Method ini yang di-hook ke Button.onClick
    public void Execute()
    {
        switch (actionType)
        {
            case ActionType.LoadSceneByName:
                // Redirect otomatis ke random kalau enableRandomScene dicentang
                if (enableRandomScene) LoadRandomScene();
                else LoadByName(sceneName);
                break;

            case ActionType.LoadSceneByIndex:
                LoadByIndex(sceneIndex);
                break;

            case ActionType.LoadSceneRandom:
                LoadRandomScene();
                break;

            case ActionType.QuitGame:
                QuitGame();
                break;
        }
    }

    private void LoadByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[ButtonAction] Scene name masih kosong!");
            return;
        }
        SceneManager.LoadScene(name);
    }

    private void LoadByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[ButtonAction] Scene index {index} tidak valid. Cek Build Settings.");
            return;
        }
        SceneManager.LoadScene(index);
    }

    private void LoadRandomScene()
    {
        // Random OFF (default) -> load fallback tunggal
        if (!enableRandomScene)
        {
            Debug.Log($"[ButtonAction] Random OFF, loading: {fallbackSceneName}");
            LoadByName(fallbackSceneName);
            return;
        }

        string picked = PickRandomScene();
        if (string.IsNullOrEmpty(picked))
        {
            Debug.LogWarning($"[ButtonAction] Tidak ada random scene valid, pakai fallback: {fallbackSceneName}");
            LoadByName(fallbackSceneName);
            return;
        }

        Debug.Log($"[ButtonAction] Random pick -> {picked}");
        SceneManager.LoadScene(picked);
    }

    // Weighted pick dengan fallback equal-weight jika semua weight <= 0
    private string PickRandomScene()
    {
        int sceneCount = 0;
        int weightedCount = 0;
        float totalWeight = 0f;

        if (randomScenes != null)
        {
            foreach (var ws in randomScenes)
            {
                if (ws == null || string.IsNullOrEmpty(ws.sceneName)) continue;
                sceneCount++;
                if (ws.weight > 0f)
                {
                    weightedCount++;
                    totalWeight += ws.weight;
                }
            }
        }

        if (sceneCount == 0)
        {
            Debug.LogWarning("[ButtonAction] randomScenes kosong / semua sceneName blank.");
            return null;
        }

        bool useEqualWeights = weightedCount == 0;
        if (useEqualWeights)
            Debug.LogWarning("[ButtonAction] Semua weight 0 -> pakai peluang sama rata.");

        float finalTotal = useEqualWeights ? sceneCount : totalWeight;
        float roll = Random.value * finalTotal;
        float accum = 0f;
        string lastValid = null;

        foreach (var ws in randomScenes)
        {
            if (ws == null || string.IsNullOrEmpty(ws.sceneName)) continue;
            if (!useEqualWeights && ws.weight <= 0f) continue;

            lastValid = ws.sceneName;
            accum += useEqualWeights ? 1f : ws.weight;
            if (roll <= accum)
            {
                Debug.Log($"[ButtonAction] Roll={roll:F3}/{finalTotal:F3} -> {ws.sceneName}");
                return ws.sceneName;
            }
        }

        return lastValid;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}