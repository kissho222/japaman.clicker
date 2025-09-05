using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// プレート上のアイテム管理システム（新設計）
/// 複数種類のアイテムを統合管理し、拡張性を確保
/// </summary>
public class PlateItemSystem : MonoBehaviour
{
    [Header("プレート設定")]
    public Transform plateContainer;
    public RectTransform plateImage;

    [Header("アイテム設定")]
    public PlateItemPrefabData[] itemPrefabs;

    [Header("統合システム設定")]
    public bool enableAutoConsolidation = true;    // 自動統合
    public int consolidationThreshold = 100;       // 統合閾値
    public float consolidationDelay = 2f;          // 統合遅延時間

    [Header("エフェクト設定")]
    public GameObject consolidationEffect;
    public AudioClip consolidationSound;

    // アイテム管理
    private List<PlateItem> plateItems = new List<PlateItem>();
    private Dictionary<PlateItemType, int> itemCounts = new Dictionary<PlateItemType, int>();

    // 統計
    private long totalValue = 0;
    private int totalItemCount = 0;

    // 参照
    private AudioSource audioSource;
    private ClickManager clickManager;

    public static PlateItemSystem Instance { get; private set; }

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
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        clickManager = FindFirstObjectByType<ClickManager>();

        // プレート情報の自動取得
        if (plateContainer == null && clickManager != null)
        {
            // ClickManagerから取得を試行
            var plateContainerField = typeof(ClickManager).GetField("plateContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (plateContainerField != null)
            {
                plateContainer = plateContainerField.GetValue(clickManager) as Transform;
            }
        }

        // プレート画像の取得
        if (plateImage == null)
        {
            // 🔥 タグではなく名前で検索
            var plateObject = GameObject.Find("Plate");
            if (plateObject == null)
            {
                plateObject = GameObject.Find("PlateImage");
            }
            if (plateObject == null)
            {
                plateObject = GameObject.Find("plateImage");
            }

            if (plateObject != null)
            {
                plateImage = plateObject.GetComponent<RectTransform>();
                Debug.Log($"🍽️ プレート画像発見: {plateObject.name}");
            }
            else
            {
                Debug.LogWarning("🍽️ プレート画像が見つかりません");
            }
        }

        InitializeItemTypes();
        Debug.Log("🍽️ PlateItemSystem初期化完了");
    }

    /// <summary>
    /// アイテム種類の初期化
    /// </summary>
    private void InitializeItemTypes()
    {
        foreach (PlateItemType itemType in System.Enum.GetValues(typeof(PlateItemType)))
        {
            itemCounts[itemType] = 0;
        }
    }

    /// <summary>
    /// アイテムをプレートに追加
    /// </summary>
    public void AddItemToPlate(PlateItemType itemType, float value, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            CreatePlateItem(itemType, value);
        }

        // 統合チェック
        if (enableAutoConsolidation)
        {
            CheckAndTriggerConsolidation();
        }

