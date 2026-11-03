
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    [Header("=== PLAYER DETECTION ===")]
    [Tooltip("Drag & drop GameObject player ke sini")]
    public Transform playerTransform;

    [Tooltip("Radius deteksi player dari pintu")]
    public float detectRadius = 1.5f;

    [Tooltip("Opsional: prompt 'Tekan E' saat dekat pintu")]
    public GameObject interactionPrompt;

    [Header("=== SCENE TRANSITION ===")]
    [Tooltip("Nama scene tujuan. Harus terdaftar di Build Settings!")]
    public string targetSceneName = "EliasHouse";

    [Tooltip("Delay sebelum pindah scene (detik)")]
    public float transitionDelay = 0.3f;

    [Tooltip("Opsional: fade out panel sebelum pindah (drag CanvasGroup)")]
    public CanvasGroup fadePanel;

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

        // Cek player dekat
        playerNear = false;
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            playerNear = dist <= detectRadius;
        }

        // Prompt
        if (interactionPrompt != null)
        {
            bool show = playerNear && !isTransitioning;
            if (interactionPrompt.activeSelf != show)
                interactionPrompt.SetActive(show);
        }

        // Input E
        if (playerNear && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (isLocked)
            {
                if (showDebug) Debug.Log($"[Door] Terkunci: {lockedMessage}");
                // Optional: bisa tampilkan UI pesan terkunci di sini
                return;
            }

            EnterDoor();
        }
    }

    void EnterDoor()
    {
        isTransitioning = true;

        if (showDebug) Debug.Log($"[Door] Pindah ke scene: {targetSceneName}");

        if (fadePanel != null)
            StartCoroutine(FadeAndLoad());
        else
            Invoke(nameof(LoadScene), transitionDelay);
    }

    System.Collections.IEnumerator FadeAndLoad()
    {
        // Fade out
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
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[Door] targetSceneName kosong!");
            return;
        }
        SceneManager.LoadScene(targetSceneName);
    }

    // ==================== PUBLIC HELPERS ====================
    public void LockDoor() { isLocked = true; }
    public void UnlockDoor() { isLocked = false; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}