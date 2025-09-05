using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeChoiceButton : MonoBehaviour
{
    [Header("UI要素")]
    public Button button;
    public TMP_Text upgradeNameText;
    public TMP_Text descriptionText;
    public TMP_Text levelText;
    public TMP_Text effectText;
    public Image iconImage;
    public GameObject newBadge; // 新規アップグレード表示用

    [Header("アニメーション")]
    public GameObject highlightEffect;
    public CanvasGroup canvasGroup;

    private UpgradeData upgradeData;
    private int choiceIndex;
    private System.Action<int> onSelected;

    /// <summary>
    /// ボタンを初期化
    /// </summary>
    public void Initialize(int index, System.Action<int> onSelectCallback)
    {
        choiceIndex = index;
        onSelected = onSelectCallback;

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // 初期状態設定
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }

        if (newBadge != null)
        {
            newBadge.SetActive(false);
        }
    }

    /// <summary>
    /// アップグレードデータを設定してUIを更新
    /// </summary>
    public void SetUpgradeData(UpgradeData data)
    {
        upgradeData = data;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (upgradeData == null) return;

        // アップグレード名
        if (upgradeNameText != null)
        {
            upgradeNameText.text = upgradeData.upgradeName;
        }

        // 説明文
        if (descriptionText != null)
        {
            descriptionText.text = upgradeData.description;
        }

        // レベル情報
        if (levelText != null)
        {
            if (upgradeData.currentLevel == 0)
            {
                levelText.text = "NEW!";
                if (newBadge != null)
                {
                    newBadge.SetActive(true);
                }
            }
            else
            {
                levelText.text = "Lv." + upgradeData.currentLevel + " → " + (upgradeData.currentLevel + 1);
                if (newBadge != null)
                {
                    newBadge.SetActive(false);
                }
            }
        }

        // 効果値
        if (effectText != null)
        {
            float currentEffect = upgradeData.GetCurrentEffect();
            float nextEffect = upgradeData.baseEffect * Mathf.Pow(upgradeData.levelMultiplier, upgradeData.currentLevel + 1);

            if (upgradeData.currentLevel == 0)
            {
                effectText.text = "効果: " + nextEffect.ToString("F1");
            }
            else
            {
                effectText.text = currentEffect.ToString("F1") + " → " + nextEffect.ToString("F1");
            }
        }

        // アイコン（今後実装）
        if (iconImage != null)
        {
            // アップグレードタイプに応じたアイコンを設定
            SetUpgradeIcon(upgradeData.upgradeType);
        }
    }

    private void SetUpgradeIcon(UpgradeType upgradeType)
    {
        // 今後、アップグレードタイプごとのアイコンを設定
        // 現在はプレースホルダー
        if (iconImage != null)
        {
            // デフォルトの色で区別
            switch (upgradeType)
            {
                case UpgradeType.ClickPower:
                    iconImage.color = Color.red;
                    break;
                case UpgradeType.Factory:
                    iconImage.color = Color.blue;
                    break;
                case UpgradeType.HelperFriend:
                    iconImage.color = Color.green;
                    break;
                case UpgradeType.RainbowJapaman:
                    iconImage.color = Color.magenta;
                    break;
                default:
                    iconImage.color = Color.white;
                    break;
            }
        }
    }

    private void OnButtonClicked()
    {
        if (upgradeData == null) return;

        Debug.Log("アップグレードボタンクリック: " + upgradeData.upgradeName);

        // クリックアニメーション
        StartCoroutine(ClickAnimation());

        // 選択コールバック実行
        onSelected?.Invoke(choiceIndex);
    }

    private System.Collections.IEnumerator ClickAnimation()
    {
        // ハイライト効果表示
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }

        // スケールアニメーション
        Vector3 originalScale = transform.localScale;

        // 縮小
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.95f, t);
            yield return null;
        }

        // 拡大
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale * 0.95f, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        // ハイライト効果非表示
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    /// <summary>
    /// ボタンの有効/無効を設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
    }

    /// <summary>
    /// ホバーエフェクト（将来的にマウス対応時）
    /// </summary>
    public void OnPointerEnter()
    {
        if (highlightEffect != null && button != null && button.interactable)
        {
            highlightEffect.SetActive(true);
        }
    }

    public void OnPointerExit()
    {
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }
}