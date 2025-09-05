using UnityEngine;


[System.Serializable]
public enum UpgradeType
{
    // ��{�n
    ClickPower,        // �N���b�N����
    Factory,           // �W���p�܂�H��i���ŁE�݊����̂��ߎc���j
    HelperFriend,      // ����`���t�����Y
    DonkeyBakery,      // ���o�̃p�����i�V�Łj

    // �m���n
    RainbowJapaman,    // ���F�̃W���p�܂�
    LuckyBeast,        // ���b�L�[�r�[�X�g�o��
    RobaBakery,        // ���o�̃p�����o���i���ŁE�݊����̂��ߎc���j
    FriendsCall,       // �t�����Y�R�[��
    LuckyTail,         // �K�^�̐K��

    // ����n
    MiraclTime,        // �~���N���^�C��
    Satisfaction,      // ������
    ChatSystem,        // �����肵�悤
    Organizer          // �܂Ƃ߂�W
}

[System.Serializable]
public class UpgradeData
{
    [Header("��{���")]
    public UpgradeType upgradeType;
    public string upgradeName;
    public string description;
    public int currentLevel = 0;
    public int maxLevel = 5;

    [Header("���ʒl")]
    public float baseEffect = 1f;
    public float levelMultiplier = 1.5f;
    public bool isActive = false;

    [Header("�o���ݒ�")]
    public int requiredStage = 1;
    public float appearanceWeight = 1f;

    [Header("���ʃ^�C�v")]
    public bool isInstantEffect = false;
    public bool isPassiveEffect = true;
    public float effectDuration = 0f;

    public float GetCurrentEffect()
    {
        if (!isActive) return 0f;
        return baseEffect * Mathf.Pow(levelMultiplier, currentLevel);
    }

    public bool LevelUp()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            return true;
        }
        return false;
    }

    public string GetDescription()
    {
        return description + "\n����Lv." + currentLevel + " (����: " + GetCurrentEffect().ToString("F1") + ")";
    }
}