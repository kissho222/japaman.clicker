using UnityEngine;
using System.Collections;

public class CharacterSwallowAnimator : MonoBehaviour
{
    [Header("飲み込みアニメーション設定")]
    public float swallowIntensity = 20f;        // 上下の揺れの強さ
    public float swallowSpeed = 3f;             // 飲み込みの速度
    public int swallowCount = 5;                // 飲み込み回数

    [Header("対象オブジェクト")]
    public Transform characterTransform;        // キャラクターのTransform

    private Vector3 originalPosition;
    private bool isSwallowing = false;
    private Coroutine swallowCoroutine;

    public static CharacterSwallowAnimator Instance { get; private set; }

    private void Awake()
    {
        // ★★★ より安全なInstance管理 ★★★
        if (Instance != null && Instance != this)
        {
            // 既に他のインスタンスが存在する場合、このコンポーネントを無効化
            Debug.LogWarning("CharacterSwallowAnimatorの重複を検出。このコンポーネントを無効化します: " + gameObject.name);
            this.enabled = false;
            return;
        }
        Instance = this;

        // キャラクターのTransformが設定されていない場合、自分自身を使用
        if (characterTransform == null)
        {
            characterTransform = transform;
        }

        // 元の位置を記録
        originalPosition = characterTransform.localPosition;

        Debug.Log("CharacterSwallowAnimator Instance設定完了: " + gameObject.name);
    }

    private void OnDestroy()
    {
        // このインスタンスが破棄される時、Instanceをクリア
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("CharacterSwallowAnimator Instance をクリアしました");
        }
    }

    /// <summary>
    /// 飲み込みアニメーション開始
    /// </summary>
    public void StartSwallowAnimation()
    {
        if (!this.enabled) return;

        // ★★★ 既にアニメーション中の場合は継続して実行 ★★★
        if (isSwallowing)
        {
            Debug.Log("既に飲み込みアニメーション中のため、継続実行");
            return;
        }

        Debug.Log("キャラクター飲み込みアニメーション開始");
        swallowCoroutine = StartCoroutine(SwallowAnimationCoroutine());
    }

    /// <summary>
    /// 飲み込みアニメーション停止
    /// </summary>
    public void StopSwallowAnimation()
    {
        if (!this.enabled) return;

        // ★★★ すぐには停止せず、現在のアニメーションを完了させる ★★★
        if (isSwallowing && swallowCoroutine != null)
        {
            Debug.Log("飲み込みアニメーション終了要求（現在のアニメーション完了後に停止）");
            StartCoroutine(DelayedStop());
        }
    }

    /// <summary>
    /// 遅延停止処理
    /// </summary>
    private IEnumerator DelayedStop()
    {
        // 現在のアニメーションが終わるまで少し待つ
        yield return new WaitForSeconds(0.2f);

        if (swallowCoroutine != null)
        {
            StopCoroutine(swallowCoroutine);
            swallowCoroutine = null;
        }

        isSwallowing = false;

        // 元の位置に戻す
        if (characterTransform != null)
        {
            StartCoroutine(ReturnToOriginalPosition());
        }

        Debug.Log("キャラクター飲み込みアニメーション終了");
    }

    /// <summary>
    /// 飲み込みアニメーションのメインコルーチン
    /// </summary>
    private IEnumerator SwallowAnimationCoroutine()
    {
        if (!this.enabled) yield break;

        isSwallowing = true;

        for (int i = 0; i < swallowCount; i++)
        {
            if (!this.enabled || !isSwallowing) yield break;

            // 下に動く（飲み込み準備）
            yield return StartCoroutine(MoveToPosition(originalPosition + Vector3.down * swallowIntensity, 0.15f));

            // 少し待つ
            yield return new WaitForSeconds(0.1f);

            // 上に動く（飲み込み）
            yield return StartCoroutine(MoveToPosition(originalPosition + Vector3.up * swallowIntensity * 0.5f, 0.1f));

            // 元の位置に戻る
            yield return StartCoroutine(MoveToPosition(originalPosition, 0.2f));

            // 次の飲み込みまでの間隔
            yield return new WaitForSeconds(1f / swallowSpeed);
        }

        isSwallowing = false;
        Debug.Log("飲み込みアニメーション完了");
    }

    /// <summary>
    /// 指定位置への移動アニメーション
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition, float duration)
    {
        if (!this.enabled || characterTransform == null) yield break;

        Vector3 startPosition = characterTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration && this.enabled)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // スムーズな移動（イージング）
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            if (characterTransform != null)
            {
                characterTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            }

            yield return null;
        }

        if (characterTransform != null)
        {
            characterTransform.localPosition = targetPosition;
        }
    }

    /// <summary>
    /// 元の位置に戻るアニメーション
    /// </summary>
    private IEnumerator ReturnToOriginalPosition()
    {
        yield return StartCoroutine(MoveToPosition(originalPosition, 0.3f));
    }

    /// <summary>
    /// 飲み込み中かどうか
    /// </summary>
    public bool IsSwallowing()
    {
        return isSwallowing && this.enabled;
    }

    /// <summary>
    /// 設定をリアルタイムで変更
    /// </summary>
    public void SetSwallowSettings(float intensity, float speed, int count)
    {
        if (!this.enabled) return;

        swallowIntensity = intensity;
        swallowSpeed = speed;
        swallowCount = count;
    }
}