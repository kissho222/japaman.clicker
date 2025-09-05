using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
/// <summary>
/// パネル表示アニメーション
/// </summary>using UnityEngine;
using UnityEngine.UI;

public class UpgradeSelectionUI : MonoBehaviour
{
    public static UpgradeSelectionUI Instance { get; private set; }

    [Header("メインUI")]
    public GameObject selectionPanel;           // 選択画面全体のパネル
    public GameObject overlayBackground;        // 背景暗転用
    public TextMeshProUGUI titleText;          // 「アップグレードを選択してください」

    [Header("選択肢コンテナ")]
    public Transform upgradeChoicesContainer;   // Horizontal Layout Group付きのコンテナ
    public GameObject upgradeButtonPrefab;      // アップグレードボタンのプレハブ

    [Header("動的生成されたボタン")]
    private List<Button> dynamicUpgradeButtons = new List<Button>();
    private List<Button> dynamicRerollButtons = new List<Button>();  // 🆕 個別リロールボタン
    private List<TextMeshProUGUI> dynamicUpgradeNames = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> dynamicUpgradeDescriptions = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> dynamicUpgradeLevels = new List<TextMeshProUGUI>();

    [Header("制御ボタン")]
    public Button globalRerollButton;           // 🔄 全体リロールボタン（別機能として残す）
    public Button skipButton;                   // スキップボタン

    [Header("リロール設定")]
    public bool enableIndividualReroll = true;  // 個別リロール有効/無効
    public int rerollCost = 1;                  // リロールコスト（後日実装）

    [Header("設定")]
    public int maxUpgradeChoices = 5;          // 最大選択肢数（拡張可能）
    public int defaultChoiceCount = 3;         // デフォルト選択肢数
    public float animationDuration = 0.3f;     // アニメーション時間

    [Header("誤クリック防止設定")]
    public float clickProtectionTime = 1.0f;   // クリック保護時間（秒）

