using UnityEngine;


[System.Serializable]
public enum UpgradeType
{
    // 基本系
    ClickPower,        // クリック強化
    Factory,           // ジャパまん工場（旧版・互換性のため残す）
    HelperFriend,      // お手伝いフレンズ
    DonkeyBakery,      // ロバのパン屋（新版）

    // 確率系
    RainbowJapaman,    // 虹色のジャパまん
    LuckyBeast,        // ラッキービースト出現
    RobaBakery,        // ロバのパン屋出現（旧版・互換性のため残す）
    FriendsCall,       // フレンズコール
    LuckyTail,         // 幸運の尻尾

    // 特殊系
    MiraclTime,        // ミラクルタイム
    Satisfaction,      // 満足感
    ChatSystem,        // お喋りしよう
    Organizer          // まとめる係
}

[System.Serializable]
public class UpgradeData
{
    [Header("基本情報")]
    public UpgradeType upgradeType;
    public string upgradeName;
    public string description;
    public int currentLevel = 0;
    public int maxLevel = 5;

    [Header("効果値")]
    public float baseEffect = 1f;
    public float levelMultiplier = 1.5f;
    public bool isActive = false;

    [Header("出現設定")]
    public int requiredStage = 1;
    public float appearanceWeight = 1f;

    [Header("効果タイプ")]
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
        return description + "\n現在Lv." + currentLevel + " (効果: " + GetCurrentEffect().ToString("F1") + ")";
    }
}