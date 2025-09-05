using UnityEngine;
using System.Collections;

/// <summary>
/// まとめる係アップグレードの管理
/// 正しい仕様版：クリックしていない間だけブースト、クリックしたらブースト解除
/// </summary>
public class OrganizerManager : MonoBehaviour
{
    [Header("まとめる係設定")]
    [SerializeField] private float baseActivationTime = 10f; // Lv1で10秒
    [SerializeField] private float boostMultiplier = 1.5f; // ブースト倍率

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;
    //[SerializeField] private bool showDebugUI = false;

    // 内部状態
    private float timeSinceLastClick = 0f; // 最後のクリックからの経過時間
    private bool isOrganizerActive = false; // まとめる係が有効かどうか
    private bool isBoostActive = false; // 現在ブースト中かどうか
    private float gameStartTime = 0f; // ゲーム開始時刻
    private bool isGameActive = false; // ゲームがアクティブかどうか
    private float currentActivationTime = 10f; // 現在の発動時間（レベルに応じて変化）

    // 参照
    private ClickManager clickManager;
    private UpgradeManager upgradeManager;

    // イベント
    public System.Action<bool> OnBoostStateChanged; // ブースト状態変更通知
    public System.Action<float> OnTimerUpdated; // タイマー更新通知

    private void Awake()
    {
        // 参照を取得
        clickManager = FindFirstObjectByType<ClickManager>();
        upgradeManager = UpgradeManager.Instance;
    }

    private void Start()
    {
        if (enableDebugLog)
            Debug.Log("🏢 OrganizerManager初期化完了");
    }

    private void Update()
    {
        // 🔥 デバッグ用：条件チェック詳細ログ（一時的）
        if (!isGameActive)
        {
            //Debug.Log($"⏰ まとめる係: ゲーム非アクティブのため時間更新停止 (isGameActive: {isGameActive})");
            return;
        }

        if (!isOrganizerActive)
        {
            //Debug.Log($"⏰ まとめる係: 機能無効のため時間更新停止 (isOrganizerActive: {isOrganizerActive})");
            return;
        }



        // ゲームがアクティブでまとめる係が有効な場合のみ更新
        if (!isGameActive || !isOrganizerActive) return;

        // 未クリック時間を更新
        timeSinceLastClick += Time.deltaTime;

        // タイマー更新通知
        OnTimerUpdated?.Invoke(timeSinceLastClick);

        // 🔥 ブースト開始条件：発動時間に達し、まだブーストしていない場合
        if (!isBoostActive && timeSinceLastClick >= currentActivationTime)
        {
            StartOrganizerBoost();
        }
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;
        Debug.Log($"🏢 まとめる係ゲーム状態変更: {active}");

        if (active)
        {
            // ゲーム開始時の初期化
            timeSinceLastClick = 0f;
            isBoostActive = false;
        }
    }

    /// <summary>
    /// ラウンド開始時の処理（カウントダウン完了時に呼び出される）
    /// </summary>
    public void OnRoundStart()
    {
        if (enableDebugLog)
            Debug.Log("🏢 === まとめる係 ラウンド開始処理 ===");

        // ラウンド開始時は未クリック時間を0秒にリセット
        timeSinceLastClick = 0f;
        gameStartTime = Time.time;
        isGameActive = true;
        isBoostActive = false;

        // まとめる係アップグレードの有効性をチェック
        UpdateOrganizerStatus();

        if (enableDebugLog)
        {
            Debug.Log($"🏢 ✅ ラウンド開始時リセット完了:");
            Debug.Log($"🏢 - timeSinceLastClick: {timeSinceLastClick}秒");
            Debug.Log($"🏢 - isOrganizerActive: {isOrganizerActive}");
            Debug.Log($"🏢 - 発動時間: {currentActivationTime}秒");
        }
    }

