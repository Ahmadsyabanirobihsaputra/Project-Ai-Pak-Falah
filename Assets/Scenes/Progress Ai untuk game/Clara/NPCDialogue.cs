
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum NPCDirection { Down, Up, Left, Right }

// === STRUCT BARU: tiap baris punya text + audio ===
[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 5)]
    public string text;

    [Tooltip("Suara untuk baris ini. Boleh kosong jika tidak ada voice.")]
    public AudioClip voiceClip;
}

[RequireComponent(typeof(SpriteRenderer))]
public class NPCDialogue : MonoBehaviour
{
    [Header("=== PLAYER DETECTION ===")]
    [Tooltip("Drag & drop GameObject player ke sini")]
    public Transform playerTransform;

    [Tooltip("Radius deteksi player dari NPC")]
    public float detectRadius = 2f;

    [Tooltip("Opsional: prompt 'Tekan E' saat dekat NPC")]
    public GameObject interactionPrompt;

    [Header("=== SPRITES PER ARAH ===")]
    public Sprite spriteDown;
    public Sprite spriteUp;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("=== DEFAULT SPRITE ===")]
    public NPCDirection defaultDirection = NPCDirection.Down;

    [Header("=== DIALOGUE UI ===")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public string speakerName = "Elias";

    [Header("=== DIALOGUE CONTENT ===")]
    [Tooltip("Isi dialog beserta audio voice tiap baris")]
    public DialogueLine[] dialogueLines = new DialogueLine[]
    {
        new DialogueLine { text = "Kau kembali lagi... atau ini pertama kalinya kau ke sini?" },
        new DialogueLine { text = "Aku sudah mencatat semuanya. Setiap perubahan kecil di kebun ini." },
        new DialogueLine { text = "Tapi ingatanku sendiri... tidak selalu bisa dipercaya." },
        new DialogueLine { text = "Kalau kau menemukan sesuatu yang aneh, catatlah. Jangan andalkan ingatanmu." }
    };

    [Header("=== AUDIO VOICE ===")]
    [Tooltip("AudioSource untuk voice dialog. Jika kosong, otomatis dibuat.")]
    public AudioSource voiceAudioSource;

    [Range(0f, 1f)]
    [Tooltip("Volume voice dialog")]
    public float voiceVolume = 1f;

    [Tooltip("Jika true, voice yang sedang main akan dihentikan saat pindah baris (default: true)")]
    public bool stopVoiceOnNext = true;

    [Header("=== SETTINGS ===")]
    public Key startKey = Key.E;
    public Key nextKey = Key.Enter;
    public bool autoStartOnApproach = false;

    [Header("=== DEBUG ===")]
    public bool showDebug = false;

    // Internal
    private SpriteRenderer sr;
    private bool playerNear;
    private bool dialogueActive;
    private int currentLineIndex;
    private NPCDirection currentDirection;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Setup AudioSource otomatis
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
            if (voiceAudioSource == null)
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
        }
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
    }

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning($"[NPC] {name}: Player belum di-assign!");
        }

        SetDirection(defaultDirection, forceUpdate: true);
    }

    void Update()
    {
        // ===== 1. CEK PLAYER DEKAT =====
        playerNear = false;
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            playerNear = dist <= detectRadius;
        }

        // ===== 2. UPDATE SPRITE =====
        if (playerNear && playerTransform != null)
            SetDirection(GetDirectionToPlayer());
        else
            SetDirection(defaultDirection);

        // ===== 3. KALAU TIDAK DEKAT & DIALOG TIDAK AKTIF =====
        if (!playerNear && !dialogueActive)
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            return;
        }

        // ===== 4. PROMPT =====
        if (interactionPrompt != null)
        {
            bool show = playerNear && !dialogueActive;
            if (interactionPrompt.activeSelf != show)
                interactionPrompt.SetActive(show);
        }

        // ===== 5. MULAI DIALOG =====
        if (!dialogueActive && playerNear)
        {
            bool startInput = autoStartOnApproach ||
                (Keyboard.current != null && Keyboard.current[startKey].wasPressedThisFrame);

            if (startInput) StartDialogue();
        }

        // ===== 6. LANJUT DIALOG =====
        if (dialogueActive)
        {
            if (Keyboard.current != null && Keyboard.current[nextKey].wasPressedThisFrame)
                NextDialogue();
        }
    }

    // ==================== SPRITE DIRECTION LOGIC ====================
    NPCDirection GetDirectionToPlayer()
    {
        Vector3 diff = playerTransform.position - transform.position;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            return diff.x > 0 ? NPCDirection.Right : NPCDirection.Left;
        else
            return diff.y > 0 ? NPCDirection.Up : NPCDirection.Down;
    }

    void SetDirection(NPCDirection dir, bool forceUpdate = false)
    {
        if (!forceUpdate && currentDirection == dir) return;
        currentDirection = dir;

        Sprite targetSprite = GetSpriteForDirection(dir);
        if (targetSprite != null && sr != null)
        {
            sr.sprite = targetSprite;
            if (showDebug) Debug.Log($"[NPC] {name} menghadap: {dir}");
        }
    }

    Sprite GetSpriteForDirection(NPCDirection dir)
    {
        switch (dir)
        {
            case NPCDirection.Down: return spriteDown != null ? spriteDown : sr.sprite;
            case NPCDirection.Up: return spriteUp != null ? spriteUp : sr.sprite;
            case NPCDirection.Left: return spriteLeft != null ? spriteLeft : sr.sprite;
            case NPCDirection.Right: return spriteRight != null ? spriteRight : sr.sprite;
        }
        return null;
    }

    // ==================== DIALOGUE LOGIC ====================
    void StartDialogue()
    {
        dialogueActive = true;
        currentLineIndex = 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speakerName;

        ShowCurrentLine();

        if (showDebug) Debug.Log($"[NPC] Dialogue started dengan {name}");
    }

    void NextDialogue()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        // === STOP AUDIO SEBELUMNYA DULU ===
        if (stopVoiceOnNext && voiceAudioSource != null && voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();

        if (currentLineIndex >= dialogueLines.Length) return;
        DialogueLine line = dialogueLines[currentLineIndex];

        // Set text
        if (dialogueText != null)
            dialogueText.text = line.text;

        // Play audio kalau ada
        if (voiceAudioSource != null && line.voiceClip != null)
        {
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.volume = voiceVolume;
            voiceAudioSource.Play();
        }
    }

    void EndDialogue()
    {
        dialogueActive = false;
        currentLineIndex = 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // === STOP AUDIO SAAT DIALOG SELESAI ===
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();

        if (showDebug) Debug.Log("[NPC] Dialogue ended");
    }

    // ==================== GIZMOS ====================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        if (playerTransform != null)
        {
            Gizmos.color = playerNear ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}