using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 取得済みアップグレード情報を表示するUIシステム（簡易版）
/// UpgradeInfoItemへの参照を削除し、基本的な表示のみ対応
/// </summary>
public class UpgradeInfoUI : MonoBehaviour
{
    public static UpgradeInfoUI Instance { get; private set; }

    [Header("メインUI")]
    public GameObject infoPanel;
    public Button toggleButton;
    public TMP_Text toggleButtonText;
    public TMP_Text titleText;
    public Button closeButton;

    [Header("スクロールビュー")]
    public ScrollRect scrollRect;
    public Transform contentContainer;
    public GameObject upgradeItemPrefab;

    [Header("統計情報")]
    public GameObject statsContainer;
    public TMP_Text totalUpgradesText;
    public TMP_Text totalLevelsText;
    public TMP_Text totalEffectPowerText;

    [Header("設定")]
    public float animationDuration = 0.3f;

    // 内部データ
    private List<GameObject> upgradeItemObjects = new List<GameObject>();
    private bool isPanelVisible = false;

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

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void InitializeUI()
    {
        if (titleText != null)
            titleText.text = "取得済みアップグレード";

        if (toggleButtonText != null)
            toggleButtonText.text = "📊 アップグレード";

        ClearUpgradeItems();
        Debug.Log("UpgradeInfoUI 初期化完了");
    }

    private void SetupEventListeners()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
    }

    public void TogglePanel()
    {
        if (isPanelVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    public void ShowPanel()
    {
        if (isPanelVisible) return;

        isPanelVisible = true;

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            StartCoroutine(PanelShowAnimation());
        }

        RefreshDisplay();
    }

    public void HidePanel()
    {
        if (!isPanelVisible) return;

        isPanelVisible = false;
        StartCoroutine(PanelHideAnimation());
    }

    public void RefreshDisplay()
    {
        if (!isPanelVisible) return;

        ClearUpgradeItems();

        var upgradeData = GetUpgradeDataFromManager();
        if (upgradeData == null || upgradeData.Count == 0)
        {
            UpdateStatistics(new List<UpgradeData>());
            return;
        }

        CreateUpgradeItems(upgradeData);
        UpdateStatistics(upgradeData);
    }

    private List<UpgradeData> GetUpgradeDataFromManager()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogWarning("UpgradeManager.Instanceが見つかりません");
            return new List<UpgradeData>();
        }

        return UpgradeManager.Instance.GetObtainedUpgrades();
    }

    private void ClearUpgradeItems()
    {
        foreach (var item in upgradeItemObjects)
        {
            if (item != null)
                Destroy(item);
        }
        upgradeItemObjects.Clear();
    }

    private void CreateUpgradeItems(List<UpgradeData> upgrades)
    {
        if (contentContainer == null || upgradeItemPrefab == null)
        {
            Debug.LogError("contentContainer または upgradeItemPrefab が設定されていません");
            return;
        }

        foreach (var upgrade in upgrades)
        {
            GameObject itemObject = Instantiate(upgradeItemPrefab, contentContainer);

            // コンパクトアイテムコンポーネントを試す
            var compactComponent = itemObject.GetComponent<UpgradeCompactItem>();
            if (compactComponent != null)
            {
                compactComponent.SetupUpgradeData(upgrade);
            }
            else
            {
                // フォールバック: 基本的なテキスト設定
                SetupBasicItem(itemObject, upgrade);
            }

            upgradeItemObjects.Add(itemObject);
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetupBasicItem(GameObject itemObject, UpgradeData upgrade)
    {
        var texts = itemObject.GetComponentsInChildren<TMP_Text>();

        foreach (var text in texts)
        {
            if (text.name.Contains("Name"))
            {
                text.text = upgrade.upgradeName;
            }
            else if (text.name.Contains("Level"))
            {
                text.text = $"Lv.{upgrade.currentLevel}/{upgrade.maxLevel}";
            }
            else if (text.name.Contains("Effect"))
            {
                text.text = $"効果: {upgrade.GetCurrentEffect():F1}";
            }
            else if (text.name.Contains("Description"))
            {
                text.text = upgrade.description;
            }
        }

        var images = itemObject.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.name.Contains("Icon"))
            {
                img.color = GetUpgradeTypeColor(upgrade.upgradeType);
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
            default: return Color.white;
        }
    }

    private void UpdateStatistics(List<UpgradeData> allUpgrades)
    {
        if (statsContainer == null) return;

        int totalUpgrades = allUpgrades.Count;
        int totalLevels = allUpgrades.Sum(u => u.currentLevel);
        float totalEffectPower = allUpgrades.Sum(u => u.GetCurrentEffect());

        if (totalUpgradesText != null)
            totalUpgradesText.text = $"取得数: {totalUpgrades}";

        if (totalLevelsText != null)
            totalLevelsText.text = $"総Lv: {totalLevels}";

        if (totalEffectPowerText != null)
            totalEffectPowerText.text = $"総効果: {totalEffectPower:F1}";
    }

    private System.Collections.IEnumerator PanelShowAnimation()
    {
        if (infoPanel == null) yield break;

        infoPanel.transform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            infoPanel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        infoPanel.transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator PanelHideAnimation()
    {
        if (infoPanel == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = infoPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            infoPanel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        infoPanel.SetActive(false);
        infoPanel.transform.localScale = Vector3.one;
    }

    public void OnUpgradeObtained()
    {
        if (isPanelVisible)
        {
            RefreshDisplay();
        }
    }

    public bool IsPanelVisible()
    {
        return isPanelVisible;
    }
}