    /// <summary>
    /// 新ステージ開始時の処理
    /// </summary>
    public void OnNewStage()
    {
        if (enableDebugLog)
            Debug.Log("🏢 === まとめる係 新ステージ処理 ===");

        // 完全リセット
        ResetOrganizerState();

        // まとめる係アップグレードの有効性を再確認
        UpdateOrganizerStatus();

        if (enableDebugLog)
            Debug.Log("🏢 ✅ 新ステージ初期化完了");
    }

    /// <summary>
    /// プレイヤーがクリックした時の処理
    /// 🔥 正しい仕様：クリック時にブースト解除＆時間リセット
    /// </summary>
    public void OnPlayerClick()
    {
        if (!isGameActive || !isOrganizerActive) return;

        if (enableDebugLog)
        {
            Debug.Log($"🏢 👆 プレイヤークリック検出:");
            Debug.Log($"🏢 - クリック前の未クリック時間: {timeSinceLastClick:F1}秒");
            Debug.Log($"🏢 - クリック前のブースト状態: {isBoostActive}");
        }

        // 🔥 正しい仕様：クリック時に未クリック時間を0秒にリセット
        timeSinceLastClick = 0f;

        // 🔥 正しい仕様：クリック時にブーストを解除
        if (isBoostActive)
        {
            StopOrganizerBoost();
            if (enableDebugLog)
                Debug.Log("🏢 ❌ プレイヤークリックによりブースト解除");
        }

        if (enableDebugLog)
        {
            Debug.Log($"🏢 ✅ クリック処理完了:");
            Debug.Log($"🏢 - 未クリック時間リセット: {timeSinceLastClick}秒");
            Debug.Log($"🏢 - 次回ブースト発動まで: {currentActivationTime}秒");
        }
    }

    /// <summary>
    /// GameManagerのOnCountdownComplete()から呼び出される
    /// カウントダウン終了＝ゲーム実際の開始時点
    /// </summary>
    public void OnGameplayStart()
    {
        if (enableDebugLog)
            Debug.Log("🏢 🎮 ゲームプレイ開始 - まとめる係タイマー開始");

        // ゲーム開始と同時に未クリック時間計測を開始
        timeSinceLastClick = 0f;
        gameStartTime = Time.time;
        isGameActive = true;
        isBoostActive = false;

        UpdateOrganizerStatus();

        if (enableDebugLog)
            Debug.Log($"🏢 ✅ タイマー開始: {timeSinceLastClick}秒からスタート");
    }

    /// <summary>
    /// まとめる係の有効性を更新
    /// </summary>
    private void UpdateOrganizerStatus()
    {
        bool wasActive = isOrganizerActive;

        if (upgradeManager != null)
        {
            // まとめる係アップグレードが取得済みかチェック
            var organizerUpgrade = upgradeManager.GetUpgradeByType(UpgradeType.Organizer);
            isOrganizerActive = organizerUpgrade != null && organizerUpgrade.currentLevel > 0;

            // レベルに応じて発動時間を調整
            if (isOrganizerActive)
            {
                // Lv1: 10秒, Lv2: 9秒, Lv3: 8秒... (最小5秒)
                currentActivationTime = Mathf.Max(5f, baseActivationTime - (organizerUpgrade.currentLevel - 1) * 1f);
            }
        }
        else
        {
            isOrganizerActive = false;
        }

        // 状態変化をログ出力
        if (wasActive != isOrganizerActive && enableDebugLog)
        {
            Debug.Log($"🏢 まとめる係状態変更: {wasActive} → {isOrganizerActive}");
            if (isOrganizerActive)
            {
                Debug.Log($"🏢 発動時間設定: {currentActivationTime}秒");
            }
        }
    }

