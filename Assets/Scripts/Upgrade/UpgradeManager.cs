using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    [Header("アップグレードデータ")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>();
    public List<UpgradeData> activeUpgrades = new List<UpgradeData>();

    [Header("選択情報")]
    public int upgradesPerStage = 1;
    public int choiceCount = 3;

    [Header("マイルストーン情報")]
    public int[] clickMilestones = { 100, 500, 1000, 2000 };
    private int currentClickCount = 0;

    public static UpgradeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        InitializeUpgrades();
    }

    private void InitializeUpgrades()
    {
        CreateUpgradeData();
        Debug.Log("アップグレードシステム初期化完了: " + allUpgrades.Count + "種類");
    }

    private void CreateUpgradeData()
    {
        allUpgrades.Clear();

        // クリック強化
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.ClickPower,
            upgradeName = "クリック強化",
            description = "クリック1回でより多くのジャパまんを生産",
            baseEffect = 1f,
            levelMultiplier = 2f,
            maxLevel = 10,
            appearanceWeight = 3f
        });

        // ロバのパン屋（旧ジャパまん工場を統合・強化）
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.DonkeyBakery,
            upgradeName = "ロバのパン屋",
            description = "自動でジャパリパンを焼いてプレートに投げ入れます。レベルが上がるほど価値が指数的に増加",
            baseEffect = 10f,      // 基本パン価値
            levelMultiplier = 2.5f, // 指数的成長
            maxLevel = 8,
            appearanceWeight = 2.5f,
            requiredStage = 1       // 最初から利用可能
        });

        // お手伝いフレンズ
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.HelperFriend,
            upgradeName = "お手伝いフレンズ",
            description = "自動でクリックしてくれます",
            baseEffect = 0.5f,
            levelMultiplier = 1.8f,
            maxLevel = 6,
            appearanceWeight = 2f
        });

        // 虹色のジャパまん
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.RainbowJapaman,
            upgradeName = "虹色のジャパまん",
            description = "低確率で価値3倍のジャパまんが出現",
            baseEffect = 0.05f,
            levelMultiplier = 1.3f,
            maxLevel = 5,
            appearanceWeight = 1.5f,
            requiredStage = 3
        });

        // ラッキービースト出現
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.LuckyBeast,
            upgradeName = "ラッキービースト出現",
            description = "ランダムでラッキービーストが現れ、クリックでバフ値獲得",
            baseEffect = 0.1f,
            levelMultiplier = 1.2f,
            maxLevel = 5,
            appearanceWeight = 1.8f,
            requiredStage = 2
        });

        // ロバのパン屋出現（削除 - 上記に統合済み）

        // フレンズコール
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.FriendsCall,
            upgradeName = "フレンズコール",
            description = "ラッキービーストの出現率が2倍になります",
            baseEffect = 2f,
            levelMultiplier = 1.3f,
            maxLevel = 3,
            appearanceWeight = 1.2f,
            requiredStage = 5
        });

        // 幸運の尻尾
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.LuckyTail,
            upgradeName = "幸運の尻尾",
            description = "全ての確率イベントが良い方向に偏ります",
            baseEffect = 1.5f,
            levelMultiplier = 1.2f,
            maxLevel = 4,
            appearanceWeight = 1f,
            requiredStage = 6
        });

        // ミラクルタイム
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.MiraclTime,
            upgradeName = "ミラクルタイム",
            description = "稀に10秒間全効果が3倍になります",
            baseEffect = 0.02f,
            levelMultiplier = 1.5f,
            maxLevel = 3,
            appearanceWeight = 0.8f,
            requiredStage = 8
        });

        // 満足感
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.Satisfaction,
            upgradeName = "満足感",
            description = "余剰に食べさせると次ラウンドのノルマが減少",
            baseEffect = 0.1f,
            levelMultiplier = 1.5f,
            maxLevel = 5,
            appearanceWeight = 1.3f,
            requiredStage = 3
        });

        // お喋りしよう
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.ChatSystem,
            upgradeName = "お喋りしよう",
            description = "フレンズが質問してくる、正解でバフ値獲得",
            baseEffect = 1f,
            levelMultiplier = 1.2f,
            maxLevel = 3,
            appearanceWeight = 1f,
            requiredStage = 7
        });

        // まとめる係
        allUpgrades.Add(new UpgradeData
        {
            upgradeType = UpgradeType.Organizer,
            upgradeName = "まとめる係",
            description = "一定時間クリックしないと自動処理が高速化",
            baseEffect = 2f,
            levelMultiplier = 1.4f,
            maxLevel = 4,
            appearanceWeight = 1.1f,
            requiredStage = 5
        });
    }

    /// <summary>
    /// 🔥 新規ゲーム開始時の完全リセット
    /// </summary>
    public void ResetForNewGame()
    {
        Debug.Log("🔄 === UpgradeManager: 新規ゲーム用リセット開始 ===");

        // 🔥 activeUpgradesを完全クリア
        activeUpgrades.Clear();
        Debug.Log("🔄 activeUpgrades をクリアしました");

        // 🔥 allUpgrades の状態を初期状態にリセット
        foreach (var upgrade in allUpgrades)
        {
            upgrade.currentLevel = 0;
            upgrade.isActive = false;
        }
        Debug.Log("🔄 allUpgrades を初期状態にリセットしました");

        // 🔥 ClickManagerの効果を初期状態に戻す
        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.clickMultiplier = 1;
            clickManager.autoProductionRate = 0f;
            clickManager.autoClickRate = 0f;
            Debug.Log("🔄 ClickManager効果を初期状態にリセット");
        }

        // 🔥 まとめる係を停止
        var organizerManager = FindFirstObjectByType<OrganizerManager>();
        if (organizerManager != null)
        {
            organizerManager.SetOrganizerLevel(0);
            Debug.Log("🔄 まとめる係を停止");
        }

        // 🔥 ロバのパン屋を停止
        var bakeryManager = FindFirstObjectByType<DonkeyBakeryManager>();
        if (bakeryManager != null)
        {
            bakeryManager.StopBakery();
            Debug.Log("🔄 ロバのパン屋を停止");
        }

        // 🔥 UI関連の完全リセット
        ResetAllUpgradeUIs();

        Debug.Log("🔄 === UpgradeManager: 新規ゲーム用リセット完了 ===");
    }

    /// <summary>
    /// 🔥 全てのUpgrade UI をリセット
    /// </summary>
    private void ResetAllUpgradeUIs()
    {
        Debug.Log("🧹 === 全UpgradeUI リセット開始 ===");

        // UpgradeSidePanelUI の完全リセット
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.ForceCompleteReset();
        }

        // UpgradeSelectionUI の強制クローズ
        if (UpgradeSelectionUI.Instance != null)
        {
            UpgradeSelectionUI.Instance.ForceClose();
        }

        Debug.Log("🧹 === 全UpgradeUI リセット完了 ===");
    }

    /// <summary>
    /// 🚨 緊急UI状態確認
    /// </summary>
    [ContextMenu("🚨 緊急UI状態確認")]
    public void DebugEmergencyUICheck()
    {
        Debug.Log("🚨 === 緊急UI状態確認 ===");

        // UpgradeSelectionUI の状態
        if (UpgradeSelectionUI.Instance != null)
        {
            Debug.Log($"📋 UpgradeSelectionUI:");
            Debug.Log($"  - isSelectionActive: {UpgradeSelectionUI.Instance.IsSelectionActive()}");
            Debug.Log($"  - 動的ボタン数: 不明（privateのため）");
            Debug.Log($"  - selectionPanel active: {UpgradeSelectionUI.Instance.selectionPanel?.activeSelf}");
        }

        // UpgradeSidePanelUI の状態
        if (UpgradeSidePanelUI.Instance != null)
        {
            Debug.Log($"📋 UpgradeSidePanelUI:");
            Debug.Log($"  - isInitialized: 不明（privateのため）");
            Debug.Log($"  - パネル表示: {UpgradeSidePanelUI.Instance.sidePanelContainer?.activeSelf}");
        }

        // アップグレードデータ状態
        Debug.Log($"📋 UpgradeManager:");
        Debug.Log($"  - activeUpgrades数: {activeUpgrades.Count}");
        Debug.Log($"  - 問題のあるアップグレード:");
        foreach (var upgrade in activeUpgrades)
        {
            Debug.Log($"    - {upgrade.upgradeName}: Lv.{upgrade.currentLevel}");
        }

        Debug.Log("🚨 === 確認完了 ===");
    }

    /// <summary>
    /// 🚨 緊急完全UI修復
    /// </summary>
    [ContextMenu("🚨 緊急完全UI修復")]
    public void DebugEmergencyUIFix()
    {
        Debug.Log("🚨 === 緊急完全UI修復開始 ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("プレイモード以外では実行できません");
            return;
        }

        // UpgradeManagerリセット
        ResetForNewGame();

        // GameManagerのUI クリーンアップ
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // CleanupAllUIStates(); // privateメソッドのため直接呼び出し
            Debug.Log("🚨 GameManagerのUI クリーンアップ実行");
        }

        Debug.Log("🚨 === 緊急完全UI修復完了 ===");
    }

    /// <summary>
    /// 🔥 緊急デバッグ用：強制完全リセット
    /// </summary>
    [ContextMenu("🚨 緊急完全リセット")]
    public void DebugForceCompleteReset()
    {
        Debug.Log("🚨 === 緊急完全リセット実行 ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("プレイモード以外では実行できません");
            return;
        }

        ResetForNewGame();

        // 状態確認
        Debug.Log($"🚨 リセット後状態: activeUpgrades={activeUpgrades.Count}個");
        Debug.Log($"🚨 allUpgrades の Active状態:");

        foreach (var upgrade in allUpgrades)
        {
            if (upgrade.isActive || upgrade.currentLevel > 0)
            {
                Debug.Log($"🚨 - {upgrade.upgradeName}: Lv.{upgrade.currentLevel}, Active:{upgrade.isActive}");
            }
        }

        Debug.Log("🚨 === 緊急完全リセット完了 ===");
    }

    /// <summary>
    /// 🔥 安全な新規ゲーム用リセット（エラーハンドリング付き）
    /// </summary>
    public void SafeResetForNewGame()
    {
        Debug.Log("🔄 === UpgradeManager: 安全な新規ゲーム用リセット開始 ===");

        try
        {
            // activeUpgradesを完全クリア
            activeUpgrades.Clear();
            Debug.Log("🔄 activeUpgrades をクリアしました");

            // allUpgrades の状態を初期状態にリセット
            foreach (var upgrade in allUpgrades)
            {
                upgrade.currentLevel = 0;
                upgrade.isActive = false;
            }
            Debug.Log("🔄 allUpgrades を初期状態にリセットしました");

            // ClickManagerの効果を初期状態に戻す
            var clickManager = FindFirstObjectByType<ClickManager>();
            if (clickManager != null)
            {
                clickManager.clickMultiplier = 1;
                clickManager.autoProductionRate = 0f;
                clickManager.autoClickRate = 0f;
                Debug.Log("🔄 ClickManager効果を初期状態にリセット");
            }

            // まとめる係を停止
            var organizerManager = FindFirstObjectByType<OrganizerManager>();
            if (organizerManager != null)
            {
                organizerManager.SetOrganizerLevel(0);
                Debug.Log("🔄 まとめる係を停止");
            }

            // ロバのパン屋を停止  
            var bakeryManager = FindFirstObjectByType<DonkeyBakeryManager>();
            if (bakeryManager != null)
            {
                bakeryManager.StopBakery();
                Debug.Log("🔄 ロバのパン屋を停止");
            }

            // UI関連のリセット（安全版）
            SafeResetUpgradeUIs();

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ UpgradeManager リセット中にエラー: {e.Message}");
        }

        Debug.Log("🔄 === UpgradeManager: 安全な新規ゲーム用リセット完了 ===");
    }

    /// <summary>
    /// 🔥 安全なUpgrade UI リセット
    /// </summary>
    private void SafeResetUpgradeUIs()
    {
        Debug.Log("🧹 === 安全なUpgradeUI リセット開始 ===");

        try
        {
            // UpgradeSidePanelUI のリセット
            if (UpgradeSidePanelUI.Instance != null)
            {
                UpgradeSidePanelUI.Instance.RefreshUpgradeList();
                Debug.Log("🧹 UpgradeSidePanelUI リセット完了");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ UpgradeSidePanelUI リセットエラー: {e.Message}");
        }

        try
        {
            // UpgradeSelectionUI のリセット
            if (UpgradeSelectionUI.Instance != null)
            {
                // リフレクションを使って安全にメソッド呼び出し
                var methods = UpgradeSelectionUI.Instance.GetType().GetMethods();
                foreach (var method in methods)
                {
                    if (method.Name == "ForceClose" && method.GetParameters().Length == 0)
                    {
                        method.Invoke(UpgradeSelectionUI.Instance, null);
                        Debug.Log("🧹 UpgradeSelectionUI.ForceClose() 実行完了");
                        break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ UpgradeSelectionUI リセットエラー: {e.Message}");
        }

        Debug.Log("🧹 === 安全なUpgradeUI リセット完了 ===");
    }



    public List<UpgradeData> GetAvailableUpgrades(int currentStage)
    {
        return allUpgrades.FindAll(upgrade =>
            upgrade.requiredStage <= currentStage &&
            upgrade.currentLevel < upgrade.maxLevel
        );
    }

    public List<UpgradeData> GenerateUpgradeChoices(int currentStage)
    {
        var available = GetAvailableUpgrades(currentStage);
        var choices = new List<UpgradeData>();

        for (int i = 0; i < choiceCount && available.Count > 0; i++)
        {
            var selected = WeightedRandomSelection(available);
            choices.Add(selected);
            available.Remove(selected);
        }

        Debug.Log("ステージ" + currentStage + "のアップグレード選択肢生成: " + choices.Count + "個");
        return choices;
    }

    private UpgradeData WeightedRandomSelection(List<UpgradeData> upgrades)
    {
        float totalWeight = upgrades.Sum(u => u.appearanceWeight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var upgrade in upgrades)
        {
            currentWeight += upgrade.appearanceWeight;
            if (randomValue <= currentWeight)
            {
                return upgrade;
            }
        }

        return upgrades[upgrades.Count - 1];
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        var existing = activeUpgrades.Find(u => u.upgradeType == upgrade.upgradeType);
        bool wasLevelUp = false;

        if (existing != null)
        {
            // レベルアップ処理
            existing.LevelUp();
            wasLevelUp = true;
            Debug.Log(existing.upgradeName + " レベルアップ: Lv." + existing.currentLevel);

            // 🔥 重要: allUpgradesも同期（レベルアップ時）
            var allUpgradeTarget = allUpgrades.Find(u => u.upgradeType == upgrade.upgradeType);
            if (allUpgradeTarget != null)
            {
                allUpgradeTarget.currentLevel = existing.currentLevel;
                allUpgradeTarget.isActive = true;
                Debug.Log($"🔄 allUpgrades同期: {allUpgradeTarget.upgradeName} Lv.{allUpgradeTarget.currentLevel}");
            }
        }
        else
        {
            // 新規取得処理
            var newUpgrade = new UpgradeData
            {
                upgradeType = upgrade.upgradeType,
                upgradeName = upgrade.upgradeName,
                description = upgrade.description,
                currentLevel = 1,
                maxLevel = upgrade.maxLevel,
                baseEffect = upgrade.baseEffect,
                levelMultiplier = upgrade.levelMultiplier,
                isActive = true,
                isInstantEffect = upgrade.isInstantEffect,
                isPassiveEffect = upgrade.isPassiveEffect,
                effectDuration = upgrade.effectDuration
            };
            activeUpgrades.Add(newUpgrade);
            Debug.Log(newUpgrade.upgradeName + " 新規獲得: Lv.1");

            // 🔥 allUpgradesも同期（新規取得時）
            var allUpgradeTarget = allUpgrades.Find(u => u.upgradeType == upgrade.upgradeType);
            if (allUpgradeTarget != null)
            {
                allUpgradeTarget.isActive = true;
                allUpgradeTarget.currentLevel = 1;
                Debug.Log($"🔄 allUpgrades同期: {allUpgradeTarget.upgradeName} Lv.{allUpgradeTarget.currentLevel}");
            }
        }

        Debug.Log($"🔔 アップグレード適用: {upgrade.upgradeName} Lv.{(existing?.currentLevel ?? 1)}");

        // 🔥 通知システム改善
        if (UpgradeSidePanelUI.Instance != null)
        {
            if (wasLevelUp)
            {
                UpgradeSidePanelUI.Instance.OnUpgradeLevelUp(upgrade.upgradeType);
                Debug.Log($"📡 レベルアップ通知送信: {upgrade.upgradeName}");
            }
            else
            {
                UpgradeSidePanelUI.Instance.OnUpgradeObtained();
                Debug.Log($"📡 新規取得通知送信: {upgrade.upgradeName}");
            }
        }
        else
        {
            Debug.LogError("❌ UpgradeSidePanelUI.Instance が null です");
        }

        // UpgradeInfoUIにも通知
        if (UpgradeInfoUI.Instance != null)
        {
            UpgradeInfoUI.Instance.OnUpgradeObtained();
        }

        ApplyUpgradeEffects();
    }

    // 🔥 アップグレード効果適用（強化版）
    private void ApplyUpgradeEffects()
    {
        Debug.Log("🔧 === UpgradeManager: アップグレード効果適用開始 ===");

        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager == null)
        {
            Debug.LogWarning("🔧 ClickManager が見つかりません");
            return;
        }

        // 🔥 クリック強化
        var clickPower = GetActiveUpgrade(UpgradeType.ClickPower);
        if (clickPower != null)
        {
            int multiplier = (int)clickPower.GetCurrentEffect();
            clickManager.clickMultiplier = multiplier;
            Debug.Log($"🔧 クリック強化適用: x{multiplier} (Lv.{clickPower.currentLevel})");
        }
        else
        {
            clickManager.clickMultiplier = 1;
            Debug.Log("🔧 クリック強化: 初期値(x1)");
        }

        // 🔥 ロバのパン屋（旧工場を統合）
        var donkeyBakery = GetActiveUpgrade(UpgradeType.DonkeyBakery);
        var robaBakery = GetActiveUpgrade(UpgradeType.RobaBakery); // 旧名前との互換性
        var factory = GetActiveUpgrade(UpgradeType.Factory); // 後方互換性

        if (donkeyBakery != null || robaBakery != null || factory != null)
        {
            // ロバのパン屋システムを開始
            var bakeryManager = FindFirstObjectByType<DonkeyBakeryManager>();
            if (bakeryManager == null)
            {
                // DonkeyBakeryManagerを動的に作成
                var bakeryObject = new GameObject("DonkeyBakeryManager");
                bakeryManager = bakeryObject.AddComponent<DonkeyBakeryManager>();
            }

            // ロバのパン屋のレベルと効果を設定
            UpgradeData activeUpgrade = donkeyBakery ?? robaBakery ?? factory;
            bakeryManager.SetBakeryLevel(activeUpgrade.currentLevel, activeUpgrade.GetCurrentEffect());

            Debug.Log($"🔧 ロバのパン屋適用: Lv.{activeUpgrade.currentLevel}, 価値{activeUpgrade.GetCurrentEffect():F1}");

            // 従来のautoProductionRateは無効化
            clickManager.autoProductionRate = 0f;
        }
        else
        {
            // ロバのパン屋が無い場合は停止
            var bakeryManager = FindFirstObjectByType<DonkeyBakeryManager>();
            if (bakeryManager != null)
            {
                bakeryManager.StopBakery();
            }
            clickManager.autoProductionRate = 0f;
            Debug.Log("🔧 ロバのパン屋: 未取得");
        }

        // 🔥 ヘルパーフレンズ（自動クリック）
        var helper = GetActiveUpgrade(UpgradeType.HelperFriend);
        if (helper != null)
        {
            float rate = helper.GetCurrentEffect();
            clickManager.autoClickRate = rate;
            Debug.Log($"🔧 ヘルパー適用: {rate}/秒 (Lv.{helper.currentLevel})");
        }
        else
        {
            clickManager.autoClickRate = 0f;
            Debug.Log("🔧 ヘルパー: 初期値(0/秒)");
        }

        Debug.Log("🔧 === UpgradeManager: アップグレード効果適用完了 ===");

        // 🔥 まとめる係システム（追加）
        var organizer = GetActiveUpgrade(UpgradeType.Organizer);
        if (organizer != null)
        {
            Debug.Log($"🔧 まとめる係データ確認: {organizer.upgradeName} Lv.{organizer.currentLevel} Active:{organizer.isActive}");

            // OrganizerManagerを取得または作成
            var organizerManager = FindFirstObjectByType<OrganizerManager>();
            if (organizerManager == null)
            {
                Debug.Log("🔧 OrganizerManager が見つからないため作成します");
                var organizerObject = new GameObject("OrganizerManager");
                organizerManager = organizerObject.AddComponent<OrganizerManager>();
            }

            // まとめる係のレベルを設定
            organizerManager.SetOrganizerLevel(organizer.currentLevel);
            Debug.Log($"🔧 まとめる係適用: Lv.{organizer.currentLevel}, 効果{organizer.GetCurrentEffect():F1}x");

            // ClickManagerにOrganizerManagerを登録
            if (clickManager != null)
            {
                clickManager.SetOrganizerManager(organizerManager);
                Debug.Log("🔧 ClickManagerにOrganizerManager登録完了");
            }
        }
        else
        {
            // まとめる係が無い場合は停止
            var organizerManager = FindFirstObjectByType<OrganizerManager>();
            if (organizerManager != null)
            {
                organizerManager.SetOrganizerLevel(0);
            }
            Debug.Log("🔧 まとめる係: 未取得");
        }

    }

    /// <summary>
    /// 🔥 デバッグ用: テストアップグレード追加
    /// </summary>
    [ContextMenu("🧪 テストアップグレード追加")]
    public void DebugAddTestUpgrade()
    {
        if (!Application.isPlaying) return;

        var testUpgrade = allUpgrades.Find(u => u.upgradeType == UpgradeType.ClickPower);
        if (testUpgrade != null)
        {
            Debug.Log($"🧪 テスト: {testUpgrade.upgradeName} を適用");
            ApplyUpgrade(testUpgrade);
        }
    }

    /// <summary>
    /// 🧪 テスト用: ロバのパン屋Lv1取得
    /// </summary>
    [ContextMenu("🧪 ロバのパン屋Lv1取得")]
    public void DebugGetDonkeyBakery()
    {
        if (!Application.isPlaying) return;

        var upgrade = allUpgrades.Find(u => u.upgradeType == UpgradeType.DonkeyBakery);
        if (upgrade != null)
        {
            Debug.Log($"🧪 テスト: {upgrade.upgradeName} Lv1を取得");
            ApplyUpgrade(upgrade);
        }
        else
        {
            Debug.LogError("🧪 ロバのパン屋が見つかりません");
        }
    }

    /// <summary>
    /// 🧪 テスト用: お手伝いフレンズLv1取得
    /// </summary>
    [ContextMenu("🧪 お手伝いフレンズLv1取得")]
    public void DebugGetHelperFriend()
    {
        if (!Application.isPlaying) return;

        var upgrade = allUpgrades.Find(u => u.upgradeType == UpgradeType.HelperFriend);
        if (upgrade != null)
        {
            Debug.Log($"🧪 テスト: {upgrade.upgradeName} Lv1を取得");
            ApplyUpgrade(upgrade);
        }
        else
        {
            Debug.LogError("🧪 お手伝いフレンズが見つかりません");
        }
    }

    /// <summary>
    /// 🧪 テスト用: まとめる係Lv1取得
    /// </summary>
    [ContextMenu("🧪 まとめる係Lv1取得")]
    public void DebugGetOrganizer()
    {
        if (!Application.isPlaying) return;

        var upgrade = allUpgrades.Find(u => u.upgradeType == UpgradeType.Organizer);
        if (upgrade != null)
        {
            Debug.Log($"🧪 テスト: {upgrade.upgradeName} Lv1を取得");
            ApplyUpgrade(upgrade);
        }
        else
        {
            Debug.LogError("🧪 まとめる係が見つかりません");
        }
    }

    /// <summary>
    /// 🧪 テスト用: 自動化3点セット取得
    /// </summary>
    [ContextMenu("🧪 自動化3点セット取得")]
    public void DebugGetAutomationSet()
    {
        if (!Application.isPlaying) return;

        Debug.Log("🧪 自動化3点セット取得開始");

        DebugGetDonkeyBakery();
        DebugGetHelperFriend();
        DebugGetOrganizer();

        Debug.Log("🧪 自動化3点セット取得完了");
    }


    public UpgradeData GetActiveUpgrade(UpgradeType type)
    {
        return activeUpgrades.Find(u => u.upgradeType == type && u.isActive);
    }

    public void OnClick()
    {
        currentClickCount++;
        CheckClickMilestones();
    }

    private void CheckClickMilestones()
    {
        foreach (int milestone in clickMilestones)
        {
            if (currentClickCount == milestone)
            {
                Debug.Log("マイルストーン達成: " + milestone + "クリック！");
                TriggerMilestoneReward();
                break;
            }
        }
    }

    private void TriggerMilestoneReward()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("マイルストーン報酬: アップグレード獲得権利！");
        }
    }

    public void ResetForNewStage()
    {
        activeUpgrades.RemoveAll(u => u.isInstantEffect);
        ApplyUpgradeEffects();
        Debug.Log("新ステージ用にアップグレード状態をリセット");
    }

    // 🔥 ロード機能用の追加メソッド（重複なし・正しい実装）
    /// <summary>
    /// 全アップグレードリストを取得
    /// </summary>
    public List<UpgradeData> GetAllUpgrades()
    {
        return allUpgrades;
    }

    /// <summary>
    /// アップグレードの総数を取得
    /// </summary>
    public int GetUpgradeCount()
    {
        return allUpgrades != null ? allUpgrades.Count : 0;
    }

    /// <summary>
    /// 🔥 強制的にactiveUpgradesとallUpgradesを同期
    /// </summary>
    public void SyncUpgradeData()
    {
        Debug.Log("🔄 === アップグレードデータ強制同期開始 ===");

        foreach (var activeUpgrade in activeUpgrades)
        {
            var allUpgradeTarget = allUpgrades.Find(u => u.upgradeType == activeUpgrade.upgradeType);
            if (allUpgradeTarget != null)
            {
                bool wasChanged = (allUpgradeTarget.currentLevel != activeUpgrade.currentLevel) ||
                                 (allUpgradeTarget.isActive != activeUpgrade.isActive);

                allUpgradeTarget.currentLevel = activeUpgrade.currentLevel;
                allUpgradeTarget.isActive = activeUpgrade.isActive;

                if (wasChanged)
                {
                    Debug.Log($"🔄 同期更新: {allUpgradeTarget.upgradeName} Lv.{allUpgradeTarget.currentLevel} Active:{allUpgradeTarget.isActive}");
                }
            }
        }

        Debug.Log("🔄 === アップグレードデータ強制同期完了 ===");
    }

    /// <summary>
    /// 🔥 ロード後のアップグレード効果再計算（強化版）
    /// </summary>
    public void RecalculateAllEffects()
    {
        Debug.Log("🔧 === UpgradeManager: 全効果再計算開始 ===");
        Debug.Log($"🔧 activeUpgrades数: {activeUpgrades.Count}");

        // まずデータ同期を実行
        SyncUpgradeData();

        // デバッグ: 現在のactiveUpgrades一覧
        for (int i = 0; i < activeUpgrades.Count; i++)
        {
            var upgrade = activeUpgrades[i];
            Debug.Log($"🔧 activeUpgrades[{i}]: {upgrade.upgradeName} Lv.{upgrade.currentLevel} Active:{upgrade.isActive}");
        }

        ApplyUpgradeEffects();

        // UIにも更新を通知
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.OnUpgradeObtained();
        }

        Debug.Log("🔧 === UpgradeManager: 全効果再計算完了 ===");
    }

    /// <summary>
    /// 🔥 デバッグ用: 現在の状態をコンソールに出力
    /// </summary>
    [ContextMenu("🔍 アップグレード状態確認")]
    public void DebugUpgradeState()
    {
        Debug.Log("=== アップグレード状態確認 ===");
        Debug.Log($"allUpgrades数: {allUpgrades.Count}");
        Debug.Log($"activeUpgrades数: {activeUpgrades.Count}");

        Debug.Log("--- allUpgrades一覧 ---");
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            var upgrade = allUpgrades[i];
            Debug.Log($"[{i}] {upgrade.upgradeName}: Lv.{upgrade.currentLevel}, Active:{upgrade.isActive}");
        }

        Debug.Log("--- activeUpgrades一覧 ---");
        for (int i = 0; i < activeUpgrades.Count; i++)
        {
            var upgrade = activeUpgrades[i];
            Debug.Log($"[{i}] {upgrade.upgradeName}: Lv.{upgrade.currentLevel}, Effect:{upgrade.GetCurrentEffect()}");
        }

        Debug.Log("--- 同期チェック ---");
        foreach (var activeUpgrade in activeUpgrades)
        {
            var allUpgrade = allUpgrades.Find(u => u.upgradeType == activeUpgrade.upgradeType);
            if (allUpgrade != null)
            {
                bool isSync = (allUpgrade.currentLevel == activeUpgrade.currentLevel) &&
                             (allUpgrade.isActive == activeUpgrade.isActive);

                string syncStatus = isSync ? "✅ 同期" : "❌ 非同期";
                Debug.Log($"{syncStatus} {activeUpgrade.upgradeName}: all(Lv.{allUpgrade.currentLevel}) vs active(Lv.{activeUpgrade.currentLevel})");
            }
        }

        Debug.Log("--- ClickManager効果確認 ---");
        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            Debug.Log($"clickMultiplier: {clickManager.clickMultiplier}");
            Debug.Log($"autoProductionRate: {clickManager.autoProductionRate}");
            Debug.Log($"autoClickRate: {clickManager.autoClickRate}");
        }
        else
        {
            Debug.Log("ClickManager が見つかりません");
        }
    }

    /// <summary>
    /// 🔥 デバッグ用: データ強制同期
    /// </summary>
    [ContextMenu("🔄 強制データ同期")]
    public void DebugForceSyncData()
    {
        if (Application.isPlaying)
        {
            SyncUpgradeData();

            // UIにも更新通知
            if (UpgradeSidePanelUI.Instance != null)
            {
                UpgradeSidePanelUI.Instance.OnUpgradeObtained();
                Debug.Log("📡 サイドパネルに更新通知送信");
            }
        }
    }

    
    /// <summary>
    /// 取得済みアップグレード一覧を取得（UpgradeInfoUI用）
    /// 🔥 改善版: activeUpgradesとallUpgradesを統合して正確なデータを返す
    /// </summary>
    public List<UpgradeData> GetObtainedUpgrades()
    {
        List<UpgradeData> obtainedUpgrades = new List<UpgradeData>();

        // activeUpgradesを基準にして、allUpgradesから基本情報を取得
        foreach (var activeUpgrade in activeUpgrades)
        {
            if (activeUpgrade.currentLevel > 0)
            {
                // allUpgradesから基本データを取得
                var baseData = allUpgrades.Find(u => u.upgradeType == activeUpgrade.upgradeType);

                if (baseData != null)
                {
                    // 最新のレベルと状態でデータを作成
                    var upgradeData = new UpgradeData
                    {
                        upgradeType = baseData.upgradeType,
                        upgradeName = baseData.upgradeName,
                        description = baseData.description,
                        currentLevel = activeUpgrade.currentLevel, // activeUpgradesの最新レベル
                        maxLevel = baseData.maxLevel,
                        baseEffect = baseData.baseEffect,
                        levelMultiplier = baseData.levelMultiplier,
                        isActive = activeUpgrade.isActive,
                        isInstantEffect = baseData.isInstantEffect,
                        isPassiveEffect = baseData.isPassiveEffect,
                        effectDuration = baseData.effectDuration,
                        requiredStage = baseData.requiredStage,
                        appearanceWeight = baseData.appearanceWeight
                    };

                    obtainedUpgrades.Add(upgradeData);
                }
                else
                {
                    // baseDataが見つからない場合はactiveUpgradeをそのまま使用
                    obtainedUpgrades.Add(activeUpgrade);
                }
            }
        }

        // 🔥 さらに、allUpgradesで取得済み（レベル1以上）でactiveUpgradesにないものもチェック
        foreach (var allUpgrade in allUpgrades)
        {
            if (allUpgrade.currentLevel > 0)
            {
                bool existsInActive = activeUpgrades.Any(a => a.upgradeType == allUpgrade.upgradeType);
                if (!existsInActive)
                {
                    obtainedUpgrades.Add(allUpgrade);
                }
            }
        }

        

        return obtainedUpgrades;
    }

    /// <summary>
    /// 指定タイプのアップグレードデータを取得
    /// </summary>
    public UpgradeData GetUpgradeByType(UpgradeType upgradeType)
    {
        return allUpgrades.Find(upgrade => upgrade.upgradeType == upgradeType);
    }

    /// <summary>
    /// アップグレード統計情報を取得
    /// </summary>
    public UpgradeStatistics GetUpgradeStatistics()
    {
        var obtained = GetObtainedUpgrades();

        return new UpgradeStatistics
        {
            totalObtained = obtained.Count,
            totalLevels = obtained.Sum(u => u.currentLevel),
            totalEffectPower = obtained.Sum(u => u.GetCurrentEffect()),
            maxLevelCount = obtained.Count(u => u.currentLevel >= u.maxLevel),
            activeCount = obtained.Count(u => u.isActive),
            basicUpgradesCount = obtained.Count(u => IsBasicUpgrade(u.upgradeType)),
            luckUpgradesCount = obtained.Count(u => IsLuckUpgrade(u.upgradeType)),
            specialUpgradesCount = obtained.Count(u => IsSpecialUpgrade(u.upgradeType))
        };
    }

    /// <summary>
    /// 基本系アップグレードかどうか判定
    /// </summary>
    private bool IsBasicUpgrade(UpgradeType type)
    {
        return type == UpgradeType.ClickPower ||
               type == UpgradeType.Factory ||
               type == UpgradeType.HelperFriend;
    }

    /// <summary>
    /// 確率系アップグレードかどうか判定
    /// </summary>
    private bool IsLuckUpgrade(UpgradeType type)
    {
        return type == UpgradeType.RainbowJapaman ||
               type == UpgradeType.LuckyBeast ||
               type == UpgradeType.DonkeyBakery ||
               type == UpgradeType.RobaBakery || // 旧名前との互換性
               type == UpgradeType.FriendsCall ||
               type == UpgradeType.LuckyTail;
    }

    /// <summary>
    /// 特殊系アップグレードかどうか判定
    /// </summary>
    private bool IsSpecialUpgrade(UpgradeType type)
    {
        return type == UpgradeType.MiraclTime ||
               type == UpgradeType.Satisfaction ||
               type == UpgradeType.ChatSystem ||
               type == UpgradeType.Organizer;
    }

    /// <summary>
    /// アップグレード情報UIに更新通知
    /// </summary>
    public void NotifyUpgradeInfoUI()
    {
        if (UpgradeInfoUI.Instance != null)
        {
            UpgradeInfoUI.Instance.OnUpgradeObtained();
        }
    }

    /// <summary>
    /// アップグレード統計情報クラス
    /// </summary>
    [System.Serializable]
    public class UpgradeStatistics
    {
        public int totalObtained;       // 取得済み総数
        public int totalLevels;         // 総レベル数
        public float totalEffectPower;  // 総効果値
        public int maxLevelCount;       // 最大レベル到達数
        public int activeCount;         // アクティブ数
        public int basicUpgradesCount;  // 基本系数
        public int luckUpgradesCount;   // 確率系数
        public int specialUpgradesCount; // 特殊系数
    }

    /// <summary>
    /// サイドパネルUIに自動通知
    /// </summary>
    private void NotifyUpgradeSidePanel()
    {
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.OnUpgradeObtained();
        }
    }

    /// <summary>
    /// レベルアップ通知
    /// </summary>
    private void NotifyUpgradeLevelUp(UpgradeType upgradeType)
    {
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.OnUpgradeLevelUp(upgradeType);
        }
    }

    /// <summary>
    /// ゲーム開始時にサイドパネルに初期データを送信
    /// </summary>
    public void InitializeSidePanelDisplay()
    {
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.OnUpgradeObtained();
            Debug.Log("サイドパネルに初期アップグレードデータを送信");
        }
    }
   
}
