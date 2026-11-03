
using UnityEngine;

public enum SortingMode
{
    Manual,         // Sorting order di-set manual
    YSort,          // Otomatis berdasarkan posisi Y (top-down standar)
    YSortWithOffset // Y-sort + offset manual (untuk fine-tuning)
}

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSortingController : MonoBehaviour
{
    [Header("=== SORTING MODE ===")]
    public SortingMode sortingMode = SortingMode.YSort;

    [Header("=== MANUAL MODE ===")]
    [Tooltip("Semakin tinggi angkanya, semakin ke depan (di atas objek lain)")]
    public int manualSortingOrder = 0;

    [Header("=== Y-SORT MODE ===")]
    [Tooltip("Semakin besar multiplier, semakin sensitif perbedaan Y")]
    public float ySortMultiplier = 100f;

    [Tooltip("Offset supaya objek bisa tampil lebih depan/belakang dari Y-nya")]
    public int sortingOffset = 0;

    [Tooltip("Pivot reference. Kalau sprite di tengah, pakai posisi Y. Kalau di kaki, pakai offset.")]
    public float yPivotOffset = 0f;

    [Header("=== SORTING LAYER ===")]
    [Tooltip("Layer untuk sort. Semua sprite harus pakai layer yang sama untuk Y-sort bekerja.")]
    public string sortingLayerName = "Default";

    [Header("=== UPDATE MODE ===")]
    [Tooltip("Update setiap frame? Matikan kalau objek statis untuk hemat performa")]
    public bool updateEveryFrame = true;

    [Tooltip("Update sorting saat Start?")]
    public bool updateOnStart = true;

    [Header("=== DEBUG ===")]
    public bool showDebug = false;

    private SpriteRenderer spriteRenderer;
    private int lastSortingOrder;
    private string lastSortingLayer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (updateOnStart)
            ApplySorting();
    }

    void Start()
    {
        // Apply lagi setelah Start untuk memastikan semua objek sudah ready
        ApplySorting();
    }

    void LateUpdate()
    {
        if (updateEveryFrame)
            ApplySorting();
    }

    /// <summary>
    /// Hitung dan apply sorting order berdasarkan mode yang dipilih.
    /// </summary>
    public void ApplySorting()
    {
        if (spriteRenderer == null) return;

        int newOrder = CalculateSortingOrder();
        string newLayer = GetSortingLayerName();

        // Hanya update kalau berubah (mengurangi overhead)
        if (newOrder != lastSortingOrder)
        {
            spriteRenderer.sortingOrder = newOrder;
            lastSortingOrder = newOrder;

            if (showDebug)
                Debug.Log($"[Sorting] {name} order={newOrder} (mode={sortingMode})");
        }

        if (newLayer != lastSortingLayer)
        {
            spriteRenderer.sortingLayerName = newLayer;
            lastSortingLayer = newLayer;
        }
    }

    int CalculateSortingOrder()
    {
        switch (sortingMode)
        {
            case SortingMode.Manual:
                return manualSortingOrder;

            case SortingMode.YSort:
                // Semakin rendah Y, semakin ke depan (standar top-down)
                // Y tinggi (jauh) = order rendah, Y rendah (dekat) = order tinggi
                float y = transform.position.y + yPivotOffset;
                return Mathf.RoundToInt(-y * ySortMultiplier);

            case SortingMode.YSortWithOffset:
                float yOff = transform.position.y + yPivotOffset;
                return Mathf.RoundToInt(-yOff * ySortMultiplier) + sortingOffset;

            default:
                return manualSortingOrder;
        }
    }

    string GetSortingLayerName()
    {
        return string.IsNullOrEmpty(sortingLayerName) ? "Default" : sortingLayerName;
    }

    // ================================================================
    // ==================== PUBLIC HELPERS ============================
    // ================================================================

    /// <summary>
    /// Paksa ubah sorting order (untuk cutscene, dialog, dll)
    /// </summary>
    public void SetManualOrder(int order)
    {
        sortingMode = SortingMode.Manual;
        manualSortingOrder = order;
        ApplySorting();
    }

    /// <summary>
    /// Kembali ke Y-sort setelah dipaksa manual
    /// </summary>
    public void ReturnToYSort()
    {
        sortingMode = SortingMode.YSortWithOffset;
        ApplySorting();
    }

    /// <summary>
    /// Naikkan sorting order sementara (misal player masuk rumah)
    /// </summary>
    public void AddTemporaryOffset(int offset)
    {
        sortingOffset += offset;
        ApplySorting();
    }

    /// <summary>
    /// Reset offset
    /// </summary>
    public void ResetOffset()
    {
        sortingOffset = 0;
        ApplySorting();
    }

    // ================================================================
    // ==================== CONTEXT MENU ==============================
    // ================================================================
    [ContextMenu("Force Update Sorting")]
    void ForceUpdate()
    {
        ApplySorting();
    }

    [ContextMenu("Debug Current Order")]
    void DebugOrder()
    {
        if (spriteRenderer != null)
            Debug.Log($"[Sorting] {name}: Order={spriteRenderer.sortingOrder}, Layer={spriteRenderer.sortingLayerName}");
    }
}