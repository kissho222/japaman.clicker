using UnityEngine;

[System.Serializable]
public enum ContainerType
{
    SmallPlate,           // ステージ1-2: 小さなお皿
    LargePlate,           // ステージ3-4: 大きなお皿
    Basin,                // ステージ5-6: たらい
    EmptyPool,            // ステージ7-8: 空のプール
    GiantStoneVessel,     // ステージ9-10: 巨大な石の器
    VolcanoCrater,        // ステージ11-12: 火山の火口
    SpaceFloatingVessel   // ステージ13-15: 宇宙に浮かぶ器（体験版最終）
}

[System.Serializable]
public class ContainerData
{
    [Header("器の基本情報")]
    public ContainerType containerType;
    public string containerName;
    public GameObject containerPrefab;

    [Header("ジャパまん配置設定")]
    [Range(0.1f, 2.0f)]
    public float sizeMultiplier = 0.8f;      // 器のサイズに対する使用範囲

    [Range(0.0f, 0.5f)]
    public float centerBias = 0.1f;          // 中央寄り度合い

    [Range(0.3f, 1.0f)]
    public float maxRadius = 0.8f;           // 最大配置半径

    [Header("落下設定")]
    public float minDropHeight = 300f;       // 最小落下高度（高く設定）
    public float maxDropHeight = 500f;       // 最大落下高度（高く設定）

    [Header("配置パターン")]
    public bool useCircularPattern = true;   // 円形配置
    public bool useStackingPattern = false;  // 積み上げ配置
    public float stackingHeight = 20f;       // 積み上げ時の高さ間隔

    [Header("特殊設定")]
    public Vector2 centerOffset = Vector2.zero;  // 中心位置のオフセット
    public bool useGravityFall = true;           // 重力落下を使用
}

public class ContainerSettings : MonoBehaviour
{
    [Header("器データ")]
    public ContainerData[] containerDatabase;

    [Header("現在の器")]
    public ContainerType currentContainer = ContainerType.SmallPlate;

    public static ContainerSettings Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        InitializeContainerDatabase();
    }

    private void InitializeContainerDatabase()
    {
        containerDatabase = new ContainerData[] {
            // ステージ1-2: 小さなお皿
            new ContainerData {
                containerType = ContainerType.SmallPlate,
                containerName = "小さなお皿",
                sizeMultiplier = 0.8f,
                centerBias = 0.1f,
                maxRadius = 0.7f,
                minDropHeight = 300f,  // ★★★ 150f → 300f に変更 ★★★
                maxDropHeight = 450f,  // ★★★ 250f → 450f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = false
            },
            
            // ステージ3-4: 大きなお皿
            new ContainerData {
                containerType = ContainerType.LargePlate,
                containerName = "大きなお皿",
                sizeMultiplier = 0.9f,
                centerBias = 0.15f,
                maxRadius = 0.8f,
                minDropHeight = 350f,  // ★★★ 180f → 350f に変更 ★★★
                maxDropHeight = 500f,  // ★★★ 280f → 500f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = false
            },
            
            // ステージ5-6: たらい
            new ContainerData {
                containerType = ContainerType.Basin,
                containerName = "たらい",
                sizeMultiplier = 0.7f,
                centerBias = 0.05f,
                maxRadius = 0.6f,
                minDropHeight = 400f,  // ★★★ 200f → 400f に変更 ★★★
                maxDropHeight = 550f,  // ★★★ 320f → 550f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = true,
                stackingHeight = 15f
            },
            
            // ステージ7-8: 空のプール
            new ContainerData {
                containerType = ContainerType.EmptyPool,
                containerName = "空のプール",
                sizeMultiplier = 0.9f,
                centerBias = 0.2f,
                maxRadius = 0.8f,
                minDropHeight = 450f,  // ★★★ 250f → 450f に変更 ★★★
                maxDropHeight = 600f,  // ★★★ 350f → 600f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = false,
                centerOffset = new Vector2(0, -5f)
            },
            
            // ステージ9-10: 巨大な石の器
            new ContainerData {
                containerType = ContainerType.GiantStoneVessel,
                containerName = "巨大な石の器",
                sizeMultiplier = 1.0f,
                centerBias = 0.25f,
                maxRadius = 0.9f,
                minDropHeight = 500f,  // ★★★ 280f → 500f に変更 ★★★
                maxDropHeight = 650f,  // ★★★ 400f → 650f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = false,
                centerOffset = new Vector2(0, -10f)
            },
            
            // ステージ11-12: 火山の火口
            new ContainerData {
                containerType = ContainerType.VolcanoCrater,
                containerName = "火山の火口",
                sizeMultiplier = 0.8f,
                centerBias = 0.15f,
                maxRadius = 0.7f,
                minDropHeight = 550f,  // ★★★ 300f → 550f に変更 ★★★
                maxDropHeight = 700f,  // ★★★ 450f → 700f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = true,
                stackingHeight = 25f,
                centerOffset = new Vector2(0, -15f)
            },
            
            // ステージ13-15: 宇宙に浮かぶ器
            new ContainerData {
                containerType = ContainerType.SpaceFloatingVessel,
                containerName = "宇宙に浮かぶ器",
                sizeMultiplier = 1.0f,
                centerBias = 0.1f,
                maxRadius = 0.8f,
                minDropHeight = 600f,  // ★★★ 350f → 600f に変更 ★★★
                maxDropHeight = 800f,  // ★★★ 500f → 800f に変更 ★★★
                useCircularPattern = true,
                useStackingPattern = false,
                useGravityFall = false,
                centerOffset = Vector2.zero
            }
        };

        Debug.Log("器データベース初期化完了（体験版・高落下位置）: " + containerDatabase.Length + "種類");
    }

    // 現在の器のデータを取得
    public ContainerData GetCurrentContainerData()
    {
        foreach (var container in containerDatabase)
        {
            if (container.containerType == currentContainer)
            {
                return container;
            }
        }
        return containerDatabase[0];  // デフォルト
    }

    // 器を変更
    public void ChangeContainer(ContainerType newContainer)
    {
        currentContainer = newContainer;
        var data = GetCurrentContainerData();
        Debug.Log("器を変更: " + data.containerName);

        // ClickManagerに通知
        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.OnContainerChanged();
        }
    }

    // ステージに応じて器を自動変更（体験版1-15ステージ）
    public void UpdateContainerForStage(int stage)
    {
        ContainerType newContainer = ContainerType.SmallPlate;

        if (stage <= 2) newContainer = ContainerType.SmallPlate;
        else if (stage <= 4) newContainer = ContainerType.LargePlate;
        else if (stage <= 6) newContainer = ContainerType.Basin;
        else if (stage <= 8) newContainer = ContainerType.EmptyPool;
        else if (stage <= 10) newContainer = ContainerType.GiantStoneVessel;
        else if (stage <= 12) newContainer = ContainerType.VolcanoCrater;
        else newContainer = ContainerType.SpaceFloatingVessel;  // ステージ13-15

        if (newContainer != currentContainer)
        {
            ChangeContainer(newContainer);
        }
    }

    // 体験版の最終ステージかチェック
    public bool IsLastStageOfDemo(int stage)
    {
        return stage >= 15;
    }

    // 体験版終了メッセージ
    public void ShowDemoEndMessage()
    {
        Debug.Log("体験版終了！続きは完全版で...");
        // ここで購入誘導UIを表示
    }
}