    /// <summary>
    /// まとめる係ブースト開始
    /// </summary>
    private void StartOrganizerBoost()
    {
        if (isBoostActive) return; // 重複発動防止

        if (enableDebugLog)
        {
            Debug.Log($"🏢 ⚡ まとめる係ブースト開始！");
            Debug.Log($"🏢 - 発動条件: {timeSinceLastClick:F1}秒 >= {currentActivationTime}秒");
            Debug.Log($"🏢 - ブースト倍率: x{boostMultiplier}");
        }

        isBoostActive = true;

        // ブースト状態変更通知
        OnBoostStateChanged?.Invoke(true);

        // 自動生産系アップグレードにブーストを適用
        ApplyBoostToAutomationUpgrades();

        // 🔥 サイドパネルのまとめる係アイコンに点滅開始を通知
        if (UpgradeSidePanelUI.Instance != null)
        {
            // まとめる係専用のエフェクト制御
            UpgradeSidePanelUI.Instance.SetOrganizerBoostEffect(true);

            // 全体のブーストエフェクトも開始
            UpgradeSidePanelUI.Instance.SetBoostEffect(true, boostMultiplier);
        }
    }

    /// <summary>
    /// まとめる係ブースト停止
    /// </summary>
    private void StopOrganizerBoost()
    {
        if (!isBoostActive) return;

        if (enableDebugLog)
            Debug.Log("🏢 🔄 まとめる係ブースト停止");

        isBoostActive = false;

        // ブースト状態変更通知
        OnBoostStateChanged?.Invoke(false);

        // ブーストエフェクト停止
        RemoveBoostFromAutomationUpgrades();

        // 🔥 サイドパネルのまとめる係アイコンに点滅停止を通知
        if (UpgradeSidePanelUI.Instance != null)
        {
            // まとめる係専用のエフェクト停止
            UpgradeSidePanelUI.Instance.SetOrganizerBoostEffect(false);

            // 全体のブーストエフェクトも停止
            UpgradeSidePanelUI.Instance.SetBoostEffect(false);
        }
    }

    /// <summary>
    /// 自動生産系アップグレードにブーストを適用
    /// </summary>
    private void ApplyBoostToAutomationUpgrades()
    {
        if (clickManager == null) return;

        // ClickManagerの一時的ブーストは無期限で適用（クリックまで継続）
        clickManager.ApplyTemporaryBoost(boostMultiplier, float.MaxValue);

        if (enableDebugLog)
        {
            Debug.Log($"🏢 📈 自動生産ブースト適用: x{boostMultiplier} (クリックまで継続)");
        }

        // サイドパネルUIにエフェクト通知
        if (UpgradeSidePanelUI.Instance != null)
        {
            // 自動生産系アップグレードを強調
            var automationTypes = new UpgradeType[]
            {
                UpgradeType.Factory,
                UpgradeType.HelperFriend,
                UpgradeType.DonkeyBakery,
                UpgradeType.RobaBakery
            };

            foreach (var upgradeType in automationTypes)
            {
                UpgradeSidePanelUI.Instance.TriggerItemActivationEffect(upgradeType);
            }
        }
    }

    /// <summary>
    /// 自動生産系アップグレードからブーストを除去
    /// </summary>
    private void RemoveBoostFromAutomationUpgrades()
    {
        if (clickManager == null) return;

        // ブーストを即座に終了
        clickManager.ApplyTemporaryBoost(1f, 0f);

        if (enableDebugLog)
        {
            Debug.Log($"🏢 📉 自動生産ブースト除去");
        }
    }

    /// <summary>
    /// 自動クリック時の処理（自動生産によるクリック）
    /// 🔥 自動クリックは未クリック時間に影響しない
    /// </summary>
    public void OnAutoClick()
    {
        // 自動クリックは未クリック時間に影響しない
        // まとめる係の機能は手動クリックのみに反応
        if (enableDebugLog && Time.frameCount % 300 == 0) // 5秒に1回ログ
        {
            Debug.Log("🏢 🤖 自動クリック検出（まとめる係への影響なし）");
        }
    }

    /// <summary>
    /// まとめる係のレベル設定
    /// </summary>
    public void SetOrganizerLevel(int level)
    {
        if (enableDebugLog)
            Debug.Log($"🏢 📊 まとめる係レベル設定: {level}");

        // レベルに応じて発動時間を調整（Lv1=10秒、最小5秒）
        currentActivationTime = Mathf.Max(5f, baseActivationTime - (level - 1) * 1f);

        UpdateOrganizerStatus();
    }

