using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeChoiceButton : MonoBehaviour
{
    [Header("UI�v�f")]
    public Button button;
    public TMP_Text upgradeNameText;
    public TMP_Text descriptionText;
    public TMP_Text levelText;
    public TMP_Text effectText;
    public Image iconImage;
    public GameObject newBadge; // �V�K�A�b�v�O���[�h�\���p

    [Header("�A�j���[�V����")]
    public GameObject highlightEffect;
    public CanvasGroup canvasGroup;

    private UpgradeData upgradeData;
    private int choiceIndex;
    private System.Action<int> onSelected;

    /// <summary>
    /// �{�^����������
    /// </summary>
    public void Initialize(int index, System.Action<int> onSelectCallback)
    {
        choiceIndex = index;
        onSelected = onSelectCallback;

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // ������Ԑݒ�
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
    /// �A�b�v�O���[�h�f�[�^��ݒ肵��UI���X�V
    /// </summary>
    public void SetUpgradeData(UpgradeData data)
    {
        upgradeData = data;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (upgradeData == null) return;

        // �A�b�v�O���[�h��
        if (upgradeNameText != null)
        {
            upgradeNameText.text = upgradeData.upgradeName;
        }

        // ������
        if (descriptionText != null)
        {
            descriptionText.text = upgradeData.description;
        }

        // ���x�����
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
                levelText.text = "Lv." + upgradeData.currentLevel + " �� " + (upgradeData.currentLevel + 1);
                if (newBadge != null)
                {
                    newBadge.SetActive(false);
                }
            }
        }

        // ���ʒl
        if (effectText != null)
        {
            float currentEffect = upgradeData.GetCurrentEffect();
            float nextEffect = upgradeData.baseEffect * Mathf.Pow(upgradeData.levelMultiplier, upgradeData.currentLevel + 1);

            if (upgradeData.currentLevel == 0)
            {
                effectText.text = "����: " + nextEffect.ToString("F1");
            }
            else
            {
                effectText.text = currentEffect.ToString("F1") + " �� " + nextEffect.ToString("F1");
            }
        }

        // �A�C�R���i��������j
        if (iconImage != null)
        {
            // �A�b�v�O���[�h�^�C�v�ɉ������A�C�R����ݒ�
            SetUpgradeIcon(upgradeData.upgradeType);
        }
    }

    private void SetUpgradeIcon(UpgradeType upgradeType)
    {
        // ����A�A�b�v�O���[�h�^�C�v���Ƃ̃A�C�R����ݒ�
        // ���݂̓v���[�X�z���_�[
        if (iconImage != null)
        {
            // �f�t�H���g�̐F�ŋ��
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

        Debug.Log("�A�b�v�O���[�h�{�^���N���b�N: " + upgradeData.upgradeName);

        // �N���b�N�A�j���[�V����
        StartCoroutine(ClickAnimation());

        // �I���R�[���o�b�N���s
        onSelected?.Invoke(choiceIndex);
    }

    private System.Collections.IEnumerator ClickAnimation()
    {
        // �n�C���C�g���ʕ\��
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }

        // �X�P�[���A�j���[�V����
        Vector3 originalScale = transform.localScale;

        // �k��
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.95f, t);
            yield return null;
        }

        // �g��
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale * 0.95f, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        // �n�C���C�g���ʔ�\��
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    /// <summary>
    /// �{�^���̗L��/������ݒ�
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
    /// �z�o�[�G�t�F�N�g�i�����I�Ƀ}�E�X�Ή����j
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