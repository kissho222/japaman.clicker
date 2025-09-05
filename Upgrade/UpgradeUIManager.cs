using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("アップグレード選択UI")]
    public GameObject upgradeSelectionPanel;
    public TMP_Text selectionTitleText;

    [Header("3つの選択肢ボタン")]
    public UpgradeChoiceButton[] choiceButtons = new UpgradeChoiceButton[3];

    [Header("スキップボタン")]
    public Button skipButton;
    public TMP_Text skipButtonText;

    [Header("リロールボタン")]
    public Button rerollButton;
    public TMP_Text rerollButtonText;
    public TMP_Text rerollCostText; // 消費アイテム数表示

    [Header("背景とアニメーション")]
    public GameObject backgroundOverlay;
    public CanvasGroup canvasGroup;

    // 現在の選択肢データ
    private List<UpgradeData> currentChoices;
    private System.Action onSelectionComplete;
    private int currentStage; // リロール用にステージ情報を保持

    // リロール回数
    private int rerollCount = 0;
    private const int MAX_REROLLS = 3; // 1回の選択で最大3回まで

    // 状態管理
    private bool isSelecting = false;

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
    }

    private void InitializeUI()
    {
        // 初期状態は非表示
        if (upgradeSelectionPanel != null)
        {
            upgradeSelectionPanel.SetActive(false);
        }

        // スキップボタンの設定
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            if (skipButtonText != null)
            {
                skipButtonText.text = "スキップ";
            }
        }

        // リロールボタンの設定
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClicked);
            if (rerollButtonText != null)
            {
                rerollButtonText.text = "リロール";
            }
        }

        // 選択肢ボタンの初期化
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                int index = i; // クロージャー対策
                choiceButtons[i].Initialize(index, OnUpgradeSelected);
            }
        }

        Debug.Log("UpgradeUIManager初期化完了");
    }

    /// <summary>
    /// アップグレード選択画面を表示
    /// </summary>
    public void ShowUpgradeSelection(List<UpgradeData> choices, int stage, System.Action onComplete)
    {
        if (isSelecting)
        {
            Debug.LogWarning("既にアップグレード選択中です");
            return;
        }

        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("選択肢がありません");
            onComplete?.Invoke();
            return;
        }

        currentChoices = choices;
        currentStage = stage;
        onSelectionComplete = onComplete;
        isSelecting = true;
        rerollCount = 0; // リロール回数リセット

        // タイトルテキスト設定
        if (selectionTitleText != null)
        {
            selectionTitleText.text = "アップグレードを選択してね！";
        }

        // 選択肢ボタンの設定
        SetupChoiceButtons();

        // リロールボタンの状態更新
        UpdateRerollButton();

        // UI表示
        ShowPanel();

        Debug.Log("アップグレード選択画面を表示: " + choices.Count + "個の選択肢");
    }

    private void SetupChoiceButtons()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                if (i < currentChoices.Count)
                {
                    // 選択肢があるボタンを設定
                    choiceButtons[i].SetUpgradeData(currentChoices[i]);
                    choiceButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    // 選択肢がないボタンは非表示
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void ShowPanel()
    {
        if (upgradeSelectionPanel != null)
        {
            upgradeSelectionPanel.SetActive(true);
            StartCoroutine(PanelFadeInAnimation());
        }
    }

    private void HidePanel()
    {
        if (upgradeSelectionPanel != null)
        {
            StartCoroutine(PanelFadeOutAnimation());
        }
    }

    private System.Collections.IEnumerator PanelFadeInAnimation()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }

    private System.Collections.IEnumerator PanelFadeOutAnimation()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.interactable = false;

        float duration = 0.2f;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (upgradeSelectionPanel != null)
        {
            upgradeSelectionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// アップグレードが選択された時の処理
    /// </summary>
    private void OnUpgradeSelected(int choiceIndex)
    {
        if (!isSelecting || currentChoices == null || choiceIndex >= currentChoices.Count)
        {
            Debug.LogWarning("不正なアップグレード選択: " + choiceIndex);
            return;
        }

        var selectedUpgrade = currentChoices[choiceIndex];
        Debug.Log("アップグレード選択: " + selectedUpgrade.upgradeName);

        // アップグレードを適用
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);
        }

        // 選択完了処理
        CompleteSelection();
    }

    /// <summary>
    /// スキップボタンが押された時の処理
    /// </summary>
    private void OnSkipButtonClicked()
    {
        Debug.Log("アップグレード選択をスキップ");
        CompleteSelection();
    }

    /// <summary>
    /// リロールボタンが押された時の処理
    /// </summary>
    private void OnRerollButtonClicked()
    {
        if (!CanReroll())
        {
            Debug.LogWarning("リロールできません");
            return;
        }

        Debug.Log("アップグレード選択肢をリロール: " + (rerollCount + 1) + "回目");

        // アイテム消費処理（将来実装）
        ConsumeRerollItem();

        // 新しい選択肢を生成
        GenerateNewChoices();

        rerollCount++;
        UpdateRerollButton();
    }

    /// <summary>
    /// リロール可能かチェック
    /// </summary>
    private bool CanReroll()
    {
        // リロール回数制限チェック
        if (rerollCount >= MAX_REROLLS)
        {
            return false;
        }

        // アイテム所持数チェック（将来実装）
        return HasRerollItem();
    }

    /// <summary>
    /// リロールアイテムを所持しているかチェック（将来実装）
    /// </summary>
    private bool HasRerollItem()
    {
        // 現在は常にtrue（後でアイテムシステムと連携）
        return true;
    }

    /// <summary>
    /// リロールアイテムを消費（将来実装）
    /// </summary>
    private void ConsumeRerollItem()
    {
        // 将来のアイテムシステムと連携
        Debug.Log("リロールアイテムを消費（仮実装）");

        // 将来の実装例:
        // ItemManager.Instance?.ConsumeItem(ItemType.RerollTicket, 1);
    }

    /// <summary>
    /// 新しい選択肢を生成
    /// </summary>
    private void GenerateNewChoices()
    {
        if (UpgradeManager.Instance != null)
        {
            var newChoices = UpgradeManager.Instance.GenerateUpgradeChoices(currentStage);
            if (newChoices != null && newChoices.Count > 0)
            {
                currentChoices = newChoices;
                SetupChoiceButtons();

                // リロールアニメーション（簡易版）
                StartCoroutine(RerollAnimation());
            }
            else
            {
                Debug.LogWarning("新しい選択肢の生成に失敗");
            }
        }
    }

    /// <summary>
    /// リロールボタンの状態を更新
    /// </summary>
    private void UpdateRerollButton()
    {
        if (rerollButton != null)
        {
            bool canReroll = CanReroll();
            rerollButton.interactable = canReroll;

            if (rerollButtonText != null)
            {
                if (rerollCount >= MAX_REROLLS)
                {
                    rerollButtonText.text = "回数制限";
                }
                else if (!HasRerollItem())
                {
                    rerollButtonText.text = "アイテム不足";
                }
                else
                {
                    rerollButtonText.text = "リロール (" + (MAX_REROLLS - rerollCount) + "回)";
                }
            }

            if (rerollCostText != null)
            {
                if (canReroll)
                {
                    rerollCostText.text = "リロール券 x1";
                }
                else
                {
                    rerollCostText.text = "";
                }
            }
        }
    }

    /// <summary>
    /// リロール時のアニメーション
    /// </summary>
    private System.Collections.IEnumerator RerollAnimation()
    {
        // 選択肢ボタンを一時非表示にするアニメーション
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null && choiceButtons[i].gameObject.activeSelf)
            {
                StartCoroutine(ButtonScaleOut(choiceButtons[i].gameObject));
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 新しい選択肢を表示するアニメーション
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null && choiceButtons[i].gameObject.activeSelf)
            {
                StartCoroutine(ButtonScaleIn(choiceButtons[i].gameObject));
                yield return new WaitForSeconds(0.1f); // 少しずつ表示
            }
        }
    }

    /// <summary>
    /// ボタンの縮小アニメーション
    /// </summary>
    private System.Collections.IEnumerator ButtonScaleOut(GameObject button)
    {
        Vector3 originalScale = button.transform.localScale;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            button.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        button.transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// ボタンの拡大アニメーション
    /// </summary>
    private System.Collections.IEnumerator ButtonScaleIn(GameObject button)
    {
        Vector3 targetScale = Vector3.one;
        float duration = 0.2f;
        float elapsed = 0f;

        button.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // イージングを適用（バウンス効果）
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            button.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easedT);
            yield return null;
        }

        button.transform.localScale = targetScale;
    }

    /// <summary>
    /// 🔥 選択完了処理（SaveUIManagerへの連携を追加）
    /// </summary>
    private void CompleteSelection()
    {
        isSelecting = false;

        // UI非表示
        HidePanel();

  

        // 元の完了コールバック実行
        onSelectionComplete?.Invoke();

        // データクリア
        currentChoices = null;
        onSelectionComplete = null;
    }

    /// <summary>
    /// 現在選択中かどうか
    /// </summary>
    public bool IsSelecting()
    {
        return isSelecting;
    }

 
}