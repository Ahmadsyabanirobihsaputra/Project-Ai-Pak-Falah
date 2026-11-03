using System.Collections;

using UnityEngine;

public enum WallType
{
    Static,         // Dinding biasa, tidak bisa dihancurkan
    Destructible,   // Bisa dihancurkan dengan damage
    Toggleable,     // Bisa muncul/hilang (time loop mechanic)
    Timed           // Muncul/hilang otomatis setiap X detik
}

[RequireComponent(typeof(Collider))]
public class WallController : MonoBehaviour
{
    [Header("=== WALL TYPE ===")]
    public WallType wallType = WallType.Static;

    [Header("=== HEALTH (Destructible) ===")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("=== SPRITES ===")]
    [Tooltip("Sprite saat wall utuh")]
    public Sprite intactSprite;

    [Tooltip("Sprite saat wall rusak (di bawah 50% health)")]
    public Sprite damagedSprite;

    [Tooltip("Sprite saat wall hancur")]
    public Sprite destroyedSprite;

    [Tooltip("SpriteRenderer target (auto-find jika kosong)")]
    public SpriteRenderer spriteRenderer;

    [Header("=== TOGGLE (Time Loop) ===")]
    [Tooltip("State awal saat game mulai")]
    public bool startsActive = true;

    [Tooltip("Interval toggle otomatis (0 = tidak auto-toggle)")]
    public float autoToggleInterval = 0f;

    [Header("=== FADE (Player Behind Wall) ===")]
    [Tooltip("Aktifkan fade saat player di belakang wall")]
    public bool fadeWhenPlayerBehind = true;

    [Tooltip("Alpha saat player di belakang (0-1)")]
    [Range(0f, 1f)]
    public float fadedAlpha = 0.3f;

    [Tooltip("Kecepatan transisi fade")]
    public float fadeSpeed = 5f;

    [Tooltip("Referensi ke player (auto-find jika kosong)")]
    public Transform player;

    [Header("=== EVENTS ===")]
    public UnityEngine.Events.UnityEvent onWallDestroyed;
    public UnityEngine.Events.UnityEvent onWallToggledOn;
    public UnityEngine.Events.UnityEvent onWallToggledOff;

    [Header("=== DEBUG ===")]
    public bool showDebug = false;

    // Internal
    private Collider wallCollider;
    private bool isActive;
    private Color originalColor;
    private bool playerBehind;
    private float toggleTimer;
    private bool isDestroyed;

    void Start()
    {
        wallCollider = GetComponent<Collider>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        currentHealth = maxHealth;
        isActive = startsActive;
        ApplyState(isActive);

        // Set sprite awal
        if (spriteRenderer != null && intactSprite != null)
            spriteRenderer.sprite = intactSprite;

        if (wallType == WallType.Timed && autoToggleInterval > 0f)
            toggleTimer = autoToggleInterval;
    }

    void Update()
    {
        // Auto-toggle (untuk Timed wall)
        if (wallType == WallType.Timed && autoToggleInterval > 0f)
        {
            toggleTimer -= Time.deltaTime;
            if (toggleTimer <= 0f)
            {
                ToggleWall();
                toggleTimer = autoToggleInterval;
            }
        }

        // Fade logic
        if (fadeWhenPlayerBehind && spriteRenderer != null && !isDestroyed)
            UpdateFade();
    }

    // ================================================================
    // ==================== FADE LOGIC ================================
    // ================================================================
    void UpdateFade()
    {
        if (player == null) return;

        // Cek apakah player ada di "belakang" wall
        // Wall dianggap menghalangi kalau player di Y lebih rendah dan X masih dalam jangkauan
        bool isBehind = CheckPlayerBehind();

        if (isBehind != playerBehind)
        {
            playerBehind = isBehind;
            if (showDebug)
                Debug.Log($"[Wall] PlayerBehind={playerBehind}");
        }

        // Smooth alpha transition
        float targetAlpha = playerBehind ? fadedAlpha : 1f;
        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
        spriteRenderer.color = c;
    }

