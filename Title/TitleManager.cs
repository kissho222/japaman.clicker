using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("タイトル画面UI")]
    public GameObject titlePanel;
    public TextMeshProUGUI titleText;
    public Button newGameButton;
    public Button loadGameButton;        // デバッグ用（削除予定）
    public Button resumeGameButton;      // 新規追加：中断セーブから再開
    public Button settingsButton;
    public Button exitButton;

    [Header("設定画面UI")]
    public GameObject settingsPanel;
    public Button settingsBackButton;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("バージョン情報")]
    public TextMeshProUGUI versionText;
    public string gameVersion = "獣人達 v1.0";

    [Header("ゲームシーン")]
    public string gameSceneName = "GameScene";

    [Header("情報ボタン")]
    public Button howToPlayButton;
    public Button aboutKemonoButton;
    public Button creditsButton;

    private InfoPanelsManager infoPanelsManager;



    void Start()
    {
        Debug.Log("🎬 TitleManager Start開始");

        // 🔧 修正: アプリケーション終了フラグのリセット
        Application.wantsToQuit -= OnApplicationWantsToQuit;
        Application.wantsToQuit += OnApplicationWantsToQuit;

        // 🔧 修正: 初期化処理の順序を調整
        InitializeTitle();
        SetupEventListeners();
        LoadTitleSettings();

        // 🔧 修正: 中断セーブ存在チェックと表示更新（遅延実行）
        StartCoroutine(DelayedSuspendSaveCheck());

        Debug.Log("✅ TitleManager Start完了");
    }

    /// <summary>
    /// 遅延実行による中断セーブチェック
    /// </summary>
    private System.Collections.IEnumerator DelayedSuspendSaveCheck()
    {
        // SaveManagerの初期化を待つ
        int waitCount = 0;
        while (SaveManager.Instance == null && waitCount < 50) // 最大5秒待機
        {
            yield return new WaitForSeconds(0.1f);
            waitCount++;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("⚠️ SaveManager.Instance の初期化タイムアウト");
        }

        // 中断セーブ表示を更新
        UpdateSuspendSaveDisplay();
    }


    /// <summary>
    /// アプリケーション終了要求時の処理
    /// </summary>
    private bool OnApplicationWantsToQuit()
    {
        Debug.Log("🔄 アプリケーション終了要求を受信");
        return true; // 通常の終了を許可
    }

    /// <summary>
    /// 中断セーブの表示を更新（デバッグ強化版）
    /// </summary>
    private void UpdateSuspendSaveDisplay()
    {
        Debug.Log("🔍 === UpdateSuspendSaveDisplay 開始 ===");

        // 🔧 修正: SaveManagerの初期化確認
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("⚠️ SaveManager.Instance が null - 初期化待機");
            // 初期化待機中は安全な初期状態を設定
            if (resumeGameButton != null)
            {
                resumeGameButton.interactable = false;
                UpdateButtonText(resumeGameButton, "中断セーブから再開");
                Debug.Log("🔧 初期化待機中: ResumeGameButton -> '中断セーブから再開' (無効)");
            }
            return;
        }

        // 🔧 修正: SuspendSaveManagerの静的メソッドを使用
        bool hasSuspendSave = SuspendSaveManager.CheckSuspendSaveExists();

        Debug.Log($"🔍 中断セーブ存在確認結果: {hasSuspendSave}");

        // resumeGameButtonの有効/無効切り替え
        if (resumeGameButton != null)
        {
            resumeGameButton.interactable = hasSuspendSave;

            // 🔧 修正: 正確なテキスト設定
            if (hasSuspendSave)
            {
                UpdateButtonText(resumeGameButton, "中断セーブから再開");
                Debug.Log("🔧 ResumeGameButton: '中断セーブから再開' (有効)");
            }
            else
            {
                UpdateButtonText(resumeGameButton, "中断セーブから再開");
                Debug.Log("🔧 ResumeGameButton: '中断セーブから再開' (無効)");
            }
        }

        // 🔧 修正: 新規ゲームボタンの処理
        if (newGameButton != null)
        {
            newGameButton.interactable = true; // 常に有効
            UpdateButtonText(newGameButton, "はじめから");
            Debug.Log("🔧 NewGameButton: 'はじめから' (有効)");
        }

        Debug.Log($"🔍 === UpdateSuspendSaveDisplay 完了 ===");
    }


    /// <summary>
    /// ボタンのテキストを更新（型安全版）
    /// </summary>
    private void UpdateButtonText(Button button, string newText)
    {
        if (button == null) return;

        // TextMeshProUGUIを先にチェック
        var tmpText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = newText;
            Debug.Log($"🔧 TMPテキスト更新: {button.name} -> {newText}");
            return;
        }

        // 次にUnityEngine.UI.Textをチェック
        var uiText = button.GetComponentInChildren<UnityEngine.UI.Text>();
        if (uiText != null)
        {
            uiText.text = newText;
            Debug.Log($"🔧 UIテキスト更新: {button.name} -> {newText}");
            return;
        }

        Debug.LogWarning($"⚠️ {button.name}にテキストコンポーネントが見つかりません");
    }



    // 中断セーブ復元ボタンのイベント
    public void OnResumeSuspendSaveClick()
    {
        SuspendSaveManager.ResumeSuspendSave();
    }

    void InitializeTitle()
    {
        infoPanelsManager = FindFirstObjectByType<InfoPanelsManager>();

        if (versionText != null)
        {
            versionText.text = gameVersion;
        }

        ShowTitlePanel();
        CheckSaveDataAvailability();
    }
    // SetupEventListeners メソッドに以下を追加
    void SetupEventListeners()
    {
        // メインボタン
        newGameButton.onClick.AddListener(OnNewGameClick);

        
        // 🔧 修正: 中断セーブから再開ボタン（静的メソッド呼び出し）
        if (resumeGameButton != null)
            resumeGameButton.onClick.AddListener(OnResumeGameClick);

        settingsButton.onClick.AddListener(OnSettingsClick);
        exitButton.onClick.AddListener(OnExitClick);

        // 情報ボタン
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OnHowToPlayClick);

        if (aboutKemonoButton != null)
            aboutKemonoButton.onClick.AddListener(OnAboutKemonoClick);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClick);

        // 設定画面
        settingsBackButton.onClick.AddListener(OnSettingsBackClick);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
    }

    void LoadTitleSettings()
    {
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        volumeSlider.value = volume;
        OnVolumeChanged(volume);

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.isOn = isFullscreen;
        Screen.fullScreen = isFullscreen;
    }

    void CheckSaveDataAvailability()
    {
        bool hasSuspendSave = CheckSuspendSaveExists();

        if (resumeGameButton != null)
        {
            // ✅ テキストを固定
            var buttonText = resumeGameButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "中断セーブから再開";  // 固定テキスト
            }

            // ✅ 有効/無効のみ制御
            resumeGameButton.interactable = hasSuspendSave;

            // ✅ ログ出力
            string status = hasSuspendSave ? "有効" : "無効（グレー）";
            Debug.Log($"📋 中断セーブボタン: {status}");
        }
        else
        {
            Debug.LogWarning("⚠️ resumeGameButton が見つかりません");
        }
    }

    bool CheckSuspendSaveExists()
    {
        // 中断セーブファイルの存在確認
        // GameManagerの中断セーブ機能に合わせて実装
        if (GameManager.Instance != null)
        {
            // GameManagerに中断セーブ存在チェック機能がある場合
            return GameManager.Instance.HasSuspendSaveData();
        }

        // または、PlayerPrefsで直接チェック
        return PlayerPrefs.HasKey("SuspendSave_Exists");
    }

    // === ボタンイベント ===

    void OnNewGameClick()
    {
        Debug.Log("新規ゲーム開始");
        // 中断セーブデータがある場合は確認ダイアログを表示
        if (CheckSuspendSaveExists())
        {
            ShowConfirmDialog("新規ゲームを開始すると中断セーブデータが削除されます。よろしいですか？",
                () =>
                {
                    ClearSuspendSaveData();
                    StartNewGame();
                });
        }
        else
        {
            StartNewGame();
        }
    }

  
    /// <summary>
    /// 中断セーブから再開（修正版）
    /// </summary>
    public void OnResumeGameClick()
    {
        Debug.Log("🔄 中断セーブから再開ボタンがクリックされました");

        // 🔧 修正: SuspendSaveManagerの静的メソッドを使用
        if (!SuspendSaveManager.CheckSuspendSaveExists())
        {
            Debug.LogWarning("⚠️ 中断セーブデータが見つかりません");
            return;
        }

        Debug.Log("✅ 中断セーブ復元処理を開始します");

        // 🔧 修正: SuspendSaveManagerに復元処理を委任
        SuspendSaveManager.ResumeSuspendSave();
    }



    void OnSettingsClick()
    {
        Debug.Log("設定画面表示");
        ShowSettingsPanel();
    }

    void OnExitClick()
    {
        Debug.Log("ゲーム終了");
        ShowConfirmDialog("ゲームを終了しますか？", ExitGame);
    }

    void OnSettingsBackClick()
    {
        Debug.Log("タイトルに戻る");
        ShowTitlePanel();
    }

    // === 情報画面ボタン ===

    void OnHowToPlayClick()
    {
        Debug.Log("遊び方画面表示");
        if (infoPanelsManager != null)
        {
            infoPanelsManager.ShowHowToPlay();
        }
        else
        {
            Debug.LogWarning("InfoPanelsManager が見つかりません");
        }
    }

    void OnAboutKemonoClick()
    {
        Debug.Log("けものフレンズとは画面表示");
        if (infoPanelsManager != null)
        {
            infoPanelsManager.ShowAboutKemono();
        }
        else
        {
            Debug.LogWarning("InfoPanelsManager が見つかりません");
        }
    }

    void OnCreditsClick()
    {
        Debug.Log("スタッフクレジット画面表示");
        if (infoPanelsManager != null)
        {
            infoPanelsManager.ShowCredits();
        }
        else
        {
            Debug.LogWarning("InfoPanelsManager が見つかりません");
        }
    }

    // === 設定機能 ===

    void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    // === ゲーム開始・終了 ===

    void StartNewGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        SceneManager.LoadScene(gameSceneName);
    }

    // TitleManager.cs の修正版メソッド

    /// <summary>
    /// 中断セーブからゲームをロード
    /// </summary>
    void LoadSuspendSaveGame()
    {
        Debug.Log("💾 中断セーブからゲームをロード開始");

        if (SuspendSaveManager.Instance != null)
        {
            try
            {
                // 中断セーブが存在するかチェック
                if (SuspendSaveManager.Instance.HasSuspendSave())
                {
                    Debug.Log("✅ 中断セーブデータを発見 - ロード実行");

                    // SuspendSaveManagerのLoadSuspendSaveメソッドを呼び出し
                    SuspendSaveManager.Instance.LoadSuspendSave();

                    // ロード成功後、GameSceneに移動（SuspendSaveManager内で処理される場合は不要）
                    // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                }
                else
                {
                    Debug.LogWarning("⚠️ 中断セーブデータが存在しません");
                    // フォールバック: 新規ゲーム開始
                    StartNewGame();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 中断セーブロード中にエラーが発生: {e.Message}");
                // エラー時は新規ゲーム開始
                StartNewGame();
            }
        }
        else
        {
            Debug.LogError("❌ SuspendSaveManager.Instance が見つかりません");
            // フォールバック: 新規ゲーム開始
            StartNewGame();
        }
    }

    /// <summary>
    /// 中断セーブデータを削除
    /// </summary>
    void ClearSuspendSaveData()
    {
        Debug.Log("🗑️ 中断セーブデータ削除開始");

        try
        {
            // SaveManagerを使って中断セーブスロット（999）を削除
            if (SaveManager.Instance != null)
            {
                const int SUSPEND_SAVE_SLOT = 999;

                // 中断セーブが存在するかチェック
                if (SaveManager.Instance.IsSlotUsed(SUSPEND_SAVE_SLOT))
                {
                    SaveManager.Instance.DeleteSaveData(SUSPEND_SAVE_SLOT);
                    Debug.Log($"✅ 中断セーブデータを削除しました (スロット{SUSPEND_SAVE_SLOT})");

                    // UI更新が必要な場合
                    UpdateSuspendSaveButton();
                }
                else
                {
                    Debug.Log("ℹ️ 削除する中断セーブデータが存在しません");
                }
            }
            else
            {
                Debug.LogError("❌ SaveManager.Instance が見つかりません");
            }

            // SuspendSaveManagerが存在する場合、UI更新を通知
            if (SuspendSaveManager.Instance != null)
            {
                // SuspendSaveManager内部の情報更新
                // 必要に応じて更新メソッドを呼び出し
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 中断セーブデータ削除中にエラーが発生: {e.Message}");
        }
    }

    /// <summary>
    /// 中断セーブボタンの表示状態を更新
    /// </summary>
    void UpdateSuspendSaveButton()
    {
        // 中断セーブボタンが存在する場合の更新処理
        // 例: ボタンのテキスト変更、有効/無効の切り替えなど

        bool hasSuspendSave = false;

        // 中断セーブの存在チェック
        if (SaveManager.Instance != null)
        {
            hasSuspendSave = SaveManager.Instance.IsSlotUsed(999);
        }

        // UIボタンの更新（実際のボタン名に合わせて変更）
        // 例: suspendSaveButton.interactable = hasSuspendSave;
        // 例: suspendSaveButtonText.text = hasSuspendSave ? "中断セーブから再開" : "はじめから";

        Debug.Log($"🔄 中断セーブボタン更新: hasSuspendSave = {hasSuspendSave}");
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // === UI表示制御 ===

    void ShowTitlePanel()
    {
        titlePanel.SetActive(true);
        settingsPanel.SetActive(false);

        // タイトルに戻った時にセーブデータ状態を更新
        CheckSaveDataAvailability();
    }

    void ShowSettingsPanel()
    {
        titlePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // === 確認ダイアログ（簡易版） ===

    void ShowConfirmDialog(string message, System.Action onConfirm)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.DisplayDialog("確認", message, "はい", "いいえ"))
        {
            onConfirm?.Invoke();
        }
#else
        Debug.Log($"確認ダイアログ: {message} - 自動的に実行します");
        onConfirm?.Invoke();
#endif
    }

    // === デバッグ情報 ===

    [ContextMenu("セーブデータ情報を表示")]
    void ShowSaveDataInfo()
    {
        // デバッグ用セーブデータ情報（削除予定）
        if (SaveManager.Instance != null)
        {
            var saveSlots = SaveManager.Instance.GetAllSaveSlots();
            Debug.Log($"デバッグ用セーブデータ数: {saveSlots.Count}");

            foreach (var slot in saveSlots)
            {
                var data = slot.Value;
                Debug.Log($"スロット{slot.Key}: {data.GetDisplayInfo()} - {data.GetSaveDateTimeString()}");
            }
        }

        // 🔧 修正: SuspendSaveManagerの静的メソッドを使用
        bool hasSuspendSave = SuspendSaveManager.CheckSuspendSaveExists();

        Debug.Log($"🔍 中断セーブ存在確認結果: {hasSuspendSave}");

        if (hasSuspendSave)
        {
            string saveDateTime = PlayerPrefs.GetString("SuspendSave_DateTime", "不明");
            string sceneName = PlayerPrefs.GetString("SuspendSave_SceneName", "不明");
            Debug.Log($"中断セーブ詳細 - 日時: {saveDateTime}, シーン: {sceneName}");
        }
    }

    [ContextMenu("全セーブデータ削除")]
    void DeleteAllSaveData()
    {
        // デバッグ用セーブデータ削除（削除予定）
        if (SaveManager.Instance != null)
        {
            for (int i = 0; i < SaveManager.Instance.maxSaveSlots; i++)
            {
                SaveManager.Instance.DeleteSaveData(i);
            }
            Debug.Log("デバッグ用セーブデータを削除しました");
        }

        // 中断セーブデータ削除
        ClearSuspendSaveData();
        Debug.Log("中断セーブデータを削除しました");

        CheckSaveDataAvailability();
    }

    [ContextMenu("中断セーブデータ削除")]
    void DeleteSuspendSaveData()
    {
        ClearSuspendSaveData();
        Debug.Log("中断セーブデータを削除しました");
    }

    /// <summary>
    /// デバッグ用: タイトル画面状態確認
    /// </summary>
    [ContextMenu("🔍 タイトル画面状態確認")]
    public void DebugTitleStatus()
    {
        Debug.Log("=== 🔍 タイトル画面状態確認 ===");

        // ボタンの状態
        if (resumeGameButton != null)
        {
            var tmpText = resumeGameButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Debug.Log($"🔘 ResumeGameButton:");
            Debug.Log($"   - interactable: {resumeGameButton.interactable}");
            Debug.Log($"   - text: '{tmpText?.text ?? "null"}'");
        }

        if (newGameButton != null)
        {
            var tmpText = newGameButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Debug.Log($"🔘 NewGameButton:");
            Debug.Log($"   - interactable: {newGameButton.interactable}");
            Debug.Log($"   - text: '{tmpText?.text ?? "null"}'");
        }

        // SaveManagerの状態
        Debug.Log($"💾 SaveManager.Instance: {(SaveManager.Instance != null ? "存在" : "null")}");

        // 中断セーブの確認
        bool hasSuspendSave = SuspendSaveManager.CheckSuspendSaveExists();
        Debug.Log($"💾 中断セーブ存在: {hasSuspendSave}");

        Debug.Log("=== 確認完了 ===");
    }

    /// <summary>
    /// デバッグ用: 強制ボタン更新
    /// </summary>
    [ContextMenu("🔧 強制ボタン更新")]
    public void DebugForceUpdateButtons()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("プレイモード中のみ実行可能");
            return;
        }

        Debug.Log("🔧 デバッグ: 強制ボタン更新");
        UpdateSuspendSaveDisplay();
    }
}

