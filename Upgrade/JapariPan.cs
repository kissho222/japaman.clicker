using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ジャパリパンオブジェクト（軽量版）
/// プレートに配置され、吸い込み時にClickManagerに価値を加算
/// </summary>
public class JapariPan : MonoBehaviour
{
    [Header("パン設定")]
    public float panValue = 10f;                // このパンの価値

    [Header("視覚効果")]
    public float glowIntensity = 1f;            // 光る強さ

    // 内部状態
    private bool isCollected = false;

    // コンポーネント参照
    private Image panImage;
    private ClickManager clickManager;

    private void Awake()
    {
        panImage = GetComponent<Image>();
        if (panImage == null)
        {
            panImage = gameObject.AddComponent<Image>();
        }
    }

    private void Start()
    {
        InitializePan();
    }

    private void InitializePan()
    {
        // ClickManager参照取得
        clickManager = FindFirstObjectByType<ClickManager>();

        // 視覚効果初期化
        InitializeVisualEffects();

        Debug.Log($"🍞 ジャパリパン生成: 価値{panValue:F0}");
    }

    /// <summary>
    /// パンの価値を設定
    /// </summary>
    public void SetPanValue(float value)
    {
        panValue = value;

        // 価値に応じて視覚的変化
        UpdateAppearanceByValue();
    }

    /// <summary>
    /// 価値に応じた見た目更新（フランスパン風）
    /// </summary>
    private void UpdateAppearanceByValue()
    {
        if (panImage == null) return;

        // 🔥 フランスパン風の色調整
        if (panValue >= 1000f)
        {
            // 超高価値（金色のフランスパン）
            panImage.color = new Color(1f, 0.8f, 0f, 1f);
            glowIntensity = 2f;
        }
        else if (panValue >= 100f)
        {
            // 高価値（銀色のフランスパン）
            panImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            glowIntensity = 1.5f;
        }
        else if (panValue >= 50f)
        {
            // 中価値（薄黄色のフランスパン）
            panImage.color = new Color(1f, 0.9f, 0.5f, 1f);
            glowIntensity = 1.2f;
        }
        else
        {
            // 基本価値（通常のフランスパン色）
            panImage.color = new Color(0.9f, 0.7f, 0.4f, 1f);
            glowIntensity = 1f;
        }

        // 🔥 価値が高いほど少し大きく表示
        float scaleMultiplier = 1f + (panValue / 1000f) * 0.2f; // 最大20%拡大
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, 1f, 1.3f);
        transform.localScale = Vector3.one * scaleMultiplier;
    }

    /// <summary>
    /// 視覚効果初期化
    /// </summary>
    private void InitializeVisualEffects()
    {
        if (panImage != null)
        {
            // 🔥 フランスパンのデフォルト色
            panImage.color = new Color(0.9f, 0.7f, 0.4f, 1f);

            // 🔥 フランスパンの形状（横長）
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null && rectTransform.sizeDelta == Vector2.zero)
            {
                rectTransform.sizeDelta = new Vector2(40, 15);
            }
        }
    }

    /// <summary>
    /// 吸い込み時の価値加算（PlateJapamanAnimatorから呼ばれる）
    /// </summary>
    public void OnSuckedIn()
    {
        if (isCollected) return;

        isCollected = true;

        // 🔥 ジャパリパンは既にプレートに積まれた時点でカウント済み
        // 吸い込み時は追加でカウントしない（重複防止）
        Debug.Log($"🍞 ジャパリパン吸い込み完了: 価値{panValue:F0} (カウント済み)");

        // 必要に応じてエフェクトのみ実行
        // 価値の追加は行わない
    }

    /// <summary>
    /// 収集済みかどうか
    /// </summary>
    public bool IsCollected()
    {
        return isCollected;
    }

    /// <summary>
    /// 手動収集（デバッグ用）
    /// </summary>
    [ContextMenu("🍞 手動収集")]
    public void DebugCollectPan()
    {
        if (Application.isPlaying && !isCollected)
        {
            OnSuckedIn();
        }
    }
}