using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面右端に常時表示されるアップグレード情報サイドパネル
/// 完全修正版: 位置、スクロール、リアルタイム更新すべて対応
/// </summary>
public class UpgradeSidePanelUI : MonoBehaviour
{
    public static UpgradeSidePanelUI Instance { get; private set; }

    [Header("サイドパネル設定")]
    public GameObject sidePanelContainer;
    public Button collapseButton;
    public TMP_Text collapseBtnText;
    public RectTransform panelRect;

    [Header("ヘッダー部分")]
    public TMP_Text titleText;
    public TMP_Text summaryText;

    [Header("フィルター（コンパクト）")]
    public TMP_Dropdown quickFilter;
    public Button sortToggleButton;
    public TMP_Text sortButtonText;

    [Header("スクロールリスト")]
    public ScrollRect upgradeScrollRect;
    public Transform upgradeListContent;
    public GameObject upgradeCompactItemPrefab;

    [Header("アニメーション設定")]
    public float slideAnimationTime = 0.3f;
    public AnimationCurve slideEasing;

    [Header("パネル位置設定")]
    public Vector2 expandedPosition = new Vector2(784f, 0f);
    public Vector2 collapsedPosition = new Vector2(959f, 0f);

    [Header("デバッグ設定")]
    public bool enableDebugLog = true;
    public bool showDebugGizmos = true;
    public Color debugContentColor = Color.cyan;

    [Header("ブーストエフェクト設定")]
    public Image panelOutlineEffect; // Inspector で設定（外枠用の Image）
    public Color boostOutlineColor = Color.yellow;
    public float outlinePulseSpeed = 2f;
    public float outlineGlowIntensity = 2f;


    // 内部状態
    private List<GameObject> upgradeItemObjects = new List<GameObject>();
    private bool isPanelExpanded = true;
    private UpgradeFilterType currentFilter = UpgradeFilterType.All;
    private bool sortByLevel = false;
    private bool isInitialized = false;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.5f; // 0.5秒ごとに更新チェック
    private bool isBoostEffectActive = false;
    private Coroutine outlineEffectCoroutine;

    // フィルター種類（コンパクト版）
    public enum UpgradeFilterType
    {
        All,        // 全て
        Active,     // アクティブのみ
        MaxLevel,   // 最大レベル
        Recent      // 最近取得（後日実装）
    }

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
        InitializeSidePanel();
        SetupEventListeners();

