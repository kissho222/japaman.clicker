using UnityEngine;

[System.Serializable]
public class SaveData
{
    [Header("基本ゲーム情報")]
    public int currentStage = 1;
    public long lastJapamanCount = 0;

    // 🔥 JsonUtility用の時刻文字列（シリアライズ対象）
    [SerializeField] private string saveDateTimeString = "";

    // 🔥 従来通りのDateTime（非シリアライズ、互換性維持）
    [System.NonSerialized] public System.DateTime saveDateTime;

    [Header("🆕 アップグレード情報")]
    public int[] upgradeQuantities = new int[0];
    public bool[] upgradeUnlocked = new bool[0];

    [Header("🆕 累積統計")]
    public long totalLifetimeJapaman = 0;
    public long totalLifetimeExtra = 0;
    public int totalStagesCompleted = 0;

    // 🔥 JsonUtility処理後に呼ばれるコールバック
    void OnAfterDeserialize()
    {
        // 文字列からDateTimeを復元
        if (!string.IsNullOrEmpty(saveDateTimeString))
        {
            if (System.DateTime.TryParse(saveDateTimeString, out System.DateTime parsedDate))
            {
                saveDateTime = parsedDate;
                Debug.Log($"💾 🔄 OnAfterDeserialize: 時刻復元 {saveDateTime:yyyy/MM/dd HH:mm:ss}");
            }
            else
            {
                saveDateTime = System.DateTime.Now;
                Debug.LogWarning($"💾 ⚠️ 時刻解析失敗、現在時刻を使用: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
            }
        }
        else
        {
            saveDateTime = System.DateTime.Now;
            Debug.Log($"💾 🆕 時刻文字列が空、新規作成: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
        }
    }

    // 🔥 JsonUtility処理前に呼ばれるコールバック
    void OnBeforeSerialize()
    {
        // DateTimeを文字列に変換
        saveDateTimeString = saveDateTime.ToString("yyyy/MM/dd HH:mm:ss");
        Debug.Log($"💾 💽 OnBeforeSerialize: 時刻保存 {saveDateTimeString}");
    }

    // デフォルトコンストラクタ
    public SaveData()
    {
        currentStage = 1;
        lastJapamanCount = 0;
        saveDateTime = System.DateTime.Now;
        saveDateTimeString = saveDateTime.ToString("yyyy/MM/dd HH:mm:ss");

        Debug.Log($"💾 🆕 SaveData() デフォルトコンストラクタ: {saveDateTime:yyyy/MM/dd HH:mm:ss}");

        // アップグレード配列を初期化
        InitializeUpgradeArrays();
    }

    // 🔥 パラメータ付きコンストラクタ（時刻保持対応版）
    public SaveData(int stage, long japamanCount, System.DateTime? preserveDateTime = null)
    {
        currentStage = stage;
        lastJapamanCount = japamanCount;

        // 🔥 時刻保持ロジック
        if (preserveDateTime.HasValue)
        {
            saveDateTime = preserveDateTime.Value;
            Debug.Log($"💾 🕐 SaveData(stage, count, preserve) 時刻保持: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
        }
        else
        {
            saveDateTime = System.DateTime.Now;
            Debug.Log($"💾 🆕 SaveData(stage, count, null) 新規時刻設定: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
        }

        // 🔥 文字列も同期
        saveDateTimeString = saveDateTime.ToString("yyyy/MM/dd HH:mm:ss");

        // アップグレード配列を初期化
        InitializeUpgradeArrays();
    }

    // 🔥 アップグレード配列の初期化（強化版）
    private void InitializeUpgradeArrays()
    {
        Debug.Log($"💾 🔧 InitializeUpgradeArrays開始 - 現在時刻: {saveDateTime:yyyy/MM/dd HH:mm:ss}");

        // UpgradeManagerから最大アップグレード数を取得
        if (UpgradeManager.Instance != null)
        {
            var allUpgrades = UpgradeManager.Instance.allUpgrades;
            if (allUpgrades != null)
            {
                int upgradeCount = allUpgrades.Count;
                upgradeQuantities = new int[upgradeCount];
                upgradeUnlocked = new bool[upgradeCount];

                Debug.Log($"💾 セーブデータ初期化: {upgradeCount}種類のアップグレード");

                // 🔥 allUpgrades と activeUpgrades の両方をチェック
                for (int i = 0; i < upgradeCount; i++)
                {
                    var upgrade = allUpgrades[i];
                    upgradeQuantities[i] = upgrade.currentLevel;
                    upgradeUnlocked[i] = upgrade.isActive;

                    Debug.Log($"💾 保存[{i}]: {upgrade.upgradeName} - Lv.{upgrade.currentLevel}, Active:{upgrade.isActive}");
                }
            }
            else
            {
                upgradeQuantities = new int[0];
                upgradeUnlocked = new bool[0];
                Debug.LogWarning("💾 UpgradeManager.allUpgrades が null");
            }
        }
        else
        {
            // フォールバック：空の配列
            upgradeQuantities = new int[0];
            upgradeUnlocked = new bool[0];
            Debug.LogWarning("💾 UpgradeManager.Instance が null");
        }

        Debug.Log($"💾 🔧 InitializeUpgradeArrays完了 - 最終時刻: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
    }

    // 🔥 現在のゲーム状態からセーブデータを作成（時刻保持対応版）
    public static SaveData CreateFromCurrentState(int nextStage, long japamanCount, System.DateTime? preserveDateTime = null)
    {
        Debug.Log($"💾 === CreateFromCurrentState開始 ===");
        Debug.Log($"💾 📥 パラメータ: nextStage={nextStage}, japamanCount={japamanCount}");

        if (preserveDateTime.HasValue)
        {
            Debug.Log($"💾 🕐 時刻保持モード: {preserveDateTime.Value:yyyy/MM/dd HH:mm:ss}");
        }
        else
        {
            Debug.Log($"💾 🆕 新規時刻モード: {System.DateTime.Now:yyyy/MM/dd HH:mm:ss}");
        }

        SaveData data = new SaveData(nextStage, japamanCount, preserveDateTime);

        Debug.Log($"💾 📦 SaveData作成後の時刻: {data.saveDateTime:yyyy/MM/dd HH:mm:ss}");

        // GameManagerから累積統計を取得
        if (GameManager.Instance != null)
        {
            data.totalLifetimeJapaman = GameManager.Instance.GetTotalLifetimeJapaman();
            data.totalLifetimeExtra = GameManager.Instance.totalLifetimeExtra;
            data.totalStagesCompleted = GameManager.Instance.totalStagesCompleted;

            Debug.Log($"💾 統計取得: ライフタイム={data.totalLifetimeJapaman}, 完了数={data.totalStagesCompleted}");
        }

        Debug.Log($"💾 📦 統計取得後の時刻: {data.saveDateTime:yyyy/MM/dd HH:mm:ss}");

        // 🔥 アップグレード情報を再度確実に保存
        data.CaptureCurrentUpgradeState();

        Debug.Log($"💾 📦 CaptureCurrentUpgradeState後の時刻: {data.saveDateTime:yyyy/MM/dd HH:mm:ss}");
        Debug.Log($"💾 ✅ CreateFromCurrentState完了: 保存ステージ={data.currentStage}, 最終時刻={data.saveDateTime:yyyy/MM/dd HH:mm:ss}");

        return data;
    }

    // 🔥 現在のアップグレード状態を確実にキャプチャ
    private void CaptureCurrentUpgradeState()
    {
        Debug.Log($"💾 🔧 CaptureCurrentUpgradeState開始 - 入力時刻: {saveDateTime:yyyy/MM/dd HH:mm:ss}");

        if (UpgradeManager.Instance == null)
        {
            Debug.LogWarning("💾 CaptureCurrentUpgradeState: UpgradeManager.Instance が null");
            return;
        }

        var allUpgrades = UpgradeManager.Instance.allUpgrades;
        var activeUpgrades = UpgradeManager.Instance.activeUpgrades;

        if (allUpgrades == null)
        {
            Debug.LogWarning("💾 CaptureCurrentUpgradeState: allUpgrades が null");
            return;
        }

        int upgradeCount = allUpgrades.Count;
        upgradeQuantities = new int[upgradeCount];
        upgradeUnlocked = new bool[upgradeCount];

        Debug.Log($"💾 アップグレード状態キャプチャ開始: 全{upgradeCount}種類, アクティブ{activeUpgrades?.Count ?? 0}種類");

        for (int i = 0; i < upgradeCount; i++)
        {
            var upgrade = allUpgrades[i];
            upgradeQuantities[i] = upgrade.currentLevel;
            upgradeUnlocked[i] = upgrade.isActive;

            Debug.Log($"💾 キャプチャ[{i}]: {upgrade.upgradeName} - Lv.{upgrade.currentLevel}, Active:{upgrade.isActive}");
        }

        Debug.Log($"💾 🔧 CaptureCurrentUpgradeState完了 - 出力時刻: {saveDateTime:yyyy/MM/dd HH:mm:ss}");
    }

    // 🔥 アップグレード状態をUpgradeManagerに適用（完全版）
    public void ApplyUpgradeData()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogWarning("💾 ApplyUpgradeData: UpgradeManager.Instance が null");
            return;
        }

        Debug.Log($"💾 === アップグレードデータ適用開始 ===");
        Debug.Log($"💾 保存データ: {upgradeQuantities.Length}種類のアップグレード");

        var allUpgrades = UpgradeManager.Instance.allUpgrades;
        if (allUpgrades == null)
        {
            Debug.LogWarning("💾 UpgradeManager.allUpgrades が null");
            return;
        }

        int currentUpgradeCount = allUpgrades.Count;
        Debug.Log($"💾 現在のアップグレード数: {currentUpgradeCount}");

        // 🔥 activeUpgradesリストを事前にクリア
        UpgradeManager.Instance.activeUpgrades.Clear();
        Debug.Log("💾 activeUpgradesリストをクリア");

        // アップグレード状態を復元
        int restoredCount = 0;
        int maxIndex = Mathf.Min(upgradeQuantities.Length, allUpgrades.Count, upgradeUnlocked.Length);

        for (int i = 0; i < maxIndex; i++)
        {
            var upgrade = allUpgrades[i];
            bool hasData = upgradeQuantities[i] > 0 || upgradeUnlocked[i];

            Debug.Log($"💾 復元処理[{i}]: {upgrade.upgradeName}");
            Debug.Log($"    保存レベル: {upgradeQuantities[i]}, 保存アクティブ: {upgradeUnlocked[i]}");

            if (hasData)
            {
                // 🔥 allUpgradesのデータを更新
                upgrade.isActive = upgradeUnlocked[i];
                upgrade.currentLevel = upgradeQuantities[i];

                // 🔥 アクティブなアップグレードをactiveUpgradesに追加
                if (upgrade.isActive && upgrade.currentLevel > 0)
                {
                    UpgradeManager.Instance.activeUpgrades.Add(upgrade);
                    restoredCount++;
                    Debug.Log($"💾 ✅ activeUpgradesに追加: {upgrade.upgradeName} Lv.{upgrade.currentLevel}");
                }
                else
                {
                    Debug.Log($"💾 ⚠️ 非アクティブのため追加しない: {upgrade.upgradeName}");
                }
            }
            else
            {
                // 🔥 データが無い場合は初期状態に戻す
                upgrade.isActive = false;
                upgrade.currentLevel = 0;
                Debug.Log($"💾 初期状態に戻す: {upgrade.upgradeName}");
            }
        }

        Debug.Log($"💾 === アップグレードデータ適用完了 ===");
        Debug.Log($"💾 復元されたアクティブアップグレード数: {restoredCount}");
        Debug.Log($"💾 最終activeUpgrades数: {UpgradeManager.Instance.activeUpgrades.Count}");

        // 🔥 効果を即座に再計算・適用
        ApplyUpgradeEffectsToClickManager();
    }

    // 🔥 ClickManagerへの効果適用を強制実行
    private void ApplyUpgradeEffectsToClickManager()
    {
        Debug.Log("💾 === ClickManagerへのアップグレード効果適用開始 ===");

        var clickManager = UnityEngine.Object.FindFirstObjectByType<ClickManager>();
        if (clickManager == null)
        {
            Debug.LogWarning("💾 ClickManager が見つかりません");
            return;
        }

        var upgradeManager = UpgradeManager.Instance;

        // 🔥 各アップグレード効果を個別に適用

        // クリック強化
        var clickPower = GetActiveUpgradeByType(UpgradeType.ClickPower);
        if (clickPower != null)
        {
            int multiplier = (int)clickPower.GetCurrentEffect();
            clickManager.clickMultiplier = multiplier;
            Debug.Log($"💾 クリック強化適用: x{multiplier}");
        }
        else
        {
            clickManager.clickMultiplier = 1;
            Debug.Log("💾 クリック強化: 初期値に設定");
        }

        // 工場（自動生産）
        var factory = GetActiveUpgradeByType(UpgradeType.Factory);
        if (factory != null)
        {
            float rate = factory.GetCurrentEffect();
            clickManager.autoProductionRate = rate;
            Debug.Log($"💾 工場適用: {rate}/秒");
        }
        else
        {
            clickManager.autoProductionRate = 0f;
            Debug.Log("💾 工場: 初期値に設定");
        }

        // ヘルパーフレンズ（自動クリック）
        var helper = GetActiveUpgradeByType(UpgradeType.HelperFriend);
        if (helper != null)
        {
            float rate = helper.GetCurrentEffect();
            clickManager.autoClickRate = rate;
            Debug.Log($"💾 ヘルパー適用: {rate}/秒");
        }
        else
        {
            clickManager.autoClickRate = 0f;
            Debug.Log("💾 ヘルパー: 初期値に設定");
        }

        // 🔥 UpgradeManagerの再計算も実行
        if (upgradeManager != null)
        {
            upgradeManager.RecalculateAllEffects();
        }

        Debug.Log("💾 === ClickManagerへのアップグレード効果適用完了 ===");
    }

    // 🔥 特定タイプのアクティブアップグレードを取得
    private UpgradeData GetActiveUpgradeByType(UpgradeType type)
    {
        if (UpgradeManager.Instance?.activeUpgrades == null) return null;

        foreach (var upgrade in UpgradeManager.Instance.activeUpgrades)
        {
            if (upgrade.upgradeType == type && upgrade.isActive)
            {
                return upgrade;
            }
        }
        return null;
    }

    // 表示用の情報を取得
    public string GetDisplayInfo()
    {
        return $"ステージ{currentStage}日目 - {saveDateTime:MM/dd HH:mm}";
    }

    // 🔥 既存コードとの互換性のため
    public string GetSaveDateTimeString()
    {
        return saveDateTime.ToString("MM/dd HH:mm");
    }

    // セーブデータの詳細情報
    public string GetDetailedInfo()
    {
        int activeUpgradeCount = 0;
        for (int i = 0; i < upgradeUnlocked.Length; i++)
        {
            if (upgradeUnlocked[i]) activeUpgradeCount++;
        }

        return $"ステージ{currentStage}日目\n" +
               $"ジャパまん: {lastJapamanCount}個\n" +
               $"累積生産: {totalLifetimeJapaman}個\n" +
               $"クリア回数: {totalStagesCompleted}回\n" +
               $"アップグレード: {activeUpgradeCount}種類\n" +
               $"保存日時: {saveDateTime:yyyy/MM/dd HH:mm:ss}";
    }
}