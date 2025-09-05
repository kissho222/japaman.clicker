using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Game State ===")]
    public GameState currentGameState = GameState.Menu;
    public static GameState CurrentState => Instance?.currentGameState ?? GameState.Menu;

    // 静的なロードデータ保持（シーン間で保持）
    private static SaveData staticPendingLoadData = null;
    public static void SetPendingLoadData(SaveData data)
    {
        staticPendingLoadData = data;
        Debug.Log($"💾 静的ロードデータを設定: ステージ{data?.currentStage}");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("🔄 重複するGameManagerを破棄します");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (gameObject != null)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ GameManager: DontDestroyOnLoad設定完了");
        }

        try
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("✅ GameManager: シーンロードコールバック登録完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ シーンロードコールバック登録失敗: {e.Message}");
        }
    }

    [Header("ゲーム設定")]
    public float timeLimit = 30f;
    private float timeRemaining;

    [Header("ステージシステム")]
    public int currentStage = 1;
    private long currentStageGoal;

    [Header("累積統計")]
    public long totalLifetimeJapaman = 0;
    public long totalLifetimeExtra = 0;
    public int totalStagesCompleted = 0;

    [Header("状態管理")]
    private bool isCleared = false;
    private bool isTimeUp = false;
    private bool isGameEnded = false;
    private bool isRoundClearing = false;
    private bool isWaitingForNextDay = false;
    private bool isWaitingForCountdown = false;
    private bool isShowingUpgradeSelection = false;
    private bool isWaitingForEating = false;
    private float gameStartTime;
    private float goalAchievedTime = 0f;

    // ロード後の状態管理フラグ
    private bool isLoadingFromSave = false;
    private SaveData pendingLoadData = null;

    [Header("参照")]
    public ClickManager clickManager;

      private int lastRoundJapamanCount = 0;

    [Header("UI参照")]
    public GameObject gameOverPanel;
    public GameObject gameCompleteClearPanel;
    public UpgradeSelectionUI upgradeSelectionUI;

    // 🆕 中断セーブダイアログ用
    [Header("中断セーブUI")]
    public GameObject suspendChoiceDialog;
    public Button continueToNextDayButton;
    public Button suspendAndReturnButton;
    public TextMeshProUGUI suspendDialogText;

    // シーンロード時のUI参照を再取得
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"🔄 シーンロード完了: {scene.name}");
        if (scene.name == "GameScene")
        {
            StartCoroutine(DelayedSceneInitialization());
        }
    }

    private IEnumerator DelayedSceneInitialization()
    {
        Debug.Log("🔄 シーン初期化開始");
        yield return null;

        RefreshUIReferences();

        // 静的ロードデータをチェック
        if (staticPendingLoadData != null)
        {
            Debug.Log($"💾 静的ロードデータを検出: ステージ{staticPendingLoadData.currentStage}");
            ApplyLoadedData(staticPendingLoadData);
            staticPendingLoadData = null;
        }
        else if (isLoadingFromSave && pendingLoadData != null)
        {
            Debug.Log($"💾 保留中のセーブデータを適用: ステージ{pendingLoadData.currentStage}");
            ApplyLoadedData(pendingLoadData);
            isLoadingFromSave = false;
            pendingLoadData = null;
        }
        else
        {
            Debug.Log("🆕 新規ゲーム開始");
            if (currentGameState == GameState.Menu)
            {
                StartNewGame();
            }
        }

        Debug.Log("✅ シーン初期化完了");
    }

    // UI参照を動的に再取得
    private void RefreshUIReferences()
    {
        try
        {
            // GameOverPanel を動的に検索
            if (gameOverPanel == null)
            {
                string[] panelNames = { "GameOverPanel", "GameOver", "OverPanel", "ResultPanel" };
                foreach (string name in panelNames)
                {
                    gameOverPanel = GameObject.Find(name);
                    if (gameOverPanel != null)
                    {
                        Debug.Log($"🔍 GameOverPanel発見: {name}");
                        break;
                    }
                }

                if (gameOverPanel == null)
                {
                    Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    if (canvases != null)
                    {
                        foreach (Canvas canvas in canvases)
                        {
                            if (canvas == null) continue;
                            foreach (string name in panelNames)
                            {
                                Transform found = canvas.transform.Find(name);
                                if (found != null)
                                {
                                    gameOverPanel = found.gameObject;
                                    Debug.Log($"🔍 Canvas内でGameOverPanel発見: {name}");
                                    break;
                                }
                            }
                            if (gameOverPanel != null) break;
                        }
                    }
                }

                if (gameOverPanel == null)
                {
                    Debug.LogWarning("⚠️ GameOverPanel が見つかりません");
                }
            }

            // ClickManager を再取得
            if (clickManager == null)
            {
                clickManager = FindFirstObjectByType<ClickManager>();
                if (clickManager != null)
                {
                    Debug.Log("🔍 ClickManager再取得完了");
                }
            }

          
            // UpgradeSelectionUI を再取得
            if (upgradeSelectionUI == null)
            {
                upgradeSelectionUI = FindFirstObjectByType<UpgradeSelectionUI>();
                if (upgradeSelectionUI != null)
                {
                    Debug.Log("🔍 UpgradeSelectionUI再取得完了");
                }
            }

            // 🆕 中断セーブダイアログの参照を取得
            RefreshSuspendDialogReferences();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ RefreshUIReferences エラー: {e.Message}");
        }
    }

    // 🆕 中断セーブダイアログの参照を取得
    private void RefreshSuspendDialogReferences()
    {
        if (suspendChoiceDialog == null)
        {
            suspendChoiceDialog = GameObject.Find("SuspendChoiceDialog");
            if (suspendChoiceDialog == null)
            {
                Debug.LogWarning("⚠️ SuspendChoiceDialog が見つかりません");
            }
        }

        if (continueToNextDayButton == null)
        {
            continueToNextDayButton = GameObject.Find("ContinueToNextDayButton")?.GetComponent<Button>();
        }

        if (suspendAndReturnButton == null)
        {
            suspendAndReturnButton = GameObject.Find("SuspendAndReturnButton")?.GetComponent<Button>();
        }

        if (suspendDialogText == null)
        {
            suspendDialogText = GameObject.Find("SuspendDialogText")?.GetComponent<TextMeshProUGUI>();
        }

        // ボタンイベントを設定
        SetupSuspendDialogButtons();
    }

    // 🆕 中断セーブダイアログのボタンイベントを設定
    private void SetupSuspendDialogButtons()
    {
        if (continueToNextDayButton != null)
        {
            continueToNextDayButton.onClick.RemoveAllListeners();
            continueToNextDayButton.onClick.AddListener(OnContinueToNextDay);
        }

        if (suspendAndReturnButton != null)
        {
            suspendAndReturnButton.onClick.RemoveAllListeners();
            suspendAndReturnButton.onClick.AddListener(OnSuspendAndReturn);
        }
    }

    public void StartNewGame()
    {
        Debug.Log("🆕 新規ゲーム開始処理");

        // 🔥 UI関連の完全リセットを最初に実行
        CleanupAllUIStates();

        SetGameState(GameState.Menu);
        ResetAllGameStates();
        AC

        Debug.Log("🆕 新規ゲーム開始処理");

        // 🔥 UI関連の安全なクリーンアップを最初に実行
        CleanupAllUIStates();

        SetGameState(GameState.Menu);
        ResetAllGameStates();

        // ステージとカウンターを完全に初期化
        currentStage = 1;
        totalLifetimeJapaman = 0;
        totalLifetimeExtra = 0;
        totalStagesCompleted = 0;
        lastRoundJapamanCount = 0;

        // 時間を正しく初期化
        timeRemaining = timeLimit;
        gameStartTime = Time.time;

        // 目標値を正しく設定
        currentStageGoal = GetStageGoal(1);

        Debug.Log($"🆕 初期化完了: ステージ{currentStage}, 目標{currentStageGoal}, 時間{timeRemaining}秒");

        // その他の初期化処理...

        StartStage(currentStage);
    }



    // 🔥 安全なUIクリーンアップメソッド
   

    // 🔥 新規メソッド：全UIの状態をクリーンアップ
    private void CleanupAllUIStates()
    {
        Debug.Log("🧹 === UI状態クリーンアップ開始 ===");

        // UpgradeSelectionUI のクリーンアップ
        if (UpgradeSelectionUI.Instance != null)
        {
            UpgradeSelectionUI.Instance.ForceClose();
            Debug.Log("🧹 UpgradeSelectionUI クリーンアップ完了");
        }

        // UpgradeSidePanelUI のクリーンアップ
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.RefreshUpgradeList();
            Debug.Log("🧹 UpgradeSidePanelUI クリーンアップ完了");
        }

        // UpgradeUIManager のクリーンアップ
        if (UpgradeUIManager.Instance != null)
        {
            // 選択画面が表示中の場合は強制終了
            if (UpgradeUIManager.Instance.IsSelecting())
            {
                //UpgradeUIManager.Instance.ForceClose();
            }
            Debug.Log("🧹 UpgradeUIManager クリーンアップ完了");
        }

        Debug.Log("🧹 === UI状態クリーンアップ完了 ===");
    }

    private void ResetAllGameStates()
    {
        Debug.Log("🔄 全ゲーム状態リセット開始");

        // フラグリセット
        isCleared = false;
        isTimeUp = false;
        isGameEnded = false;
        isRoundClearing = false;
        isWaitingForNextDay = false;
        isWaitingForCountdown = false;
        isShowingUpgradeSelection = false;
        isWaitingForEating = false;
        isLoadingFromSave = false;
        goalAchievedTime = 0f;

        // 🔥 時間を確実にリセット
        timeRemaining = timeLimit; // 30秒
        gameStartTime = Time.time;

        Debug.Log($"🔄 ゲーム状態フラグリセット完了 - 時間: {timeRemaining}秒");
    }

    void StartStage(int stageNumber)
    {
        Debug.Log($"🎮 ステージ{stageNumber}開始");

        // まとめる係の新ステージ処理
        var organizerManager = FindFirstObjectByType<OrganizerManager>();
        if (organizerManager != null)
        {
            organizerManager.OnNewStage();
        }

        // 🔥 直接InitializeStage()を呼び出し（ディレイなし）
        InitializeStage();
    }


    private void InitializeStage()
    {
        Debug.Log($"🎮 ステージ{currentStage}初期化開始");

        // UI参照を最初に取得
        RefreshUIReferences();

        // 状態を確実にリセット
        ResetAllGameStates();

        // 時間を正しく初期化（重要）
        timeRemaining = timeLimit; // 30秒に設定
        gameStartTime = Time.time;
        isWaitingForCountdown = true;
        SetGameState(GameState.Menu);

        // 目標値を再計算
        currentStageGoal = GetStageGoal(currentStage);

        Debug.Log($"🎮 時間初期化: {timeRemaining}秒, 目標: {currentStageGoal}個");

        // UIManager存在確認とUI更新
        if (UIManager.Instance != null)
        {
            Debug.Log("UIManager発見、UI更新開始");

            UIManager.Instance.UpdateGoalText(currentStageGoal);
            UIManager.Instance.UpdateStageText(currentStage);
            UIManager.Instance.UpdateTimeText(timeRemaining);
            UIManager.Instance.SetPhaseUI(true);
            UIManager.Instance.HideAllResultPanels();

            // 🔥 ディレイを削除：即座にカウントダウン開始
            Debug.Log("🎬 即座にカウントダウン開始");
            UIManager.Instance.StartRoundCountdown();
        }
        else
        {
            Debug.LogError("❌ UIManager.Instance が見つかりません！");
            return;
        }

        // 他のコンポーネント初期化
        if (ContainerSettings.Instance != null)
        {
            ContainerSettings.Instance.UpdateContainerForStage(currentStage);
        }

        // ClickManager初期化
        if (clickManager == null)
            clickManager = FindFirstObjectByType<ClickManager>();

        if (clickManager != null)
        {
            clickManager.goalCount = currentStageGoal;
            clickManager.ResetForNewStage(); // これで0にリセットされる
            clickManager.SetClickEnabled(false); // カウントダウン中は無効
        }
        else
        {
            Debug.LogError("❌ ClickManagerが見つかりません！");
        }

        Debug.Log($"🎮 ステージ{currentStage}初期化完了 - 目標: {currentStageGoal}個, 時間: {timeRemaining}秒");
    }



    public void OnCountdownComplete()
    {
        Debug.Log("🎮 カウントダウン完了、ゲーム開始処理");

        isWaitingForCountdown = false;
        SetGameState(GameState.Playing);

        // 🔥 ゲーム開始時刻を正確に記録
        gameStartTime = Time.time;

        // 🔥 時間を再度確認
        if (timeRemaining <= 0)
        {
            Debug.LogWarning($"⚠️ 時間が不正: {timeRemaining} → {timeLimit}に修正");
            timeRemaining = timeLimit;
        }

        if (clickManager != null)
        {
            clickManager.SetClickEnabled(true);
        }

        Debug.Log($"🎮 ゲーム開始完了 - 残り時間: {timeRemaining}秒");
    }


    private long GetStageGoal(int stage)
    {
        return (long)Mathf.Floor(50 * Mathf.Pow(1.5f, stage - 1));
    }

    public void LoadGameData(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("💾 ロードエラー: セーブデータがnullです");
            return;
        }

        Debug.Log($"💾 LoadGameData開始: ステージ{saveData.currentStage}");

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene")
        {
            isLoadingFromSave = true;
            pendingLoadData = saveData;
            Debug.Log("💾 シーン初期化前のため、ロードデータを保留");
            return;
        }

        ApplyLoadedData(saveData);
    }

    private void ApplyLoadedData(SaveData saveData)
    {
        Debug.Log($"💾 === ApplyLoadedData デバッグ開始 ===");
        if (saveData == null)
        {
            Debug.LogError("💾 ❌ saveDataがnullです！");
            return;
        }

        Debug.Log($"💾 📖 ロードするセーブデータ内容:");
        Debug.Log($"💾 - saveData.currentStage: {saveData.currentStage}");
        Debug.Log($"💾 - saveData.lastJapamanCount: {saveData.lastJapamanCount}");
        Debug.Log($"💾 - saveData.totalLifetimeJapaman: {saveData.totalLifetimeJapaman}");
        Debug.Log($"💾 - saveData.totalStagesCompleted: {saveData.totalStagesCompleted}");

        int previousStage = currentStage;
        currentStage = saveData.currentStage;
        lastRoundJapamanCount = (int)saveData.lastJapamanCount;

        Debug.Log($"💾 ✅ ステージ設定完了:");
        Debug.Log($"💾 - 変更: {previousStage} → {currentStage}");
        Debug.Log($"💾 - lastRoundJapamanCount: {lastRoundJapamanCount}");

        ResetGameStatesForLoad();

        totalLifetimeJapaman = saveData.totalLifetimeJapaman;
        totalLifetimeExtra = saveData.totalLifetimeExtra;
        totalStagesCompleted = saveData.totalStagesCompleted;

        Debug.Log($"💾 📊 累積統計復元:");
        Debug.Log($"💾 - totalLifetimeJapaman: {totalLifetimeJapaman}");
        Debug.Log($"💾 - totalStagesCompleted: {totalStagesCompleted}");

        Debug.Log("💾 🔧 アップグレードデータ適用開始...");
        saveData.ApplyUpgradeData();

        RefreshUIReferences();

        StartCoroutine(LoadGameInitialization(currentStage));

        Debug.Log($"💾 ✅ ApplyLoadedData完了: 最終ステージ={currentStage}");
        Debug.Log($"💾 === ApplyLoadedData デバッグ終了 ===");
    }

    private void ResetGameStatesForLoad()
    {
        Debug.Log("💾 ロード用状態リセット（アップグレード保持）");
        StopAllCoroutines();
        Debug.Log("💾 既存のコルーチンを全て停止しました");

        isWaitingForEating = false;
        Debug.Log("💾 WaitForEatingSequence停止フラグをリセット");

        isTimeUp = false;
        timeRemaining = timeLimit;
        Debug.Log($"💾 時間状態をリセット: timeRemaining={timeRemaining}秒");

        isCleared = false;
        isGameEnded = false;
        isRoundClearing = false;
        isWaitingForNextDay = false;
        isWaitingForCountdown = false;
        isShowingUpgradeSelection = false;
        isLoadingFromSave = false;
        goalAchievedTime = 0f;
        Debug.Log("💾 ゲーム状態フラグをリセット完了");
    }

    private IEnumerator LoadGameInitialization(int stage)
    {
        Debug.Log($"🔍 ロード前状態確認:");
        Debug.Log($" - isGameEnded: {isGameEnded}");
        Debug.Log($" - isTimeUp: {isTimeUp}");
        Debug.Log($" - isCleared: {isCleared}");
        Debug.Log($" - timeRemaining: {timeRemaining}");
        Debug.Log($" - gameStartTime: {gameStartTime}");
        Debug.Log($" - Time.time: {Time.time}");

        yield return null;

        Debug.Log($"💾 ロード後初期化: ステージ{stage}");
        RefreshUIReferences();

        if (UpgradeManager.Instance != null)
        {
            Debug.Log($"💾 activeUpgrades数: {UpgradeManager.Instance.activeUpgrades.Count}");
            UpgradeManager.Instance.RecalculateAllEffects();
        }
        else
        {
            Debug.LogError("💾 UpgradeManager.Instance が null です！");
        }

        if (clickManager != null)
        {
            Debug.Log($"💾 ClickManager効果: クリック×{clickManager.clickMultiplier}, 自動生産{clickManager.autoProductionRate}/秒");
        }

        InitializeStageForLoad(stage);
        Debug.Log($"💾 ロード初期化完了: ステージ{stage}");
    }

    private void InitializeStageForLoad(int stage)
    {
        Debug.Log($"💾 ロード時ステージ初期化: ステージ{stage}");
        RefreshUIReferences();

        isCleared = false;
        isTimeUp = false;
        isGameEnded = false;
        isRoundClearing = false;
        isWaitingForNextDay = false;
        isWaitingForCountdown = true;
        isShowingUpgradeSelection = false;
        isWaitingForEating = false;
        goalAchievedTime = 0f;

        timeRemaining = timeLimit;
        gameStartTime = Time.time;
        SetGameState(GameState.Menu);

        currentStageGoal = GetStageGoal(currentStage);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGoalText(currentStageGoal);
            UIManager.Instance.UpdateStageText(currentStage);
            UIManager.Instance.SetPhaseUI(true);
            UIManager.Instance.HideAllResultPanels();
            Debug.Log("💾 ロード時カウントダウン開始");
            UIManager.Instance.StartRoundCountdown();
        }

        if (ContainerSettings.Instance != null)
        {
            ContainerSettings.Instance.UpdateContainerForStage(currentStage);
        }

        if (clickManager != null)
        {
            clickManager.goalCount = currentStageGoal;
            clickManager.ResetForNewStage();
            clickManager.SetClickEnabled(false);
        }
        else
        {
            Debug.LogError("💾 ClickManagerが見つかりません！");
        }

        Debug.Log($"💾 ロード時ステージ{currentStage}初期化完了 - 目標: {currentStageGoal}個");
    }

    private void Update()
    {
        // 時間更新をブロックする条件を強化
        if (isRoundClearing || isWaitingForCountdown || isShowingUpgradeSelection || isGameEnded)
        {
            return;
        }

        bool countdownActive = UIManager.Instance != null && UIManager.Instance.IsCountdownActive();
        if (countdownActive)
        {
            return;
        }

        if (currentGameState != GameState.Playing)
        {
            return;
        }

        float elapsedSinceStart = Time.time - gameStartTime;
        if (elapsedSinceStart < 1f)
        {
            return;
        }

        if (!isTimeUp && !isGameEnded)
        {
            timeRemaining -= Time.deltaTime;
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTimeText(timeRemaining);

            if (timeRemaining <= 0f)
            {
                Debug.Log($"⏰ 時間切れ！経過時間: {elapsedSinceStart:F1}秒");
                isTimeUp = true;
                if (!isWaitingForEating)
                {
                    Debug.Log("🍽️ WaitForEatingSequenceを開始します（時間切れから）");
                    StartCoroutine(WaitForEatingSequence());
                }
                else
                {
                    Debug.Log("⚠️ 既にWaitForEatingSequence実行中");
                }
            }
        }

        // ノルマ達成判定
        if (!isCleared && !isGameEnded && currentGameState == GameState.Playing &&
            clickManager != null && clickManager.japamanCount >= currentStageGoal)
        {
            isCleared = true;
            goalAchievedTime = Time.time - gameStartTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPhaseUI(false);
                UIManager.Instance.ShowPhaseChangeMessage("ノルマ達成！\nフレンズにジャパまんをあげよう！");
            }
        }
    }

    public void EndGame()
    {
        SetGameState(GameState.Result);
        if (isGameEnded) return;
        isGameEnded = true;

        float timeUsed = Time.time - gameStartTime;

        if (clickManager != null)
        {
            long currentRoundTotal = clickManager.GetTotalJapamanCount();
            long currentRoundPlate = clickManager.GetPlateJapamanCount();
            long currentRoundExtra = clickManager.GetExtraJapamanCount();

            totalLifetimeJapaman += currentRoundTotal;
            totalLifetimeExtra += currentRoundExtra;

            if (isCleared)
            {
                totalStagesCompleted++;
                StartCoroutine(RoundClearSequence(currentRoundTotal, currentRoundPlate, currentRoundExtra, timeUsed));
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOver(currentRoundTotal, currentRoundPlate, currentRoundExtra,
                        totalLifetimeJapaman, currentStage, timeUsed);
                }
            }
        }
    }

    private IEnumerator RoundClearSequence(long roundTotal, long plateCount, long roundExtra, float totalTime)
    {
        Debug.Log("🎯 === RoundClearSequence 開始 ===");
        isRoundClearing = true;
        yield return new WaitForSeconds(0.5f);

        bool hasNextStage = HasNextStage();
        Debug.Log($"🔍 hasNextStage: {hasNextStage}, ShouldShowUpgradeSelection: {ShouldShowUpgradeSelection()}");

        if (hasNextStage && ShouldShowUpgradeSelection())
        {
            Debug.Log("🔄 アップグレード選択シーケンス開始");
            yield return StartCoroutine(ShowUpgradeSelectionSequence());
            Debug.Log("✅ アップグレード選択シーケンス完了");
        }

        // 🆕 中断セーブダイアログ表示
        Debug.Log("🎯 アップグレード選択完了、中断セーブ選択フローに移行");
        ShowSuspendOrContinueDialog();

        Debug.Log("🎯 === RoundClearSequence 終了 ===");
        isRoundClearing = false;
    }

    private IEnumerator WaitForEatingSequence()
    {
        if (isWaitingForEating)
        {
            Debug.Log("⚠️ WaitForEatingSequence: 既に実行中、終了");
            yield break;
        }

        if (currentGameState != GameState.Playing && !isTimeUp)
        {
            Debug.Log($"⚠️ WaitForEatingSequence: 不正な呼び出し - state:{currentGameState}, timeUp:{isTimeUp}");
            yield break;
        }

        Debug.Log($"🍽️ 時間切れ処理開始 - 経過時間: {Time.time - gameStartTime:F1}秒");
        isWaitingForEating = true;

        if (clickManager != null)
        {
            clickManager.SetClickEnabled(false);
        }

        yield return new WaitForSeconds(1f);

        long currentTotal = clickManager != null ? clickManager.GetTotalJapamanCount() : 0;
        bool achieved = currentTotal >= currentStageGoal;

        Debug.Log($"📊 最終結果: {currentTotal}/{currentStageGoal} (達成: {achieved})");

        if (achieved)
        {
            Debug.Log("🎉 ノルマ達成");
            isCleared = true;
            EndGame();
        }
        else
        {
            Debug.Log("❌ ゲームオーバー");
            if (GameOverDialogManager.Instance != null && clickManager != null)
            {
                GameOverDialogManager.Instance.StartGameOverDialog(
                    currentStage,
                    currentTotal,
                    currentStageGoal,
                    () => {
                        ShowGameOverPanel();
                    }
                );
            }
            else
            {
                Debug.LogError("❌ GameOverDialogManager または ClickManager が見つかりません");
                ShowGameOverPanel();
            }
        }

        isWaitingForEating = false;
    }

    private bool ShouldShowUpgradeSelection()
    {
        return true;
    }

    private IEnumerator ShowUpgradeSelectionSequence()
    {
        SetGameState(GameState.UpgradeSelection);
        isShowingUpgradeSelection = true;

        var upgradeSelectionUI = FindFirstObjectByType<UpgradeSelectionUI>();
        if (upgradeSelectionUI != null)
        {
            bool selectionComplete = false;
            upgradeSelectionUI.ShowUpgradeSelection(currentStage, () => {
                selectionComplete = true;
            });

            while (!selectionComplete)
            {
                yield return null;
            }
        }

        isShowingUpgradeSelection = false;
        SetGameState(GameState.Playing);
    }

    private bool HasNextStage()
    {
        int maxStage = 30;
        return currentStage < maxStage;
    }

    private void ShowGameComplete()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameClearPanel();
        }
    }



    // ✅ 修正: 既存のUIManagerに合わせたシンプルな実装
    // GameManager.cs の最もシンプルで確実な修正

    void ShowSuspendOrContinueDialog()
    {
        Debug.Log("中断セーブ選択ダイアログを表示");

        if (UIManager.Instance != null)
        {
            // 新しい選択システムを使用
            UIManager.Instance.ShowStageCompleteChoice(
                currentStage,
                () => {
                    Debug.Log("次のステージ選択");
                    OnNextDay();
                },
                () => {
                    Debug.Log("中断セーブ選択");
                    OnSuspendAndReturn();
                }
            );
        }
        else
        {
            Debug.LogError("UIManager.Instance が見つかりません");
            OnNextDay();
        }
    }


    
    // 🆕 次の日に進むボタンのイベント
    public void OnContinueToNextDay()
    {
        Debug.Log("🌅 次の日に進む選択");

        if (suspendChoiceDialog != null)
        {
            suspendChoiceDialog.SetActive(false);
        }

        if (HasNextStage())
        {
            OnNextDay();
        }
        else
        {
            Debug.Log("🎉 全ステージクリア！");
            ShowGameComplete();
        }
    }

    // 🆕 中断セーブしてタイトルに戻るボタンのイベント
    public void OnSuspendAndReturn()
    {
        Debug.Log("💾 中断セーブしてタイトルに戻る選択");

        if (suspendChoiceDialog != null)
        {
            suspendChoiceDialog.SetActive(false);
        }

        // SuspendSaveManagerに中断セーブ作成を依頼
        CreateSuspendSaveAndReturnToTitle();
    }

    // 🆕 中断セーブ作成とタイトル復帰
    private void CreateSuspendSaveAndReturnToTitle()
    {
        Debug.Log("💾 === 中断セーブ作成とタイトル復帰開始 ===");

        try
        {
            // 次のステージの情報で中断セーブを作成
            int nextStage = HasNextStage() ? currentStage + 1 : currentStage;
            SaveData suspendData = CreateCurrentSaveData();

            if (suspendData != null)
            {
                // 中断セーブ用スロット（999番）に保存
                bool saveSuccess = SaveManager.Instance.SaveGame(999, suspendData);

                if (saveSuccess)
                {
                    Debug.Log($"✅ 中断セーブ作成完了: ステージ{suspendData.currentStage}");

                    // タイトルシーンに移行
                    UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
                }
                else
                {
                    Debug.LogError("❌ 中断セーブの保存に失敗");
                    // フォールバック: 通常の次の日進行
                    OnContinueToNextDay();
                }
            }
            else
            {
                Debug.LogError("❌ 中断セーブデータの作成に失敗");
                OnContinueToNextDay();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 中断セーブ処理エラー: {e.Message}");
            OnContinueToNextDay();
        }
    }

    // ゲームオーバーパネル表示を改良
    private void ShowGameOverPanel()
    {
        Debug.Log("=== ShowGameOverPanel 開始 ===");
        RefreshUIReferences();

        if (gameOverPanel == null)
        {
            Debug.LogError("gameOverPanel が見つかりません！");
            return;
        }

        if (clickManager == null)
        {
            Debug.LogError("clickManager が見つかりません！");
            return;
        }

        gameOverPanel.SetActive(true);
        SetGameOverPanelData(currentStage, clickManager.GetTotalJapamanCount(), currentStageGoal);
        SetupGameOverButtons();

        Debug.Log("✅ ゲームオーバーパネル表示完了");
    }

    private void SetupGameOverButtons()
    {
        if (gameOverPanel == null) return;

        Debug.Log("🔘 ゲームオーバーボタン設定開始");

        Button retryButton = FindButtonInPanel(gameOverPanel, "RetryButton");
        if (retryButton == null) retryButton = FindButtonInPanel(gameOverPanel, "Retry");
        if (retryButton == null) retryButton = FindButtonInPanel(gameOverPanel, "1日目から");

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryFromDay1Button);
            Debug.Log("✅ リトライボタン設定完了");
        }
        else
        {
            Debug.LogWarning("⚠️ リトライボタンが見つかりません");
        }

        // 🆕 中断セーブから復帰ボタン（ロード機能の代替）
        Button resumeButton = FindButtonInPanel(gameOverPanel, "ResumeButton");
        if (resumeButton == null) resumeButton = FindButtonInPanel(gameOverPanel, "Resume");
        if (resumeButton == null) resumeButton = FindButtonInPanel(gameOverPanel, "中断から");

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeFromSuspendSave);

            // 中断セーブがあるかチェックしてボタンの有効性を設定
            bool hasSuspendSave = SaveManager.Instance.IsSlotUsed(999);
            resumeButton.interactable = hasSuspendSave;

            Debug.Log($"✅ 中断セーブ復帰ボタン設定完了 (有効: {hasSuspendSave})");
        }
        else
        {
            Debug.LogWarning("⚠️ 中断セーブ復帰ボタンが見つかりません");
        }

        Button titleButton = FindButtonInPanel(gameOverPanel, "TitleButton");
        if (titleButton == null) titleButton = FindButtonInPanel(gameOverPanel, "Title");
        if (titleButton == null) titleButton = FindButtonInPanel(gameOverPanel, "タイトル");

        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(OnBackToTitleButton);
            Debug.Log("✅ タイトルボタン設定完了");
        }
        else
        {
            Debug.LogWarning("⚠️ タイトルボタンが見つかりません");
        }

        Debug.Log("🔘 ゲームオーバーボタン設定完了");
    }

    private Button FindButtonInPanel(GameObject panel, string buttonName)
    {
        if (panel == null) return null;

        Transform directChild = panel.transform.Find(buttonName);
        if (directChild != null)
        {
            Button button = directChild.GetComponent<Button>();
            if (button != null) return button;
        }

        Button[] allButtons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button button in allButtons)
        {
            if (button.name.Contains(buttonName) || button.gameObject.name.Contains(buttonName))
            {
                return button;
            }

            TMPro.TextMeshProUGUI text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null && text.text.Contains(buttonName))
            {
                return button;
            }
        }

        return null;
    }

    private void SetGameOverPanelData(int stage, long japamanCount, long goalCount)
    {
        if (gameOverPanel == null) return;

        Transform resultArea = gameOverPanel.transform.Find("ResultArea");
        if (resultArea != null)
        {
            TMPro.TextMeshProUGUI stageText = resultArea.Find("StageText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (stageText != null)
            {
                stageText.text = $"ステージ {stage}日目";
            }

            TMPro.TextMeshProUGUI scoreText = resultArea.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (scoreText != null)
            {
                scoreText.text = $"ジャパまん: {japamanCount} / {goalCount}個";
            }
        }
    }

    public void OnRetryFromDay1Button()
    {
        Debug.Log("🔄 1日目からリトライが選択されました");
        Debug.Log("🔘 OnRetryFromDay1Button: 実行開始");

        ResetAllGameStates();
        currentStage = 1;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🔘 ゲームオーバーパネルを非表示にしました");
        }

        currentStageGoal = GetStageGoal(1);
        InitializeStage();

        Debug.Log("🔘 OnRetryFromDay1Button: 実行完了");
    }

    // 🆕 中断セーブから復帰
    public void OnResumeFromSuspendSave()
    {
        Debug.Log("🔄 中断セーブから復帰が選択されました");
        Debug.Log("🔘 OnResumeFromSuspendSave: 実行開始");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🔘 ゲームオーバーパネルを非表示にしました");
        }

        // 中断セーブデータを読み込み
        SaveData suspendData = SaveManager.Instance.LoadSaveData(999);
        if (suspendData != null)
        {
            Debug.Log($"💾 中断セーブデータ読み込み完了: ステージ{suspendData.currentStage}");

            // ゲーム状態を復元
            LoadGameData(suspendData);

            // 中断セーブは使用後削除
            SaveManager.Instance.DeleteSaveData(999);
            Debug.Log("💾 中断セーブデータを削除しました");
        }
        else
        {
            Debug.LogError("❌ 中断セーブデータが見つかりません");
            OnRetryFromDay1Button(); // フォールバック
        }

        Debug.Log("🔘 OnResumeFromSuspendSave: 実行完了");
    }

    public void OnBackToTitleButton()
    {
        Debug.Log("🔄 タイトルに戻るが選択されました");
        Debug.Log("🔘 OnBackToTitleButton: 実行開始");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🔘 ゲームオーバーパネルを非表示にしました");
        }

        Debug.Log("🔘 TitleSceneをロード中...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");

        Debug.Log("🔘 OnBackToTitleButton: 実行完了");
    }

    // 🆕 簡素化されたラウンド完了処理
    public void OnRoundComplete(int japamanCount)
    {
        Debug.Log($"🎉 === OnRoundComplete 簡素化版開始 ===");
        Debug.Log($"🎉 📝 ラウンド完了パラメータ: japamanCount={japamanCount}, currentStage={currentStage}");

        lastRoundJapamanCount = japamanCount;
        Debug.Log($"🎉 ✅ lastRoundJapamanCount更新: {lastRoundJapamanCount}");

        if (!isCleared)
        {
            isCleared = true;
            Debug.Log("🎉 ✅ クリア状態をtrueに設定");
        }

        totalStagesCompleted++;
        Debug.Log($"🎉 ✅ 完了ステージ数更新: {totalStagesCompleted}");

        // 🗑️ ShowSaveSelection()は削除 - 直接中断セーブ選択に移行
        Debug.Log("🎉 ラウンド完了 - EndGameに移行");
        EndGame(); // これによりRoundClearSequenceが呼ばれ、中断セーブダイアログが表示される

        Debug.Log($"🎉 === OnRoundComplete 簡素化版終了 ===");
    }

   

    void OnNextDay()
    {
        Debug.Log($"🌅 次の日: {currentStage} → {currentStage + 1}");
        currentStage++;
        isCleared = false;
        Debug.Log("🔄 次の日のためクリア状態をリセット");

        if (UIManager.Instance != null)
        {
            long nextGoal = GetStageGoal(currentStage);
            UIManager.Instance.ShowStageTransition(currentStage, nextGoal);
        }

        Invoke("InitializeStage", 0.5f);
    }

    public void OnNextDayButtonPressed()
    {
        Debug.Log("OnNextDayButtonPressed呼び出し（レガシー）");
        // 新しいシステムでは使用しない
        // フォールバック処理として次のステージに進む
        OnNextDay();
    }

    // 🔥 時刻保持対応版のセーブデータ作成メソッド
    public SaveData CreateCurrentSaveData(System.DateTime? preserveDateTime = null)
    {
        Debug.Log($"💾 === CreateCurrentSaveData デバッグ開始 ===");
        Debug.Log($"💾 🔍 現在の状態:");
        Debug.Log($"💾 - currentStage: {currentStage}");
        Debug.Log($"💾 - isCleared: {isCleared}");
        Debug.Log($"💾 - totalStagesCompleted: {totalStagesCompleted}");
        Debug.Log($"💾 - lastRoundJapamanCount: {lastRoundJapamanCount}");
        Debug.Log($"💾 - isGameEnded: {isGameEnded}");
        Debug.Log($"💾 - currentGameState: {currentGameState}");

        int saveStage;
        long saveJapamanCount = lastRoundJapamanCount;

        // クリア済み判定ロジック
        if (isCleared && isGameEnded && totalStagesCompleted >= 1)
        {
            saveStage = currentStage + 1; // 次のステージを保存
            Debug.Log($"💾 ✅ クリア済み判定: 次ステージ{saveStage}を保存");
            Debug.Log($"💾 判定理由: isCleared={isCleared}, isGameEnded={isGameEnded}, totalStagesCompleted={totalStagesCompleted}");
        }
        else
        {
            saveStage = currentStage; // 現在のステージを保存
            Debug.Log($"💾 🔄 継続中判定: 現在ステージ{saveStage}を保存");
            Debug.Log($"💾 判定理由: isCleared={isCleared}, isGameEnded={isGameEnded}, totalStagesCompleted={totalStagesCompleted}");
        }

        Debug.Log($"💾 📝 セーブデータ作成:");
        Debug.Log($"💾 - 現在のステージ: {currentStage}");
        Debug.Log($"💾 - 保存するステージ: {saveStage}");
        Debug.Log($"💾 - 最後のジャパまん数: {saveJapamanCount}");

        if (preserveDateTime.HasValue)
        {
            Debug.Log($"💾 🕐 時刻保持モードでセーブデータ作成: {preserveDateTime.Value:yyyy/MM/dd HH:mm:ss}");
        }
        else
        {
            Debug.Log($"💾 🆕 新規時刻モードでセーブデータ作成");
        }

        SaveData data = SaveData.CreateFromCurrentState(saveStage, saveJapamanCount, preserveDateTime);

        Debug.Log($"💾 ✅ セーブデータ作成完了:");
        Debug.Log($"💾 - data.currentStage: {data.currentStage}");
        Debug.Log($"💾 - data.lastJapamanCount: {data.lastJapamanCount}");
        Debug.Log($"💾 - アップグレード数: {data.upgradeQuantities?.Length ?? 0}種類");

        if (data.currentStage == saveStage)
        {
            Debug.Log($"💾 ✅ セーブデータ整合性確認OK: {data.currentStage}");
        }
        else
        {
            Debug.LogError($"💾 ❌ セーブデータ不整合！期待:{saveStage}, 実際:{data.currentStage}");
        }

        Debug.Log($"💾 === CreateCurrentSaveData デバッグ終了 ===");
        return data;
    }

    public SaveData CreateCurrentSaveData()
    {
        return CreateCurrentSaveData(null);
    }

    public void SetGameState(GameState newState)
    {
        GameState previousState = currentGameState;
        currentGameState = newState;
        Debug.Log($"ゲーム状態変更: {previousState} → {newState}");
    }

    public bool CanAutoProduction()
    {
        return currentGameState == GameState.Playing && !IsTimeUp();
    }

    private void OnDestroy()
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("✅ GameManager: シーンロードコールバック解除完了");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ シーンロードコールバック解除時エラー: {e.Message}");
        }

        if (Instance == this)
        {
            Instance = null;
            Debug.Log("🔄 GameManager Instance参照をクリア");
        }
    }

    // アクセサメソッド
    public bool IsTimeUp() => isTimeUp;
    public bool IsGameEnded() => isGameEnded;
    public bool IsCleared() => isCleared;
    public bool IsShowingUpgradeSelection() => isShowingUpgradeSelection;
    public long GetCurrentStageGoal() => currentStageGoal;
    public float GetRemainingTime() => timeRemaining;
    public int GetCurrentStage() => currentStage;
    public long GetTotalLifetimeJapaman() => totalLifetimeJapaman;
    public bool IsWaitingForEating() => isWaitingForEating;
    public bool IsWaitingForNextDay() => isWaitingForNextDay;

    /// <summary>
    /// 中断セーブデータが存在するかチェック
    /// </summary>
    public bool HasSuspendSaveData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager.Instance が null です");
            return false;
        }

        const int SUSPEND_SAVE_SLOT = 999;
        return SaveManager.Instance.IsSlotUsed(SUSPEND_SAVE_SLOT);
    }

    public void SetGoalAchieved()
    {
        if (!isCleared)
        {
            isCleared = true;
            goalAchievedTime = Time.time - gameStartTime;
            Debug.Log($"🎯 ノルマ達成設定: 時間{goalAchievedTime:F1}秒");
        }
    }

    public bool IsGoalAchieved()
    {
        return isCleared;
    }

    // === デバッグメソッド（簡素化版） ===
    [ContextMenu("🔍 現在のゲーム状態確認")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== 🔍 ゲーム状態デバッグ ===");
        Debug.Log($"🎮 基本情報:");
        Debug.Log($" - currentGameState: {currentGameState}");
        Debug.Log($" - currentStage: {currentStage}");
        Debug.Log($" - currentStageGoal: {currentStageGoal}");
        Debug.Log($"⭐ クリア関連:");
        Debug.Log($" - isCleared: {isCleared}");
        Debug.Log($" - totalStagesCompleted: {totalStagesCompleted}");
        Debug.Log($" - lastRoundJapamanCount: {lastRoundJapamanCount}");
        Debug.Log($"⏰ 時間・状態管理:");
        Debug.Log($" - timeLimit: {timeLimit}秒");
        Debug.Log($" - timeRemaining: {timeRemaining}秒");
        Debug.Log($" - isTimeUp: {isTimeUp}");
        Debug.Log($" - isWaitingForCountdown: {isWaitingForCountdown}");
        Debug.Log($" - isGameEnded: {isGameEnded}");
        Debug.Log($"💾 中断セーブ:");
        Debug.Log($" - 中断セーブ存在: {HasSuspendSaveData()}");
        Debug.Log($"🔧 その他:");
        Debug.Log($" - 自動生産可能: {CanAutoProduction()}");
        Debug.Log($" - UI参照状態: gameOverPanel={gameOverPanel != null}");
        Debug.Log($" - 中断ダイアログ: suspendChoiceDialog={suspendChoiceDialog != null}");
        Debug.Log($"=== デバッグ終了 ===");
    }

    [ContextMenu("💾 セーブデータ作成テスト")]
    public void DebugCreateSaveData()
    {
        Debug.Log("=== 💾 セーブデータ作成テスト開始 ===");
        SaveData testData = CreateCurrentSaveData();
        if (testData != null)
        {
            Debug.Log($"✅ テスト成功: ステージ{testData.currentStage}のセーブデータ作成");
        }
        else
        {
            Debug.LogError("❌ テスト失敗: セーブデータがnull");
        }
        Debug.Log("=== テスト完了 ===");
    }

    [ContextMenu("🎉 テスト用ラウンドクリア")]
    public void DebugTestRoundComplete()
    {
        Debug.Log("=== 🎉 テスト用ラウンドクリア実行 ===");
        OnRoundComplete(100);
        Debug.Log("=== テスト完了 ===");
    }

    [ContextMenu("🔄 状態リセット")]
    public void DebugResetStates()
    {
        Debug.Log("=== 🔄 状態リセット実行 ===");
        ResetAllGameStates();
        Debug.Log("✅ 状態リセット完了");
    }

    [ContextMenu("💾 中断セーブ作成テスト")]
    public void DebugCreateSuspendSave()
    {
        Debug.Log("=== 💾 中断セーブ作成テスト開始 ===");
        CreateSuspendSaveAndReturnToTitle();
        Debug.Log("=== テスト完了 ===");
    }

    [ContextMenu("💾 中断セーブ状態確認")]
    public void DebugCheckSuspendSave()
    {
        Debug.Log("=== 💾 中断セーブ状態確認 ===");
        bool hasSuspendSave = HasSuspendSaveData();
        Debug.Log($"📋 中断セーブ存在: {hasSuspendSave}");

        if (hasSuspendSave && SaveManager.Instance != null)
        {
            SaveData suspendData = SaveManager.Instance.LoadSaveData(999);
            if (suspendData != null)
            {
                Debug.Log($"📄 中断セーブ詳細:");
                Debug.Log($" - ステージ: {suspendData.currentStage}");
                Debug.Log($" - ジャパまん数: {suspendData.lastJapamanCount}");
                Debug.Log($" - 保存日時: {suspendData.GetSaveDateTimeString()}");
            }
        }
        Debug.Log("=== 確認完了 ===");
    }

    [ContextMenu("🔍 カウントダウンデバッグ")]
    public void DebugCountdownIssue()
    {
        Debug.Log("=== カウントダウンデバッグ開始 ===");

        // UIManagerの存在確認
        Debug.Log($"UIManager.Instance: {(UIManager.Instance != null ? "存在" : "null")}");

        if (UIManager.Instance != null)
        {
            Debug.Log($"countdownPanel: {(UIManager.Instance.countdownPanel != null ? "設定済み" : "null")}");
            Debug.Log($"countdownText: {(UIManager.Instance.countdownText != null ? "設定済み" : "null")}");

            if (UIManager.Instance.countdownPanel != null)
            {
                Debug.Log($"countdownPanel.activeSelf: {UIManager.Instance.countdownPanel.activeSelf}");
            }
        }

        // GameManagerの状態確認
        Debug.Log($"currentGameState: {currentGameState}");
        Debug.Log($"isWaitingForCountdown: {isWaitingForCountdown}");
        Debug.Log($"currentStage: {currentStage}");
        Debug.Log($"currentStageGoal: {currentStageGoal}");

        Debug.Log("=== デバッグ終了 ===");
    }
    [ContextMenu("🎬 手動カウントダウン開始")]
    public void DebugStartCountdown()
    {
        Debug.Log("手動カウントダウン開始");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartRoundCountdown();
            Debug.Log("StartRoundCountdown呼び出し完了");
        }
        else
        {
            Debug.LogError("UIManager.Instance が null です");
        }
    }


    [ContextMenu("🎯 中断ダイアログテスト")]
    public void DebugShowSuspendDialog()
    {
        Debug.Log("=== 🎯 中断ダイアログテスト ===");
        ShowSuspendOrContinueDialog();
        Debug.Log("=== テスト完了 ===");
    }

    [ContextMenu("🚨 完全リセット")]
    public void CompleteReset()
    {
        Debug.Log("🚨 完全リセット実行");

        // GameManager状態
        currentStage = 1;
        timeRemaining = timeLimit;
        currentStageGoal = 50;
        totalLifetimeJapaman = 0;
        isCleared = false;
        isTimeUp = false;
        isGameEnded = false;

        // ClickManager状態
        if (clickManager != null)
        {
            clickManager.japamanCount = 0;
            clickManager.extraJapamanCount = 0;
            clickManager.goalCount = 50;
        }

        // UI更新
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateJapamanText(0);
            UIManager.Instance.UpdateGoalText(50);
            UIManager.Instance.UpdateTimeText(30);
        }

        Debug.Log("🚨 完全リセット完了: じゃぱまん0個, 目標50個, 時間30秒");
    }

    /// <summary>
    /// 🔍 現在のゲーム状態を詳細確認
    /// </summary>
    [ContextMenu("🔍 新規ゲーム状態検証")]
    public void DebugVerifyNewGameState()
    {
        Debug.Log("=== 🔍 新規ゲーム状態検証 ===");

        // GameManager状態
        Debug.Log($"📋 GameManager:");
        Debug.Log($"  - currentStage: {currentStage}");
        Debug.Log($"  - timeRemaining: {timeRemaining}");
        Debug.Log($"  - currentStageGoal: {currentStageGoal}");

        // ClickManager状態
        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            Debug.Log($"📋 ClickManager:");
            Debug.Log($"  - japamanCount: {clickManager.japamanCount}");
            Debug.Log($"  - clickMultiplier: {clickManager.clickMultiplier}");
            Debug.Log($"  - autoClickRate: {clickManager.autoClickRate}");
        }

        // UpgradeManager状態
        if (UpgradeManager.Instance != null)
        {
            Debug.Log($"📋 UpgradeManager:");
            Debug.Log($"  - activeUpgrades数: {UpgradeManager.Instance.activeUpgrades.Count}");

            int activeAllUpgrades = 0;
            foreach (var upgrade in UpgradeManager.Instance.allUpgrades)
            {
                if (upgrade.isActive || upgrade.currentLevel > 0)
                {
                    activeAllUpgrades++;
                    Debug.Log($"  - アクティブなallUpgrade: {upgrade.upgradeName} Lv.{upgrade.currentLevel}");
                }
            }
            Debug.Log($"  - アクティブなallUpgrades数: {activeAllUpgrades}");
        }

        // 期待される初期状態
        Debug.Log($"📋 期待される初期状態:");
        Debug.Log($"  - ジャパまん: 0個");
        Debug.Log($"  - 時間: 30秒");
        Debug.Log($"  - 目標: 50個");
        Debug.Log($"  - クリック倍率: x1");
        Debug.Log($"  - 自動クリック: 0/秒");
        Debug.Log($"  - アップグレード: 0個");

        Debug.Log("=== 検証完了 ===");
    }

}



public enum GameState
{
    Menu, Playing, Result, UpgradeSelection, Paused, Loading
}

