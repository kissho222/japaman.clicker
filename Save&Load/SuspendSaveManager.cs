using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class SuspendSaveManager : MonoBehaviour
{
    [Header("中断セーブUI")]
    public GameObject suspendSavePanel;
    public Button suspendSaveButton;
    public Button resumeGameButton;
    public Button titleButton;
    public Button newGameButton;
    public TextMeshProUGUI suspendSaveInfoText;

    [Header("確認ダイアログ")]
    public GameObject confirmDialog;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [TextArea(3, 5)]
    public string suspendSaveMessage = "中断セーブを作成してタイトルに戻りますか？\n\n※現在のゲーム進行状況が保存されます";

    private bool isSuspendSaveInProgress = false;

    [Header("アニメーション設定")]
    public float animationDuration = 0.3f;
    public AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 中断セーブ専用スロット番号
    private const int SUSPEND_SAVE_SLOT = 999;

    // コールバック
    private Action onResumeCallback;
    private Action onTitleCallback;
    private Action onNewGameCallback;

    // アニメーション制御
    private bool isAnimating = false;

    private static SuspendSaveManager instance;
    public static SuspendSaveManager Instance => instance;

    void Awake()
    {
        // 🔧 修正: シングルトンパターンの改善
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"💾 SuspendSaveManager の重複インスタンスを削除: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 🔧 修正: DontDestroyOnLoadの条件付き適用
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("💾 SuspendSaveManager を初期化しました (DontDestroyOnLoad適用)");
        }
        else
        {
            Debug.Log("💾 SuspendSaveManager を初期化しました (親オブジェクト有り)");
        }
    }

    void OnDestroy()
    {
        // 🔧 インスタンス参照のクリア
        if (instance == this)
        {
            instance = null;
            Debug.Log("💾 SuspendSaveManager インスタンスをクリアしました");
        }
    }

    void Start()
    {
        SetupUI();
        HideAllPanels();
        SetupConfirmDialog();
    }

    void SetupConfirmDialog()
    {
        if (confirmDialog != null)
        {
            confirmDialog.SetActive(false);
            if (confirmText != null)
            {
                confirmText.text = suspendSaveMessage;
            }
            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            }
            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.AddListener(OnConfirmNo);
            }
        }
    }

    void OnConfirmYes()
    {
        Debug.Log("✅ 中断セーブ実行が確認されました");
        HideConfirmDialog();
        ExecuteSuspendSaveAndReturnToTitle();
    }

    void OnConfirmNo()
    {
        Debug.Log("❌ 中断セーブがキャンセルされました");
        HideConfirmDialog();
    }

    /// <summary>
    /// 中断セーブ実行とタイトル復帰
    /// </summary>
    private void ExecuteSuspendSaveAndReturnToTitle()
    {
        if (isSuspendSaveInProgress)
        {
            Debug.LogWarning("⚠️ 中断セーブが既に実行中です");
            return;
        }

        Debug.Log("💾 === 中断セーブ実行開始 ===");
        StartCoroutine(SuspendSaveCoroutine());
    }

    /// <summary>
    /// 中断セーブコルーチン（通常セーブフロー回避版）
    /// </summary>
    IEnumerator SuspendSaveCoroutine()
    {
        isSuspendSaveInProgress = true;
        bool saveSuccess = false;
        System.Exception saveException = null;

        // 🔧 修正: 通常のセーブUIを一切呼び出さない
        try
        {
            Debug.Log("💾 中断セーブを作成中...");

            // 直接中断セーブを実行
            saveSuccess = CreateSuspendSaveInternal();
        }
        catch (System.Exception e)
        {
            saveException = e;
        }

        // 結果に応じた処理
        if (saveException != null)
        {
            Debug.LogError($"❌ 中断セーブ処理中にエラーが発生: {saveException.Message}");
        }
        else if (saveSuccess)
        {
            Debug.Log("✅ 中断セーブ作成完了");

            // 少し待機（UIフィードバックのため）
            yield return new WaitForSeconds(0.5f);

            // 🔧 修正: 直接タイトルシーンに移行（セーブUIを回避）
            Debug.Log("🔄 タイトルシーンに直接移行中...");
            ReturnToTitleScene();
        }
        else
        {
            Debug.LogError("❌ 中断セーブの作成に失敗しました");
        }

        isSuspendSaveInProgress = false;
    }

    // ===========================================
    // 1. SuspendSaveManager.cs の中断セーブ作成修正
    // ===========================================

    /// <summary>
    /// 中断セーブ作成処理（修正版）
    /// </summary>
    bool CreateSuspendSaveInternal()
    {
        try
        {
            Debug.Log("💾 === CreateSuspendSaveInternal 開始 ===");

            // 現在のゲーム状態を取得
            if (GameManager.Instance == null)
            {
                Debug.LogError("❌ GameManager.Instance が null");
                return false;
            }

            // 🔧 修正: GameManagerから正確なセーブデータを取得
            SaveData saveData = GameManager.Instance.CreateCurrentSaveData();

            if (saveData == null)
            {
                Debug.LogError("❌ セーブデータの作成に失敗");
                return false;
            }

            Debug.Log($"💾 作成されたセーブデータ: ステージ{saveData.currentStage}");

            // 🔧 修正: SaveManagerの存在確認を強化
            if (SaveManager.Instance == null)
            {
                Debug.LogError("❌ SaveManager.Instance が null");
                return false;
            }

            // 中断セーブ専用スロット（999番）に保存
            bool success = SaveManager.Instance.SaveGame(SUSPEND_SAVE_SLOT, saveData);

            if (success)
            {
                Debug.Log($"✅ 中断セーブ作成完了 - スロット{SUSPEND_SAVE_SLOT}, ステージ{saveData.currentStage}");

                // 🔧 修正: UI更新は安全に実行
                try
                {
                    UpdateSuspendSaveInfo();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ UI更新でエラー（無視）: {e.Message}");
                    // UI更新失敗してもセーブは成功
                }
            }
            else
            {
                Debug.LogError("❌ SaveManager.SaveGame が失敗");
            }

            Debug.Log("💾 === CreateSuspendSaveInternal 終了 ===");
            return success;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 中断セーブ作成中にエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// タイトルシーン復帰処理（UI参照破損対策版）
    /// </summary>
    void ReturnToTitleScene()
    {
        try
        {
            Debug.Log("🔄 タイトルシーン移行開始");

            // 🔧 修正: UI参照をクリアしてからシーン移行
            ClearUIReferences();

            // GameManagerに安全な終了通知
            if (GameManager.Instance != null)
            {
                // カスタムの終了処理があれば呼び出し
                Debug.Log("🔄 GameManager終了処理通知");
            }

            // シーンを切り替え
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
            Debug.Log("🔄 タイトルシーン移行完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ タイトルシーン移行中にエラー: {e.Message}");
        }
    }

    /// <summary>
    /// UI参照のクリア処理（新規追加）
    /// </summary>
    private void ClearUIReferences()
    {
        Debug.Log("🔧 UI参照をクリア中...");

        // UI参照を null に設定（シーン移行時の破損を防ぐ）
        suspendSavePanel = null;
        confirmDialog = null;
        suspendSaveButton = null;
        resumeGameButton = null;
        titleButton = null;
        newGameButton = null;
        suspendSaveInfoText = null;
        confirmText = null;
        confirmYesButton = null;
        confirmNoButton = null;

        Debug.Log("✅ UI参照クリア完了");
    }

    void SetupUI()
    {
        // ボタンイベント設定
        if (suspendSaveButton != null)
            suspendSaveButton.onClick.AddListener(CreateSuspendSave);

        if (resumeGameButton != null)
            resumeGameButton.onClick.AddListener(ShowResumeConfirmation);

        if (titleButton != null)
            titleButton.onClick.AddListener(ShowTitleConfirmation);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(ShowNewGameConfirmation);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(HideConfirmDialog);
    }

    void HideAllPanels()
    {
        if (suspendSavePanel != null)
            suspendSavePanel.SetActive(false);

        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }

    /// <summary>
    /// ゲーム終了時の中断セーブ画面を表示
    /// </summary>
    public void ShowSuspendSaveMenu(Action onResume = null, Action onTitle = null, Action onNewGame = null)
    {
        Debug.Log("💾 中断セーブメニューを表示");

        onResumeCallback = onResume;
        onTitleCallback = onTitle;
        onNewGameCallback = onNewGame;

        UpdateSuspendSaveInfo();
        ShowPanelWithAnimation(suspendSavePanel);
    }

    /// <summary>
    /// 中断セーブ情報を更新
    /// </summary>
    void UpdateSuspendSaveInfo()
    {
        if (suspendSaveInfoText == null) return;

        bool hasSuspendSave = HasSuspendSave();

        if (hasSuspendSave)
        {
            SaveData suspendData = SaveManager.Instance.LoadSaveData(SUSPEND_SAVE_SLOT);
            if (suspendData != null)
            {
                suspendSaveInfoText.text = $"中断セーブあり\n{suspendData.currentStage}日目 - {suspendData.GetSaveDateTimeString()}";
            }
            else
            {
                suspendSaveInfoText.text = "中断セーブあり（詳細不明）";
            }
        }
        else
        {
            suspendSaveInfoText.text = "中断セーブなし";
        }

        // ボタンの有効/無効設定
        if (resumeGameButton != null)
            resumeGameButton.interactable = hasSuspendSave;
    }
    /// <summary>
    /// 中断セーブを作成（フロー修正版）
    /// </summary>
    public void CreateSuspendSave()
    {
        if (isSuspendSaveInProgress)
        {
            Debug.Log("⚠️ 中断セーブが既に実行中です");
            return;
        }

        Debug.Log("🎯 中断セーブボタンがクリックされました");

        // 🔧 修正: 確認ダイアログを表示（通常セーブフローを回避）
        ShowSuspendSaveConfirmDialog();
    }

    // SuspendSaveManager.cs の修正

    /// <summary>
    /// 中断セーブ確認ダイアログ表示（GameManagerから呼び出し可能）
    /// </summary>
    public void ShowSuspendSaveConfirmDialog()  // ← privateをpublicに変更
    {
        if (confirmDialog != null)
        {
            Debug.Log("📋 中断セーブ専用確認ダイアログを表示");

            // 🔧 確認メッセージを中断セーブ専用に設定
            if (confirmText != null)
            {
                confirmText.text = "中断セーブを作成してタイトルに戻りますか？\n\n※現在のゲーム進行状況が保存されます";
            }

            // 🔧 Yesボタンの動作を明確に設定
            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(() => {
                    Debug.Log("✅ 中断セーブ確認 - はい");
                    HideConfirmDialog();
                    ExecuteSuspendSaveAndReturnToTitle();
                });
            }

            // 🔧 Noボタンの動作を設定（元の選択に戻る）
            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(() => {
                    Debug.Log("❌ 中断セーブ確認 - いいえ");
                    HideConfirmDialog();
                    // 必要に応じてゲーム続行処理
                });
            }

            confirmDialog.SetActive(true);

            if (confirmYesButton != null)
            {
                confirmYesButton.Select();
            }
        }
        else
        {
            Debug.LogError("❌ 確認ダイアログがnullです - 直接実行します");
            ExecuteSuspendSaveAndReturnToTitle();
        }
    }

    /// <summary>
    /// 中断セーブが存在するかチェック（修正版）
    /// </summary>
    public bool HasSuspendSave()
    {
        // 🔧 修正: SaveManagerの存在確認を追加
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("💾 SaveManager.Instance が null のため、中断セーブ確認できません");
            return false;
        }

        bool exists = SaveManager.Instance.IsSlotUsed(SUSPEND_SAVE_SLOT);
        Debug.Log($"🔍 中断セーブ存在確認結果: {exists} (スロット{SUSPEND_SAVE_SLOT})");
        return exists;
    }

    /// <summary>
    /// 中断セーブが存在するかチェック（静的メソッド版）
    /// </summary>
    public static bool CheckSuspendSaveExists()
    {
        Debug.Log("🔍 中断セーブ存在チェック開始");

        // 🔧 修正: SaveManagerの存在確認を強化
        if (SaveManager.Instance == null)
        {
            Debug.Log("🔍 SaveManager.Instance が null - 中断セーブなし判定");
            return false;
        }

        // 🔧 修正: 直接SaveManagerで確認
        bool exists = SaveManager.Instance.IsSlotUsed(999); // SUSPEND_SAVE_SLOT
        Debug.Log($"🔍 SaveManager直接確認結果: {exists}");

        return exists;
    }

    /// <summary>
    /// 中断セーブからゲームを再開（修正版）
    /// </summary>
    public static void ResumeSuspendSave()
    {
        Debug.Log("🔄 中断セーブから再開開始");

        // 🔧 修正: SaveManagerの存在確認
        if (SaveManager.Instance == null)
        {
            Debug.LogError("❌ SaveManager.Instance が見つかりません");
            return;
        }

        // 🔧 修正: 中断セーブデータの存在確認
        const int SUSPEND_SAVE_SLOT = 999;
        if (!SaveManager.Instance.IsSlotUsed(SUSPEND_SAVE_SLOT))
        {
            Debug.LogWarning("⚠️ 中断セーブデータが見つかりません");
            return;
        }

        // 🔧 修正: セーブデータをロードしてGameSceneに移行
        SaveData suspendData = SaveManager.Instance.LoadSaveData(SUSPEND_SAVE_SLOT);
        if (suspendData == null)
        {
            Debug.LogError("❌ 中断セーブデータの読み込みに失敗しました");
            return;
        }

        Debug.Log($"💾 中断セーブデータロード成功: ステージ{suspendData.currentStage}");

        // 🔧 修正: GameManagerに静的ロードデータを設定
        GameManager.SetPendingLoadData(suspendData);

        // 🔧 修正: 中断セーブは一度使ったら削除
        SaveManager.Instance.DeleteSaveData(SUSPEND_SAVE_SLOT);
        Debug.Log("🗑️ 中断セーブデータを削除しました");

        // 🔧 修正: GameSceneに移行
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        Debug.Log("🔄 GameSceneに移行中...");
    }

    /// <summary>
    /// 中断セーブからゲームを再開
    /// </summary>
    public void LoadSuspendSave()
    {
        Debug.Log("💾 中断セーブからゲームを復元中...");
        SaveData suspendData = SaveManager.Instance.LoadSaveData(SUSPEND_SAVE_SLOT);

        if (suspendData == null)
        {
            Debug.LogError("❌ 中断セーブデータが見つかりません");
            return;
        }

        try
        {
            // ゲーム状態を復元
            LoadGameFromSaveData(suspendData);

            // 中断セーブは一度使ったら削除
            SaveManager.Instance.DeleteSaveData(SUSPEND_SAVE_SLOT);
            Debug.Log("✅ 中断セーブからの復元完了");

            // メニューを非表示
            HideAllPanels();

            // コールバック実行
            onResumeCallback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 中断セーブ復元エラー: {e.Message}");
        }
    }

    /// <summary>
    /// セーブデータからゲームを読み込み
    /// </summary>
    void LoadGameFromSaveData(SaveData saveData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance が null です");
            return;
        }

        // ゲーム状態を復元
        GameManager.Instance.currentStage = saveData.currentStage;
        GameManager.Instance.totalLifetimeJapaman = saveData.totalLifetimeJapaman;
        GameManager.Instance.totalLifetimeExtra = saveData.totalLifetimeExtra;
        GameManager.Instance.totalStagesCompleted = saveData.totalStagesCompleted;

        // アップグレード状態を復元
        saveData.ApplyUpgradeData();

        Debug.Log($"💾 ゲーム状態復元: ステージ{saveData.currentStage}");
    }

    // ===== 確認ダイアログ関連 =====
    void ShowResumeConfirmation()
    {
        ShowConfirmation("中断セーブからゲームを再開しますか？\n（中断セーブは削除されます）", LoadSuspendSave);
    }

    void ShowTitleConfirmation()
    {
        ShowConfirmation("タイトル画面に戻りますか？", () => {
            HideAllPanels();
            onTitleCallback?.Invoke();
        });
    }

    void ShowNewGameConfirmation()
    {
        string message = HasSuspendSave() ?
            "新しいゲームを開始しますか？\n（中断セーブは削除されます）" :
            "新しいゲームを開始しますか？";

        ShowConfirmation(message, () => {
            if (HasSuspendSave())
            {
                SaveManager.Instance.DeleteSaveData(SUSPEND_SAVE_SLOT);
            }
            HideAllPanels();
            onNewGameCallback?.Invoke();
        });
    }

    void ShowConfirmation(string message, Action onConfirm)
    {
        if (confirmDialog == null) return;

        if (confirmText != null)
            confirmText.text = message;

        confirmDialog.SetActive(true);

        // Yes ボタンの設定
        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(() => {
            HideConfirmDialog();
            onConfirm?.Invoke();
        });
    }

    /// <summary>
    /// 確認ダイアログを隠す（強化版）
    /// </summary>
    private void HideConfirmDialog()
    {
        Debug.Log("📋 確認ダイアログを非表示にします");

        if (confirmDialog != null)
        {
            confirmDialog.SetActive(false);
            Debug.Log("✅ confirmDialog.SetActive(false) 実行完了");

            // 追加の安全処理
            var canvasGroup = confirmDialog.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                Debug.Log("✅ CanvasGroup無効化完了");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ confirmDialog が null です");
        }

        // 🔧 修正: 全ての確認ダイアログを強制非表示
        ForceHideAllConfirmDialogs();
    }
    /// <summary>