    // 内部データ
    private List<UpgradeData> currentChoices = new List<UpgradeData>();
    private bool isSelectionActive = false;
    private bool isClickProtectionActive = false;  // 🆕 クリック保護中フラグ
    private Action onSelectionComplete;
    private int currentChoiceCount = 3;        // 現在の選択肢数



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
        InitializeUI();
        SetupEventListeners();
    }

    private void InitializeUI()
    {
        // 最初は全て非表示
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (overlayBackground != null)
            overlayBackground.SetActive(false);

        // タイトルテキスト設定
        if (titleText != null)
            titleText.text = "アップグレードを選択してください";

        // デフォルト選択肢数を設定
        currentChoiceCount = defaultChoiceCount;

        // リロールボタンは後日実装のため無効化
        if (globalRerollButton != null)
        {
            globalRerollButton.gameObject.SetActive(false);
            globalRerollButton.interactable = false;
        }

        // 既存の動的ボタンをクリア
        ClearDynamicButtons();

        Debug.Log("UpgradeSelectionUI 初期化完了（動的生成対応）");
    }

    private void SetupEventListeners()
    {
        // スキップボタン
        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipSelected);

        // リロールボタン（後日実装）
        if (globalRerollButton != null)
            globalRerollButton.onClick.AddListener(OnGlobalRerollSelected);

        // 動的ボタンのイベントは生成時に設定
    }

    /// <summary>
    /// 動的ボタンをクリア
    /// </summary>
    private void ClearDynamicButtons()
    {
        Debug.Log($"🧹 動的ボタンクリア開始: {dynamicUpgradeButtons.Count}個");

        // 既存のボタンを確実に削除
        foreach (var button in dynamicUpgradeButtons)
        {
            if (button != null && button.gameObject != null)
            {
                // イベントリスナーも削除
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
        }

        // リロールボタンも削除
        foreach (var rerollBtn in dynamicRerollButtons)
        {
            if (rerollBtn != null && rerollBtn.gameObject != null)
            {
                rerollBtn.onClick.RemoveAllListeners();
                Destroy(rerollBtn.gameObject);
            }
        }

        // リスト完全クリア
        dynamicUpgradeButtons.Clear();
        dynamicUpgradeNames.Clear();
        dynamicUpgradeDescriptions.Clear();
        dynamicUpgradeLevels.Clear();
        dynamicRerollButtons.Clear();

        // 🔥 コンテナの子オブジェクトも確認して削除
        if (upgradeChoicesContainer != null)
        {
            for (int i = upgradeChoicesContainer.childCount - 1; i >= 0; i--)
            {
                var child = upgradeChoicesContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Debug.Log("🧹 動的ボタンクリア完了");
    }

    /// <summary>
    /// 指定数のアップグレードボタンを動的生成
    /// </summary>
    private void CreateUpgradeButtons(int count)
    {
        ClearDynamicButtons();

        if (upgradeChoicesContainer == null || upgradeButtonPrefab == null)
        {
            Debug.LogError("upgradeChoicesContainer または upgradeButtonPrefab が設定されていません");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // プレハブからボタンを生成
            GameObject buttonObj = Instantiate(upgradeButtonPrefab, upgradeChoicesContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button == null)
            {
                Debug.LogError($"プレハブにButtonコンポーネントがありません: {upgradeButtonPrefab.name}");
                continue;
            }

            // テキストコンポーネントを取得（子オブジェクトから）
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
            Button[] buttons = buttonObj.GetComponentsInChildren<Button>();

            TextMeshProUGUI nameText = null;
            TextMeshProUGUI descText = null;
            TextMeshProUGUI levelText = null;
            Button rerollButton = null;

            // テキストコンポーネントを名前で識別
            foreach (var text in texts)
            {
                if (text.name.Contains("Name"))
                    nameText = text;
                else if (text.name.Contains("Description"))
                    descText = text;
                else if (text.name.Contains("Level"))
                    levelText = text;
            }

            // リロールボタンを識別（メインボタン以外）
            foreach (var btn in buttons)
            {
                if (btn != button && btn.name.Contains("Reroll"))
                {
                    rerollButton = btn;
                    break;
                }
            }

            // リストに追加
            dynamicUpgradeButtons.Add(button);
            dynamicUpgradeNames.Add(nameText);
            dynamicUpgradeDescriptions.Add(descText);
            dynamicUpgradeLevels.Add(levelText);
            dynamicRerollButtons.Add(rerollButton);

            // メインボタンのイベントリスナーを設定
            int index = i; // クロージャ対策
            button.onClick.AddListener(() => OnUpgradeSelected(index));

            // 個別リロールボタンのイベントリスナーを設定
            if (rerollButton != null && enableIndividualReroll)
            {
                rerollButton.onClick.AddListener(() => OnIndividualReroll(index));
                rerollButton.gameObject.SetActive(true);
            }
            else if (rerollButton != null)
            {
                rerollButton.gameObject.SetActive(false);
            }

            Debug.Log($"アップグレードボタン {i + 1} を生成しました");
        }
    }

    /// <summary>
    /// 3択選択画面を表示
    /// </summary>
    public void ShowUpgradeSelection(int currentStage, Action onComplete = null)
    {
        ShowUpgradeSelection(currentStage, currentChoiceCount, onComplete);
    }

    /// <summary>
    /// 指定数の選択肢でアップグレード選択画面を表示
    /// </summary>
    public void ShowUpgradeSelection(int currentStage, int choiceCount, Action onComplete = null)
    {
        if (isSelectionActive)
        {
            Debug.LogWarning("既に選択画面が表示中です");
            return;
        }

        // 選択肢数の制限
        choiceCount = Mathf.Clamp(choiceCount, 1, maxUpgradeChoices);
        currentChoiceCount = choiceCount;

        Debug.Log($"アップグレード選択画面を表示: ステージ{currentStage}, 選択肢数{choiceCount}");

        // UpgradeManagerから選択肢を取得
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager.Instanceが見つかりません");
            return;
        }

        // 選択肢数に応じてUpgradeManagerの設定を変更
        int originalChoiceCount = UpgradeManager.Instance.choiceCount;
        UpgradeManager.Instance.choiceCount = choiceCount;

        currentChoices = UpgradeManager.Instance.GenerateUpgradeChoices(currentStage);
        onSelectionComplete = onComplete;

        // UpgradeManagerの設定を元に戻す
        UpgradeManager.Instance.choiceCount = originalChoiceCount;

        // 動的にボタンを生成
        CreateUpgradeButtons(choiceCount);

        // 選択肢をUIに反映
        UpdateChoiceDisplay();

        // UI表示
        ShowSelectionPanel();

        isSelectionActive = true;

        // 🆕 クリック保護開始
        StartClickProtection();
    }

    private void UpdateChoiceDisplay()
    {
        for (int i = 0; i < dynamicUpgradeButtons.Count; i++)
        {
            if (i < currentChoices.Count && dynamicUpgradeButtons[i] != null)
            {
                var upgrade = currentChoices[i];
                var button = dynamicUpgradeButtons[i];

                // ボタンを有効化
                button.gameObject.SetActive(true);
                button.interactable = true;

                // テキスト設定
                if (dynamicUpgradeNames[i] != null)
                    dynamicUpgradeNames[i].text = upgrade.upgradeName;

                if (dynamicUpgradeDescriptions[i] != null)
                    dynamicUpgradeDescriptions[i].text = upgrade.GetDescription();

                if (dynamicUpgradeLevels[i] != null)
                {
                    string levelText = upgrade.currentLevel == 0
                        ? "新規"
                        : $"Lv.{upgrade.currentLevel} → Lv.{upgrade.currentLevel + 1}";
                    dynamicUpgradeLevels[i].text = levelText;
                }

                Debug.Log($"選択肢{i + 1}: {upgrade.upgradeName}");
            }
            else
            {
                // 選択肢が足りない場合は非表示
                if (dynamicUpgradeButtons[i] != null)
                    dynamicUpgradeButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void ShowSelectionPanel()
    {
        // 背景オーバーレイ表示
        if (overlayBackground != null)
            overlayBackground.SetActive(true);

        // パネル表示
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
            StartCoroutine(PanelShowAnimation());
        }
    }

    private void HideSelectionPanel()
    {
        StartCoroutine(PanelHideAnimation());
    }

    /// <summary>
    /// クリック保護システム開始
    /// </summary>
    private void StartClickProtection()
    {
        isClickProtectionActive = true;

        // 全てのアップグレードボタンを一時無効化
        SetUpgradeButtonsInteractable(false);

        // スキップボタンも無効化
        if (skipButton != null)
            skipButton.interactable = false;

        Debug.Log($"クリック保護開始: {clickProtectionTime}秒間");

        // シンプルなタイマーのみ
        StartCoroutine(ClickProtectionTimer());
    }

    /// <summary>
    /// クリック保護タイマー
    /// </summary>
    private System.Collections.IEnumerator ClickProtectionTimer()
    {
        yield return new WaitForSeconds(clickProtectionTime);
        EndClickProtection();
    }

    /// <summary>
    /// クリック保護終了
    /// </summary>
    private void EndClickProtection()
    {
        isClickProtectionActive = false;

        // ボタンを有効化
        SetUpgradeButtonsInteractable(true);

        if (skipButton != null)
            skipButton.interactable = true;

        Debug.Log("クリック保護終了 - 選択可能になりました");
    }

    /// <summary>
    /// アップグレードボタンの有効/無効切り替え
    /// </summary>
    private void SetUpgradeButtonsInteractable(bool interactable)
    {
        foreach (var button in dynamicUpgradeButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }

        foreach (var rerollBtn in dynamicRerollButtons)
        {
            if (rerollBtn != null)
                rerollBtn.interactable = interactable;
        }
    }
    private System.Collections.IEnumerator PanelShowAnimation()
    {
        if (selectionPanel == null) yield break;

        // 小さく開始して大きくする
        selectionPanel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // イージング効果
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            selectionPanel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easedT);

            yield return null;
        }

        selectionPanel.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// パネル非表示アニメーション
    /// </summary>
    private System.Collections.IEnumerator PanelHideAnimation()
    {
        if (selectionPanel == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = selectionPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            selectionPanel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        // 完全に非表示
        selectionPanel.SetActive(false);
        if (overlayBackground != null)
            overlayBackground.SetActive(false);
    }

    /// <summary>
    /// アップグレード選択時の処理
    /// </summary>
    /// <summary>
    /// アップグレード選択時の処理
    /// </summary>
    private void OnUpgradeSelected(int upgradeIndex)
    {
        // 🛡️ クリック保護中は無効
        if (isClickProtectionActive)
        {
            Debug.Log("クリック保護中のためアップグレード選択を無視しました");
            return;
        }

        if (!isSelectionActive || upgradeIndex < 0 || upgradeIndex >= currentChoices.Count)
        {
            Debug.LogWarning($"無効なアップグレード選択: {upgradeIndex}");
            return;
        }

        // 選択されたアップグレードを取得
        var selectedUpgrade = currentChoices[upgradeIndex];
        Debug.Log("アップグレード選択: " + selectedUpgrade.upgradeName);

        // アップグレード適用
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);
        }

        // 重複選択防止
        DisableAllButtons();

       
        // 選択完了処理
        CompleteSelection();
    }

    /// <summary>
    /// スキップ選択時の処理
    /// </summary>
    private void OnSkipSelected()
    {
        // 🛡️ クリック保護中は無効
        if (isClickProtectionActive)
        {
            Debug.Log("クリック保護中のためスキップを無視しました");
            return;
        }

        if (!isSelectionActive)
        {
            Debug.LogWarning("選択画面が非アクティブです");
            return;
        }

        Debug.Log("アップグレード選択をスキップしました");

        // 重複選択防止
        DisableAllButtons();

        // スキップ完了処理
        CompleteSelection();
    }

    /// <summary>
    /// 個別リロール選択時の処理
    /// </summary>
    private void OnIndividualReroll(int index)
    {
        // 🛡️ クリック保護中は無効
        if (isClickProtectionActive)
        {
            Debug.Log("クリック保護中のためリロールを無視しました");
            return;
        }

        if (!isSelectionActive || index < 0 || index >= currentChoices.Count)
        {
            Debug.LogWarning($"無効な個別リロール: {index}");
            return;
        }

        Debug.Log($"選択肢 {index + 1} をリロールします");

        // TODO: アイテム消費チェック（後日実装）
        // if (!CanAffordReroll()) return;

        // UpgradeManagerから新しい選択肢を1つ取得
        if (UpgradeManager.Instance != null)
        {
            // 現在の選択肢を除外して新しい選択肢を生成
            var availableUpgrades = UpgradeManager.Instance.GetAvailableUpgrades(GetCurrentStage());
            availableUpgrades.RemoveAll(upgrade =>
                currentChoices.Any(choice => choice.upgradeType == upgrade.upgradeType));

            if (availableUpgrades.Count > 0)
            {
                // 重み付きランダムで新しい選択肢を選出
                var newChoice = WeightedRandomSelection(availableUpgrades);
                currentChoices[index] = newChoice;

                // UIを更新
                UpdateSingleChoiceDisplay(index);

                Debug.Log($"選択肢 {index + 1} を {newChoice.upgradeName} にリロールしました");
            }
            else
            {
                Debug.LogWarning("リロール可能なアップグレードがありません");
            }
        }
    }

    /// <summary>
    /// 全体リロール選択時の処理
    /// </summary>
    private void OnGlobalRerollSelected()
    {
        Debug.Log("全体リロール機能は後日実装予定です");
        // TODO: 全選択肢をリロール
    }

    /// <summary>
    /// 単一選択肢の表示を更新
    /// </summary>
    private void UpdateSingleChoiceDisplay(int index)
    {
        if (index < 0 || index >= currentChoices.Count || index >= dynamicUpgradeButtons.Count)
            return;

        var upgrade = currentChoices[index];

        // テキスト更新
        if (dynamicUpgradeNames[index] != null)
            dynamicUpgradeNames[index].text = upgrade.upgradeName;

        if (dynamicUpgradeDescriptions[index] != null)
            dynamicUpgradeDescriptions[index].text = upgrade.GetDescription();

        if (dynamicUpgradeLevels[index] != null)
        {
            string levelText = upgrade.currentLevel == 0
                ? "新規"
                : $"Lv.{upgrade.currentLevel} → Lv.{upgrade.currentLevel + 1}";
            dynamicUpgradeLevels[index].text = levelText;
        }

        // リロールアニメーション（簡易版）
        if (dynamicUpgradeButtons[index] != null)
        {
            StartCoroutine(RerollAnimation(dynamicUpgradeButtons[index].gameObject));
        }
    }

    /// <summary>
    /// リロールアニメーション
    /// </summary>
    private System.Collections.IEnumerator RerollAnimation(GameObject target)
    {
        Vector3 originalScale = target.transform.localScale;

        // 縮小
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.8f, t);
            yield return null;
        }

        // 拡大
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.transform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, t);
            yield return null;
        }

        target.transform.localScale = originalScale;
    }

    /// <summary>
    /// 重み付きランダム選択（UpgradeManagerから移植）
    /// </summary>
    private UpgradeData WeightedRandomSelection(List<UpgradeData> upgrades)
    {
        float totalWeight = upgrades.Sum(u => u.appearanceWeight);
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
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

    /// <summary>
    /// 現在のステージ取得（GameManagerから）
    /// </summary>
    private int GetCurrentStage()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null ? gameManager.GetCurrentStage() : 1;
    }

    /// <summary>
    /// 全ボタンを無効化（重複選択防止）
    /// </summary>
    private void DisableAllButtons()
    {
        foreach (var button in dynamicUpgradeButtons)
        {
            if (button != null)
                button.interactable = false;
        }

        if (skipButton != null)
            skipButton.interactable = false;

        if (globalRerollButton != null)
            globalRerollButton.interactable = false;
    }

    /// <summary>
    /// 選択完了処理
    /// </summary>
    private void CompleteSelection()
    {
        isSelectionActive = false;

        // 少し待ってからUI非表示
        StartCoroutine(DelayedHide());
    }

    private System.Collections.IEnumerator DelayedHide()
    {
        yield return new WaitForSeconds(0.5f);

        HideSelectionPanel();

        yield return new WaitForSeconds(animationDuration);

        // コールバック実行
        onSelectionComplete?.Invoke();

        // ボタンを再有効化
        ResetButtons();
    }

    /// <summary>
    /// ボタン状態をリセット
    /// </summary>
    private void ResetButtons()
    {
        foreach (var button in dynamicUpgradeButtons)
        {
            if (button != null)
                button.interactable = true;
        }

        if (skipButton != null)
            skipButton.interactable = true;

        if (globalRerollButton != null)
            globalRerollButton.interactable = true;
    }

    /// <summary>
    /// 選択画面が表示中かどうか
    /// </summary>
    public bool IsSelectionActive()
    {
        return isSelectionActive;
    }

    /// <summary>
    /// 🔥 ForceClose()メソッドの強化
    /// </summary>
    public void ForceClose()
    {
        Debug.Log("🧹 UpgradeSelectionUI 強制クリーンアップ開始");

        // 状態フラグリセット
        isSelectionActive = false;
        isClickProtectionActive = false;

        // 全てのコルーチンを停止
        StopAllCoroutines();

        // UIパネル非表示
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
        if (overlayBackground != null)
            overlayBackground.SetActive(false);

        // 🔥 動的生成されたボタンを完全削除
        ClearDynamicButtons();

        // データクリア
        currentChoices.Clear();
        onSelectionComplete = null;

        // ボタン状態リセット
        ResetButtons();

        Debug.Log("🧹 UpgradeSelectionUI 強制クリーンアップ完了");
    }

    /// <summary>
    /// 🔥 安全な動的ボタンクリア
    /// </summary>
    private void SafeClearDynamicButtons()
    {
        try
        {
            Debug.Log($"🧹 動的ボタン安全クリア開始");

            // dynamicUpgradeButtons が存在する場合のみ処理
            if (dynamicUpgradeButtons != null)
            {
                foreach (var button in dynamicUpgradeButtons)
                {
                    if (button != null && button.gameObject != null)
                    {
                        button.onClick.RemoveAllListeners();
                        Destroy(button.gameObject);
                    }
                }
                dynamicUpgradeButtons.Clear();
            }

            // 他のリストもクリア（存在する場合のみ）
            if (dynamicRerollButtons != null)
            {
                foreach (var rerollBtn in dynamicRerollButtons)
                {
                    if (rerollBtn != null && rerollBtn.gameObject != null)
                    {
                        rerollBtn.onClick.RemoveAllListeners();
                        Destroy(rerollBtn.gameObject);
                    }
                }
                dynamicRerollButtons.Clear();
            }

            // その他のリストもクリア
            dynamicUpgradeNames?.Clear();
            dynamicUpgradeDescriptions?.Clear();
            dynamicUpgradeLevels?.Clear();

            // コンテナの子オブジェクトもクリーンアップ
            if (upgradeChoicesContainer != null)
            {
                for (int i = upgradeChoicesContainer.childCount - 1; i >= 0; i--)
                {
                    var child = upgradeChoicesContainer.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            Debug.Log("🧹 動的ボタン安全クリア完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 動的ボタンクリアエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 選択肢数を変更する（デバッグ・管理用）
    /// </summary>
    public void SetChoiceCount(int count)
    {
        currentChoiceCount = Mathf.Clamp(count, 1, maxUpgradeChoices);
        Debug.Log($"選択肢数を {currentChoiceCount} に設定しました");
    }

    /// <summary>
    /// 現在の選択肢数を取得
    /// </summary>
    public int GetCurrentChoiceCount()
    {
        return currentChoiceCount;
    }

    /// <summary>
    /// デバッグ用：現在の選択肢を表示
    /// </summary>
    [ContextMenu("デバッグ: 現在の選択肢表示")]
    private void DebugShowCurrentChoices()
    {
        if (currentChoices == null || currentChoices.Count == 0)
        {
            Debug.Log("現在選択肢はありません");
            return;
        }

        Debug.Log($"=== 現在の選択肢（{currentChoices.Count}個） ===");
        for (int i = 0; i < currentChoices.Count; i++)
        {
            var choice = currentChoices[i];
            Debug.Log($"{i + 1}. {choice.upgradeName} (Lv.{choice.currentLevel})");
            Debug.Log($"   説明: {choice.description}");
            Debug.Log($"   効果: {choice.GetCurrentEffect()}");
        }
    }

    /// <summary>
    /// デバッグ用：選択肢数テスト
    /// </summary>
    [ContextMenu("デバッグ: 5択テスト")]
    private void DebugTest5Choices()
    {
        if (Application.isPlaying)
        {
            ShowUpgradeSelection(1, 5);
        }
    }

    /// <summary>
    /// デバッグ用：2択テスト
    /// </summary>
    [ContextMenu("デバッグ: 2択テスト")]
    private void DebugTest2Choices()
    {
        if (Application.isPlaying)
        {
            ShowUpgradeSelection(1, 2);
        }
    }
}