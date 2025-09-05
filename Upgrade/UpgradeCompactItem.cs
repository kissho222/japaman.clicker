using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// アップグレードサイドパネル用のコンパクトアイテム表示
/// エフェクト修正版：新規取得点滅削除、自動生産系は瞬間光のみ、常時発生系は継続点滅
/// </summary>
public class UpgradeCompactItem : MonoBehaviour
{
    [Header("UI要素")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text effectText;
    public Image backgroundImage;
    public Button itemButton; // アイテム全体のボタン

    [Header("エフェクト設定")]
    public float quickFlashDuration = 0.3f; // アクティベート時の瞬間光
    public float slowPulseSpeed = 1.5f; // 常時発生系のゆっくり点滅速度
    public Color activeGlowColor = new Color(1f, 1f, 0.8f, 1f); // アクティベート時の光色
    public Color constantGlowColor = new Color(0.8f, 1f, 0.8f, 1f); // 常時発生系の光色

    [Header("デバッグ")]
    public bool enableDebugLog = false;

    // 内部状態
    private UpgradeData currentUpgradeData;
    private Color originalIconColor;
    private Color originalBackgroundColor;
    private bool isConstantGlowActive = false;
    private Coroutine constantGlowCoroutine;
    private Coroutine activationFlashCoroutine;

    // 🔥 高速アクティベート制御用
    private float lastActivationTime = 0f;
    private const float MIN_ACTIVATION_INTERVAL = 0.2f; // 最小発動間隔（秒）

    // エフェクトタイプ分類
    private enum EffectType
    {
        None,             // エフェクトなし
        ActivationOnly,   // アクティベート時のみ瞬間光
        ConstantGlow,     // 常時ゆっくり点滅
        ConditionalGlow   // 条件付き点滅（まとめる係用）
    }

    private void Awake()
    {
        // 元の色を保存
        if (iconImage != null)
            originalIconColor = iconImage.color;
        if (backgroundImage != null)
            originalBackgroundColor = backgroundImage.color;
    }

    private void Start()
    {
        // ボタンクリック時の処理（必要に応じて）
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// アップグレードデータを設定してUIを更新
    /// </summary>
    public void SetupUpgradeData(UpgradeData upgradeData)
    {
        if (upgradeData == null) return;

        currentUpgradeData = upgradeData;

        // UI更新
        UpdateUI();

        // エフェクト設定
        SetupEffectsForUpgradeType(upgradeData.upgradeType);

        if (enableDebugLog)
            Debug.Log($"📋 UpgradeCompactItem設定完了: {upgradeData.upgradeName} Lv.{upgradeData.currentLevel}");
    }

    /// <summary>
    /// UIテキストとアイコンの更新
    /// </summary>
    private void UpdateUI()
    {
        if (currentUpgradeData == null) return;

        // アイコンの色設定
        if (iconImage != null)
        {
            iconImage.color = GetUpgradeTypeColor(currentUpgradeData.upgradeType);

            // 非アクティブ時は半透明
            if (!currentUpgradeData.isActive)
            {
                Color color = iconImage.color;
                color.a = 0.5f;
                iconImage.color = color;
            }
        }

        // 名前（短縮表示）
        if (nameText != null)
        {
            string displayName = currentUpgradeData.upgradeName.Length > 8
                ? currentUpgradeData.upgradeName.Substring(0, 8) + "..."
                : currentUpgradeData.upgradeName;
            nameText.text = displayName;
        }

        // レベル表示
        if (levelText != null)
        {
            levelText.text = $"Lv.{currentUpgradeData.currentLevel}";

            // 最大レベル到達時は色を変更
            if (currentUpgradeData.currentLevel >= currentUpgradeData.maxLevel)
            {
                levelText.color = Color.yellow;
            }
            else
            {
                levelText.color = Color.white;
            }
        }

        // 効果値表示
        if (effectText != null)
        {
            float effectValue = currentUpgradeData.GetCurrentEffect();
            effectText.text = $"{effectValue:F0}";
        }
    }

    /// <summary>
    /// アップグレードタイプに応じたエフェクトを設定
    /// </summary>
    private void SetupEffectsForUpgradeType(UpgradeType upgradeType)
    {
        // 既存のエフェクトを停止
        StopAllEffects();

        EffectType effectType = GetEffectTypeForUpgrade(upgradeType);

        switch (effectType)
        {
            case EffectType.ConstantGlow:
                // 常時発生系：ゆっくり点滅を開始
                StartConstantGlow();
                if (enableDebugLog)
                    Debug.Log($"🌟 常時点滅開始: {upgradeType}");
                break;

            case EffectType.ActivationOnly:
                // 自動生産系：エフェクトなし（アクティベート時のみ瞬間光）
                if (enableDebugLog)
                    Debug.Log($"⚡ アクティベート待機: {upgradeType}");
                break;

            case EffectType.ConditionalGlow:
                // まとめる係：条件付き点滅（ブースト中のみ）
                // 初期状態では点滅しない
                if (enableDebugLog)
                    Debug.Log($"🏢 条件付きエフェクト待機: {upgradeType}");
                break;

            case EffectType.None:
            default:
                // エフェクトなし
                break;
        }
    }

    /// <summary>
    /// アップグレードタイプに応じたエフェクトタイプを決定
    /// </summary>
    private EffectType GetEffectTypeForUpgrade(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            // まとめる係：条件付き点滅（ブースト中のみ）
            case UpgradeType.Organizer:
                return EffectType.ConditionalGlow;

            // 常時発生系（ゆっくり点滅）
            case UpgradeType.MiraclTime:
            case UpgradeType.Satisfaction:
            case UpgradeType.ChatSystem:
            case UpgradeType.RainbowJapaman:
            case UpgradeType.LuckyBeast:
            case UpgradeType.LuckyTail:
            case UpgradeType.FriendsCall:
                return EffectType.ConstantGlow;

            // 自動生産系（アクティベート時のみ瞬間光）
            case UpgradeType.Factory:
            case UpgradeType.HelperFriend:
            case UpgradeType.DonkeyBakery:
            case UpgradeType.RobaBakery:
                return EffectType.ActivationOnly;

            // 基本系（エフェクトなし）
            case UpgradeType.ClickPower:
            default:
                return EffectType.None;
        }
    }

    /// <summary>
    /// 常時発生系のゆっくり点滅を開始
    /// </summary>
    private void StartConstantGlow()
    {
        if (iconImage == null) return;

        isConstantGlowActive = true;
        constantGlowCoroutine = StartCoroutine(ConstantGlowAnimation());
    }

    /// <summary>
    /// 常時点滅アニメーション
    /// </summary>
    private IEnumerator ConstantGlowAnimation()
    {
        while (isConstantGlowActive && iconImage != null)
        {
            // ゆっくりとした sine wave による点滅
            float pulse = Mathf.Sin(Time.time * slowPulseSpeed) * 0.5f + 0.5f;

            // 元の色と光る色の間で補間
            Color currentColor = Color.Lerp(originalIconColor, constantGlowColor, pulse * 0.6f);
            iconImage.color = currentColor;

            yield return null;
        }

        // 終了時は元の色に戻す
        if (iconImage != null)
        {
            iconImage.color = originalIconColor;
        }
    }

    /// <summary>
    /// アクティベート時の瞬間光エフェクト（自動生産系用）
    /// 外部から呼び出される
    /// </summary>
    public void TriggerActivationFlash()
    {
        if (iconImage == null) return;

        EffectType effectType = GetEffectTypeForUpgrade(currentUpgradeData?.upgradeType ?? UpgradeType.ClickPower);

        // 自動生産系のみ瞬間光を実行
        if (effectType == EffectType.ActivationOnly)
        {
            // 🔥 高速連続発動を制限
            float currentTime = Time.time;
            if (currentTime - lastActivationTime < MIN_ACTIVATION_INTERVAL)
            {
                if (enableDebugLog)
                    Debug.Log($"⚡ アクティベーション制限: 前回から{currentTime - lastActivationTime:F2}秒（最小{MIN_ACTIVATION_INTERVAL}秒）");
                return;
            }
            lastActivationTime = currentTime;

            // 🔥 既に実行中の場合は停止してから新しいエフェクトを開始
            if (activationFlashCoroutine != null)
            {
                StopCoroutine(activationFlashCoroutine);
                // アイコンの状態を確実にリセット
                ResetIconToOriginalState();
            }
            activationFlashCoroutine = StartCoroutine(ActivationFlashAnimation());

            if (enableDebugLog)
                Debug.Log($"⚡ アクティベーション瞬間光実行: {currentUpgradeData?.upgradeType}");
        }
    }

    /// <summary>
    /// アイコンを元の状態にリセット
    /// </summary>
    private void ResetIconToOriginalState()
    {
        if (iconImage != null)
        {
            iconImage.color = originalIconColor;
            iconImage.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// アクティベート時の瞬間光アニメーション
    /// </summary>
    private IEnumerator ActivationFlashAnimation()
    {
        if (iconImage == null) yield break;

        // 🔥 開始時の状態を確実に記録
        Color startColor = originalIconColor;
        Vector3 startScale = Vector3.one; // 常に1,1,1から開始
        Vector3 flashScale = startScale * 1.15f;

        // 🔥 開始時に確実にリセット
        iconImage.color = startColor;
        iconImage.transform.localScale = startScale;

        float elapsed = 0f;
        float halfDuration = quickFlashDuration * 0.5f;

        // 拡大＋光らせる
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;

            iconImage.color = Color.Lerp(startColor, activeGlowColor, progress);
            iconImage.transform.localScale = Vector3.Lerp(startScale, flashScale, progress);

            yield return null;
        }

        elapsed = 0f;

        // 縮小＋元の色に戻す
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;

            iconImage.color = Color.Lerp(activeGlowColor, startColor, progress);
            iconImage.transform.localScale = Vector3.Lerp(flashScale, startScale, progress);

            yield return null;
        }

        // 🔥 確実に元の状態に戻す
        ResetIconToOriginalState();

        // 🔥 コルーチン参照をクリア
        activationFlashCoroutine = null;
    }

    /// <summary>
    /// 全てのエフェクトを停止
    /// </summary>
    private void StopAllEffects()
    {
        // 常時点滅を停止
        isConstantGlowActive = false;
        if (constantGlowCoroutine != null)
        {
            StopCoroutine(constantGlowCoroutine);
            constantGlowCoroutine = null;
        }

        // アクティベーション瞬間光を停止
        if (activationFlashCoroutine != null)
        {
            StopCoroutine(activationFlashCoroutine);
            activationFlashCoroutine = null;
        }

        // 🔥 アイコンを確実に元の状態に戻す
        ResetIconToOriginalState();
    }

    /// <summary>
    /// アップグレードタイプに応じた色を取得
    /// </summary>
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
            case UpgradeType.DonkeyBakery: return new Color(0.8f, 0.4f, 0f);
            case UpgradeType.Organizer: return Color.cyan;
            case UpgradeType.MiraclTime: return new Color(1f, 0.8f, 1f);
            case UpgradeType.Satisfaction: return new Color(0.8f, 1f, 0.8f);
            case UpgradeType.ChatSystem: return new Color(0.6f, 0.8f, 1f);
            case UpgradeType.LuckyTail: return new Color(1f, 1f, 0.6f);
            case UpgradeType.FriendsCall: return new Color(0.9f, 0.7f, 0.9f);
            default: return Color.white;
        }
    }

    /// <summary>
    /// 外部API：アップグレードタイプ別エフェクト処理
    /// UpgradeSidePanelUIから呼び出される
    /// </summary>
    public void HandleUpgradeTypeEffect(UpgradeType upgradeType, bool isActive)
    {
        if (currentUpgradeData?.upgradeType != upgradeType) return;

        EffectType effectType = GetEffectTypeForUpgrade(upgradeType);

        if (effectType == EffectType.ConditionalGlow)
        {
            // まとめる係：ブースト状態に応じて点滅制御
            if (isActive)
            {
                // ブースト開始：点滅開始
                StartConstantGlow();
                if (enableDebugLog)
                    Debug.Log($"🏢 まとめる係ブースト開始 - 点滅開始: {upgradeType}");
            }
            else
            {
                // ブースト終了：点滅停止
                StopAllEffects();
                if (enableDebugLog)
                    Debug.Log($"🏢 まとめる係ブースト終了 - 点滅停止: {upgradeType}");
            }
        }
        else if (effectType == EffectType.ActivationOnly && isActive)
        {
            // 自動生産系：アクティベート瞬間光
            TriggerActivationFlash();
        }
        // 常時発生系は既に動作中なので追加処理不要
    }

    /// <summary>
    /// 外部API：汎用光エフェクト（削除 - 新規取得時の点滅を除去）
    /// </summary>
    public void TriggerGlowEffect()
    {
        // 新規取得時の3回点滅を削除
        // 何もしない
        if (enableDebugLog)
            Debug.Log($"🚫 新規取得エフェクトは無効化されました: {currentUpgradeData?.upgradeName}");
    }

    /// <summary>
    /// 外部API：ハイライト表示
    /// </summary>
    public void Highlight(float duration = 1f)
    {
        if (backgroundImage != null)
        {
            StartCoroutine(HighlightAnimation(duration));
        }
    }

    /// <summary>
    /// ハイライトアニメーション
    /// </summary>
    private IEnumerator HighlightAnimation(float duration)
    {
        Color highlightColor = new Color(1f, 1f, 1f, 0.3f);
        Color originalColor = backgroundImage.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Sin(elapsed / duration * Mathf.PI * 6) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(originalColor, highlightColor, alpha);
            backgroundImage.color = currentColor;
            yield return null;
        }

        backgroundImage.color = originalColor;
    }

    // アクセサ・プロパティ
    public UpgradeData GetUpgradeData() => currentUpgradeData;
    public UpgradeType GetUpgradeType() => currentUpgradeData?.upgradeType ?? UpgradeType.ClickPower;

    /// <summary>
    /// アイテムクリック時の処理
    /// </summary>
    private void OnItemClicked()
    {
        if (currentUpgradeData != null)
        {
            Debug.Log($"🖱️ アップグレードアイテムクリック: {currentUpgradeData.upgradeName}");
            // 必要に応じて詳細画面表示など
        }
    }

    /// <summary>
    /// GameManager.CanAutoProduction()の代替
    /// GameManagerが見つからない場合のフォールバック
    /// </summary>
    private bool CanAutoProduction()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.CanAutoProduction();
        }
        else
        {
            // フォールバック：基本的な条件をチェック
            return Time.timeScale > 0; // ゲームが一時停止されていなければOK
        }
    }

    private void OnDestroy()
    {
        // エフェクトをクリーンアップ
        StopAllEffects();
    }

    /// <summary>
    /// 削除されたメソッドの互換性維持（空実装）
    /// 他のスクリプトから呼び出される可能性があるため残す
    /// </summary>
    [System.Obsolete("ForceUpdateGlowEffect is deprecated. Use TriggerActivationFlash() or SetupEffectsForUpgradeType() instead.")]
    public void ForceUpdateGlowEffect()
    {
        // 空実装 - 何もしない
        // 新しいエフェクトシステムでは不要
        if (enableDebugLog)
            Debug.Log($"⚠️ ForceUpdateGlowEffect は廃止されました: {currentUpgradeData?.upgradeName}");
    }

    // デバッグメソッド
    [ContextMenu("デバッグ: エフェクト情報表示")]
    private void DebugShowEffectInfo()
    {
        if (currentUpgradeData == null)
        {
            Debug.Log("❌ アップグレードデータがありません");
            return;
        }

        EffectType effectType = GetEffectTypeForUpgrade(currentUpgradeData.upgradeType);
        Debug.Log($"=== {currentUpgradeData.upgradeName} エフェクト情報 ===");
        Debug.Log($"エフェクトタイプ: {effectType}");
        Debug.Log($"常時点滅アクティブ: {isConstantGlowActive}");
        Debug.Log($"常時点滅コルーチン: {constantGlowCoroutine != null}");
        Debug.Log($"瞬間光コルーチン: {activationFlashCoroutine != null}");
    }

    [ContextMenu("デバッグ: アクティベーション瞬間光テスト")]
    private void DebugTestActivationFlash()
    {
        if (Application.isPlaying)
        {
            TriggerActivationFlash();
        }
    }

    [ContextMenu("デバッグ: エフェクト強制停止")]
    private void DebugStopAllEffects()
    {
        if (Application.isPlaying)
        {
            StopAllEffects();
        }
    }
}