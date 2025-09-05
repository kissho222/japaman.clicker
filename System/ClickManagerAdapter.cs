using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ClickManagerと新しいPlateItemSystemを統合するアダプター
/// 既存コードを最小限の変更で新システムに対応
/// </summary>
public class ClickManagerAdapter : MonoBehaviour
{
    [Header("統合設定")]
    public bool useNewPlateSystem = true;       // 新システムを使用するか
    public bool enableItemConsolidation = true; // アイテム統合を有効にするか

    // 参照
    private ClickManager clickManager;
    private PlateItemSystem plateItemSystem;

    public static ClickManagerAdapter Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeAdapter();
    }

    private void InitializeAdapter()
    {
        // ClickManagerを取得
        clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager == null)
        {
            Debug.LogError("ClickManagerが見つかりません");
            return;
        }

        // PlateItemSystemを取得または作成
        plateItemSystem = FindFirstObjectByType<PlateItemSystem>();
        if (plateItemSystem == null && useNewPlateSystem)
        {
            // PlateItemSystemを動的作成
            var plateSystemObj = new GameObject("PlateItemSystem");
            plateItemSystem = plateSystemObj.AddComponent<PlateItemSystem>();

            // 設定コピー
            CopySettingsToPlateSystem();
        }

        // ClickManagerの既存メソッドをフック
        HookClickManagerMethods();

        Debug.Log("🔗 ClickManagerAdapter初期化完了");
    }

    /// <summary>
    /// 設定をPlateItemSystemにコピー
    /// </summary>
    private void CopySettingsToPlateSystem()
    {
        if (plateItemSystem == null || clickManager == null) return;

        // リフレクションでplateContainerを取得
        var plateContainerField = typeof(ClickManager).GetField("plateContainer",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (plateContainerField != null)
        {
            var plateContainer = plateContainerField.GetValue(clickManager) as Transform;
            if (plateContainer != null)
            {
                plateItemSystem.plateContainer = plateContainer;
            }
        }

        // その他の設定
        plateItemSystem.enableAutoConsolidation = enableItemConsolidation;
    }

    /// <summary>
    /// ClickManagerのメソッドをフック（既存コード互換性のため）
    /// </summary>
    private void HookClickManagerMethods()
    {
        // 新システムを使用する場合、ClickManagerの一部機能を無効化
        if (useNewPlateSystem && clickManager != null)
        {
            Debug.Log("🔗 新PlateItemSystemに処理を移行");
        }
    }

    /// <summary>
    /// クリック時のジャパまん追加処理（新システム対応）
    /// </summary>
    public void OnJapamanClick(int clickMultiplier, long currentJapamanCount, long goalCount)
    {
        if (!useNewPlateSystem)
        {
            // 従来システムを使用
            return;
        }

        // 新システムでの処理
        for (int i = 0; i < clickMultiplier; i++)
        {
            if (currentJapamanCount + i < goalCount)
            {
                // 通常ジャパまんをプレートに追加
                AddJapamanToPlate(PlateItemType.NormalJapaman, 1f);
            }
            else
            {
                // 直接投げ込み（既存システム継続使用）
                break;
            }
        }
    }

    /// <summary>
    /// ジャパまんをプレートに追加
    /// </summary>
    public void AddJapamanToPlate(PlateItemType itemType, float value, int count = 1)
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            plateItemSystem.AddItemToPlate(itemType, value, count);
        }
        else
        {
            // フォールバック：従来システム
            FallbackToOldSystem(value, count);
        }
    }

    /// <summary>
    /// ジャパリパンをプレートに追加
    /// </summary>
    public void AddJapariPanToPlate(float panValue)
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            plateItemSystem.AddItemToPlate(PlateItemType.JapariPan, panValue, 1);
        }
        else
        {
            // フォールバック：ClickManagerに直接加算
            if (clickManager != null)
            {
                clickManager.AddJapamanFromPan(panValue);
            }
        }
    }

    /// <summary>
    /// レアジャパまん追加
    /// </summary>
    public void AddRareJapaman(PlateItemType rareType, float value)
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            plateItemSystem.AddItemToPlate(rareType, value, 1);
        }
        else
        {
            FallbackToOldSystem(value, 1);
        }
    }

    /// <summary>
    /// 従来システムへのフォールバック
    /// </summary>
    private void FallbackToOldSystem(float value, int count)
    {
        if (clickManager != null)
        {
            for (int i = 0; i < count; i++)
            {
                clickManager.AddJapamanFromPan(value);
            }
        }
    }

    /// <summary>
    /// 全アイテム吸い込み処理
    /// </summary>
    public void SuckAllItems()
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            plateItemSystem.SuckAllItems();
        }
        else
        {
            // 従来システムの吸い込み処理を呼び出し
            if (clickManager != null)
            {
                clickManager.StartSuckAllJapamanToMouth();
            }
        }
    }

    /// <summary>
    /// 新ステージリセット
    /// </summary>
    public void ResetForNewStage()
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            plateItemSystem.ResetForNewStage();
        }

        // ClickManagerのリセットも呼び出し
        if (clickManager != null)
        {
            clickManager.ResetForNewStage();
        }
    }

    /// <summary>
    /// 敵による盗難処理用のアイテム取得
    /// </summary>
    public PlateItem[] GetStealableItems()
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            var allItems = plateItemSystem.GetPlateItems();
            return allItems.FindAll(item => item.IsStealable && !item.IsBeingStolen).ToArray();
        }

        return new PlateItem[0];
    }

    /// <summary>
    /// アイテム統計取得
    /// </summary>
    public PlateSystemStatistics GetPlateStatistics()
    {
        if (useNewPlateSystem && plateItemSystem != null)
        {
            var itemStats = plateItemSystem.GetItemStatistics();
            return new PlateSystemStatistics
            {
                totalItems = itemStats.Values.Sum(),
                totalValue = plateItemSystem.GetTotalValue(),
                normalJapamanCount = plateItemSystem.GetItemCount(PlateItemType.NormalJapaman),
                silverJapamanCount = plateItemSystem.GetItemCount(PlateItemType.SilverJapaman),
                goldJapamanCount = plateItemSystem.GetItemCount(PlateItemType.GoldJapaman),
                japariPanCount = plateItemSystem.GetItemCount(PlateItemType.JapariPan),
                rareItemCount = plateItemSystem.GetItemCount(PlateItemType.RainbowJapaman)
            };
        }

        // フォールバック：従来システムの統計
        return new PlateSystemStatistics
        {
            totalItems = (int)(clickManager?.GetPlateJapamanCount() ?? 0),
            totalValue = clickManager?.GetTotalJapamanCount() ?? 0,
            normalJapamanCount = (int)(clickManager?.GetPlateJapamanCount() ?? 0)
        };
    }

    /// <summary>
    /// システム切り替え
    /// </summary>
    public void SwitchToNewSystem(bool useNew)
    {
        useNewPlateSystem = useNew;

        if (useNew && plateItemSystem == null)
        {
            // PlateItemSystemを作成
            var plateSystemObj = new GameObject("PlateItemSystem");
            plateItemSystem = plateSystemObj.AddComponent<PlateItemSystem>();
            CopySettingsToPlateSystem();
        }

        Debug.Log($"🔄 プレートシステム切り替え: {(useNew ? "新システム" : "従来システム")}");
    }

    /// <summary>
    /// デバッグ用：システム状態表示
    /// </summary>
    [ContextMenu("🔍 システム状態確認")]
    public void DebugShowSystemState()
    {
        Debug.Log("=== ClickManagerAdapter状態 ===");
        Debug.Log($"新システム使用: {useNewPlateSystem}");
        Debug.Log($"アイテム統合: {enableItemConsolidation}");
        Debug.Log($"ClickManager: {(clickManager != null ? "有効" : "無効")}");
        Debug.Log($"PlateItemSystem: {(plateItemSystem != null ? "有効" : "無効")}");

        if (useNewPlateSystem && plateItemSystem != null)
        {
            var stats = GetPlateStatistics();
            Debug.Log($"--- 新システム統計 ---");
            Debug.Log($"総アイテム数: {stats.totalItems}");
            Debug.Log($"総価値: {stats.totalValue}");
            Debug.Log($"通常ジャパまん: {stats.normalJapamanCount}");
            Debug.Log($"銀ジャパまん: {stats.silverJapamanCount}");
            Debug.Log($"金ジャパまん: {stats.goldJapamanCount}");
            Debug.Log($"ジャパリパン: {stats.japariPanCount}");
            Debug.Log($"レアアイテム: {stats.rareItemCount}");
        }
    }
}

/// <summary>
/// プレートシステム統計情報
/// </summary>
[System.Serializable]
public class PlateSystemStatistics
{
    public int totalItems;
    public long totalValue;
    public int normalJapamanCount;
    public int silverJapamanCount;
    public int goldJapamanCount;
    public int japariPanCount;
    public int rareItemCount;
}