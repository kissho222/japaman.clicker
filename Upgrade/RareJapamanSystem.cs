using UnityEngine;

/// <summary>
/// レアジャパまんシステム
/// 低確率で高価値のジャパまんを生成
/// </summary>
public class RareJapamanSystem : MonoBehaviour
{
    [Header("レア発生設定")]
    public float baseRareChance = 0.05f;        // 基本レア確率（5%）
    public float rainbowChance = 0.01f;         // 虹ジャパまん確率（1%）
    public float specialChance = 0.002f;        // 特殊アイテム確率（0.2%）

    [Header("価値倍率")]
    public float rareValueMultiplier = 3f;      // レア価値倍率
    public float rainbowValueMultiplier = 10f;  // 虹価値倍率
    public float specialValueMultiplier = 50f;  // 特殊価値倍率

    [Header("効果音")]
    public AudioClip rareSound;
    public AudioClip rainbowSound;
    public AudioClip specialSound;

    // 内部状態
    private float currentRareChance;
    private int rareItemsGenerated = 0;
    private int rainbowItemsGenerated = 0;
    private int specialItemsGenerated = 0;

    // 参照
    private AudioSource audioSource;
    private ClickManagerAdapter clickManagerAdapter;

    public static RareJapamanSystem Instance { get; private set; }

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
        currentRareChance = baseRareChance;

        Debug.Log("🌈 RareJapamanSystem初期化完了");
    }

    /// <summary>
    /// クリック時のレアジャパまんチェック
    /// </summary>
    public void OnClick()
    {
        // アップグレードによる確率上昇を取得
        float bonusChance = GetRareBonusFromUpgrades();
        float totalRareChance = currentRareChance + bonusChance;

        // レアジャパまん判定
        if (Random.Range(0f, 1f) < totalRareChance)
        {
            GenerateRareItem();
        }
    }

    /// <summary>
    /// レアアイテム生成
    /// </summary>
    private void GenerateRareItem()
    {
        float roll = Random.Range(0f, 1f);

        if (roll < specialChance)
        {
            // 特殊アイテム（最高レア）
            GenerateSpecialItem();
        }
        else if (roll < rainbowChance)
        {
            // 虹ジャパまん（超レア）
            GenerateRainbowJapaman();
        }
        else
        {
            // 通常レアジャパまん
            GenerateRareJapaman();
        }
    }

    /// <summary>
    /// レアジャパまん生成
    /// </summary>
    private void GenerateRareJapaman()
    {
        if (clickManagerAdapter == null) return;

        float baseValue = 1f; // 基本価値
        float rareValue = baseValue * rareValueMultiplier;

        clickManagerAdapter.AddRareJapaman(PlateItemType.RainbowJapaman, rareValue);

        // エフェクト・音響
        PlayRareEffects(rareSound);

        rareItemsGenerated++;
        Debug.Log($"🌟 レアジャパまん生成: 価値{rareValue} (総生成数: {rareItemsGenerated})");
    }

    /// <summary>
    /// 虹ジャパまん生成
    /// </summary>
    private void GenerateRainbowJapaman()
    {
        if (clickManagerAdapter == null) return;

        float baseValue = 1f;
        float rainbowValue = baseValue * rainbowValueMultiplier;

        clickManagerAdapter.AddRareJapaman(PlateItemType.RainbowJapaman, rainbowValue);

        // エフェクト・音響
        PlayRareEffects(rainbowSound);

        rainbowItemsGenerated++;
        Debug.Log($"🌈 虹ジャパまん生成: 価値{rainbowValue} (総生成数: {rainbowItemsGenerated})");
    }

    /// <summary>
    /// 特殊アイテム生成
    /// </summary>
    private void GenerateSpecialItem()
    {
        if (clickManagerAdapter == null) return;

        float baseValue = 1f;
        float specialValue = baseValue * specialValueMultiplier;

        clickManagerAdapter.AddRareJapaman(PlateItemType.SpecialItem, specialValue);

        // エフェクト・音響
        PlayRareEffects(specialSound);

        specialItemsGenerated++;
        Debug.Log($"✨ 特殊アイテム生成: 価値{specialValue} (総生成数: {specialItemsGenerated})");
    }

    /// <summary>
    /// レアエフェクト再生
    /// </summary>
    private void PlayRareEffects(AudioClip sound)
    {
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }

        // 画面エフェクト（将来実装）
        // ScreenEffectManager.PlayRareEffect();
    }

    /// <summary>
    /// アップグレードからのレア確率ボーナス取得
    /// </summary>
    private float GetRareBonusFromUpgrades()
    {
        float bonus = 0f;

        if (UpgradeManager.Instance != null)
        {
            // 虹色のジャパまんアップグレード
            var rainbowUpgrade = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.RainbowJapaman);
            if (rainbowUpgrade != null)
            {
                bonus += rainbowUpgrade.GetCurrentEffect();
            }

            // 幸運の尻尾アップグレード
            var luckyTail = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.LuckyTail);
            if (luckyTail != null)
            {
                bonus *= luckyTail.GetCurrentEffect(); // 倍率として適用
            }
        }

        return bonus;
    }

    /// <summary>
    /// レア確率を設定
    /// </summary>
    public void SetRareChance(float newChance)
    {
        currentRareChance = Mathf.Clamp01(newChance);
        Debug.Log($"🌈 レア確率設定: {currentRareChance * 100}%");
    }

    /// <summary>
    /// レア統計情報取得
    /// </summary>
    public RareStatistics GetRareStatistics()
    {
        return new RareStatistics
        {
            currentRareChance = currentRareChance,
            rareItemsGenerated = rareItemsGenerated,
            rainbowItemsGenerated = rainbowItemsGenerated,
            specialItemsGenerated = specialItemsGenerated,
            totalRareItems = rareItemsGenerated + rainbowItemsGenerated + specialItemsGenerated
        };
    }

    /// <summary>
    /// デバッグ用：レア統計表示
    /// </summary>
    [ContextMenu("🌈 レア統計表示")]
    public void DebugShowRareStatistics()
    {
        var stats = GetRareStatistics();
        Debug.Log("=== レアアイテム統計 ===");
        Debug.Log($"現在のレア確率: {stats.currentRareChance * 100}%");
        Debug.Log($"レアジャパまん生成数: {stats.rareItemsGenerated}個");
        Debug.Log($"虹ジャパまん生成数: {stats.rainbowItemsGenerated}個");
        Debug.Log($"特殊アイテム生成数: {stats.specialItemsGenerated}個");
        Debug.Log($"総レアアイテム数: {stats.totalRareItems}個");
    }

    /// <summary>
    /// デバッグ用：手動レア生成
    /// </summary>
    [ContextMenu("🌈 手動レア生成")]
    public void DebugManualRareGeneration()
    {
        if (Application.isPlaying)
        {
            GenerateRareItem();
        }
    }

    /// <summary>
    /// 新ステージ用リセット
    /// </summary>
    public void ResetForNewStage()
    {
        // 確率はリセットしない（累積成長）
        // 統計もリセットしない（累積記録）
        Debug.Log("🌈 RareJapamanSystem: 新ステージ継続");
    }
}

/// <summary>
/// レア統計情報
/// </summary>
[System.Serializable]
public class RareStatistics
{
    public float currentRareChance;
    public int rareItemsGenerated;
    public int rainbowItemsGenerated;
    public int specialItemsGenerated;
    public int totalRareItems;
}