    /// <summary>
    /// 状態情報を取得
    /// </summary>
    public OrganizerStatus GetStatus()
    {
        return new OrganizerStatus
        {
            isActive = isOrganizerActive,
            isBoostActive = isBoostActive,
            timeSinceLastClick = timeSinceLastClick,
            timeUntilActivation = TimeUntilActivation,
            activationTime = currentActivationTime,
            boostMultiplier = boostMultiplier,
            effectDuration = float.MaxValue // クリックまで継続
        };
    }

    /// <summary>
    /// デバッグ用強制ブースト
    /// </summary>
    public void DebugForceBoost()
    {
        if (enableDebugLog)
            Debug.Log("🏢 🔧 デバッグ用強制ブースト発動");

        StartOrganizerBoost();
    }

    /// <summary>
    /// 状態を完全リセット
    /// </summary>
    private void ResetOrganizerState()
    {
        timeSinceLastClick = 0f;
        isBoostActive = false;
        isGameActive = false;
        gameStartTime = 0f;

        // ブーストエフェクト停止
        if (isBoostActive)
        {
            StopOrganizerBoost();
        }

        if (enableDebugLog)
            Debug.Log("🏢 🔄 まとめる係状態完全リセット");
    }

    /// <summary>
    /// ゲーム終了時の処理
    /// </summary>
    public void OnGameEnd()
    {
        isGameActive = false;
        if (isBoostActive)
        {
            StopOrganizerBoost();
        }

        if (enableDebugLog)
            Debug.Log("🏢 🏁 ゲーム終了 - まとめる係停止");
    }

    /// <summary>
    /// ゲーム一時停止時の処理
    /// </summary>
    public void OnGamePause(bool isPaused)
    {
        if (isPaused)
        {
            isGameActive = false;
        }
        else
        {
            isGameActive = true;
            // 再開時は現在時刻を記録
            gameStartTime = Time.time - timeSinceLastClick;
        }

        if (enableDebugLog)
            Debug.Log($"🏢 ⏸️ ゲーム一時停止状態変更: {isPaused}");
    }

    // アクセサ・プロパティ
    public bool IsOrganizerActive => isOrganizerActive;
    public bool IsBoostActive => isBoostActive;
    public float TimeSinceLastClick => timeSinceLastClick;
    public float TimeUntilActivation => Mathf.Max(0, currentActivationTime - timeSinceLastClick);
    public float ActivationTime => currentActivationTime;
    public float BoostMultiplier => boostMultiplier;

    /// <summary>
    /// 外部からの強制時間リセット（デバッグ用）
    /// </summary>
    [ContextMenu("🔄 未クリック時間リセット")]
    public void ForceResetTimer()
    {
        timeSinceLastClick = 0f;
        gameStartTime = Time.time;

        if (isBoostActive)
        {
            StopOrganizerBoost();
        }

        if (enableDebugLog)
            Debug.Log("🏢 🔧 強制タイマーリセット実行（ブースト解除）");
    }

    /// <summary>
    /// デバッグ用：強制ブースト発動
    /// </summary>
    [ContextMenu("⚡ 強制ブースト発動")]
    public void ForceBoostActivation()
    {
        if (Application.isPlaying && isOrganizerActive)
        {
            StartOrganizerBoost();
        }
    }

    /// <summary>
    /// デバッグ用：状態表示
    /// </summary>
    [ContextMenu("🔍 まとめる係状態表示")]
  
    private void OnGUI()
    {
       

    }
}

/// <summary>
/// まとめる係の状態情報構造体
/// </summary>
[System.Serializable]
public struct OrganizerStatus
{
    public bool isActive;
    public bool isBoostActive;
    public float timeSinceLastClick;
    public float timeUntilActivation;
    public float activationTime;
    public float boostMultiplier;
    public float effectDuration;
}