    bool CheckPlayerBehind()
    {
        if (player == null) return false;

        // Player dianggap "behind" kalau:
        // 1. Posisi Z atau Y-nya lebih jauh dari wall (tergantung orientasi)
        // 2. Masih dalam rentang X wall

        Vector3 wallPos = transform.position;
        Vector3 playerPos = player.position;

        // Untuk top-down 2.5D: cek player di atas wall (Y lebih tinggi)
        float yDiff = playerPos.y - wallPos.y;
        float xDiff = Mathf.Abs(playerPos.x - wallPos.x);

        // Kalau player di atas wall (yDiff > 0) dan dalam jangkauan X
        // Kalau wall pakai sprite, ambil lebar sprite
        float wallWidth = 1f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            wallWidth = spriteRenderer.sprite.bounds.size.x * transform.lossyScale.x;

        bool behindY = yDiff > 0.1f; // Player lebih tinggi dari wall
        bool inXRange = xDiff < wallWidth * 0.7f; // Setengah lebar wall

        return behindY && inXRange;
    }

    // ================================================================
    // ==================== DAMAGE SYSTEM =============================
    // ================================================================
    public void TakeDamage(float amount)
    {
        if (wallType != WallType.Destructible) return;
        if (isDestroyed) return;

        currentHealth -= amount;
        if (showDebug)
            Debug.Log($"[Wall] Damage={amount}, HP={currentHealth}/{maxHealth}");

        UpdateDamageSprite();

        if (currentHealth <= 0f)
            DestroyWall();
    }

    void UpdateDamageSprite()
    {
        if (spriteRenderer == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent > 0.5f)
        {
            if (intactSprite != null) spriteRenderer.sprite = intactSprite;
        }
        else
        {
            if (damagedSprite != null) spriteRenderer.sprite = damagedSprite;
        }
    }

    void DestroyWall()
    {
        isDestroyed = true;
        currentHealth = 0f;

        if (spriteRenderer != null && destroyedSprite != null)
            spriteRenderer.sprite = destroyedSprite;

        // Matikan collider
        if (wallCollider != null)
            wallCollider.enabled = false;

        // Fade out sprite
        StartCoroutine(FadeOutAndDisable(0.5f));

        onWallDestroyed?.Invoke();

        if (showDebug)
            Debug.Log("[Wall] Destroyed!");
    }

    IEnumerator FadeOutAndDisable(float duration)
    {
        if (spriteRenderer == null) yield break;

        float t = 0f;
        Color c = spriteRenderer.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / duration);
            spriteRenderer.color = c;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // ================================================================
    // ==================== TOGGLE SYSTEM =============================
    // ================================================================
    public void ToggleWall()
    {
        isActive = !isActive;
        ApplyState(isActive);
    }

    public void SetActive(bool active)
    {
        if (isActive == active) return;
        isActive = active;
        ApplyState(isActive);
    }

    void ApplyState(bool active)
    {
        if (wallCollider != null)
            wallCollider.enabled = active;

        if (spriteRenderer != null)
            spriteRenderer.enabled = active;

        if (active)
            onWallToggledOn?.Invoke();
        else
            onWallToggledOff?.Invoke();

        if (showDebug)
            Debug.Log($"[Wall] Active={active}");
    }

    // ================================================================
    // ==================== TIME LOOP RESET ===========================
    // ================================================================
    /// <summary>
    /// Panggil ini saat time loop reset untuk mengembalikan wall ke state awal.
    /// </summary>
    public void ResetWall()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
        isActive = startsActive;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = intactSprite;
            spriteRenderer.color = originalColor;
        }

        if (wallCollider != null)
            wallCollider.enabled = true;

        gameObject.SetActive(true);

        if (showDebug)
            Debug.Log("[Wall] Reset to initial state");
    }

    // ================================================================
    // ==================== GIZMOS ====================================
    // ================================================================
    void OnDrawGizmosSelected()
    {
        // Visualisasi area fade detection
        if (fadeWhenPlayerBehind && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            float wallWidth = spriteRenderer.sprite.bounds.size.x * transform.lossyScale.x;
            Gizmos.DrawWireCube(
                transform.position + Vector3.up * 2f,
                new Vector3(wallWidth, 4f, 1f)
            );
        }
    }
}