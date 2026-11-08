
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    // ⬇️ WeightedScene DIPINDAH KE ATAS sebelum dipakai
    [System.Serializable]
    public class WeightedScene
    {
        public string sceneName;
        [Tooltip("Semakin besar, semakin besar peluang terpilih. 0 = tidak pernah dipilih (kecuali semua 0).")]
        public float weight = 1f;
    }

    [Header("=== PLAYER DETECTION ===")]
    [Tooltip("Drag & drop GameObject player ke sini")]
    public Transform playerTransform;

    [Tooltip("Radius deteksi player dari pintu")]
    public float detectRadius = 1.5f;

    [Tooltip("Opsional: prompt 'Tekan E' saat dekat pintu")]
    public GameObject interactionPrompt;

    [Header("=== SCENE TRANSITION ===")]
    [Tooltip("Nama scene tujuan (dipakai saat Random Scene OFF). Harus terdaftar di Build Settings!")]
    public string targetSceneName = "EliasHouse";

    [Tooltip("Delay sebelum pindah scene (detik)")]
    public float transitionDelay = 0.3f;

    [Tooltip("Opsional: fade out panel sebelum pindah (drag CanvasGroup)")]
    public CanvasGroup fadePanel;

    [Header("=== RANDOM SCENE (OPSIONAL) ===")]
    [Tooltip("OFF (default) = selalu load targetSceneName. ON = pilih acak dari daftar di bawah.")]
    public bool enableRandomScene = false;

    [Tooltip("Daftar scene dengan bobot (weight).")]
    public List<WeightedScene> randomScenes = new List<WeightedScene>();

    [Header("=== OPTIONS ===")]
    [Tooltip("Tombol untuk masuk pintu")]
    public Key interactKey = Key.E;

    [Tooltip("Nonaktifkan pintu sementara?")]
    public bool isLocked = false;

    [Tooltip("Pesan kalau pintu terkunci")]
    public string lockedMessage = "Pintu terkunci...";

    [Header("=== DEBUG ===")]
    public bool showDebug = false;

    // Internal
    private bool playerNear;
    private bool isTransitioning;
    private string pendingSceneName;

    void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning($"[Door] {name}: Player belum di-assign!");
        }
    }

    void Update()
    {
        if (isTransitioning) return;

        playerNear = false;
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            playerNear = dist <= detectRadius;
        }

        if (interactionPrompt != null)
        {
            bool show = playerNear && !isTransitioning;
            if (interactionPrompt.activeSelf != show)
                interactionPrompt.SetActive(show);
        }

        if (playerNear && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (isLocked)
            {
                if (showDebug) Debug.Log($"[Door] Terkunci: {lockedMessage}");
                return;
            }

            EnterDoor();
        }
    }

    void EnterDoor()
    {
        pendingSceneName = PickScene();

        if (string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.LogWarning("[Door] Scene tujuan tidak valid, transisi dibatalkan.");
            return;
        }

        isTransitioning = true;

        Debug.Log($"[Door] {name} -> pindah ke scene: {pendingSceneName}");

        if (fadePanel != null)
            StartCoroutine(FadeAndLoad());
        else
            Invoke(nameof(LoadScene), transitionDelay);
    }

    // Weighted pick dengan fallback equal-weight jika semua weight <= 0
    private string PickScene()
    {
        if (!enableRandomScene)
            return targetSceneName;

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
            Debug.LogWarning($"[Door] {name}: randomScenes kosong / semua sceneName blank. Fallback ke '{targetSceneName}'.");
            return targetSceneName;
        }

        bool useEqualWeights = weightedCount == 0;
        if (useEqualWeights)
            Debug.LogWarning($"[Door] {name}: Semua weight 0 -> pakai peluang sama rata.");

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
                Debug.Log($"[Door] {name}: Roll={roll:F3}/{finalTotal:F3} -> {ws.sceneName}");
                return ws.sceneName;
            }
        }

        return lastValid ?? targetSceneName;
    }

    System.Collections.IEnumerator FadeAndLoad()
    {
        float t = 0f;
        float duration = transitionDelay;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        fadePanel.alpha = 1f;

        LoadScene();
    }

    void LoadScene()
    {
        if (string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.LogWarning("[Door] pendingSceneName kosong!");
            return;
        }
        SceneManager.LoadScene(pendingSceneName);
    }

    // ==================== PUBLIC HELPERS ====================
    public void LockDoor() { isLocked = true; }
    public void UnlockDoor() { isLocked = false; }

    public void ForceTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        enableRandomScene = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}