using UnityEngine;
using System.Collections;

/// <summary>
/// ロバのパン屋管理システム（プレート統合版）
/// 修正版：投げ込みアニメーション対応
/// </summary>
public class DonkeyBakeryManager : MonoBehaviour
{
    [Header("パン屋設定")]
    public GameObject japariPanPrefab;          // ジャパリパンのPrefab（フランスパン外観）
    public Transform plateContainer;            // プレートコンテナ（ClickManagerと同じ）
    public RectTransform plateImage;            // プレート画像（位置計算用）
    public Transform bakerySpawnPoint;          // パン生成位置

    [Header("パン投入設定")]
    public float baseProductionInterval = 3f;   // 基本生産間隔（秒）
    public float intervalReduction = 0.2f;      // レベルごとの間隔短縮
    public float minInterval = 0.5f;            // 最小間隔

    [Header("価値設定")]
    public float basePanValue = 10f;            // 基本パン価値
    public float valueMultiplier = 2.5f;        // レベルごとの価値倍率

    [Header("ジャパリパン落下設定")]
    public float minDropHeight = 300f;          // 最小落下高度
    public float maxDropHeight = 500f;          // 最大落下高度

    [Header("視覚効果")]
    public AudioClip bakingSound;               // パン焼き音
    public AudioClip dropSound;                 // 落下音

    [Header("まとめる係連携")]
    [SerializeField] private float organizerBoostMultiplier = 1f;
    [SerializeField] private bool isOrganizerBoostActive = false;



    // 内部状態
    private int currentLevel = 0;
    private float currentPanValue = 0f;
    private float currentInterval = 3f;
    private bool isBakingActive = false;
    private Coroutine bakingCoroutine;
    private bool isActive = false;

    // 統計
    private int totalPansProduced = 0;
    private float totalValueProduced = 0f;

