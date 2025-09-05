using UnityEngine;
using System.Collections;

public class PlateJapamanAnimator : MonoBehaviour
{
    [Header("重力落下設定")]
    public float gravity = 200f;
    public float bounceDamping = 0.3f;
    public float groundingDelay = 0.5f;

    [Header("重なり判定設定")]
    [Range(5f, 50f)]
    public float collisionRadius = 20f;        // ジャパまんの当たり判定半径

    [Header("移動・回転設定")]
    public float rollSpeed = 200f;
    public float maxRollDistance = 100f;

    [Header("皿の横幅設定")]
    [SerializeField, Range(50f, 300f)]
    private float plateWidth = 140f;          // 皿の横幅（Inspector調整用）

    [SerializeField]
    private bool autoDetectPlateWidth = true;  // 自動検出するかどうか

    [SerializeField]
    private string plateContainerName = "PlateContainer";  // PlateContainerの名前

    [Header("アニメーション設定")]
    public float idleIntensity = 2f;
    public float idleSpeed = 1f;

    [Header("デバッグ設定")]
    [SerializeField]
    private bool enableDebugLog = false;  // デバッグログの有効/無効

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;  // 透明度制御用
    private bool isGrounded = false;
    private bool isIdle = false;
    private bool isSettling = false;
    private bool isBeingConsumed = false;  // 吸い込み中フラグ
    private float groundingTimer = 0f;
    private Vector2 basePosition;
    private Vector2 originalPosition;  // 元の位置を保存
    private float baseRotation;
    private float detectedPlateWidth = 140f;  // 検出された皿の横幅

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("PlateJapamanAnimator: RectTransform component not found!");
        }

        // CanvasGroupを取得または追加
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        basePosition = rectTransform.anchoredPosition;
        originalPosition = rectTransform.anchoredPosition;  // 元の位置を保存
        baseRotation = rectTransform.rotation.eulerAngles.z;

        // 皿の横幅を検出または設定
        DetectOrSetPlateWidth();

        // ジャパまんのサイズに応じて自動調整
        AutoAdjustCollisionRadius();
    }

    /// <summary>
    /// 吸い込みアニメーション開始時に呼び出す
    /// </summary>
    public void StartConsumption()
    {
        isBeingConsumed = true;
        isIdle = false;

        // 他のアニメーションを停止
        StopAllCoroutines();

        DebugLog("ジャパまん吸い込み開始");
    }

    /// <summary>
    /// 吸い込みアニメーション完了時に呼び出す
    /// </summary>
    public void OnConsumptionComplete()
    {
        DebugLog("ジャパまん吸い込み完了 - オブジェクトを非表示化");

        // 即座に非表示にする
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // GameObjectも無効化
        gameObject.SetActive(false);
    }

    /// <summary>
    /// オブジェクトをリセット（プール使用時など）
    /// </summary>
    public void ResetJapaman()
    {
        DebugLog("ジャパまんリセット");

        // 状態をリセット
        isBeingConsumed = false;
        isGrounded = false;
        isIdle = false;
        isSettling = false;
        groundingTimer = 0f;

        // 表示を復元
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // 位置と回転をリセット
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.rotation = Quaternion.Euler(0, 0, baseRotation);
        }

        basePosition = originalPosition;

        // GameObjectを有効化
        gameObject.SetActive(true);
    }

    /// <summary>
    /// デバッグログを出力（条件付き）
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[{gameObject.name}] {message}");
        }
    }

    /// <summary>
    /// 皿の横幅を自動検出または手動設定値を使用
    /// </summary>
    private void DetectOrSetPlateWidth()
    {
        if (autoDetectPlateWidth)
        {
            // PlateContainerを自動検出
            detectedPlateWidth = DetectPlateContainerWidth();
            if (detectedPlateWidth > 0)
            {
                plateWidth = detectedPlateWidth;
                DebugLog($"皿の横幅を自動検出: {plateWidth:F1}px");
            }
            else
            {
                // エラーログは残す（重要な情報のため）
                Debug.LogWarning($"PlateContainer '{plateContainerName}' が見つかりません。手動設定値を使用: {plateWidth:F1}px");
            }
        }
        else
        {
            DebugLog($"皿の横幅を手動設定で使用: {plateWidth:F1}px");
        }
    }

    /// <summary>
    /// PlateContainerの横幅を検出
    /// </summary>
    private float DetectPlateContainerWidth()
    {
        // 親オブジェクトからPlateContainerを探す
        Transform parent = transform.parent;
        while (parent != null)
        {
            // 現在の親でPlateContainerを検索
            Transform plateContainer = parent.Find(plateContainerName);
            if (plateContainer != null)
            {
                RectTransform plateRect = plateContainer.GetComponent<RectTransform>();
                if (plateRect != null)
                {
                    float width = plateRect.rect.width;
                    DebugLog($"PlateContainer検出成功: {plateContainer.name}, 横幅: {width:F1}px");
                    return width;
                }
            }

            // より上の階層を検索
            parent = parent.parent;
        }

        // シーン全体からPlateContainerを検索（最後の手段）
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == plateContainerName)
            {
                RectTransform plateRect = obj.GetComponent<RectTransform>();
                if (plateRect != null)
                {
                    float width = plateRect.rect.width;
                    DebugLog($"PlateContainer全体検索で発見: {obj.name}, 横幅: {width:F1}px");
                    return width;
                }
            }
        }

        // エラーログは残す
        Debug.LogWarning($"PlateContainer '{plateContainerName}' が見つかりませんでした");
        return 0f;
    }

    /// <summary>
    /// ジャパまんのサイズに応じて当たり判定を自動調整
    /// </summary>
    private void AutoAdjustCollisionRadius()
    {
        if (rectTransform != null)
        {
            // RectTransformのサイズを基準に当たり判定を設定
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float averageSize = (width + height) / 2f;

            // サイズの40%を当たり判定とする（重なりすぎを防ぐ）
            collisionRadius = averageSize * 0.4f;

            DebugLog("ジャパまん当たり判定自動調整: サイズ=" + averageSize.ToString("F1") + ", 半径=" + collisionRadius.ToString("F1"));
        }
    }

    /// <summary>
    /// Inspector での手動調整用
    /// </summary>
    public void SetCollisionRadius(float radius)
    {
        collisionRadius = Mathf.Clamp(radius, 5f, 100f);
        DebugLog("ジャパまん当たり判定手動設定: 半径=" + collisionRadius.ToString("F1"));
    }

    /// <summary>
    /// 皿の横幅を手動設定（Inspector用）
    /// </summary>
    public void SetPlateWidth(float width)
    {
        plateWidth = Mathf.Clamp(width, 50f, 300f);
        autoDetectPlateWidth = false;  // 手動設定時は自動検出をオフ
        DebugLog($"皿の横幅を手動設定: {plateWidth:F1}px");
    }

    /// <summary>
    /// 皿の横幅を再検出
    /// </summary>
    [ContextMenu("皿の横幅を再検出")]
    public void RefreshPlateWidth()
    {
        DetectOrSetPlateWidth();
    }

    private void Update()
    {
        // 吸い込み中は通常のアニメーションを停止
        if (isBeingConsumed) return;

        if (isIdle && !isSettling)
        {
            PerformIdleAnimation();
        }
    }

    public void StartGravityFall()
    {
        if (rectTransform == null)
        {
            Debug.LogError("PlateJapamanAnimator: rectTransform is null!");
            return;
        }

        // 吸い込み中の場合は落下を開始しない
        if (isBeingConsumed) return;

        StartCoroutine(GravityFallCoroutine());
    }

    private IEnumerator GravityFallCoroutine()
    {
        if (rectTransform == null)
        {
            Debug.LogError("PlateJapamanAnimator: rectTransform is null!");
            yield break;
        }

        Vector2 velocity = new Vector2(Random.Range(-50f, 50f), 0f);
        Vector2 gravityForce = new Vector2(0f, -gravity);

        float minimumGroundLevel = -20f;

        while (rectTransform.anchoredPosition.y > GetGroundLevel() && !isBeingConsumed)
        {
            velocity += gravityForce * Time.deltaTime;
            Vector2 newPos = rectTransform.anchoredPosition + velocity * Time.deltaTime;

            // 皿の端で跳ね返り（設定された横幅を使用）
            float plateHalfWidth = plateWidth * 0.5f;
            if (Mathf.Abs(newPos.x) > plateHalfWidth)
            {
                velocity.x *= -0.7f;
                newPos.x = Mathf.Clamp(newPos.x, -plateHalfWidth, plateHalfWidth);
            }

            // 地面との衝突で跳ね返り
            float groundLevel = GetGroundLevel();
            groundLevel = Mathf.Max(groundLevel, minimumGroundLevel);

            if (newPos.y <= groundLevel)
            {
                newPos.y = groundLevel;
                velocity.y *= -bounceDamping;
                if (Mathf.Abs(velocity.y) < 20f)
                {
                    velocity.y = 0f;
                    if (!isSettling)
                    {
                        isSettling = true;
                        groundingTimer = 0f;
                    }
                }
            }
            else
            {
                isSettling = false;
                groundingTimer = 0f;
            }

            rectTransform.anchoredPosition = newPos;
            rectTransform.Rotate(0, 0, velocity.magnitude * Time.deltaTime);

            yield return null;
        }

        // 吸い込み中に落下が中断された場合は処理を終了
        if (isBeingConsumed) yield break;

        // 落下ループ終了後の着地処理
        if (!isSettling)
        {
            isSettling = true;
            groundingTimer = 0f;
        }

        while (isSettling && !isGrounded && !isBeingConsumed)
        {
            groundingTimer += Time.deltaTime;

            if (groundingTimer >= groundingDelay)
            {
                isGrounded = true;
                isSettling = false;
                baseRotation = rectTransform.rotation.eulerAngles.z;
            }

            Vector2 currentPos = rectTransform.anchoredPosition;
            if (currentPos.y > GetGroundLevel() + 1f)
            {
                isSettling = false;
                groundingTimer = 0f;
                break;
            }

            yield return null;
        }

        // 吸い込み中に着地処理が中断された場合は処理を終了
        if (isBeingConsumed) yield break;

        basePosition = rectTransform.anchoredPosition;
        isIdle = true;
    }

    private float GetGroundLevel()
    {
        float plateGroundLevel = 0f;

        if (ContainerSettings.Instance != null)
        {
            var containerData = ContainerSettings.Instance.GetCurrentContainerData();
            if (containerData != null)
            {
                plateGroundLevel += containerData.centerOffset.y;
            }
        }

        // ★★★ 調整された当たり判定を使用 ★★★
        var allJapaman = FindObjectsByType<PlateJapamanAnimator>(FindObjectsSortMode.None);
        foreach (var other in allJapaman)
        {
            if (other == this || other.rectTransform == null) continue;
            if (!other.isGrounded || other.isBeingConsumed) continue;  // 吸い込み中は無視

            float distance = Vector2.Distance(rectTransform.anchoredPosition, other.rectTransform.anchoredPosition);

            // ★★★ 両方の当たり判定半径を合計 ★★★
            float combinedRadius = collisionRadius + other.collisionRadius;

            if (distance < combinedRadius)
            {
                plateGroundLevel = Mathf.Max(plateGroundLevel, other.rectTransform.anchoredPosition.y + combinedRadius);
            }
        }

        return plateGroundLevel;
    }

    public void RollToPosition(Vector2 targetPosition, float duration)
    {
        if (rectTransform == null || isBeingConsumed) return;

        DebugLog("PlateJapamanAnimator: RollToPosition() → target=(" + targetPosition.x.ToString("F2") + ", " + targetPosition.y.ToString("F2") + "), duration=" + duration);
        StartCoroutine(RollToPositionCoroutine(targetPosition, duration));
    }

    private IEnumerator RollToPositionCoroutine(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float startRotation = rectTransform.rotation.eulerAngles.z;

        float distance = Vector2.Distance(startPosition, targetPosition);
        float targetRotation = startRotation + (distance / 10f) * 360f;

        float elapsed = 0f;

        while (elapsed < duration && !isBeingConsumed)
        {
            if (rectTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector2 currentPos = Vector2.Lerp(startPosition, targetPosition, t);
            float currentRot = Mathf.Lerp(startRotation, targetRotation, t);

            rectTransform.anchoredPosition = currentPos;
            rectTransform.rotation = Quaternion.Euler(0, 0, currentRot);

            yield return null;
        }

        if (rectTransform != null && !isBeingConsumed)
        {
            rectTransform.anchoredPosition = targetPosition;
        }
    }

    private void PerformIdleAnimation()
    {
        if (rectTransform == null || isBeingConsumed) return;

        float time = Time.time * idleSpeed;
        float offsetY = Mathf.Sin(time) * idleIntensity;
        float offsetRotation = Mathf.Sin(time * 0.7f) * (idleIntensity * 0.5f);

        rectTransform.anchoredPosition = basePosition + new Vector2(0, offsetY);
        rectTransform.rotation = Quaternion.Euler(0, 0, baseRotation + offsetRotation);
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public bool IsIdle()
    {
        return isIdle;
    }

    public bool IsBeingConsumed()
    {
        return isBeingConsumed;
    }

    /// <summary>
    /// 現在の皿の横幅を取得
    /// </summary>
    public float GetPlateWidth()
    {
        return plateWidth;
    }

    /// <summary>
    /// デバッグ用：当たり判定の可視化
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (rectTransform != null)
        {
            // 当たり判定の可視化
            Gizmos.color = Color.red;
            Vector3 worldPos = rectTransform.position;
            Gizmos.DrawWireSphere(worldPos, collisionRadius);

            // 皿の横幅の可視化
            Gizmos.color = Color.blue;
            float plateHalfWidth = plateWidth * 0.5f;
            Vector3 leftEdge = worldPos + new Vector3(-plateHalfWidth, 0, 0);
            Vector3 rightEdge = worldPos + new Vector3(plateHalfWidth, 0, 0);
            Gizmos.DrawLine(leftEdge + Vector3.up * 50, leftEdge + Vector3.down * 50);
            Gizmos.DrawLine(rightEdge + Vector3.up * 50, rightEdge + Vector3.down * 50);
            Gizmos.DrawLine(leftEdge, rightEdge);
        }
    }
}