
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum PlotState { Empty, Planted, Growing, Ready }

[RequireComponent(typeof(SpriteRenderer))]
public class FarmPlot : MonoBehaviour
{
    [Header("=== SPRITES PER STATE ===")]
    public Sprite emptySprite;
    public Sprite plantedSprite;
    public Sprite growingSprite;
    public Sprite readySprite;

    [Header("=== GROWTH TIMING (detik) ===")]
    public float timeToGrowing = 5f;
    public float timeToReady = 5f;

    [Header("=== PLAYER ===")]
    [Tooltip("Drag & drop GameObject player ke sini")]
    public Transform playerTransform;

    [Tooltip("Radius deteksi player dari plot")]
    public float detectRadius = 1.5f;

    [Header("=== HIGHLIGHT ===")]
    public Color highlightColor = new Color(1f, 1f, 0.6f, 1f);

    [Header("=== EVENTS ===")]
    public UnityEvent onPlanted;
    public UnityEvent onHarvested;

    [Header("=== DEBUG ===")]
    public bool showDebug = false;

    // Internal
    private SpriteRenderer sr;
    private PlotState state = PlotState.Empty;
    private float growthTimer;
    private bool playerNear;
    private Color originalColor;

    public bool CanPlant => state == PlotState.Empty;
    public bool CanHarvest => state == PlotState.Ready;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    void Start()
    {
        ApplySprite();

        // Kalau playerTransform belum di-assign, coba auto-find via tag (opsional)
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                if (showDebug) Debug.Log("[Plot] Player auto-found via tag");
            }
            else
            {
                Debug.LogWarning($"[Plot] {name}: Player belum di-assign & tidak ada tag 'Player'!");
            }
        }
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

        // ===== 2. HANDLE INPUT E =====
        if (playerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (CanPlant) Plant();
            else if (CanHarvest) Harvest();
        }

        // ===== 3. GROWTH TIMER =====
        if (state == PlotState.Planted || state == PlotState.Growing)
        {
            growthTimer += Time.deltaTime;

            if (state == PlotState.Planted && growthTimer >= timeToGrowing)
            {
                SetState(PlotState.Growing);
                growthTimer = 0f;
            }
            else if (state == PlotState.Growing && growthTimer >= timeToReady)
            {
                SetState(PlotState.Ready);
                growthTimer = 0f;
            }
        }

        // ===== 4. HIGHLIGHT =====
        bool shouldHighlight = playerNear && (CanPlant || CanHarvest);
        Color target = shouldHighlight ? highlightColor : originalColor;
        sr.color = Color.Lerp(sr.color, target, 8f * Time.deltaTime);
    }

    // ==================== ACTIONS ====================
    void Plant()
    {
        if (!CanPlant) return;
        SetState(PlotState.Planted);
        growthTimer = 0f;
        onPlanted?.Invoke();

        if (showDebug) Debug.Log($"[Plot] {name} ditanam!");
    }

    void Harvest()
    {
        if (!CanHarvest) return;
        SetState(PlotState.Empty);
        onHarvested?.Invoke();

        if (FarmingSystem.Instance != null)
            FarmingSystem.Instance.OnPlotHarvested(this);

        if (showDebug) Debug.Log($"[Plot] {name} dipanen!");
    }

    void SetState(PlotState newState)
    {
        state = newState;
        ApplySprite();
    }

    void ApplySprite()
    {
        switch (state)
        {
            case PlotState.Empty: if (emptySprite) sr.sprite = emptySprite; break;
            case PlotState.Planted: if (plantedSprite) sr.sprite = plantedSprite; break;
            case PlotState.Growing: if (growingSprite) sr.sprite = growingSprite; break;
            case PlotState.Ready: if (readySprite) sr.sprite = readySprite; break;
        }
    }

    // ==================== RESET ====================
    public void ResetPlot()
    {
        SetState(PlotState.Empty);
        growthTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}