        // UpgradeManagerの初期化を待つ
        StartCoroutine(WaitForUpgradeManagerAndInitialize());
    }

    /// <summary>
    /// 🔥 完全リセットメソッドを追加
    /// </summary>
    public void ForceCompleteReset()
    {
        Debug.Log("🧹 UpgradeSidePanelUI 完全リセット開始");

        // 既存のアイテムを完全削除
        ClearUpgradeItems();

        // 状態リセット
        isInitialized = false;
        lastUpdateTime = 0f;

        // UI表示リセット
        if (summaryText != null)
            summaryText.text = "取得: 0個 | 総Lv: 0";

        // フィルター・ソートリセット
        currentFilter = UpgradeFilterType.All;
        sortByLevel = false;
        if (sortButtonText != null)
            sortButtonText.text = "名前順";
        if (quickFilter != null)
            quickFilter.value = 0;

        Debug.Log("🧹 UpgradeSidePanelUI 完全リセット完了");
    }

    private void Update()
    {
        // 定期的にアップグレード状況をチェック
        if (isInitialized && Time.time - lastUpdateTime > UPDATE_INTERVAL)
        {
            lastUpdateTime = Time.time;
            CheckAndUpdateUpgrades();
        }
    }

    /// <summary>
    /// UpgradeManagerの準備を待って初期化
    /// </summary>
    private System.Collections.IEnumerator WaitForUpgradeManagerAndInitialize()
    {
        // UpgradeManagerが準備できるまで待機
        while (UpgradeManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // さらに少し待って確実に初期化完了
        yield return new WaitForSeconds(0.5f);

        // 初期データ表示
        RefreshUpgradeList();
        isInitialized = true;

        Debug.Log("サイドパネル初期化完了 - UpgradeManager連携開始");
    }

    /// <summary>
    /// アップグレード状況の変化をチェック
    /// </summary>
    private void CheckAndUpdateUpgrades()
    {
        if (UpgradeManager.Instance == null) return;

        var currentUpgrades = GetUpgradeDataFromManager();

        // 現在表示中のアイテム数と実際のデータ数を比較
        if (upgradeItemObjects.Count != currentUpgrades.Count)
        {
            if (enableDebugLog)
                Debug.Log($"アップグレード数変化検出: {upgradeItemObjects.Count} → {currentUpgrades.Count}");

            RefreshUpgradeList();
            return;
        }

        // 個別アイテムのレベル変化をチェック（改善版）
        bool hasLevelChange = false;
        for (int i = 0; i < upgradeItemObjects.Count && i < currentUpgrades.Count; i++)
        {
            var itemComponent = upgradeItemObjects[i].GetComponent<UpgradeCompactItem>();
            if (itemComponent != null)
            {
                var currentData = itemComponent.GetUpgradeData();
                var newData = currentUpgrades.Find(u => u.upgradeType == currentData.upgradeType);

                if (newData != null && currentData.currentLevel != newData.currentLevel)
                {
                    if (enableDebugLog)
                        Debug.Log($"レベル変化検出: {newData.upgradeName} Lv.{currentData.currentLevel} → Lv.{newData.currentLevel}");

                    // 個別アイテムのみ更新
                    itemComponent.SetupUpgradeData(newData);
                    hasLevelChange = true;
                }
            }
        }

        // レベル変化があった場合はサマリーも更新
        if (hasLevelChange)
        {
            UpdateSummaryText(currentUpgrades.Count, currentUpgrades.Sum(u => u.currentLevel));
        }
    }

    private void InitializeSidePanel()
    {
        if (titleText != null)
            titleText.text = "アップグレード";

        if (panelRect != null)
        {
            panelRect.anchoredPosition = expandedPosition;
        }

        SetupQuickFilter();

        if (sortButtonText != null)
            sortButtonText.text = "名前順";

        if (collapseBtnText != null)
            collapseBtnText.text = "◀";

        if (slideEasing == null || slideEasing.keys.Length == 0)
        {
            slideEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        // ScrollViewの初期設定を修正
        FixScrollViewSettings();

        Debug.Log("アップグレードサイドパネル初期化完了");
    }

    /// <summary>
    /// ScrollViewの設定を修正
    /// </summary>
    private void FixScrollViewSettings()
    {
        if (upgradeScrollRect == null) return;

        // スクロール設定 - Elasticに戻す
        upgradeScrollRect.horizontal = false;
        upgradeScrollRect.vertical = true;
        upgradeScrollRect.movementType = ScrollRect.MovementType.Elastic; // 戻す: Elastic
        upgradeScrollRect.elasticity = 0.1f;
        upgradeScrollRect.inertia = true;
        upgradeScrollRect.decelerationRate = 0.135f;
        upgradeScrollRect.scrollSensitivity = 1f;

        // Content設定の修正
        if (upgradeListContent != null)
        {
            var contentRect = upgradeListContent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                // Contentの位置とサイズを修正
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.offsetMin = new Vector2(15f, contentRect.offsetMin.y); // 左マージン
                contentRect.offsetMax = new Vector2(-15f, contentRect.offsetMax.y); // 右マージン

                // VerticalLayoutGroupの設定 - 指定された値に変更
                var layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                // カスタム設定値に変更
                layoutGroup.padding = new RectOffset(-60, 8, 110, 8); // Left: -60, Top: 110
                layoutGroup.spacing = 60f; // Spacing: 60
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                layoutGroup.childControlWidth = false;  // 重要：幅制御しない
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;

                // ContentSizeFitterの設定
                var sizeFitter = contentRect.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                }

                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        Debug.Log("ScrollView設定を修正しました (MovementType: Elastic, Padding: Left=-60 Top=110, Spacing=60)");
    }

    private void SetupQuickFilter()
    {
        if (quickFilter == null) return;

        quickFilter.ClearOptions();
        var options = new List<string> { "全て", "アクティブ", "MAX", "最近" };
        quickFilter.AddOptions(options);
    }

    private void SetupEventListeners()
    {
        if (collapseButton != null)
            collapseButton.onClick.AddListener(TogglePanel);

        if (quickFilter != null)
            quickFilter.onValueChanged.AddListener(OnFilterChanged);

        if (sortToggleButton != null)
            sortToggleButton.onClick.AddListener(ToggleSort);
    }

    public void TogglePanel()
    {
        isPanelExpanded = !isPanelExpanded;

        Vector2 targetPosition = isPanelExpanded ? expandedPosition : collapsedPosition;
        StartCoroutine(SlideToPosition(targetPosition));

        if (collapseBtnText != null)
            collapseBtnText.text = isPanelExpanded ? "◀" : "▶";

        Debug.Log($"サイドパネルを{(isPanelExpanded ? "展開" : "折りたたみ")}");
    }

    private System.Collections.IEnumerator SlideToPosition(Vector2 targetPosition)
    {
        if (panelRect == null) yield break;

        Vector2 startPosition = panelRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideAnimationTime;
            float easedT = slideEasing.Evaluate(t);

            panelRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// アップグレードリストを更新
    /// </summary>
    public void RefreshUpgradeList()
    {
        if (enableDebugLog)
            Debug.Log("=== RefreshUpgradeList 開始 ===");

        ClearUpgradeItems();

        var upgradeData = GetUpgradeDataFromManager();
        if (upgradeData == null || upgradeData.Count == 0)
        {
            if (enableDebugLog)
                Debug.Log("アップグレードデータが0件のため処理終了");
            UpdateSummaryText(0, 0);
            return;
        }

        var filteredData = ApplyQuickFilter(upgradeData);
        var sortedData = ApplySort(filteredData);

        CreateCompactUpgradeItems(sortedData);
        UpdateSummaryText(upgradeData.Count, upgradeData.Sum(u => u.currentLevel));

        // スクロール位置をリセットしない（ユーザーの操作を尊重）
        // upgradeScrollRect.verticalNormalizedPosition = 1f; ← 削除

        if (enableDebugLog)
            Debug.Log($"サイドパネル更新完了: {sortedData.Count}個表示");
    }

    private List<UpgradeData> GetUpgradeDataFromManager()
    {
        if (UpgradeManager.Instance == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("UpgradeManager.Instanceが見つかりません");
            return new List<UpgradeData>();
        }

        return UpgradeManager.Instance.GetObtainedUpgrades();
    }

    private List<UpgradeData> ApplyQuickFilter(List<UpgradeData> data)
    {
        switch (currentFilter)
        {
            case UpgradeFilterType.Active:
                return data.Where(u => u.isActive).ToList();

            case UpgradeFilterType.MaxLevel:
                return data.Where(u => u.currentLevel >= u.maxLevel).ToList();

            case UpgradeFilterType.Recent:
                return data.TakeLast(5).ToList();

            default: // All
                return data;
        }
    }

    private List<UpgradeData> ApplySort(List<UpgradeData> data)
    {
        if (sortByLevel)
        {
            return data.OrderByDescending(u => u.currentLevel).ThenBy(u => u.upgradeName).ToList();
        }
        else
        {
            return data.OrderBy(u => u.upgradeName).ToList();
        }
    }

    private void ClearUpgradeItems()
    {
        Debug.Log($"🧹 アップグレードアイテムクリア開始: {upgradeItemObjects.Count}個");

        foreach (var item in upgradeItemObjects)
        {
            if (item != null)
            {
                // コンポーネントのイベントリスナーもクリア
                var buttons = item.GetComponentsInChildren<Button>();
                foreach (var btn in buttons)
                {
                    if (btn != null)
                        btn.onClick.RemoveAllListeners();
                }

                Destroy(item);
            }
        }
        upgradeItemObjects.Clear();

        // 🔥 コンテナの子オブジェクトも確認
        if (upgradeListContent != null)
        {
            for (int i = upgradeListContent.childCount - 1; i >= 0; i--)
            {
                var child = upgradeListContent.GetChild(i);
                if (child != null && !child.name.Contains("Prefab"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Debug.Log("🧹 アップグレードアイテムクリア完了");
    }

    private void CreateCompactUpgradeItems(List<UpgradeData> upgrades)
    {
        if (upgradeListContent == null || upgradeCompactItemPrefab == null)
        {
            Debug.LogError("upgradeListContent または upgradeCompactItemPrefab が設定されていません");
            return;
        }

        foreach (var upgrade in upgrades)
        {
            GameObject itemObject = Instantiate(upgradeCompactItemPrefab, upgradeListContent);
            itemObject.name = $"UpgradeItem_{upgrade.upgradeName}";

            // Prefabのサイズと位置を確実に設定
            var itemRect = itemObject.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.sizeDelta = new Vector2(290f, 42f); // 幅を少し狭く、高さを少し高く

                // アンカーとピボット設定
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(1f, 1f);
                itemRect.pivot = new Vector2(0.5f, 1f);
            }

            // LayoutElementを追加/設定して重なりを防ぐ
            var layoutElement = itemObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = itemObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 290f;
            layoutElement.minHeight = 42f;
            layoutElement.preferredWidth = 290f;
            layoutElement.preferredHeight = 42f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                compactItem.SetupUpgradeData(upgrade);
            }
            else
            {
                SetupCompactItemFallback(itemObject, upgrade);
            }

            upgradeItemObjects.Add(itemObject);
        }

        // レイアウト強制更新 - より確実に
        if (upgradeListContent is RectTransform contentRect)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // さらに次フレームでも更新
            StartCoroutine(DelayedLayoutUpdate(contentRect));
        }
    }

    /// <summary>
    /// 遅延レイアウト更新（重なり問題の完全解決用）
    /// </summary>
    private System.Collections.IEnumerator DelayedLayoutUpdate(RectTransform contentRect)
    {
        yield return null; // 1フレーム待機

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        if (enableDebugLog)
        {
            Debug.Log($"遅延レイアウト更新完了 - Content最終サイズ: {contentRect.sizeDelta}");
        }
    }

    private void SetupCompactItemFallback(GameObject itemObject, UpgradeData upgrade)
    {
        var texts = itemObject.GetComponentsInChildren<TMP_Text>();
        var images = itemObject.GetComponentsInChildren<Image>();

        foreach (var text in texts)
        {
            if (text.name.Contains("Name"))
            {
                string shortName = upgrade.upgradeName.Length > 8
                    ? upgrade.upgradeName.Substring(0, 8) + "..."
                    : upgrade.upgradeName;
                text.text = shortName;
            }
            else if (text.name.Contains("Level"))
            {
                text.text = $"Lv.{upgrade.currentLevel}";
                if (upgrade.currentLevel >= upgrade.maxLevel)
                {
                    text.color = Color.yellow;
                }
            }
            else if (text.name.Contains("Effect"))
            {
                text.text = $"{upgrade.GetCurrentEffect():F0}";
            }
        }

        foreach (var img in images)
        {
            if (img.name.Contains("Icon"))
            {
                img.color = GetUpgradeTypeColor(upgrade.upgradeType);
                if (!upgrade.isActive)
                {
                    var color = img.color;
                    color.a = 0.5f;
                    img.color = color;
                }
            }
        }
    }

    private Color GetUpgradeTypeColor(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.ClickPower: return Color.red;
            case UpgradeType.Factory: return Color.blue;
            case UpgradeType.HelperFriend: return Color.green;
            case UpgradeType.RainbowJapaman: return Color.magenta;
            case UpgradeType.LuckyBeast: return Color.yellow;
            case UpgradeType.RobaBakery: return new Color(1f, 0.6f, 0.2f);
            default: return Color.white;
        }
    }

    private void UpdateSummaryText(int totalCount, int totalLevels)
    {
        if (summaryText != null)
        {
            summaryText.text = $"取得: {totalCount}個 | 総Lv: {totalLevels}";
        }
    }

    private void OnFilterChanged(int filterIndex)
    {
        currentFilter = (UpgradeFilterType)filterIndex;
        RefreshUpgradeList();
    }

    private void ToggleSort()
    {
        sortByLevel = !sortByLevel;

        if (sortButtonText != null)
            sortButtonText.text = sortByLevel ? "Lv順" : "名前順";

        RefreshUpgradeList();
    }

  

    /// <summary>
    /// アップグレードタイプの分類判定
    /// </summary>
    private bool IsAutomationUpgrade(UpgradeType upgradeType)
    {
        return upgradeType == UpgradeType.DonkeyBakery ||
               upgradeType == UpgradeType.RobaBakery ||
               upgradeType == UpgradeType.Factory ||
               upgradeType == UpgradeType.HelperFriend;
    }

    private bool IsLuckUpgrade(UpgradeType upgradeType)
    {
        return upgradeType == UpgradeType.RainbowJapaman ||
               upgradeType == UpgradeType.LuckyBeast ||
               upgradeType == UpgradeType.LuckyTail ||
               upgradeType == UpgradeType.FriendsCall;
    }

    private bool IsSpecialUpgrade(UpgradeType upgradeType)
    {
        return upgradeType == UpgradeType.MiraclTime ||
               upgradeType == UpgradeType.Satisfaction ||
               upgradeType == UpgradeType.ChatSystem ||
               upgradeType == UpgradeType.Organizer;
    }

    private bool IsBasicUpgrade(UpgradeType upgradeType)
    {
        return upgradeType == UpgradeType.ClickPower;
    }





    // UpgradeSidePanelUI.cs の修正が必要な部分のみ

    /// <summary>
    /// アップグレードレベルアップ通知（アイコン光エフェクト版）
    /// </summary>
    public void OnUpgradeLevelUp(UpgradeType upgradeType)
    {
        if (enableDebugLog)
            Debug.Log($"🆙 レベルアップ通知: {upgradeType}");

        // 該当アイテムのアイコン光エフェクトを更新
        bool itemFound = false;
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null && compactItem.GetUpgradeType() == upgradeType)
            {
                if (UpgradeManager.Instance != null)
                {
                    var newData = UpgradeManager.Instance.GetUpgradeByType(upgradeType);
                    if (newData != null)
                    {
                        if (enableDebugLog)
                            Debug.Log($"✅ アイテム更新: {newData.upgradeName} Lv.{newData.currentLevel}");

                        compactItem.SetupUpgradeData(newData);
                        itemFound = true;
                        break;
                    }
                }
            }
        }

        if (!itemFound && enableDebugLog)
        {
            Debug.LogWarning($"⚠️ 該当アイテムが見つかりません: {upgradeType}");
        }

        // サマリーも更新
        var allUpgrades = GetUpgradeDataFromManager();
        if (allUpgrades.Count > 0)
        {
            UpdateSummaryText(allUpgrades.Count, allUpgrades.Sum(u => u.currentLevel));
        }
    }

    /// <summary>
    /// ブーストエフェクトの開始/停止（アイコン光エフェクト版）
    /// </summary>
    public void SetBoostEffect(bool isActive, float boostMultiplier = 1f)
    {
        isBoostEffectActive = isActive;

        if (isActive)
        {
            // タイトル更新
            if (titleText != null)
            {
                titleText.text = $"アップグレード ⚡×{boostMultiplier:F1}";
                titleText.color = boostOutlineColor;
            }

            // 対象アイテムのアイコン光エフェクトを強化
            HighlightBoostedItems();

            if (enableDebugLog)
                Debug.Log($"🎨 ブーストエフェクト開始: x{boostMultiplier:F1}");
        }
        else
        {
            // タイトル復元
            if (titleText != null)
            {
                titleText.text = "アップグレード";
                titleText.color = Color.white;
            }

            // アイテム強調解除
            ClearItemHighlights();

            if (enableDebugLog)
                Debug.Log("🎨 ブーストエフェクト終了");
        }
    }

    /// <summary>
    /// ブースト対象アイテムの光エフェクトを強化
    /// </summary>
    private void HighlightBoostedItems()
    {
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                var upgradeType = compactItem.GetUpgradeType();

                // ブースト対象の場合は光エフェクトを更新
                if (IsBoostedUpgradeType(upgradeType))
                {
                    compactItem.TriggerActivationFlash(); // OutlineEffect → GlowEffect に修正

                    // 追加の視覚効果：背景を少し明るくする
                    var backgroundImage = itemObject.transform.Find("Background")?.GetComponent<Image>();
                    if (backgroundImage != null)
                    {
                        var color = backgroundImage.color;
                        color.a = 0.8f; // 少し明るく
                        backgroundImage.color = color;
                    }
                }
            }
        }
    }

    /// <summary>
    /// アイテムの強調解除
    /// </summary>
    private void ClearItemHighlights()
    {
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                // 通常の光エフェクト判定に戻す
                compactItem.TriggerActivationFlash(); // OutlineEffect → GlowEffect に修正

                // 背景を元に戻す
                var backgroundImage = itemObject.transform.Find("Background")?.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    var color = backgroundImage.color;
                    color.a = 1f; // 元の透明度
                    backgroundImage.color = color;
                }
            }
        }
    }

    /// <summary>
    /// アップグレード取得時のエフェクト通知（アイコン光エフェクト版）
    /// </summary>
    public void OnUpgradeObtained()
    {
        if (enableDebugLog)
            Debug.Log("🎯 アップグレード取得通知を受信 - UI更新開始");

        RefreshUpgradeList();

        // 🎨 新規取得されたアップグレードのアイコンにエフェクト
        TriggerNewUpgradeIconEffects();
    }

    /// <summary>
    /// 新規取得アップグレードのアイコンエフェクトを適用
    /// </summary>
    private void TriggerNewUpgradeIconEffects()
    {
        if (UpgradeManager.Instance == null) return;

        var allUpgrades = GetUpgradeDataFromManager();
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                var upgradeType = compactItem.GetUpgradeType();
                var upgradeData = allUpgrades.Find(u => u.upgradeType == upgradeType);

                if (upgradeData != null && upgradeData.isActive)
                {
                    // 手動取得時のアイコン点滅エフェクト
                    compactItem.TriggerGlowEffect();
                }
            }
        }
    }

    /// <summary>
    /// アップグレード効果の表示状態を一括更新（アイコン光エフェクト版）
    /// </summary>
    public void UpdateUpgradeEffectStates()
    {
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                compactItem.TriggerActivationFlash(); // OutlineEffect → GlowEffect に修正
            }
        }
    }

    /// <summary>
    /// ブースト対象のアップグレードタイプかチェック
    /// </summary>
    private bool IsBoostedUpgradeType(UpgradeType upgradeType)
    {
        return upgradeType == UpgradeType.DonkeyBakery ||
               upgradeType == UpgradeType.RobaBakery ||
               upgradeType == UpgradeType.Factory ||
               upgradeType == UpgradeType.HelperFriend;
    }


    /// <summary>
    /// アイテムアクティベーションエフェクトのコルーチン
    /// </summary>
    private IEnumerator ItemActivationEffect(Image iconImage)
    {
        // 元の色と大きさを保存
        Color originalColor = iconImage.color;
        Vector3 originalScale = iconImage.transform.localScale;

        // エフェクト用の値
        Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // 薄い黄色
        Vector3 enlargedScale = originalScale * 1.1f;

        float duration = 0.3f;
        float elapsed = 0f;

        // 光らせて拡大
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);

            iconImage.color = Color.Lerp(originalColor, highlightColor, progress);
            iconImage.transform.localScale = Vector3.Lerp(originalScale, enlargedScale, progress);

            yield return null;
        }

        elapsed = 0f;

        // 元に戻す
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);

            iconImage.color = Color.Lerp(highlightColor, originalColor, progress);
            iconImage.transform.localScale = Vector3.Lerp(enlargedScale, originalScale, progress);

            yield return null;
        }

        // 確実に元の状態に戻す
        iconImage.color = originalColor;
        iconImage.transform.localScale = originalScale;
    }

    /// <summary>
    /// 複数アイテムの同時エフェクト（工場など）
    /// </summary>
    public void TriggerMultipleItemEffect(UpgradeType[] upgradeTypes)
    {
        foreach (var upgradeType in upgradeTypes)
        {
            StartCoroutine(DelayedItemEffect(upgradeType, Random.Range(0f, 0.2f)));
        }
    }

    /// <summary>
    /// 遅延付きアイテムエフェクト
    /// </summary>
    private IEnumerator DelayedItemEffect(UpgradeType upgradeType, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerItemActivationEffect(upgradeType);
    }

    /// <summary>
    /// アップグレードアイテムのアクティベーションエフェクト
    /// </summary>
    /// <param name="upgradeType">エフェクトを表示するアップグレード種類</param>
    public void TriggerItemActivationEffect(UpgradeType upgradeType)
    {
        // 対象のアップグレードアイテムを探す
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null && compactItem.GetUpgradeType() == upgradeType)
            {
                // エフェクト実行
                var iconImage = GetItemIconImage(itemObject);
                if (iconImage != null)
                {
                    StartCoroutine(ItemActivationEffect(iconImage));
                    if (enableDebugLog)
                        Debug.Log($"🎨 アクティベーションエフェクト実行: {upgradeType}");
                }
                return;
            }
        }

        if (enableDebugLog)
            Debug.LogWarning($"⚠️ エフェクト対象が見つかりません: {upgradeType}");
    }

    /// <summary>
    /// アイテムからアイコンImageを取得
    /// </summary>
    private Image GetItemIconImage(GameObject itemObject)
    {
        // まずUpgradeCompactItemからアイコンを取得を試みる
        var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
        if (compactItem != null)
        {
            // UpgradeCompactItemにGetIconImage()メソッドがある場合
            var iconMethod = compactItem.GetType().GetMethod("GetIconImage");
            if (iconMethod != null)
            {
                return iconMethod.Invoke(compactItem, null) as Image;
            }
        }

        // フォールバック: 子オブジェクトからIconを探す
        var images = itemObject.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.name.Contains("Icon") || img.name.Contains("icon"))
            {
                return img;
            }
        }

        // 最後の手段: 最初のImageを使用
        return images.Length > 0 ? images[0] : null;
    }

    /// <summary>
    /// アップグレードタイプに応じたエフェクト種類を決定
    /// </summary>
    private UpgradeEffectType GetUpgradeEffectType(UpgradeType upgradeType)
    {
        if (upgradeType == UpgradeType.Organizer)
            return UpgradeEffectType.PermanentGlow; // 永続光エフェクト

        if (IsAutomationUpgrade(upgradeType))
            return UpgradeEffectType.BoostHighlight; // ブースト中の背景強調

        if (IsLuckUpgrade(upgradeType))
            return UpgradeEffectType.SpecialEffect; // 特別エフェクト

        if (IsSpecialUpgrade(upgradeType))
            return UpgradeEffectType.SpecialEffect; // 特別エフェクト

        return UpgradeEffectType.BasicGlow; // 基本光エフェクト
    }

    /// <summary>
    /// エフェクト種類の列挙
    /// </summary>
    private enum UpgradeEffectType
    {
        BasicGlow,        // 基本的な光エフェクト
        BoostHighlight,   // ブースト中の背景強調
        PermanentGlow,    // 永続的な光エフェクト
        SpecialEffect     // 特別エフェクト
    }

    public void ForceCollapse()
    {
        if (isPanelExpanded)
        {
            TogglePanel();
        }
    }

    public bool IsExpanded()
    {
        return isPanelExpanded;
    }

    public void HighlightUpgrade(UpgradeType upgradeType)
    {
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null && compactItem.GetUpgradeType() == upgradeType)
            {
                compactItem.Highlight(2f);
                break;
            }
        }
    }
    // UpgradeSidePanelUI.cs に以下のメソッドを追加

    /// <summary>
    /// まとめる係専用のブーストエフェクト制御
    /// </summary>
    /// <param name="isBoostActive">ブーストがアクティブかどうか</param>
    public void SetOrganizerBoostEffect(bool isBoostActive)
    {
        // まとめる係のアイテムを探して点滅制御
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null && compactItem.GetUpgradeType() == UpgradeType.Organizer)
            {
                // まとめる係アイテムの点滅制御
                compactItem.HandleUpgradeTypeEffect(UpgradeType.Organizer, isBoostActive);

                if (enableDebugLog)
                {
                    Debug.Log($"🏢 まとめる係アイコンエフェクト: {(isBoostActive ? "点滅開始" : "点滅停止")}");
                }
                break;
            }
        }
    }

    /// <summary>
    /// デバッグ用：手動更新
    /// </summary>
    [ContextMenu("デバッグ: 手動更新")]
    private void DebugManualRefresh()
    {
        if (Application.isPlaying)
        {
            RefreshUpgradeList();
        }
    }

    /// <summary>
    /// デバッグ用：ScrollView設定を強制修正
    /// </summary>
    [ContextMenu("デバッグ: ScrollView強制修正")]
    private void DebugFixScrollView()
    {
        if (Application.isPlaying)
        {
            FixScrollViewSettings();
            RefreshUpgradeList();
        }
    }

    /// <summary>
    /// デバッグ用：スペーシング調整テスト
    /// </summary>
    [ContextMenu("デバッグ: スペーシング調整")]
    private void DebugAdjustSpacing()
    {
        if (!Application.isPlaying || upgradeListContent == null) return;

        var layoutGroup = upgradeListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            // スペーシングを段階的に増やしてテスト
            layoutGroup.spacing += 2f;
            Debug.Log($"スペーシングを調整: {layoutGroup.spacing}px");

            // レイアウト更新
            LayoutRebuilder.ForceRebuildLayoutImmediate(upgradeListContent as RectTransform);
        }
    }

    /// <summary>
    /// デバッグ用：アイテム位置詳細表示
    /// </summary>
    [ContextMenu("デバッグ: アイテム位置詳細")]
    private void DebugShowItemPositions()
    {
        if (!Application.isPlaying) return;

        Debug.Log("=== アイテム位置詳細 ===");
        for (int i = 0; i < upgradeItemObjects.Count; i++)
        {
            var item = upgradeItemObjects[i];
            if (item == null) continue;

            var rect = item.GetComponent<RectTransform>();
            var layoutElement = item.GetComponent<LayoutElement>();

            Debug.Log($"アイテム[{i}]: {item.name}");
            Debug.Log($"  位置: {rect.anchoredPosition}");
            Debug.Log($"  サイズ: {rect.sizeDelta}");
            Debug.Log($"  実際のRect: {rect.rect}");

            if (layoutElement != null)
            {
                Debug.Log($"  LayoutElement: Min({layoutElement.minWidth}x{layoutElement.minHeight}) Pref({layoutElement.preferredWidth}x{layoutElement.preferredHeight})");
            }

            // 他のアイテムとの重なりチェック
            for (int j = i + 1; j < upgradeItemObjects.Count; j++)
            {
                var otherItem = upgradeItemObjects[j];
                if (otherItem == null) continue;

                var otherRect = otherItem.GetComponent<RectTransform>();
                bool isOverlapping = rect.rect.Overlaps(otherRect.rect);

                if (isOverlapping)
                {
                    Debug.LogWarning($"  ⚠️ アイテム[{j}]と重なっています: {otherItem.name}");
                }
            }
        }
    }

    /// <summary>
    /// デバッグ用：レベルアップ通知をテスト
    /// </summary>
    [ContextMenu("デバッグ: レベルアップ通知テスト")]
    private void DebugTestLevelUpNotification()
    {
        if (!Application.isPlaying || upgradeItemObjects.Count == 0) return;

        // 最初のアイテムでテスト
        var firstItem = upgradeItemObjects[0].GetComponent<UpgradeCompactItem>();
        if (firstItem != null)
        {
            var upgradeType = firstItem.GetUpgradeType();
            Debug.Log($"🧪 レベルアップ通知テスト開始: {upgradeType}");
            OnUpgradeLevelUp(upgradeType);
        }
    }

    /// <summary>
    /// デバッグ用：現在のアップグレード状況表示
    /// </summary>
    [ContextMenu("デバッグ: アップグレード状況表示")]
    private void DebugShowUpgradeStatus()
    {
        if (!Application.isPlaying) return;

        Debug.Log("=== 現在のアップグレード状況 ===");
        var upgrades = GetUpgradeDataFromManager();

        foreach (var upgrade in upgrades)
        {
            Debug.Log($"📊 {upgrade.upgradeName}: Lv.{upgrade.currentLevel}/{upgrade.maxLevel} (効果: {upgrade.GetCurrentEffect():F1})");
        }

        Debug.Log($"📋 表示中アイテム数: {upgradeItemObjects.Count}");
        Debug.Log($"📋 実際のデータ数: {upgrades.Count}");
    }

    /// <summary>
    /// デバッグ用：強制UI同期
    /// </summary>
    [ContextMenu("デバッグ: 強制UI同期")]
    private void DebugForceSyncUI()
    {
        if (!Application.isPlaying) return;

        Debug.Log("🔄 強制UI同期開始");

        // 現在のデータを強制取得
        var currentUpgrades = GetUpgradeDataFromManager();

        // 各アイテムを強制更新
        foreach (var itemObject in upgradeItemObjects)
        {
            var compactItem = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactItem != null)
            {
                var upgradeType = compactItem.GetUpgradeType();
                var newData = currentUpgrades.Find(u => u.upgradeType == upgradeType);

                if (newData != null)
                {
                    Debug.Log($"🔄 強制更新: {newData.upgradeName} Lv.{newData.currentLevel}");
                    compactItem.SetupUpgradeData(newData);
                }
            }
        }

        // サマリー更新
        UpdateSummaryText(currentUpgrades.Count, currentUpgrades.Sum(u => u.currentLevel));

        Debug.Log("✅ 強制UI同期完了");
    }
}