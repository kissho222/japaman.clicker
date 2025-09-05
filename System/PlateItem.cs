using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレート上の個別アイテム
/// 種類・価値・状態を管理
/// </summary>
public class PlateItem : MonoBehaviour
{
    [Header("アイテム情報")]
    public PlateItemType itemType;
    public float value;
    public bool isStealable = true;          // 盗める対象かどうか
    public bool isConsolidatable = true;     // 統合対象かどうか

    [Header("視覚効果")]
    public float glowIntensity = 1f;
    public bool enableHoverEffect = true;

    // 内部状態
    private bool isCollected = false;
    private bool isBeingStolen = false;
    private PlateItemSystem plateSystem;

    // コンポーネント参照
    private Image itemImage;
    private RectTransform rectTransform;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        if (itemImage == null)
        {
            itemImage = gameObject.AddComponent<Image>();
        }

        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// アイテム初期化
    /// </summary>
    public void Initialize(PlateItemType type, float itemValue, PlateItemSystem system)
    {
        itemType = type;
        value = itemValue;
        plateSystem = system;

        SetupVisualAppearance();
        SetupItemProperties();

        Debug.Log($"🍽️ PlateItem初期化: {type} (価値: {value})");
    }

    /// <summary>
    /// 見た目設定
    /// </summary>
    private void SetupVisualAppearance()
    {
        if (itemImage == null) return;

        switch (itemType)
        {
            case PlateItemType.NormalJapaman:
                itemImage.color = new Color(1f, 0.9f, 0.7f, 1f); // クリーム色
                rectTransform.sizeDelta = new Vector2(25, 25);
                glowIntensity = 1f;
                break;

            case PlateItemType.SilverJapaman:
                itemImage.color = new Color(0.8f, 0.8f, 0.9f, 1f); // シルバー
                rectTransform.sizeDelta = new Vector2(30, 30);
                glowIntensity = 1.5f;
                break;

            case PlateItemType.GoldJapaman:
                itemImage.color = new Color(1f, 0.8f, 0.2f, 1f); // ゴールド
                rectTransform.sizeDelta = new Vector2(35, 35);
                glowIntensity = 2f;
                break;

            case PlateItemType.RainbowJapaman:
                itemImage.color = Color.white; // レインボーエフェクトは別途実装
                rectTransform.sizeDelta = new Vector2(40, 40);
                glowIntensity = 3f;
                StartRainbowEffect();
                break;

            case PlateItemType.JapariPan:
                itemImage.color = new Color(0.9f, 0.7f, 0.4f, 1f); // フランスパン色
                rectTransform.sizeDelta = new Vector2(40, 15); // 横長
                glowIntensity = 1.2f;
                break;

            case PlateItemType.SpecialItem:
                itemImage.color = new Color(0.8f, 0.4f, 0.8f, 1f); // 紫色
                rectTransform.sizeDelta = new Vector2(30, 30);
                glowIntensity = 1.8f;
                break;
        }

        // 価値に応じたサイズ調整
        ApplyValueBasedScaling();
    }

    /// <summary>
    /// 価値に基づくスケーリング
    /// </summary>
    private void ApplyValueBasedScaling()
    {
        float scaleMultiplier = 1f;

        if (value >= 10000f)
        {
            scaleMultiplier = 1.3f;
        }
        else if (value >= 1000f)
        {
            scaleMultiplier = 1.2f;
        }
        else if (value >= 100f)
        {
            scaleMultiplier = 1.1f;
        }

        transform.localScale = Vector3.one * scaleMultiplier;
    }

    /// <summary>
    /// アイテム属性設定
    /// </summary>
    private void SetupItemProperties()
    {
        switch (itemType)
        {
            case PlateItemType.NormalJapaman:
                isStealable = true;
                isConsolidatable = true;
                break;

            case PlateItemType.SilverJapaman:
            case PlateItemType.GoldJapaman:
                isStealable = true;
                isConsolidatable = true;
                break;

            case PlateItemType.RainbowJapaman:
                isStealable = false; // レアアイテムは盗めない
                isConsolidatable = false;
                break;

            case PlateItemType.JapariPan:
                isStealable = true;
                isConsolidatable = false; // パンは統合しない
                break;

            case PlateItemType.SpecialItem:
                isStealable = false;
                isConsolidatable = false;
                break;
        }
    }