        UpdateStatistics();
        Debug.Log($"🍽️ アイテム追加: {itemType} x{count} (価値: {value})");
    }

    /// <summary>
    /// プレートアイテムを生成
    /// </summary>
    private void CreatePlateItem(PlateItemType itemType, float value)
    {
        var prefabData = GetPrefabData(itemType);
        if (prefabData == null || prefabData.prefab == null)
        {
            Debug.LogWarning($"プレファブが見つかりません: {itemType}");
            return;
        }

        // アイテム生成
        GameObject itemObj = Instantiate(prefabData.prefab, plateContainer);

        // PlateItemコンポーネント設定
        var plateItem = itemObj.GetComponent<PlateItem>();
        if (plateItem == null)
        {
            plateItem = itemObj.AddComponent<PlateItem>();
        }

        plateItem.Initialize(itemType, value, this);

        // 位置設定
        SetupItemPosition(itemObj);

        // リストに追加
        plateItems.Add(plateItem);
        itemCounts[itemType]++;

        // 落下アニメーション開始
        var animator = itemObj.GetComponent<PlateJapamanAnimator>();
        if (animator == null)
        {
            animator = itemObj.AddComponent<PlateJapamanAnimator>();
        }
        animator.StartGravityFall();
    }

    /// <summary>
    /// アイテムの位置設定
    /// </summary>
    private void SetupItemPosition(GameObject item)
    {
        // ContainerSettingsを使用した位置計算
        ContainerData containerData = null;
        if (ContainerSettings.Instance != null)
        {
            containerData = ContainerSettings.Instance.GetCurrentContainerData();
        }

        float containerRadius = GetContainerRadius(containerData);

        float centerBias = containerData?.centerBias ?? 0.1f;
        float maxRadius = containerData?.maxRadius ?? 0.8f;
        float minDropHeight = containerData?.minDropHeight ?? 300f;
        float maxDropHeight = containerData?.maxDropHeight ?? 500f;

        float startRadius = Random.Range(containerRadius * centerBias, containerRadius * maxRadius);
        float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector2 targetPos = new Vector2(
            Mathf.Cos(startAngle) * startRadius,
            0f
        );

        Vector2 startPos = new Vector2(targetPos.x, Random.Range(minDropHeight, maxDropHeight));

        var rt = item.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = startPos;
        }
    }

    /// <summary>
    /// 器の半径を取得
    /// </summary>
    private float GetContainerRadius(ContainerData containerData)
    {
        float containerRadius = 50f;

        if (plateImage != null)
        {
            Vector3 scale = plateImage.lossyScale;
            float width = plateImage.rect.width * scale.x;
            float multiplier = containerData?.sizeMultiplier ?? 0.8f;
            containerRadius = (width * multiplier) / 2f;
        }

        return containerRadius;
    }

    /// <summary>
    /// 統合チェックと実行
    /// </summary>
    private void CheckAndTriggerConsolidation()
    {
        // 通常ジャパまんが閾値を超えた場合
        if (itemCounts[PlateItemType.NormalJapaman] >= consolidationThreshold)
        {
            StartCoroutine(ConsolidateItems(PlateItemType.NormalJapaman, PlateItemType.SilverJapaman));
        }

        // 銀ジャパまんが閾値を超えた場合
        if (itemCounts[PlateItemType.SilverJapaman] >= consolidationThreshold)
        {
            StartCoroutine(ConsolidateItems(PlateItemType.SilverJapaman, PlateItemType.GoldJapaman));
        }
    }

    /// <summary>
    /// アイテム統合処理
    /// </summary>
    private System.Collections.IEnumerator ConsolidateItems(PlateItemType fromType, PlateItemType toType)
    {
        yield return new WaitForSeconds(consolidationDelay);

        var itemsToConsolidate = plateItems.Where(item =>
            item.ItemType == fromType && item.gameObject != null).Take(consolidationThreshold).ToList();

        if (itemsToConsolidate.Count < consolidationThreshold)
        {
            yield break;
        }

        // 統合価値計算
        float totalConsolidationValue = itemsToConsolidate.Sum(item => item.Value);

        // エフェクト再生
        PlayConsolidationEffect(itemsToConsolidate[0].transform.position);

        // 元のアイテムを削除
        foreach (var item in itemsToConsolidate)
        {
            RemoveItem(item);
        }

        // 新しいアイテムを生成
        yield return new WaitForSeconds(0.5f);
        CreatePlateItem(toType, totalConsolidationValue);

        Debug.Log($"🔄 アイテム統合: {fromType} x{consolidationThreshold} → {toType} x1 (価値: {totalConsolidationValue})");
    }

    /// <summary>
    /// 統合エフェクト再生
    /// </summary>
    private void PlayConsolidationEffect(Vector3 position)
    {
        if (consolidationEffect != null)
        {
            var effect = Instantiate(consolidationEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (consolidationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(consolidationSound);
        }
    }

    /// <summary>
    /// アイテムを削除
    /// </summary>
    public void RemoveItem(PlateItem item)
    {
        if (item == null) return;

        plateItems.Remove(item);
        if (itemCounts.ContainsKey(item.ItemType))
        {
            itemCounts[item.ItemType]--;
        }

        if (item.gameObject != null)
        {
            Destroy(item.gameObject);
        }

        UpdateStatistics();
    }

    /// <summary>
    /// 全アイテムを吸い込み
    /// </summary>
    public void SuckAllItems()
    {
        StartCoroutine(SuckAllItemsCoroutine());
    }

    /// <summary>
    /// 吸い込み処理
    /// </summary>
    private System.Collections.IEnumerator SuckAllItemsCoroutine()
    {
        // 残り時間に応じた速度調整
        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateSuckSpeedMultiplier(remainingTime);

        // キャラクターアニメーション開始
        if (CharacterSwallowAnimator.Instance != null)
        {
            float swallowSpeed = 3f * speedMultiplier;
            int swallowCount = Mathf.Max(1, Mathf.RoundToInt(5f / speedMultiplier));
            CharacterSwallowAnimator.Instance.SetSwallowSettings(20f, swallowSpeed, swallowCount);
            CharacterSwallowAnimator.Instance.StartSwallowAnimation();
        }

        // 全アイテムを順次吸い込み
        var itemsToSuck = plateItems.ToList();
        foreach (var item in itemsToSuck)
        {
            if (item != null && item.gameObject != null)
            {
                // 価値をClickManagerに加算
                if (clickManager != null)
                {
                    clickManager.AddJapamanFromPan(item.Value);
                }

                // 吸い込みアニメーション
                StartCoroutine(SuckSingleItem(item, speedMultiplier));
                yield return new WaitForSeconds(0.05f / speedMultiplier);
            }
        }

        yield return new WaitForSeconds(0.6f / speedMultiplier);

        // アニメーション終了
        if (CharacterSwallowAnimator.Instance != null)
        {
            CharacterSwallowAnimator.Instance.StopSwallowAnimation();
        }

        // リストクリア
        plateItems.Clear();
        InitializeItemTypes();
        UpdateStatistics();
    }

    /// <summary>
    /// 個別アイテム吸い込み
    /// </summary>
    private System.Collections.IEnumerator SuckSingleItem(PlateItem item, float speedMultiplier)
    {
        if (item == null || item.gameObject == null) yield break;

        // 吸い込み先の位置計算
        var friendsMouthTarget = GameObject.Find("FriendsMouthTarget");
        if (friendsMouthTarget == null) yield break;

        Vector2 targetPos = GetLocalPositionInContainer(friendsMouthTarget.transform.position);

        // アニメーション処理は既存のランダム軌道システムを使用
        var animator = item.GetComponent<PlateJapamanAnimator>();
        if (animator != null)
        {
            float duration = 0.4f / speedMultiplier;
            yield return StartCoroutine(AnimateToPosition(item.gameObject, targetPos, duration));
        }

        yield return new WaitForSeconds(0.3f / speedMultiplier);

        // フェードアウト
        yield return StartCoroutine(FadeOutItem(item.gameObject, speedMultiplier));
    }

    /// <summary>
    /// アイテムをフェードアウト
    /// </summary>
    private System.Collections.IEnumerator FadeOutItem(GameObject item, float speedMultiplier)
    {
        var image = item.GetComponent<UnityEngine.UI.Image>();
        if (image == null) yield break;

        Color startColor = image.color;
        Vector3 startScale = item.transform.localScale;
        float duration = 0.2f / speedMultiplier;
        float elapsed = 0f;

        while (elapsed < duration && item != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (image != null)
                image.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), progress);

            item.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);

            yield return null;
        }

        if (item != null)
        {
            Destroy(item);
        }
    }

    /// <summary>
    /// コンテナ内のローカル座標を取得
    /// </summary>
    private Vector2 GetLocalPositionInContainer(Vector3 worldPosition)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            plateContainer.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(null, worldPosition),
            null,
            out localPos
        );
        return localPos;
    }

    /// <summary>
    /// 位置アニメーション
    /// </summary>
    private System.Collections.IEnumerator AnimateToPosition(GameObject item, Vector2 targetPos, float duration)
    {
        var rectTransform = item.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration && item != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
            }

            yield return null;
        }
    }

    /// <summary>
    /// プレファブデータを取得
    /// </summary>
    private PlateItemPrefabData GetPrefabData(PlateItemType itemType)
    {
        return itemPrefabs?.FirstOrDefault(data => data.itemType == itemType);
    }

    /// <summary>
    /// 統計更新
    /// </summary>
    private void UpdateStatistics()
    {
        totalValue = (long)plateItems.Sum(item => item?.Value ?? 0);
        totalItemCount = plateItems.Count;
    }

    /// <summary>
    /// 新ステージ用リセット
    /// </summary>
    public void ResetForNewStage()
    {
        // 全アイテム削除
        foreach (var item in plateItems.ToList())
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        plateItems.Clear();
        InitializeItemTypes();
        UpdateStatistics();

        Debug.Log("🍽️ PlateItemSystem: 新ステージ用リセット完了");
    }

    /// <summary>
    /// 残り時間取得
    /// </summary>
    private float GetRemainingTime()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        return gameManager?.GetRemainingTime() ?? 30f;
    }

    /// <summary>
    /// 吸い込み速度倍率計算
    /// </summary>
    private float CalculateSuckSpeedMultiplier(float remainingTime)
    {
        if (remainingTime <= 1f) return 5f;
        if (remainingTime <= 3f) return 4f;
        if (remainingTime <= 5f) return 3f;
        if (remainingTime <= 10f) return 2f;
        return 1f;
    }

    /// <summary>
    /// 外部から敵による盗難などのアクション用
    /// </summary>
    public List<PlateItem> GetPlateItems()
    {
        return plateItems.Where(item => item != null).ToList();
    }

    /// <summary>
    /// 特定タイプのアイテム数取得
    /// </summary>
    public int GetItemCount(PlateItemType itemType)
    {
        return itemCounts.ContainsKey(itemType) ? itemCounts[itemType] : 0;
    }

    /// <summary>
    /// 総価値取得
    /// </summary>
    public long GetTotalValue()
    {
        return totalValue;
    }

    /// <summary>
    /// アイテム種類別統計取得
    /// </summary>
    public Dictionary<PlateItemType, int> GetItemStatistics()
    {
        return new Dictionary<PlateItemType, int>(itemCounts);
    }
}

/// <summary>
/// プレートアイテムの種類
/// </summary>
public enum PlateItemType
{
    NormalJapaman,    // 通常ジャパまん
    SilverJapaman,    // 銀ジャパまん（100個統合）
    GoldJapaman,      // 金ジャパまん（10000個統合）
    RainbowJapaman,   // 虹ジャパまん（レア）
    JapariPan,        // ジャパリパン（ロバのパン屋）
    SpecialItem       // その他特殊アイテム
}

/// <summary>
/// プレートアイテムのプレファブデータ
/// </summary>
[System.Serializable]
public class PlateItemPrefabData
{
    public PlateItemType itemType;
    public GameObject prefab;
    public string displayName;
    public Color itemColor = Color.white;
    public Vector2 itemSize = new Vector2(30, 30);
}