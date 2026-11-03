
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FarmingSystem : MonoBehaviour
{
    public static FarmingSystem Instance { get; private set; }

    // ====== Tracking berapa kali tiap scene dibuka (persist antar scene, reset saat app restart) ======
    private static readonly Dictionary<string, int> sceneOpenCounts = new Dictionary<string, int>();

    public static int GetSceneOpenCount(string sceneName)
        => sceneOpenCounts.TryGetValue(sceneName, out int v) ? v : 0;

    public static void ResetSceneOpenCounts() => sceneOpenCounts.Clear();

    [Header("=== COUNTER UI ===")]
    public TextMeshProUGUI counterText;
    public string counterFormat = "{0} / {1}";

    [Header("=== RANDOM TARGET PER SCENE-OPEN ===")]
    [Tooltip("Kalau true, target panen dipilih random sesuai jumlah scene ini sudah dibuka.")]
    public bool useRandomTarget = true;

    [Tooltip("Buka ke-1: X = min, Y = max (inklusif).")]
    public Vector2Int firstOpenRange = new Vector2Int(1, 5);

    [Tooltip("Buka ke-2: X = min, Y = max (inklusif).")]
    public Vector2Int secondOpenRange = new Vector2Int(5, 10);

    [Tooltip("Buka ke-3 dst: X = min, Y = max (inklusif).")]
    public Vector2Int thirdPlusOpenRange = new Vector2Int(10, 20);

    [Tooltip("Kalau useRandomTarget = false, pakai angka ini. 0 = otomatis pakai total plot.")]
    public int requiredHarvests = 0;

    [Header("=== WIN CONDITION ===")]
    public string nextSceneName = "Level2";
    public float nextSceneDelay = 2f;
    public UnityEvent onWin;

    [Header("=== TIMER ===")]
    public TextMeshProUGUI timerText;
    [Tooltip("Durasi timer (detik). 0 = hitung naik tanpa batas.")]
    public float timerDuration = 0f;
    [Tooltip("Format: {0} = menit, {1} = detik")]
    public string timerFormat = "{0:00}:{1:00}";
    [Tooltip("Scene yang diload saat waktu habis. Kosong = hanya invoke event.")]
    public string timeUpSceneName = "";
    public UnityEvent onTimeUp;

    [Header("=== JUMPSCARE / REGRESSION ===")]
    [Tooltip("Panel/Image jumpscare di Canvas. Otomatis disembunyikan saat Start.")]
    public GameObject jumpscarePanel;

    [Range(0f, 100f)]
    [Tooltip("Peluang jumpscare muncul tiap cek (persen).")]
    public float jumpscareChance = 20f;

    [Tooltip("Interval antar cek (detik). X = min, Y = max.")]
    public Vector2 jumpscareIntervalRange = new Vector2(10f, 20f);

    [Tooltip("Berapa lama jumpscare tampil (detik).")]
    public float jumpscareDuration = 2f;

    [Tooltip("Kalau true, semua plot di-reset ke Empty saat jumpscare muncul.")]
    public bool jumpscareResetsPlots = true;

    [Header("=== DEBUG ===")]
    public bool showDebug = true;

    // ---- Runtime ----
    private int harvestedCount;
    private int totalPlots;
    private int targetCount;
    private bool finished;

    private FarmPlot[] plots;

    private bool timerRunning;
    private float timerValue;

    private bool jumpscareEnabled;
    private bool jumpscareShowing;
    private float jumpscareTimer;
    private float jumpscareCheckTimer;
    private float nextJumpscareCheck;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        plots = FindObjectsOfType<FarmPlot>();
        totalPlots = plots.Length;
        harvestedCount = 0;
        finished = false;

        // === Hitung open count untuk scene ini ===
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneOpenCounts.ContainsKey(sceneName)) sceneOpenCounts[sceneName] = 0;
        sceneOpenCounts[sceneName]++;
        int openCount = sceneOpenCounts[sceneName];

        // === Tentukan target ===
        if (useRandomTarget)
        {
            if (openCount <= 1) targetCount = RollRange(firstOpenRange);
            else if (openCount == 2) targetCount = RollRange(secondOpenRange);
            else targetCount = RollRange(thirdPlusOpenRange);
        }
        else
        {
            targetCount = requiredHarvests > 0 ? requiredHarvests : totalPlots;
        }

        // === Jumpscare setup ===
        jumpscareEnabled = (jumpscarePanel != null) && (jumpscareChance > 0f);
        if (jumpscarePanel != null) jumpscarePanel.SetActive(false);
        if (jumpscareEnabled) ScheduleNextJumpscareCheck();

        // === Timer setup ===
        timerRunning = true;
        timerValue = timerDuration > 0f ? timerDuration : 0f;

        UpdateCounterUI();
        UpdateTimerUI();

        if (showDebug)
            Debug.Log($"[FarmingSystem] Scene '{sceneName}' dibuka {openCount}x. " +
                      $"Target: {targetCount} (total plot: {totalPlots})");
    }

    void Update()
    {
        UpdateTimer();
        UpdateJumpscare();
    }

    // ==================== TARGET ====================
    int RollRange(Vector2Int r)
    {
        int min = Mathf.Min(r.x, r.y);
        int max = Mathf.Max(r.x, r.y);
        return Random.Range(min, max + 1); // inklusif
    }

    // ==================== TIMER ====================
    void UpdateTimer()
    {
        if (!timerRunning || finished) return;

        if (timerDuration > 0f) // countdown
        {
            timerValue -= Time.deltaTime;
            if (timerValue <= 0f)
            {
                timerValue = 0f;
                timerRunning = false;
                UpdateTimerUI();
                OnTimeUp();
                return;
            }
        }
        else // count-up
        {
            timerValue += Time.deltaTime;
        }
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(timerValue / 60f);
        int s = Mathf.FloorToInt(timerValue % 60f);
        timerText.text = string.Format(timerFormat, m, s);
    }

    void OnTimeUp()
    {
        if (showDebug) Debug.Log("[FarmingSystem] Time up!");
        onTimeUp?.Invoke();
        if (!string.IsNullOrEmpty(timeUpSceneName))
            SceneManager.LoadScene(timeUpSceneName);
    }

    // ==================== JUMPSCARE / REGRESSION ====================
    void UpdateJumpscare()
    {
        if (!jumpscareEnabled || finished) return;

        if (jumpscareShowing)
        {
            jumpscareTimer -= Time.deltaTime;
            if (jumpscareTimer <= 0f)
            {
                jumpscareShowing = false;
                jumpscarePanel.SetActive(false);
                ScheduleNextJumpscareCheck();
            }
            return;
        }

        jumpscareCheckTimer += Time.deltaTime;
        if (jumpscareCheckTimer >= nextJumpscareCheck)
        {
            jumpscareCheckTimer = 0f;
            if (Random.Range(0f, 100f) < jumpscareChance) TriggerJumpscare();
            else ScheduleNextJumpscareCheck();
        }
    }

    void ScheduleNextJumpscareCheck()
    {
        jumpscareCheckTimer = 0f;
        float min = Mathf.Min(jumpscareIntervalRange.x, jumpscareIntervalRange.y);
        float max = Mathf.Max(jumpscareIntervalRange.x, jumpscareIntervalRange.y);
        nextJumpscareCheck = (Mathf.Approximately(min, max)) ? min : Random.Range(min, max);
    }

    public void TriggerJumpscare()
    {
        if (!jumpscareEnabled) return;
        jumpscareShowing = true;
        jumpscareTimer = jumpscareDuration;
        jumpscarePanel.SetActive(true);

        if (showDebug) Debug.Log("[FarmingSystem] JUMPSCARE! Reset semua plot...");
        if (jumpscareResetsPlots) TriggerRegression();
    }

    public void TriggerRegression()
    {
        foreach (var p in plots)
            if (p != null) p.ResetPlot(); // FarmPlot.ResetPlot() sudah ada, tidak perlu diubah
    }

    // ==================== HARVEST ====================
    public void OnPlotHarvested(FarmPlot plot)
    {
        harvestedCount++;
        UpdateCounterUI();

        if (showDebug) Debug.Log($"[FarmingSystem] Harvested: {harvestedCount}/{targetCount}");

        if (harvestedCount >= targetCount && !finished)
        {
            finished = true;
            timerRunning = false;
            if (showDebug) Debug.Log("[FarmingSystem] Target reached!");
            onWin?.Invoke();
            Invoke(nameof(LoadNextScene), nextSceneDelay);
        }
    }

    void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = string.Format(counterFormat, harvestedCount, targetCount);
    }

    void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[FarmingSystem] nextSceneName kosong!");
            return;
        }
        if (showDebug) Debug.Log($"[FarmingSystem] Load scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    // ==================== GETTERS ====================
    public int GetHarvestedCount() => harvestedCount;
    public int GetTotalPlots() => totalPlots;
    public int GetTargetCount() => targetCount;
    public bool IsComplete() => harvestedCount >= targetCount;
    public float GetTimerValue() => timerValue;
    public bool IsJumpscareShowing() => jumpscareShowing;

    [ContextMenu("Test: Trigger Jumpscare Sekarang")]
    void DebugTriggerJumpscare() { jumpscareEnabled = jumpscarePanel != null; TriggerJumpscare(); }
}