    /// <summary>
    /// レインボーエフェクト
    /// </summary>
    private void StartRainbowEffect()
    {
        StartCoroutine(RainbowEffectCoroutine());
    }

    private System.Collections.IEnumerator RainbowEffectCoroutine()
    {
        while (!isCollected && gameObject != null)
        {
            float hue = (Time.time * 0.5f) % 1f;
            itemImage.color = Color.HSVToRGB(hue, 0.8f, 1f);
            yield return null;
        }
    }

    /// <summary>
    /// アイテムが盗まれる処理
    /// </summary>
    public void BeStolen(Transform thiefTransform)
    {
        if (isBeingStolen || !isStealable) return;

        isBeingStolen = true;
        StartCoroutine(StolenAnimation(thiefTransform));
    }

    /// <summary>
    /// 盗難アニメーション
    /// </summary>
    private System.Collections.IEnumerator StolenAnimation(Transform thief)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = thief.position;
        float duration = 1f;
        float elapsed = 0f;

        // 盗まれる際の色変化
        Color originalColor = itemImage.color;
        Color stolenColor = new Color(originalColor.r * 0.5f, originalColor.g * 0.5f, originalColor.b * 0.5f, 0.7f);

        while (elapsed < duration && gameObject != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 位置とサイズの変化
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            // 色の変化
            itemImage.color = Color.Lerp(originalColor, stolenColor, t);

            yield return null;
        }

        // 統計から除外
        if (plateSystem != null)
        {
            plateSystem.RemoveItem(this);
        }

        Debug.Log($"💀 アイテム盗難: {itemType} (価値: {value})");

        // アイテム削除
        Destroy(gameObject);
    }

    /// <summary>
    /// アイテムが収集される処理
    /// </summary>
    public void BeCollected()
    {
        if (isCollected || isBeingStolen) return;

        isCollected = true;
        Debug.Log($"✨ アイテム収集: {itemType} (価値: {value})");
    }

    /// <summary>
    /// ホバーエフェクト（将来の拡張用）
    /// </summary>
    public void OnPointerEnter()
    {
        if (!enableHoverEffect || isCollected || isBeingStolen) return;

        // ホバー時の効果
        transform.localScale *= 1.1f;
        itemImage.color = Color.Lerp(itemImage.color, Color.white, 0.2f);
    }

    public void OnPointerExit()
    {
        if (!enableHoverEffect || isCollected || isBeingStolen) return;

        // ホバー終了時の効果復元
        SetupVisualAppearance();
    }

    /// <summary>
    /// アイテム情報の取得
    /// </summary>
    public PlateItemInfo GetItemInfo()
    {
        return new PlateItemInfo
        {
            itemType = this.itemType,
            value = this.value,
            isStealable = this.isStealable,
            isConsolidatable = this.isConsolidatable,
            position = transform.position,
            isCollected = this.isCollected,
            isBeingStolen = this.isBeingStolen
        };
    }

    /// <summary>
    /// デバッグ用：アイテム情報表示
    /// </summary>
    [ContextMenu("🔍 アイテム情報表示")]
    public void DebugShowInfo()
    {
        Debug.Log($"=== PlateItem情報 ===");
        Debug.Log($"種類: {itemType}");
        Debug.Log($"価値: {value}");
        Debug.Log($"盗める: {isStealable}");
        Debug.Log($"統合可能: {isConsolidatable}");
        Debug.Log($"収集済み: {isCollected}");
        Debug.Log($"盗難中: {isBeingStolen}");
        Debug.Log($"位置: {transform.position}");
    }

    // プロパティ
    public PlateItemType ItemType => itemType;
    public float Value => value;
    public bool IsStealable => isStealable;
    public bool IsConsolidatable => isConsolidatable;
    public bool IsCollected => isCollected;
    public bool IsBeingStolen => isBeingStolen;
}

/// <summary>
/// アイテム情報構造体
/// </summary>
[System.Serializable]
public class PlateItemInfo
{
    public PlateItemType itemType;
    public float value;
    public bool isStealable;
    public bool isConsolidatable;
    public Vector3 position;
    public bool isCollected;
    public bool isBeingStolen;
}