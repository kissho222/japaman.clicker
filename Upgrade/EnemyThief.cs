using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// 敵による盗難システム
/// プレート上のアイテムをランダムに盗む
/// </summary>
public class EnemyThief : MonoBehaviour
{
    [Header("盗難設定")]
    public float stealInterval = 10f;           // 盗難間隔（秒）
    public int maxStealCount = 3;               // 一回に盗む最大数
    public float stealChance = 0.3f;            // 盗難発生確率

    [Header("敵の動作")]
    public float moveSpeed = 200f;              // 移動速度
    public Vector3 spawnPosition = new Vector3(-300, 0, 0);  // 出現位置
    public Vector3 exitPosition = new Vector3(300, 0, 0);   // 退場位置

    [Header("視覚・音響効果")]
    public AudioClip stealSound;                // 盗難音
    public GameObject stealEffect;              // 盗難エフェクト

    // 内部状態
    private bool isActive = false;
    private bool isStealActive = false;
    private Coroutine stealCoroutine;

    // 参照
    private AudioSource audioSource;
    private ClickManagerAdapter clickManagerAdapter;

    // 統計
    private int totalStolenItems = 0;
    private float totalStolenValue = 0f;

    public static EnemyThief Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        clickManagerAdapter = FindFirstObjectByType<ClickManagerAdapter>();

        // 初期位置設定
        transform.position = spawnPosition;

        Debug.Log("💀 EnemyThief初期化完了");
    }

    /// <summary>
    /// 盗難システム開始
    /// </summary>
    public void StartStealSystem()
    {
        if (isActive) return;

        isActive = true;
        if (stealCoroutine != null)
        {
            StopCoroutine(stealCoroutine);
        }

        stealCoroutine = StartCoroutine(StealLoop());
        Debug.Log("💀 敵の盗難システム開始");
    }

    /// <summary>
    /// 盗難システム停止
    /// </summary>
    public void StopStealSystem()
    {
        isActive = false;

        if (stealCoroutine != null)
        {
            StopCoroutine(stealCoroutine);
            stealCoroutine = null;
        }

        Debug.Log("💀 敵の盗難システム停止");
    }

    /// <summary>
    /// 盗難ループ
    /// </summary>
    private IEnumerator StealLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(stealInterval);

            if (isActive && !isStealActive)
            {
                // ゲーム状態チェック
                if (GameManager.Instance != null && GameManager.Instance.CanAutoProduction())
                {
                    if (Random.Range(0f, 1f) < stealChance)
                    {
                        yield return StartCoroutine(ExecuteSteal());
                    }
                }
            }
        }
    }

    /// <summary>
    /// 盗難実行
    /// </summary>
    private IEnumerator ExecuteSteal()
    {
        if (clickManagerAdapter == null || isStealActive) yield break;

        isStealActive = true;

        // 盗める対象を取得
        var stealableItems = clickManagerAdapter.GetStealableItems();
        if (stealableItems.Length == 0)
        {
            isStealActive = false;
            yield break;
        }

        Debug.Log($"💀 盗難開始: {stealableItems.Length}個の対象を発見");

        // 敵が画面に登場
        yield return StartCoroutine(EnemyAppearance());

        // 盗難対象を選択
        int stealCount = Mathf.Min(maxStealCount, stealableItems.Length);
        var selectedItems = stealableItems.OrderBy(x => Random.value).Take(stealCount).ToArray();

        // 盗難実行
        foreach (var item in selectedItems)
        {
            if (item != null && !item.IsBeingStolen)
            {
                StealSingleItem(item);
                yield return new WaitForSeconds(0.3f);
            }
        }

        // 盗難完了待機
        yield return new WaitForSeconds(1f);

        // 敵が退場
        yield return StartCoroutine(EnemyExit());

        isStealActive = false;
    }

    /// <summary>
    /// 敵の登場アニメーション
    /// </summary>
    private IEnumerator EnemyAppearance()
    {
        Vector3 targetPos = Vector3.zero; // プレート付近
        float duration = 1f;
        float elapsed = 0f;

        Vector3 startPos = spawnPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        transform.position = targetPos;
        Debug.Log("💀 敵登場完了");
    }

    /// <summary>
    /// 敵の退場アニメーション
    /// </summary>
    private IEnumerator EnemyExit()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = exitPosition;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        transform.position = spawnPosition; // 次回用にリセット
        Debug.Log("💀 敵退場完了");
    }

    /// <summary>
    /// 個別アイテム盗難
    /// </summary>
    private void StealSingleItem(PlateItem item)
    {
        if (item == null) return;

        // 統計更新
        totalStolenItems++;
        totalStolenValue += item.Value;

        // 盗難エフェクト再生
        PlayStealEffects(item.transform.position);

        // アイテムを盗む
        item.BeStolen(transform);

        Debug.Log($"💀 アイテム盗難: {item.ItemType} (価値: {item.Value})");
    }

    /// <summary>
    /// 盗難エフェクト再生
    /// </summary>
    private void PlayStealEffects(Vector3 position)
    {
        // 盗難エフェクト
        if (stealEffect != null)
        {
            var effect = Instantiate(stealEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 盗難音再生
        if (stealSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(stealSound);
        }
    }

    /// <summary>
    /// 盗難設定の調整
    /// </summary>
    public void SetStealSettings(float interval, int maxCount, float chance)
    {
        stealInterval = interval;
        maxStealCount = maxCount;
        stealChance = chance;

        Debug.Log($"💀 盗難設定更新: 間隔{interval}秒, 最大{maxCount}個, 確率{chance * 100}%");
    }

    /// <summary>
    /// 盗難統計取得
    /// </summary>
    public StealStatistics GetStealStatistics()
    {
        return new StealStatistics
        {
            totalStolenItems = totalStolenItems,
            totalStolenValue = totalStolenValue,
            isActive = isActive,
            currentStealInterval = stealInterval,
            currentStealChance = stealChance
        };
    }

    /// <summary>
    /// デバッグ用：盗難統計表示
    /// </summary>
    [ContextMenu("💀 盗難統計表示")]
    public void DebugShowStealStatistics()
    {
        var stats = GetStealStatistics();
        Debug.Log("=== 敵盗難統計 ===");
        Debug.Log($"盗まれたアイテム数: {stats.totalStolenItems}個");
        Debug.Log($"盗まれた総価値: {stats.totalStolenValue}");
        Debug.Log($"システム状態: {(stats.isActive ? "稼働中" : "停止中")}");
        Debug.Log($"盗難間隔: {stats.currentStealInterval}秒");
        Debug.Log($"盗難確率: {stats.currentStealChance * 100}%");
    }

    /// <summary>
    /// デバッグ用：手動盗難実行
    /// </summary>
    [ContextMenu("💀 手動盗難実行")]
    public void DebugManualSteal()
    {
        if (Application.isPlaying && !isStealActive)
        {
            StartCoroutine(ExecuteSteal());
        }
    }

    /// <summary>
    /// 新ステージ用リセット
    /// </summary>
    public void ResetForNewStage()
    {
        StopStealSystem();
        transform.position = spawnPosition;

        // 統計はリセットしない（累積）
        Debug.Log("💀 EnemyThief: 新ステージ用リセット完了");
    }

    private void OnDestroy()
    {
        StopStealSystem();
    }
}

/// <summary>
/// 盗難統計情報
/// </summary>
[System.Serializable]
public class StealStatistics
{
    public int totalStolenItems;
    public float totalStolenValue;
    public bool isActive;
    public float currentStealInterval;
    public float currentStealChance;
}