/// 全ての確認ダイアログを強制非表示（新規追加）
/// </summary>
private void ForceHideAllConfirmDialogs()
{
    Debug.Log("🔧 全確認ダイアログの強制非表示開始");
    
    // 名前でダイアログを検索して非表示
    string[] dialogNames = { "ConfirmDialog", "SuspendConfirmDialog", "Dialog", "Popup" };
    
    foreach (string name in dialogNames)
    {
        GameObject dialog = GameObject.Find(name);
        if (dialog != null && dialog.activeInHierarchy)
        {
            dialog.SetActive(false);
            Debug.Log($"🔧 強制非表示: {name}");
        }
    }
    
    // Canvas配下のダイアログも検索
    Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
    foreach (Canvas canvas in canvases)
    {
        if (canvas == null) continue;
        
        foreach (string name in dialogNames)
        {
            Transform dialog = canvas.transform.Find(name);
            if (dialog != null && dialog.gameObject.activeInHierarchy)
            {
                dialog.gameObject.SetActive(false);
                Debug.Log($"🔧 Canvas内で強制非表示: {name}");
            }
        }
    }
    
    Debug.Log("🔧 全確認ダイアログの強制非表示完了");
}

    // ===== アニメーション関連 =====
    void ShowPanelWithAnimation(GameObject panel)
    {
        if (panel == null || isAnimating) return;

        StartCoroutine(ShowPanelCoroutine(panel));
    }

    IEnumerator ShowPanelCoroutine(GameObject panel)
    {
        isAnimating = true;

        panel.SetActive(true);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        RectTransform rectTransform = panel.GetComponent<RectTransform>();

        // 初期状態
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        if (rectTransform != null)
            rectTransform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / animationDuration;
            float easedProgress = showCurve.Evaluate(progress);

            canvasGroup.alpha = easedProgress;
            if (rectTransform != null)
                rectTransform.localScale = Vector3.one * easedProgress;

            yield return null;
        }

        // 最終状態
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;

        isAnimating = false;
    }

    IEnumerator DelayedAction(Action action, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    /// <summary>
    /// デバッグ用: 中断セーブ削除
    /// </summary>
    [ContextMenu("🗑️ 中断セーブ削除")]
    public void DebugDeleteSuspendSave()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("プレイモード中のみ実行可能");
            return;
        }

        Debug.Log("🗑️ デバッグ: 中断セーブ削除");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSaveData(999);
            Debug.Log("✅ 中断セーブ削除完了");

            // UI更新
            UpdateSuspendSaveInfo();
        }
        else
        {
            Debug.LogError("❌ SaveManager.Instance が見つかりません");
        }
    }

    /// <summary>
    /// デバッグ用: 中断セーブマネージャー状態表示
    /// </summary>
    [ContextMenu("🔍 SuspendSaveManager状態")]
    public void DebugSuspendSaveManagerStatus()
    {
        Debug.Log("=== 🔍 SuspendSaveManager状態 ===");
        Debug.Log($"Instance: {(Instance != null ? "存在" : "null")}");
        Debug.Log($"isSuspendSaveInProgress: {isSuspendSaveInProgress}");

        // UI要素の確認
        Debug.Log($"suspendSavePanel: {(suspendSavePanel != null ? "設定済み" : "null")}");
        Debug.Log($"confirmDialog: {(confirmDialog != null ? "設定済み" : "null")}");
        Debug.Log($"suspendSaveButton: {(suspendSaveButton != null ? "設定済み" : "null")}");

        // 中断セーブの存在確認
        bool hasSuspend = HasSuspendSave();
        Debug.Log($"中断セーブ存在: {hasSuspend}");

        if (hasSuspend && SaveManager.Instance != null)
        {
            SaveData data = SaveManager.Instance.LoadSaveData(999);
            if (data != null)
            {
                Debug.Log($"中断セーブ詳細: ステージ{data.currentStage}, {data.GetSaveDateTimeString()}");
            }
        }

        Debug.Log("=== 状態確認完了 ===");
    }

    internal bool HasSuspendSaveData()
    {
        throw new NotImplementedException();
    }
}