    // 参照
    private AudioSource audioSource;
    private ClickManager clickManager;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        InitializeBakery();
    }

    private void InitializeBakery()
    {
        // ClickManagerの参照取得
        clickManager = FindFirstObjectByType<ClickManager>();

        if (clickManager == null)
        {
            Debug.LogWarning("ClickManagerが見つかりません");
            return;
        }

        // 既存のClickManagerからプレート情報を取得
        if (plateContainer == null && clickManager.plateContainer != null)
        {
            plateContainer = clickManager.plateContainer;
        }

        // デフォルトのジャパリパンPrefabを作成（必要に応じて）
        if (japariPanPrefab == null)
        {
            CreateDefaultJapariPanPrefab();
        }

    }

    /// <summary>
    /// パン屋のレベルと効果を設定
    /// </summary>
    public void SetBakeryLevel(int level, float effectValue)
    {
        currentLevel = level;
        isActive = level > 0; // アクティブ状態を設定

        // 価値計算（指数的成長）
        float rawValue = basePanValue * Mathf.Pow(valueMultiplier, level);
        currentPanValue = Mathf.Ceil(rawValue);

        // 生産間隔計算（レベルが上がるほど速くなる）
        currentInterval = Mathf.Max(minInterval, baseProductionInterval - (intervalReduction * level));


        // パン屋開始
        StartBakery();
    }

    /// <summary>
    /// パン屋の開始
    /// </summary>
    public void StartBakery()
    {
        if (currentLevel <= 0) return;

        if (bakingCoroutine != null)
        {
            StopCoroutine(bakingCoroutine);
        }

        isBakingActive = true;
        bakingCoroutine = StartCoroutine(BakingLoop());

    }

    /// <summary>
    /// パン屋の停止
    /// </summary>
    public void StopBakery()
    {
        isBakingActive = false;

        if (bakingCoroutine != null)
        {
            StopCoroutine(bakingCoroutine);
            bakingCoroutine = null;
        }

    }

    /// <summary>
    /// パン焼きループ（生産速度ブースト対応版）
    /// </summary>
    private IEnumerator BakingLoop()
    {
        while (isBakingActive && currentLevel > 0)
        {
            // 🔥 まとめる係のブーストを適用した間隔を計算
            float effectiveInterval = currentInterval;
            if (isOrganizerBoostActive && organizerBoostMultiplier > 1f)
            {
                effectiveInterval = currentInterval / organizerBoostMultiplier;
                // Debug.Log($"🍞 生産速度ブースト: {currentInterval:F1}秒 → {effectiveInterval:F1}秒 ({organizerBoostMultiplier:F1}x)");
            }

            yield return new WaitForSeconds(effectiveInterval);

            if (isBakingActive)
            {
                // ゲーム状態チェック
                if (GameManager.Instance != null && !GameManager.Instance.CanAutoProduction())
                {
                    continue;
                }

                // 🔥 通常の価値でパン生産（ブーストは速度のみ）
                ProducePan();
            }
        }
    }

    /// <summary>
    /// パン生産処理（生産速度ブースト版）
    /// </summary>
    private void ProducePan()
    {
        if (japariPanPrefab == null)
        {
            Debug.LogWarning("パン生産に必要な要素が不足しています");
            return;
        }

        bool isGoalAchieved = IsGoalAlreadyAchieved();

        if (isGoalAchieved)
        {
            // ノルマ達成後：直接投げ込み（通常価値）
            ProducePanDirectThrow();
        }
        else
        {
            // ノルマ達成前：プレートに積む（通常価値）
            ProducePanToPlate();
        }

        PlayBakingEffects();
        totalPansProduced++;
        totalValueProduced += currentPanValue; // 通常価値で統計更新

        // 🎨 UIエフェクトをトリガー
        if (UpgradeSidePanelUI.Instance != null)
        {
            var method = UpgradeSidePanelUI.Instance.GetType().GetMethod("TriggerItemActivationEffect");
            if (method != null)
            {
                UpgradeSidePanelUI.Instance.TriggerItemActivationEffect(UpgradeType.DonkeyBakery);
            }
            else
            {
                Debug.LogWarning("TriggerItemActivationEffect メソッドが見つかりません");
            }
        }
    }

    /// <summary>
    /// ノルマ達成状況をチェック
    /// </summary>
    private bool IsGoalAlreadyAchieved()
    {
        if (clickManager == null) return false;

        // ClickManagerのノルマ達成状況を確認
        long currentCount = clickManager.GetTotalJapamanCount();
        long goalCount = clickManager.goalCount;

        return currentCount >= goalCount;
    }

    /// <summary>
    /// プレートに積むパン生産（生産速度ブースト版）
    /// </summary>
    private void ProducePanToPlate()
    {
        if (plateContainer == null) return;

        // Debug.Log($"🔥 プレート積み: 価値{currentPanValue}（ブーストは速度のみ）");

        // ジャパリパンをプレートに生成（通常価値）
        GameObject newPan = Instantiate(japariPanPrefab, plateContainer);

        var japariPan = newPan.GetComponent<JapariPan>();
        if (japariPan == null)
        {
            japariPan = newPan.AddComponent<JapariPan>();
        }
        japariPan.SetPanValue(currentPanValue); // 通常価値



        // 🔥 ClickManagerのノルマカウンターに加算
        if (clickManager != null)
        {
            // 整数値に切り上げてカウンターに追加
            long panCount = (long)Mathf.Ceil(currentPanValue);

            // 🔥 クリック前の状態を記録
            long previousCount = clickManager.japamanCount;
            clickManager.japamanCount += panCount;

            // UIも更新
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateJapamanText(clickManager.japamanCount);
            }


            // 🔥 ノルマ達成判定（プレート積み時）
            if (previousCount < clickManager.goalCount && clickManager.japamanCount >= clickManager.goalCount)
            {

                // 🔥 GameManagerのパブリックメソッドを使用
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    // 🔥 ノルマ達成をGameManagerに通知
                    var currentCount = clickManager.japamanCount;
                    var goalCount = clickManager.goalCount;

                    // Debug.Log($"🔥 GameManagerに達成通知: {currentCount}/{goalCount}");

                    // 🔥 UIの変更のみここで実行
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.SetPhaseUI(false);
                        UIManager.Instance.ShowPhaseChangeMessage("ノルマ達成！\nフレンズにジャパまんをあげよう！");
                    }
                }

                // 🔥 吸い込み開始をClickManagerに依頼
                clickManager.TriggerDelayedSuck();
            }
        }

        // 🔥 ステージに応じてサイズ調整
        ApplyStageBasedScale(newPan);

        // 🔥 プレート上の位置を計算
        SetupJapariPanPosition(newPan);
    }

    // 既存のDonkeyBakeryManagerクラスに以下のメソッドを追加してください

    /// <summary>
    /// 直接投げ込みパン生産（生産速度ブースト版）
    /// </summary>
    private void ProducePanDirectThrow()
    {
        // Debug.Log($"🔥 直接投げ込み: 価値{currentPanValue}（ブーストは速度のみ）");

        // 🔥 通常価値でClickManagerに加算（ブーストは速度のみ）
        if (clickManager != null)
        {
            clickManager.AddJapamanFromPan(currentPanValue);
        }

        CreateVisualThrowAnimationSimple();
    }

    /// <summary>
    /// 確実に見える投げ込みアニメーション（既存JaparipanPrefab使用版）
    /// </summary>
    private void CreateVisualThrowAnimationSimple()
    {

        GameObject throwPan = null;
        string prefabName = "";

        // 🔥 優先順位1: 既存のjapariPanPrefabを使用（カスタマイズなし）
        if (japariPanPrefab != null)
        {
            throwPan = Instantiate(japariPanPrefab);
            prefabName = japariPanPrefab.name;
        }
        // 🔥 優先順位2: ClickManagerのmouthJapamanPrefab（口投げ込み用）
        else if (clickManager != null && clickManager.mouthJapamanPrefab != null)
        {
            throwPan = Instantiate(clickManager.mouthJapamanPrefab);
            prefabName = clickManager.mouthJapamanPrefab.name;
        }
        // 🔥 優先順位3: ClickManagerのflyingJapamanPrefab（フォールバック）
        else if (clickManager != null && clickManager.flyingJapamanPrefab != null)
        {
            throwPan = Instantiate(clickManager.flyingJapamanPrefab);
            prefabName = clickManager.flyingJapamanPrefab.name;
        }
        else
        {
            Debug.LogError("🔥 使用可能なPrefabが見つかりません");
            return;
        }

        if (throwPan == null)
        {
            Debug.LogError("🔥 Prefabのインスタンス化に失敗");
            return;
        }

        // 🔥 見た目カスタマイズは行わない（既存Prefabの外観をそのまま使用）

        // 親を設定（ClickManagerと同じ階層）
        if (clickManager != null && clickManager.spawnPoint != null && clickManager.spawnPoint.parent != null)
        {
            throwPan.transform.SetParent(clickManager.spawnPoint.parent);
        }

        // 開始位置：ClickManagerのspawnPointと同じ
        Vector3 startPos = clickManager?.spawnPoint != null ?
            clickManager.spawnPoint.position : Vector3.zero;

        // 目標位置：ClickManagerのfriendsMouseTargetと同じ
        Vector3 targetPos = clickManager?.friendsMouthTarget != null ?
            clickManager.friendsMouthTarget.position : Vector3.zero;

        throwPan.transform.position = startPos;

        // 🔥 JapariPanコンポーネントの価値設定のみ行う
        var japariPanComponent = throwPan.GetComponent<JapariPan>();
        if (japariPanComponent != null)
        {
            japariPanComponent.SetPanValue(currentPanValue);
        }

        // 🔥 アニメーション実行
        StartCoroutine(CopyClickManagerAnimation(throwPan, startPos, targetPos));
    }

    /// <summary>
    /// デバッグ用：利用可能なPrefab確認（簡略版）
    /// </summary>
    [ContextMenu("🍞 利用可能Prefab確認")]
    public void DebugAvailablePrefabs()
    {

        if (clickManager != null)
        {
            Debug.Log($"🍞 ClickManager.mouthJapamanPrefab: {(clickManager.mouthJapamanPrefab != null ? clickManager.mouthJapamanPrefab.name : "null")}");
            Debug.Log($"🍞 ClickManager.flyingJapamanPrefab: {(clickManager.flyingJapamanPrefab != null ? clickManager.flyingJapamanPrefab.name : "null")}");
        }
        else
        {
            Debug.LogError("❌ ClickManagerがnull");
        }

        // 現在の使用Prefab判定
        if (japariPanPrefab != null)
        {
            Debug.Log("✅ 現在使用予定: japariPanPrefab（既存の専用Prefab）");
        }
        else if (clickManager?.mouthJapamanPrefab != null)
        {
            Debug.Log("⚠️ 現在使用予定: mouthJapamanPrefab（代替）");
        }
        else if (clickManager?.flyingJapamanPrefab != null)
        {
            Debug.Log("⚠️ 現在使用予定: flyingJapamanPrefab（フォールバック）");
        }
        else
        {
            Debug.LogError("❌ 使用可能なPrefabがありません");
        }
    }

    /// <summary>
    /// ClickManagerのアニメーションをコピー（回転エフェクト強化版）
    /// </summary>
    private System.Collections.IEnumerator CopyClickManagerAnimation(GameObject throwPan, Vector3 startPos, Vector3 targetPos)
    {

        if (throwPan == null) yield break;

        // ClickManagerのThrowToFriendsMouthと同じ処理
        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);

        var rt = throwPan.GetComponent<UnityEngine.UI.Image>();
        float dur = 0.8f / speedMultiplier;
        float t = 0;

        // 🍞 回転設定
        float rotationSpeed = Random.Range(360f, 720f); // 1秒間に1-2回転
        bool clockwise = Random.Range(0, 2) == 0;
        if (!clockwise) rotationSpeed = -rotationSpeed;


        while (t < dur && throwPan != null)
        {
            t += Time.deltaTime;
            float progress = t / dur;

            // 放物線軌道（ClickManagerと同じ）
            float arc = Mathf.Sin(progress * Mathf.PI) * 100;
            Vector3 pos = Vector3.Lerp(startPos, targetPos, progress);
            pos.y += arc;

            throwPan.transform.position = pos;

            // 🍞 回転エフェクト追加
            throwPan.transform.Rotate(Vector3.forward, Time.deltaTime * rotationSpeed);

            // フェードアウト（ClickManagerと同じ）
            float fadeStartPoint = 0.7f;
            if (progress > fadeStartPoint && rt != null)
            {
                float fadeProgress = (progress - fadeStartPoint) / (1f - fadeStartPoint);
                rt.color = Color.Lerp(rt.color, new Color(1, 1, 1, 0.3f), fadeProgress);
            }

            // 🔥 定期的にログ出力（回転情報も含む）
            // if (Mathf.FloorToInt(t * 10) % 10 == 0) // 1秒ごと
            // {
            //     float currentRotation = throwPan.transform.eulerAngles.z;
            // }

            yield return null;
        }


        // 最終エフェクト（回転も継続）
        if (throwPan != null)
        {
            StartCoroutine(SimpleMouthEffectWithRotation(throwPan, speedMultiplier, rotationSpeed));
        }
    }

    /// <summary>
    /// 簡単な口エフェクト（回転継続版）
    /// </summary>
    private System.Collections.IEnumerator SimpleMouthEffectWithRotation(GameObject throwPan, float speedMultiplier, float rotationSpeed)
    {
        if (throwPan == null) yield break;


        float t = 0;
        Vector3 scale = throwPan.transform.localScale;
        var img = throwPan.GetComponent<UnityEngine.UI.Image>();
        Color startColor = img != null ? img.color : Color.white;

        float duration = 0.2f / speedMultiplier;

        while (t < duration && throwPan != null)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // スケール縮小
            throwPan.transform.localScale = Vector3.Lerp(scale, Vector3.zero, progress);

            // 🍞 回転継続（速度を徐々に落とす）
            float currentRotationSpeed = rotationSpeed * (1f - progress * 0.5f); // 最終的に半分の速度
            throwPan.transform.Rotate(Vector3.forward, Time.deltaTime * currentRotationSpeed);

            // フェードアウト
            if (img != null)
            {
                img.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), progress);
            }

            yield return null;
        }

        if (throwPan != null)
        {

            Destroy(throwPan);
        }

    }

    /// <summary>
    /// デバッグ用：回転アニメーションテスト
    /// </summary>
    [ContextMenu("🍞 回転アニメーションテスト")]
    public void DebugTestRotationAnimation()
    {
        CreateVisualThrowAnimationSimple();
    }

    /// <summary>
    /// 高速回転版のアニメーション（オプション）
    /// </summary>
    private System.Collections.IEnumerator CopyClickManagerAnimationFastRotation(GameObject throwPan, Vector3 startPos, Vector3 targetPos)
    {

        if (throwPan == null) yield break;

        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);

        var rt = throwPan.GetComponent<UnityEngine.UI.Image>();
        float dur = 0.8f / speedMultiplier;
        float t = 0;

        // 🍞 高速回転設定
        float rotationSpeed = Random.Range(720f, 1440f); // 1秒間に2-4回転
        bool clockwise = Random.Range(0, 2) == 0;
        if (!clockwise) rotationSpeed = -rotationSpeed;


        while (t < dur && throwPan != null)
        {
            t += Time.deltaTime;
            float progress = t / dur;

            // 放物線軌道
            float arc = Mathf.Sin(progress * Mathf.PI) * 100;
            Vector3 pos = Vector3.Lerp(startPos, targetPos, progress);
            pos.y += arc;

            throwPan.transform.position = pos;

            // 🍞 高速回転エフェクト
            throwPan.transform.Rotate(Vector3.forward, Time.deltaTime * rotationSpeed);

            // フェードアウト
            float fadeStartPoint = 0.7f;
            if (progress > fadeStartPoint && rt != null)
            {
                float fadeProgress = (progress - fadeStartPoint) / (1f - fadeStartPoint);
                rt.color = Color.Lerp(rt.color, new Color(1, 1, 1, 0.3f), fadeProgress);
            }

            yield return null;
        }


        if (throwPan != null)
        {
            StartCoroutine(SimpleMouthEffectWithRotation(throwPan, speedMultiplier, rotationSpeed));
        }
    }

    /// <summary>
    /// 高速回転版を使用する場合のメソッド（オプション）
    /// </summary>
    private void CreateVisualThrowAnimationFastRotation()
    {

        GameObject throwPan = null;
        string prefabName = "";

        if (japariPanPrefab != null)
        {
            throwPan = Instantiate(japariPanPrefab);
            prefabName = japariPanPrefab.name;
        }
        else if (clickManager != null && clickManager.mouthJapamanPrefab != null)
        {
            throwPan = Instantiate(clickManager.mouthJapamanPrefab);
            prefabName = clickManager.mouthJapamanPrefab.name;
        }
        else
        {
            return;
        }

        if (throwPan == null) return;

        // 親と位置設定
        if (clickManager != null && clickManager.spawnPoint != null && clickManager.spawnPoint.parent != null)
        {
            throwPan.transform.SetParent(clickManager.spawnPoint.parent);
        }

        Vector3 startPos = clickManager?.spawnPoint != null ? clickManager.spawnPoint.position : Vector3.zero;
        Vector3 targetPos = clickManager?.friendsMouthTarget != null ? clickManager.friendsMouthTarget.position : Vector3.zero;

        throwPan.transform.position = startPos;


        // 🔥 高速回転版アニメーション実行
        StartCoroutine(CopyClickManagerAnimationFastRotation(throwPan, startPos, targetPos));
    }

    /// <summary>
    /// デバッグ用：ClickManager設定確認
    /// </summary>
    [ContextMenu("🔍 ClickManager設定確認")]
    public void DebugClickManagerSettings()
    {
        Debug.Log("=== ClickManager設定確認 ===");

        if (clickManager == null)
        {
            Debug.LogError("❌ clickManagerがnull");
            return;
        }

        Debug.Log($"✅ ClickManager: {clickManager.gameObject.name}");
        Debug.Log($"  - spawnPoint: {(clickManager.spawnPoint != null ? clickManager.spawnPoint.name + " at " + clickManager.spawnPoint.position : "null")}");
        Debug.Log($"  - friendsMouthTarget: {(clickManager.friendsMouthTarget != null ? clickManager.friendsMouthTarget.name + " at " + clickManager.friendsMouthTarget.position : "null")}");
        Debug.Log($"  - flyingJapamanPrefab: {(clickManager.flyingJapamanPrefab != null ? clickManager.flyingJapamanPrefab.name : "null")}");
        Debug.Log($"  - mouthJapamanPrefab: {(clickManager.mouthJapamanPrefab != null ? clickManager.mouthJapamanPrefab.name : "null")}");

        // spawnPointの親階層確認
        if (clickManager.spawnPoint != null && clickManager.spawnPoint.parent != null)
        {
            Debug.Log($"  - spawnPoint親: {clickManager.spawnPoint.parent.name}");
        }
    }



    /// <summary>
    /// 適切な親オブジェクトを見つける
    /// </summary>
    private Transform FindAppropriateParent()
    {
        // 1. plateContainerを使用（既存のジャパまんと同じ階層）
        if (plateContainer != null)
        {
            // Debug.Log($"🔥 plateContainer使用: {plateContainer.name}");
            return plateContainer;
        }

        // 2. ClickManagerのplateContainerを使用
        if (clickManager != null && clickManager.plateContainer != null)
        {
            // Debug.Log($"🔥 ClickManager.plateContainer使用: {clickManager.plateContainer.name}");
            return clickManager.plateContainer;
        }

        // 3. Canvas直下を検索
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            // Debug.Log($"🔥 Canvas使用: {canvas.name}");
            return canvas.transform;
        }

        // 4. フォールバック
        Debug.LogWarning("🔥 適切な親が見つからないため、nullを返します");
        return null;
    }

    /// <summary>
    /// UI座標系での開始位置取得
    /// </summary>
    private Vector3 GetThrowStartPositionUI(GameObject throwPan)
    {
        // RectTransformを使用したUI座標での位置計算
        var panRect = throwPan.GetComponent<RectTransform>();
        if (panRect == null)
        {
            Debug.LogError("🔥 throwPanにRectTransformがありません");
            return Vector3.zero;
        }

        // 1. bakerySpawnPointがある場合（World→UI変換）
        if (bakerySpawnPoint != null)
        {
            Vector3 worldPos = bakerySpawnPoint.position;
            Vector2 uiPos = WorldToUIPosition(worldPos, panRect);
            // Debug.Log($"🔥 bakerySpawnPoint World: {worldPos} → UI: {uiPos}");
            return uiPos;
        }

        // 2. ClickManagerのspawnPointを使用
        if (clickManager != null && clickManager.spawnPoint != null)
        {
            Vector3 worldPos = clickManager.spawnPoint.position;
            Vector2 uiPos = WorldToUIPosition(worldPos, panRect);
            // Debug.Log($"🔥 ClickManager.spawnPoint World: {worldPos} → UI: {uiPos}");
            return uiPos;
        }

        // 3. 画面上部を使用
        Vector2 fallbackPos = new Vector2(0, 300f); // 画面中央上部
        // Debug.Log($"🔥 fallback UI位置: {fallbackPos}");
        return fallbackPos;
    }

    /// <summary>
    /// UI座標系での口の位置取得
    /// </summary>
    private Vector3 GetFriendsMouthPositionUI(GameObject throwPan)
    {
        var panRect = throwPan.GetComponent<RectTransform>();
        if (panRect == null)
        {
            return Vector3.zero;
        }

        // 1. ClickManagerのfriendsMouseTargetを使用
        if (clickManager != null && clickManager.friendsMouthTarget != null)
        {
            Vector3 worldPos = clickManager.friendsMouthTarget.position;
            Vector2 uiPos = WorldToUIPosition(worldPos, panRect);
            // Debug.Log($"🔥 friendsMouthTarget World: {worldPos} → UI: {uiPos}");
            return uiPos;
        }

        // 2. 名前で検索
        string[] targetNames = {
        "FriendsMouthTarget", "friendsMouthTarget", "MouthTarget",
        "mouth", "Mouth", "Friends Mouth", "Character Mouth"
    };

        foreach (string name in targetNames)
        {
            var target = GameObject.Find(name);
            if (target != null)
            {
                Vector3 worldPos = target.transform.position;
                Vector2 uiPos = WorldToUIPosition(worldPos, panRect);
                // Debug.Log($"🔥 検索で発見 {name} World: {worldPos} → UI: {uiPos}");
                return uiPos;
            }
        }

        // 3. フォールバック：画面中央
        Vector2 fallbackPos = new Vector2(0, 0);
        // Debug.Log($"🔥 口の位置フォールバック: {fallbackPos}");
        return fallbackPos;
    }

    /// <summary>
    /// World座標からUI座標への変換
    /// </summary>
    private Vector2 WorldToUIPosition(Vector3 worldPos, RectTransform uiRect)
    {
        var canvas = uiRect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("🔥 Canvasが見つかりません");
            return Vector2.zero;
        }

        var camera = canvas.worldCamera ?? Camera.main;

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);

        // スクリーン座標をUI座標に変換
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiRect.parent as RectTransform,
            screenPos,
            canvas.worldCamera,
            out uiPos
        );

        // Debug.Log($"🔥 座標変換: World{worldPos} → Screen{screenPos} → UI{uiPos}");
        return uiPos;
    }

    /// <summary>
    /// パンの位置を設定
    /// </summary>
    private void SetPanPosition(GameObject throwPan, Vector3 position)
    {
        var rectTransform = throwPan.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
            // Debug.Log($"🔥 RectTransform.anchoredPosition設定: {position}");
        }
        else
        {
            throwPan.transform.position = position;
            // Debug.Log($"🔥 Transform.position設定: {position}");
        }
    }

    /// <summary>
    /// パンの状態をログ出力
    /// </summary>
    private void LogPanState(GameObject throwPan, string label)
    {
        if (throwPan == null)
        {
            // Debug.Log($"🔥 {label}: パンがnull");
            return;
        }

        // Debug.Log($"🔥 {label}:");
        // Debug.Log($"  - GameObject: {throwPan.name}");
        // Debug.Log($"  - Active: {throwPan.activeInHierarchy}");
        // Debug.Log($"  - Transform.position: {throwPan.transform.position}");
        // Debug.Log($"  - Transform.localScale: {throwPan.transform.localScale}");

        var rectTransform = throwPan.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Debug.Log($"  - RectTransform.anchoredPosition: {rectTransform.anchoredPosition}");
            // Debug.Log($"  - RectTransform.sizeDelta: {rectTransform.sizeDelta}");
            // Debug.Log($"  - RectTransform.localScale: {rectTransform.localScale}");
        }

        var image = throwPan.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            // Debug.Log($"  - Image.color: {image.color}");
            // Debug.Log($"  - Image.enabled: {image.enabled}");
        }

        // Debug.Log($"  - Parent: {throwPan.transform.parent?.name ?? "null"}");
    }

    /// <summary>
    /// 投げ込みアニメーションのメインコルーチン（デバッグ強化版）
    /// </summary>
    private System.Collections.IEnumerator ThrowAnimationCoroutineDebug(GameObject throwPan, Vector3 startPos, Vector3 targetPos)
    {
        // Debug.Log("🔥 ThrowAnimationCoroutineDebug開始");

        if (throwPan == null)
        {
            Debug.LogError("🔥 throwPanがnull");
            yield break;
        }

        // 🔥 初期状態確認
        LogPanState(throwPan, "アニメーション開始時");

        // 🔥 残り時間に応じて速度調整
        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);
        float duration = 2.0f / speedMultiplier; // 時間を長めに設定してアニメーションを確認しやすく

        // Debug.Log($"🔥 アニメーション設定: 時間{duration:F1}秒, 速度倍率{speedMultiplier}x");

        // 🔥 放物線アニメーション（デバッグ版）
        yield return StartCoroutine(ThrowParabolicAnimationDebug(throwPan, startPos, targetPos, duration));

        // 🔥 口に到達時のエフェクト
        if (throwPan != null)
        {
            LogPanState(throwPan, "エフェクト開始前");
            yield return StartCoroutine(MouthReachEffect(throwPan, speedMultiplier));
        }

        // Debug.Log("🔥 ThrowAnimationCoroutineDebug完了");
    }

    /// <summary>
    /// 放物線アニメーション（デバッグ強化版）
    /// </summary>
    private System.Collections.IEnumerator ThrowParabolicAnimationDebug(GameObject throwPan, Vector3 startPos, Vector3 targetPos, float duration)
    {
        if (throwPan == null) yield break;

        // Debug.Log($"🔥 放物線アニメーション開始: {startPos} → {targetPos}, 時間{duration}秒");

        float elapsed = 0f;
        Vector3 direction = targetPos - startPos;
        float distance = direction.magnitude;

        // 🔥 弧の高さを距離に応じて調整
        float arcHeight = Mathf.Min(distance * 0.3f, 150f);
        // Debug.Log($"🔥 放物線設定: 距離{distance:F0}, 弧の高さ{arcHeight:F0}");

        int frameCount = 0;
        while (elapsed < duration && throwPan != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 🔥 放物線軌道計算
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            float arcOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
            currentPos.y += arcOffset;

            // 🔥 位置設定
            SetPanPosition(throwPan, currentPos);

            // 🔥 回転エフェクト
            float rotationSpeed = 180f; // 回転を遅くして確認しやすく
            throwPan.transform.Rotate(Vector3.forward, Time.deltaTime * rotationSpeed);

            // 🔥 定期的に状態をログ出力
            frameCount++;
            // if (frameCount % 30 == 0) // 30フレームごと（0.5秒間隔）
            // {
            //     Debug.Log($"🔥 アニメーション進行: {progress:P0}");
            //     Debug.Log($"  - 計算位置: {currentPos}");
            //     LogPanState(throwPan, $"フレーム{frameCount}");
            // }

            yield return null;
        }

        // Debug.Log("🔥 放物線アニメーション完了");
        if (throwPan != null)
        {
            LogPanState(throwPan, "アニメーション完了時");
        }
    }

    /// <summary>
    /// 口に到達時のエフェクト
    /// </summary>
    private System.Collections.IEnumerator MouthReachEffect(GameObject throwPan, float speedMultiplier)
    {
        if (throwPan == null) yield break;

        // Debug.Log("🔥 MouthReachEffect開始");

        float duration = 0.3f / speedMultiplier;
        float elapsed = 0f;

        Vector3 startScale = throwPan.transform.localScale;
        Color startColor = Color.white;

        // Image コンポーネントを取得
        var image = throwPan.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            startColor = image.color;
        }

        // 🔥 縮小 + フェードアウト
        while (elapsed < duration && throwPan != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // スケール縮小
            if (throwPan != null)
            {
                throwPan.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            }

            // フェードアウト
            if (image != null)
            {
                Color currentColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), progress);
                image.color = currentColor;
            }

            yield return null;
        }

        // 最終削除
        if (throwPan != null)
        {
            // Debug.Log("🔥 ジャパリパン削除");
            Destroy(throwPan);
        }

        // Debug.Log("🔥 MouthReachEffect完了");
    }

    /// <summary>
    /// 残り時間を取得
    /// </summary>
    private float GetRemainingTime()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            return gameManager.GetRemainingTime();
        }
        return 30f; // デフォルト値
    }

    /// <summary>
    /// 投げ込み速度の倍率計算
    /// </summary>
    private float CalculateThrowSpeedMultiplier(float remainingTime)
    {
        if (remainingTime <= 2f) return 4f;
        if (remainingTime <= 5f) return 3f;
        if (remainingTime <= 10f) return 2f;
        return 1f;
    }

    /// <summary>
    /// ステージに応じてジャパリパンのサイズを調整
    /// </summary>
    private void ApplyStageBasedScale(GameObject japariPan)
    {
        // Debug.Log("🔥 ApplyStageBasedScale開始");

        try
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("🔥 GameManagerが見つかりません");
                return;
            }

            int currentStage = gameManager.GetCurrentStage();
            // Debug.Log($"🔥 現在のステージ: {currentStage}");

            // ステージに応じたスケール計算（ClickManagerと同じ）
            float baseScale = 1.0f;
            float scaleReduction = (currentStage - 1) * 0.02f; // 1ステージごとに2%縮小
            float finalScale = Mathf.Clamp(baseScale - scaleReduction, 0.5f, 1.0f); // 最小50%

            // Debug.Log($"🔥 スケール計算: {baseScale} - {scaleReduction} = {finalScale}");

            // RectTransformのスケールを調整
            var rectTransform = japariPan.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * finalScale;
                // Debug.Log($"🔥 RectTransformスケール適用: {finalScale}");
            }
            else
            {
                // Transform直接調整
                japariPan.transform.localScale = Vector3.one * finalScale;
                // Debug.Log($"🔥 Transformスケール適用: {finalScale}");
            }

            // Debug.Log($"🔥 ステージ{currentStage} ジャパリパンスケール: {finalScale * 100f:F0}%");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔥 ApplyStageBasedScaleでエラー: {e.Message}");
        }

        // Debug.Log("🔥 ApplyStageBasedScale完了");
    }

    /// <summary>
    /// ジャパリパンの位置をセットアップ（改善版）
    /// </summary>
    private void SetupJapariPanPosition(GameObject japariPan)
    {
        // 器の設定を取得
        ContainerData containerData = null;
        if (ContainerSettings.Instance != null)
        {
            containerData = ContainerSettings.Instance.GetCurrentContainerData();
        }

        // 器のサイズ計算（複数の方法で取得を試行）
        float containerRadius = GetContainerRadius(containerData);

        // 位置計算（ClickManagerのロジックを踏襲）
        float centerBias = containerData != null ? containerData.centerBias : 0.1f;
        float maxRadius = containerData != null ? containerData.maxRadius : 0.8f;

        float startRadius = Random.Range(containerRadius * centerBias, containerRadius * maxRadius);
        float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector2 targetPos = new Vector2(
            Mathf.Cos(startAngle) * startRadius,
            0f
        );

        Vector2 startPos = new Vector2(targetPos.x, Random.Range(minDropHeight, maxDropHeight));

        var rt = japariPan.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchoredPosition = startPos;
        }

        // 🔥 PlateJapamanAnimatorと同じ落下アニメーション
        var anim = japariPan.GetComponent<PlateJapamanAnimator>();
        if (anim == null)
        {
            anim = japariPan.AddComponent<PlateJapamanAnimator>();
        }
        anim.StartGravityFall();

        // 🔥 ClickManagerのリストに追加（一緒に吸い込まれるように）
        AddToClickManagerList(anim);
    }

    /// <summary>
    /// 器の半径を取得（複数の方法で試行）
    /// </summary>
    private float GetContainerRadius(ContainerData containerData)
    {
        float containerRadius = 50f; // デフォルト値

        // 1. 自分のplateImageから取得
        if (plateImage != null)
        {
            Vector3 scale = plateImage.lossyScale;
            float width = plateImage.rect.width * scale.x;
            float multiplier = containerData != null ? containerData.sizeMultiplier : 0.8f;
            containerRadius = (width * multiplier) / 2f;
            return containerRadius;
        }

        // 2. Plateタグから検索
        var plateObject = GameObject.FindGameObjectWithTag("Plate");
        if (plateObject != null)
        {
            var plateRect = plateObject.GetComponent<RectTransform>();
            if (plateRect != null)
            {
                Vector3 scale = plateRect.lossyScale;
                float width = plateRect.rect.width * scale.x;
                float multiplier = containerData != null ? containerData.sizeMultiplier : 0.8f;
                containerRadius = (width * multiplier) / 2f;
                return containerRadius;
            }
        }

        // 3. plateContainerから推定
        if (plateContainer != null)
        {
            var containerRect = plateContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                float width = containerRect.rect.width;
                float multiplier = containerData != null ? containerData.sizeMultiplier : 0.8f;
                containerRadius = (width * multiplier) / 2f;
                return containerRadius;
            }
        }

        // 4. フォールバック：デフォルト値
        Debug.LogWarning("プレートサイズを取得できませんでした。デフォルト値を使用します。");
        return containerRadius;
    }

    /// <summary>
    /// ClickManagerのリストに追加（安全な方法）
    /// </summary>
    private void AddToClickManagerList(PlateJapamanAnimator anim)
    {
        if (clickManager == null) return;

        // 🔥 publicフィールドに直接アクセス
        if (clickManager.plateJapamanList != null)
        {
            clickManager.plateJapamanList.Add(anim);
            // Debug.Log("ジャパリパンをClickManagerの吸い込みリストに追加しました");
            return;
        }

        Debug.LogWarning("ClickManagerのplateJapamanListにアクセスできませんでした");
    }

    /// <summary>
    /// デフォルトのジャパリパンPrefabを作成
    /// </summary>
    private void CreateDefaultJapariPanPrefab()
    {
        // 🔥 フランスパン風の見た目でPrefab作成
        japariPanPrefab = new GameObject("JapariPan");

        // UI Image コンポーネント追加
        var image = japariPanPrefab.AddComponent<UnityEngine.UI.Image>();

        // 🔥 フランスパン色（薄茶色）
        image.color = new Color(0.9f, 0.7f, 0.4f, 1f);

        // RectTransform設定
        var rectTransform = japariPanPrefab.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(40, 15); // 横長のフランスパン形状

        // JapariPanコンポーネント
        japariPanPrefab.AddComponent<JapariPan>();

        // PlateJapamanAnimator（落下アニメーション用）
        japariPanPrefab.AddComponent<PlateJapamanAnimator>();

        // Debug.Log("🍞 デフォルトジャパリパンPrefab作成完了（フランスパン風）");
    }

    /// <summary>
    /// パン焼きエフェクト再生
    /// </summary>
    private void PlayBakingEffects()
    {
        // 焼き音再生
        if (bakingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bakingSound, 0.3f);
        }

        // 落下音再生（少し遅延）
        if (dropSound != null && audioSource != null)
        {
            StartCoroutine(PlayDelayedDropSound());
        }
    }

    /// <summary>
    /// 遅延落下音再生
    /// </summary>
    private IEnumerator PlayDelayedDropSound()
    {
        yield return new WaitForSeconds(0.5f);
        if (dropSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropSound, 0.2f);
        }
    }

    /// <summary>
    /// 統計情報取得
    /// </summary>
    public DonkeyBakeryStats GetStats()
    {
        return new DonkeyBakeryStats
        {
            level = currentLevel,
            panValue = currentPanValue,
            productionInterval = currentInterval,
            totalPansProduced = totalPansProduced,
            totalValueProduced = totalValueProduced,
            isActive = isBakingActive
        };
    }

    /// <summary>
    /// デバッグ用：統計表示
    /// </summary>
    [ContextMenu("🫏 ロバのパン屋統計表示")]
    public void DebugShowStats()
    {
        var stats = GetStats();
        Debug.Log($"=== ロバのパン屋統計 ===");
        Debug.Log($"レベル: {stats.level}");
        Debug.Log($"パン価値: {stats.panValue:F0}");
        Debug.Log($"生産間隔: {stats.productionInterval:F1}秒");
        Debug.Log($"総生産数: {stats.totalPansProduced}個");
        Debug.Log($"総価値: {stats.totalValueProduced:F0}");
        Debug.Log($"稼働状態: {(stats.isActive ? "稼働中" : "停止中")}");
    }

    /// <summary>
    /// デバッグ用：手動パン生産
    /// </summary>
    [ContextMenu("🍞 手動パン生産")]
    public void DebugManualProducePan()
    {
        if (Application.isPlaying && currentLevel > 0)
        {
            ProducePan();
        }
    }

    /// <summary>
    /// デバッグ用：投げ込みアニメーションテスト
    /// </summary>
    [ContextMenu("🎬 投げ込みアニメーションテスト")]
    public void DebugTestThrowAnimation()
    {
        if (Application.isPlaying)
        {
            CreateVisualThrowAnimationSimple();
        }
    }

    private void OnDestroy()
    {
        StopBakery();
    }

    /// <summary>
    /// まとめる係からのブースト効果を適用（生産速度版）
    /// </summary>
    public void ApplyOrganizerBoost(float multiplier)
    {
        // Debug.Log($"🔍 まとめる係ブースト適用 - 生産速度: {multiplier:F1}x");

        organizerBoostMultiplier = multiplier;
        isOrganizerBoostActive = multiplier > 1f;

        // 現在の生産間隔を表示
        if (isOrganizerBoostActive)
        {
            float boostedInterval = currentInterval / organizerBoostMultiplier;
            // Debug.Log($"🍞 生産間隔変更: {currentInterval:F1}秒 → {boostedInterval:F1}秒");
        }
    }

    /// <summary>
    /// まとめる係のブースト効果をリセット（デバッグ強化版）
    /// </summary>
    public void ResetOrganizerBoost()
    {
        // Debug.Log($"🔍 ResetOrganizerBoost呼び出し - 前: {organizerBoostMultiplier:F1} → 後: 1.0");

        organizerBoostMultiplier = 1f;
        isOrganizerBoostActive = false;

        // Debug.Log($"🔍 ブーストリセット完了");
    }

    /// <summary>
    /// デバッグ用: ブースト状態確認
    /// </summary>
    [ContextMenu("🔍 ブースト状態確認")]
    public void DebugBoostStatus()
    {
        Debug.Log("=== DonkeyBakery ブースト状態 ===");
        Debug.Log($"organizerBoostMultiplier: {organizerBoostMultiplier:F1}");
        Debug.Log($"isOrganizerBoostActive: {isOrganizerBoostActive}");
        Debug.Log($"currentPanValue: {currentPanValue:F1}");
        Debug.Log($"計算されるブースト価値: {currentPanValue * organizerBoostMultiplier:F1}");
    }

    /// <summary>
    /// デバッグ用: 生産速度確認
    /// </summary>
    [ContextMenu("🔍 生産速度確認")]
    public void DebugProductionSpeed()
    {
        Debug.Log("=== ロバのパン屋 生産速度確認 ===");
        Debug.Log($"基本間隔: {currentInterval:F1}秒");
        Debug.Log($"ブースト倍率: {organizerBoostMultiplier:F1}x");
        Debug.Log($"ブースト中: {isOrganizerBoostActive}");

        if (isOrganizerBoostActive)
        {
            float boostedInterval = currentInterval / organizerBoostMultiplier;
            Debug.Log($"実際の間隔: {boostedInterval:F1}秒");
            Debug.Log($"1分間の生産数: 基本{60f / currentInterval:F1}個 → ブースト{60f / boostedInterval:F1}個");
        }
    }
    /// <summary>
    /// 現在の生産レートを取得（まとめる係のブーストを含む）
    /// </summary>
    public float GetCurrentProductionRate()
    {
        if (!isBakingActive || currentLevel <= 0) return 0f;

        // 基本生産レート（1秒あたりの生産数）
        float baseRate = 1f / currentInterval;

        // まとめる係のブーストを適用
        if (isOrganizerBoostActive && organizerBoostMultiplier > 1f)
        {
            float boostedRate = baseRate * organizerBoostMultiplier;
            // Debug.Log($"🔍 生産レート計算: 基本{baseRate:F2}/秒 → ブースト{boostedRate:F2}/秒 ({organizerBoostMultiplier:F1}x)");
            return boostedRate;
        }

        return baseRate;
    }

    /// <summary>
    /// 現在の生産間隔を取得（まとめる係のブーストを含む）
    /// </summary>
    public float GetCurrentProductionInterval()
    {
        if (!isBakingActive || currentLevel <= 0) return 0f;

        // まとめる係のブーストを適用
        if (isOrganizerBoostActive && organizerBoostMultiplier > 1f)
        {
            float boostedInterval = currentInterval / organizerBoostMultiplier;
            return boostedInterval;
        }

        return currentInterval;
    }

    /// <summary>
    /// 現在のパン価値を取得
    /// </summary>
    public float GetCurrentPanValue()
    {
        return currentPanValue;
    }

    /// <summary>
    /// パン屋がアクティブかどうか
    /// </summary>
    public bool IsActive()
    {
        return isBakingActive && currentLevel > 0;
    }
    /// <summary>
    /// デバッグ用: 詳細なパン屋状態表示
    /// </summary>
    [ContextMenu("🔍 詳細パン屋状態")]
    public void DebugDetailedBakeryState()
    {
        Debug.Log("=== ロバのパン屋 詳細状態 ===");
        Debug.Log($"レベル: {currentLevel}");
        Debug.Log($"アクティブ: {isBakingActive}");
        Debug.Log($"基本間隔: {currentInterval:F1}秒");
        Debug.Log($"基本パン価値: {currentPanValue:F1}");
        Debug.Log($"ブースト中: {isOrganizerBoostActive}");
        Debug.Log($"ブースト倍率: {organizerBoostMultiplier:F1}x");

        if (isOrganizerBoostActive)
        {
            float boostedInterval = GetCurrentProductionInterval();
            float boostedRate = GetCurrentProductionRate();
            Debug.Log($"ブースト後間隔: {boostedInterval:F1}秒");
            Debug.Log($"ブースト後レート: {boostedRate:F2}/秒");

            // 1分間の生産予測
            float baseProduction = 60f / currentInterval;
            float boostedProduction = 60f / boostedInterval;
            Debug.Log($"1分間生産予測: {baseProduction:F1}個 → {boostedProduction:F1}個");
        }

        Debug.Log($"総生産数: {totalPansProduced}個");
        Debug.Log($"総価値: {totalValueProduced:F1}");
    }

}





/// <summary>
/// ロバのパン屋統計情報
/// </summary>
[System.Serializable]
public class DonkeyBakeryStats
{
    public int level;
    public float panValue;
    public float productionInterval;
    public int totalPansProduced;
    public float totalValueProduced;
    public bool isActive;
}