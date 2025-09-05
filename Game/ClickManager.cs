using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ClickManager : MonoBehaviour
{
    public long japamanCount = 0, extraJapamanCount = 0, goalCount = 50;
    public GameObject flyingJapamanPrefab, plateJapamanPrefab, mouthJapamanPrefab;
    public Transform spawnPoint, plateContainer, friendsMouthTarget;
    public AudioClip clickSound, mouthFeedSound;
    public Button clickButton; // インスペクターでButton_ClickJapamanを設定

    [Header("アップグレード効果")]
    public int clickMultiplier = 1;        // クリック倍率
    public float autoProductionRate = 0f;  // 自動生産レート（秒間）
    public float autoClickRate = 0f;       // 自動クリックレート（秒間）

    [Header("ジャパまん落下設定")]
    public float globalMinDropHeight = 500f;  // 全ステージ共通最小落下高度
    public float globalMaxDropHeight = 800f;  // 全ステージ共通最大落下高度

    [Header("クリック位置記録")]
    private Vector3 lastClickPosition = Vector3.zero;
    private Camera uiCamera; // UI用カメラ参照

    [Header("追加フィールド")]
    private long totalJapamanProduced = 0; // 総生産数
    

    [Header("まとめる係連携")]
    [SerializeField] private OrganizerManager organizerManager;
    private float autoClickTimer = 0f;

    private float lastAutoProduction = 0f;

    [Header("一時的ブースト管理")]
    private float temporaryBoostMultiplier = 1f;
    private float temporaryBoostEndTime = 0f;
    private bool isTemporaryBoostActive = false;

    // クリック制御フラグ
    private bool clickEnabled = true;

    private AudioSource audioSource;
    public List<PlateJapamanAnimator> plateJapamanList = new List<PlateJapamanAnimator>(); private int japamanOnPlate = 0;

    // ClickManagerクラスに追加するフィールド
    [SerializeField] private RectTransform plateImage; // InspectorでPlateのImageをアサイン

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // OrganizerManagerを取得
        organizerManager = FindFirstObjectByType<OrganizerManager>();
        if (organizerManager != null)
        {
            Debug.Log("🔧 ClickManager: OrganizerManagerを取得しました");
        }
        else
        {
            Debug.Log("🔧 ClickManager: OrganizerManagerが見つかりません（後で作成される予定）");
        }
    }

    /// <summary>
    /// 一時的ブーストを適用（まとめる係用）
    /// </summary>
    /// <param name="multiplier">ブースト倍率</param>
    /// <param name="duration">持続時間（秒）</param>
    public void ApplyTemporaryBoost(float multiplier, float duration)
    {
        temporaryBoostMultiplier = multiplier;
        temporaryBoostEndTime = Time.time + duration;
        isTemporaryBoostActive = true;

        Debug.Log($"🚀 一時的ブースト適用: x{multiplier} for {duration}秒");

        // 自動生産レートを即座に再計算
        RecalculateAutoProductionRate();
    }

    /// <summary>
    /// 現在のブースト倍率を取得（一時的ブーストを含む）
    /// </summary>
    private float GetCurrentBoostMultiplier()
    {
        float totalMultiplier = 1f;

        // 一時的ブースト（まとめる係）
        if (isTemporaryBoostActive)
        {
            if (Time.time < temporaryBoostEndTime)
            {
                totalMultiplier *= temporaryBoostMultiplier;
            }
            else
            {
                // ブースト期間終了
                isTemporaryBoostActive = false;
                temporaryBoostMultiplier = 1f;
                Debug.Log("🚀 一時的ブースト終了");

                // 自動生産レートを再計算
                RecalculateAutoProductionRate();
            }
        }

        return totalMultiplier;
    }

    /// <summary>
    /// 自動生産レート再計算（一時的ブーストを考慮）
    /// </summary>
    private void RecalculateAutoProductionRate()
    {
        // 基本自動生産レートを計算
        float baseRate = CalculateBaseAutoProductionRate();

        // 一時的ブーストを適用
        float boostMultiplier = GetCurrentBoostMultiplier();
        autoProductionRate = baseRate * boostMultiplier;

        Debug.Log($"🔄 自動生産レート更新: {baseRate} x {boostMultiplier} = {autoProductionRate}/秒");
    }

    /// <summary>
    /// 基本自動生産レート計算（既存のロジックを使用）
    /// </summary>
    private float CalculateBaseAutoProductionRate()
    {
        // 既存のautoProductionRate計算ロジックをここに移動
        // または既存の計算結果を一時的ブースト適用前の値として使用

        float baseRate = 0f;

        // 工場アップグレードの効果
        if (UpgradeManager.Instance != null)
        {
            var factoryUpgrade = UpgradeManager.Instance.GetUpgradeByType(UpgradeType.Factory);
            if (factoryUpgrade != null && factoryUpgrade.currentLevel > 0)
            {
                baseRate += factoryUpgrade.GetCurrentEffect();
            }

            // その他の自動生産系アップグレードも同様に追加
            var helperUpgrade = UpgradeManager.Instance.GetUpgradeByType(UpgradeType.HelperFriend);
            if (helperUpgrade != null && helperUpgrade.currentLevel > 0)
            {
                baseRate += helperUpgrade.GetCurrentEffect();
            }

            var donkeyUpgrade = UpgradeManager.Instance.GetUpgradeByType(UpgradeType.DonkeyBakery);
            if (donkeyUpgrade != null && donkeyUpgrade.currentLevel > 0)
            {
                baseRate += donkeyUpgrade.GetCurrentEffect();
            }

            var robaUpgrade = UpgradeManager.Instance.GetUpgradeByType(UpgradeType.RobaBakery);
            if (robaUpgrade != null && robaUpgrade.currentLevel > 0)
            {
                baseRate += robaUpgrade.GetCurrentEffect();
            }
        }

        return baseRate;
    }

    /// <summary>
    /// Update内で一時的ブーストの状態をチェック
    /// 既存のUpdateメソッドに以下を追加
    /// </summary>
    private void UpdateTemporaryBoost()
    {
        if (isTemporaryBoostActive && Time.time >= temporaryBoostEndTime)
        {
            // ブースト終了処理
            isTemporaryBoostActive = false;
            temporaryBoostMultiplier = 1f;

            Debug.Log("🚀 一時的ブースト自動終了");
            RecalculateAutoProductionRate();
        }
    }
    void Update()
    {
        // 1. カウントダウン中やクリック無効時は自動機能も停止
        if (!clickEnabled) return;

        // 2. ゲーム状態チェック：プレイ中のみ自動機能を有効
        if (GameManager.Instance == null || !GameManager.Instance.CanAutoProduction())
        {
            return;
        }

        // 一時的ブースト状態をチェック
        UpdateTemporaryBoost();

        // 3. 自動生産処理（プレイ中のみ）
        if (autoProductionRate > 0f)
        {
            if (Time.time - lastAutoProduction >= 1f / autoProductionRate)
            {
                AutoProduceJapaman();
                lastAutoProduction = Time.time;
            }
        }

        // 4. 🔥 新しい自動クリック処理のみ残す
        ProcessAutoClick();

        // 🔥 古い自動クリック処理は削除
        /*
        if (autoClickRate > 0f)
        {
            if (Time.time - lastAutoClick >= 1f / autoClickRate)
            {
                AutoClick();
                lastAutoClick = Time.time;
            }
        }
        */
    }


    /// <summary>
    /// SetClickEnabled メソッド（ログ削減版）
    /// </summary>
    public void SetClickEnabled(bool enabled)
    {
        clickEnabled = enabled;

        // ボタンの見た目も制御
        if (clickButton != null)
        {
            clickButton.interactable = enabled;
        }

        // ログは重要な変更時のみ
        if (enabled)
        {
            Debug.Log("🎮 ゲーム開始");
        }
        else
        {
            Debug.Log("⏸️ クリック無効");
        }
    }
    /// <summary>
    /// クリック位置を記録（ボタン位置ベース）
    /// </summary>
    private void RecordClickPosition()
    {
        // 🔥 シンプル版：ボタンの位置を使用
        if (clickButton != null)
        {
            lastClickPosition = clickButton.transform.position;

            // ボタンの範囲内でランダムに位置を調整
            RectTransform buttonRect = clickButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-buttonRect.rect.width * 0.3f, buttonRect.rect.width * 0.3f),
                    Random.Range(-buttonRect.rect.height * 0.3f, buttonRect.rect.height * 0.3f),
                    0
                );
                lastClickPosition += randomOffset;
            }
        }
        else
        {
            // フォールバック：spawnPointを使用
            if (spawnPoint != null)
            {
                lastClickPosition = spawnPoint.position;
            }
            else
            {
                lastClickPosition = Vector3.zero;
            }
        }

        ///Debug.Log($"クリック位置記録（ボタンベース）: {lastClickPosition}");
    }


    /// <summary>
    /// ラウンド開始時の処理
    /// </summary>
    public void OnRoundStart()
    {
        // まとめる係のタイマーリセット
        if (organizerManager != null)
        {
            organizerManager.OnRoundStart();
        }
    }

    /// <summary>
    /// 新ステージ開始時の処理
    /// </summary>
    public void OnNewStage()
    {
        // まとめる係の完全リセット
        if (organizerManager != null)
        {
            organizerManager.OnNewStage();
        }
    }


    public void OnClick() // 既存のメソッド名はそのまま
    {
        OnPlayerClick(); // 新しいメソッドを呼び出す
    }

    /// <summary>
    /// 複数個のジャパまんを皿に追加（処理負荷軽減版）
    /// </summary>
    private void AddMultipleJapamanToPlate(int count)
    {
        ///Debug.Log($"★★★ 複数個ジャパまん追加: {count}個 ★★★");

        // 処理負荷を考慮した制限
        int actualCount = Mathf.Min(count, 10); // 最大10個まで
        if (count > 10)
        {
            ///Debug.Log($"処理負荷軽減のため {count}個を{actualCount}個に制限");
        }

        for (int i = 0; i < actualCount; i++)
        {
            if (japamanOnPlate >= goalCount) break;

            GameObject j = Instantiate(plateJapamanPrefab, plateContainer);

            // ステージに応じてジャパまんサイズを調整
            ApplyStageBasedScale(j);

            // 器の設定を取得（既存のコード）
            ContainerData containerData = null;
            if (ContainerSettings.Instance != null)
            {
                containerData = ContainerSettings.Instance.GetCurrentContainerData();
            }

            // 🔥 複数個の場合は少し散らばらせる
            float containerRadius = 50f;
            if (plateImage != null)
            {
                RectTransform plateRect = plateImage.GetComponent<RectTransform>();
                if (plateRect != null)
                {
                    Vector3 scale = plateRect.lossyScale;
                    float width = plateImage.rect.width * scale.x;
                    float multiplier = containerData != null ? containerData.sizeMultiplier : 0.8f;
                    containerRadius = (width * multiplier) / 2f;
                }
            }

            // 位置計算（少し散らばりを追加）
            float centerBias = containerData != null ? containerData.centerBias : 0.1f;
            float maxRadius = containerData != null ? containerData.maxRadius : 0.8f;

            // 複数個の場合は少し位置をずらす
            float radiusOffset = (i % 3) * 5f; // 3個ごとに半径を少しずらす
            float startRadius = Random.Range(containerRadius * centerBias, containerRadius * maxRadius) + radiusOffset;
            float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            float minDropHeight = globalMinDropHeight;
            float maxDropHeight = globalMaxDropHeight;

            Vector2 targetPos = new Vector2(
                Mathf.Cos(startAngle) * startRadius,
                0f
            );

            Vector2 startPos = new Vector2(targetPos.x, Random.Range(minDropHeight, maxDropHeight));

            var rt = j.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchoredPosition = startPos;
            }

            var anim = j.GetComponent<PlateJapamanAnimator>() ?? j.AddComponent<PlateJapamanAnimator>();

            // 🔥 少し時間差で落下開始（処理分散）
            StartCoroutine(DelayedGravityFall(anim, i * 0.05f));

            plateJapamanList.Add(anim);
            japamanOnPlate++;
        }

       /// Debug.Log($"皿にジャパまん{actualCount}個追加完了");
    }

    /// <summary>
    /// 時間差落下処理
    /// </summary>
    private IEnumerator DelayedGravityFall(PlateJapamanAnimator anim, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (anim != null)
        {
            anim.StartGravityFall();
        }
    }

    /// <summary>
    /// 複数個の飛行アニメーション
    /// </summary>
    private IEnumerator MultipleFlyJapaman(long count)
    {
        int actualCount = Mathf.Min((int)count, 5); // 飛行アニメーションは最大5個

        for (int i = 0; i < actualCount; i++)
        {
            StartCoroutine(FlyJapaman());
            yield return new WaitForSeconds(0.1f); // 0.1秒間隔で発射
        }
    }

    /// <summary>
    /// 複数個の投げ込みアニメーション
    /// </summary>
    private IEnumerator MultipleThrowToFriendsMouth(long count, float speedMultiplier)
    {
        int actualCount = Mathf.Min((int)count, 5); // 投げ込みアニメーションは最大5個

        for (int i = 0; i < actualCount; i++)
        {
            StartCoroutine(ThrowToFriendsMouth());
            yield return new WaitForSeconds(0.1f / speedMultiplier); // 速度に応じた間隔
        }
    }

    /// <summary>
    /// クリック位置からの複数個飛行アニメーション
    /// </summary>
    private IEnumerator MultipleFlyJapamanFromClick(long count)
    {
        int actualCount = Mathf.Min((int)count, 8); // 最大8個まで

        for (int i = 0; i < actualCount; i++)
        {
            StartCoroutine(FlyJapamanFromClick(i, actualCount));
            yield return new WaitForSeconds(0.05f); // 0.05秒間隔で発射
        }
    }

    /// <summary>
    /// クリック位置からランダム方向に真っ直ぐ飛行
    /// </summary>
    private IEnumerator FlyJapamanFromClick(int index, int totalCount)
    {
        // クリック位置から開始
        Vector3 startPosition = lastClickPosition;

        // フォールバック：spawnPointがある場合はそれを使用
        if (startPosition == Vector3.zero && spawnPoint != null)
        {
            startPosition = spawnPoint.position;
        }

        var obj = Instantiate(flyingJapamanPrefab, startPosition, Quaternion.identity, spawnPoint?.parent);
        var rt = obj.GetComponent<Image>();

        Vector3 start = startPosition;

        // 🔥 ランダム方向に真っ直ぐ飛ぶ終点を計算
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(100f, 200f);
        Vector3 end = start + (Quaternion.Euler(0, 0, angle) * Vector3.right) * distance;

        // 一定時間で飛行
        float duration = Random.Range(0.5f, 0.8f);
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // 🔥 シンプルな直線移動
            obj.transform.position = Vector3.Lerp(start, end, progress);

            // 🔥 シンプルな回転
            obj.transform.rotation = Quaternion.Euler(0, 0, t * 360f);

            // フェードアウト
            rt.color = Color.Lerp(rt.color, new Color(1, 1, 1, 0), t / duration);

            yield return null;
        }

        Destroy(obj);
    }

    /// <summary>
    /// ランダムな終点位置を計算
    /// </summary>


    private IEnumerator ShakeButton()
    {
        Transform buttonTransform = clickButton.transform;
        float duration = 0.2f, intensity = 10f, t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 50f) * intensity * (1 - t / duration);
            buttonTransform.rotation = Quaternion.Euler(0, 0, shake);
            yield return null;
        }

        buttonTransform.rotation = Quaternion.identity;
    }

    private IEnumerator FlyJapaman()
    {
        // 新しいメソッドを呼び出し
        yield return StartCoroutine(FlyJapamanFromClick(0, 1));
    }

    private IEnumerator ThrowToFriendsMouth()
    {
        // 残り時間に応じて投げ込み速度を調整
        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);

        // フレンズの飲み込みアニメーション（連打対応）
        if (CharacterSwallowAnimator.Instance != null)
        {
            // 連打時も常にアニメーションを開始（既に動いていても継続）
            float swallowSpeed = 3f * speedMultiplier;
            int swallowCount = 2;

            CharacterSwallowAnimator.Instance.SetSwallowSettings(15f, swallowSpeed, swallowCount);
            CharacterSwallowAnimator.Instance.StartSwallowAnimation();
            Debug.Log("直接投げ込み用飲み込みアニメーション開始（連打対応）");
        }

        var obj = Instantiate(mouthJapamanPrefab ?? flyingJapamanPrefab, spawnPoint.position, Quaternion.identity, spawnPoint.parent);
        obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // 直接投げ込み用ジャパまんにもステージスケーリングを適用
        ApplyStageBasedScale(obj);

        var rt = obj.GetComponent<Image>();

        Vector3 start = obj.transform.position, end = friendsMouthTarget.position;
        float dur = 0.8f / speedMultiplier; // 速度倍率を適用
        float t = 0;

        while (t < dur)
        {
            t += Time.deltaTime;
            float progress = t / dur;
            float arc = Mathf.Sin(progress * Mathf.PI) * 100;
            Vector3 pos = Vector3.Lerp(start, end, progress);
            pos.y += arc;
            obj.transform.position = pos;

            // フェードアウトのタイミングも調整
            float fadeStartPoint = 0.7f;
            if (progress > fadeStartPoint)
            {
                float fadeProgress = (progress - fadeStartPoint) / (1f - fadeStartPoint);
                rt.color = Color.Lerp(rt.color, new Color(1, 1, 1, 0.3f), fadeProgress);
            }
            yield return null;
        }

        StartCoroutine(MouthFeedEffect(obj, speedMultiplier));

        // 停止処理は呼ばない（連打時は継続させる）
        // 他の投げ込みがない場合のみ、一定時間後に自然停止
        yield return new WaitForSeconds(1f); // 1秒待って他の投げ込みがなければ停止
    }

    private IEnumerator MouthFeedEffect(GameObject obj, float speedMultiplier = 1f)
    {
        float t = 0;
        Vector3 scale = obj.transform.localScale;
        Image img = obj.GetComponent<Image>(); // Image取得
        Color startColor = img.color;

        float duration = 0.2f / speedMultiplier; // 速度倍率を適用

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            obj.transform.localScale = Vector3.Lerp(scale, Vector3.zero, progress);

            // フェードアウト追加
            if (img != null)
                img.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), progress);

            yield return null;
        }
        Destroy(obj);
    }

    // AddJapamanToPlateメソッド - 最新版
    private void AddJapamanToPlate()
    {
        Debug.Log("★★★ 新しいコードが実行されています！ ★★★");

        if (japamanOnPlate >= goalCount) return;

        GameObject j = Instantiate(plateJapamanPrefab, plateContainer);

        // ステージに応じてジャパまんサイズを調整
        ApplyStageBasedScale(j);

        // 器の設定を取得
        ContainerData containerData = null;
        if (ContainerSettings.Instance != null)
        {
            containerData = ContainerSettings.Instance.GetCurrentContainerData();
        }

        // 器のサイズ計算
        float containerRadius = 50f; // デフォルト値
        if (plateImage != null)
        {
            RectTransform plateRect = plateImage.GetComponent<RectTransform>();
            if (plateRect != null)
            {
                Vector3 scale = plateRect.lossyScale;
                float width = plateImage.rect.width * scale.x;

                // 器の設定を使用
                float multiplier = containerData != null ? containerData.sizeMultiplier : 0.8f;
                containerRadius = (width * multiplier) / 2f;
            }
        }

        // 器の設定に基づいて位置計算
        float centerBias = containerData != null ? containerData.centerBias : 0.1f;
        float maxRadius = containerData != null ? containerData.maxRadius : 0.8f;

        // ★★★ Inspector で調整可能な超高落下位置 ★★★
        float minDropHeight = globalMinDropHeight;
        float maxDropHeight = globalMaxDropHeight;

       
        // 落下位置計算
        float startRadius = Random.Range(containerRadius * centerBias, containerRadius * maxRadius);
        float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dropHeight = Random.Range(minDropHeight, maxDropHeight);


        Vector2 targetPos = new Vector2(
            Mathf.Cos(startAngle) * startRadius,
            0f
        );

        Vector2 startPos = new Vector2(targetPos.x, dropHeight);


        var rt = j.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchoredPosition = startPos;
        }

        var anim = j.GetComponent<PlateJapamanAnimator>() ?? j.AddComponent<PlateJapamanAnimator>();
        anim.StartGravityFall();

        plateJapamanList.Add(anim);
        japamanOnPlate++;

        // デバッグログ
        string containerName = containerData != null ? containerData.containerName : "デフォルト";
    }

    /// <summary>
    /// ステージに応じてジャパまんのサイズを調整
    /// </summary>
    private void ApplyStageBasedScale(GameObject japaman)
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null) return;

        int currentStage = gameManager.GetCurrentStage();

        // ステージに応じたスケール計算
        float baseScale = 1.0f;
        float scaleReduction = (currentStage - 1) * 0.02f; // 1ステージごとに2%縮小
        float finalScale = Mathf.Clamp(baseScale - scaleReduction, 0.5f, 1.0f); // 最小50%

        // RectTransformのスケールを調整
        var rectTransform = japaman.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * finalScale;
        }

        // PlateJapamanAnimatorの当たり判定も調整
        var animator = japaman.GetComponent<PlateJapamanAnimator>();
        if (animator != null)
        {
            // 元の当たり判定にスケールを適用
            float baseCollisionRadius = 20f; // デフォルト値
            animator.SetCollisionRadius(baseCollisionRadius * finalScale);
        }

    }

    private IEnumerator DelayedSuck()
    {
        yield return new WaitForSeconds(0.5f);
        StartSuckAllJapamanToMouth();
    }

    // 🔥 パブリックメソッドとして公開
    public void TriggerDelayedSuck()
    {
        StartCoroutine(DelayedSuck());
    }

    public void StartSuckAllJapamanToMouth() => StartCoroutine(SuckJapamanCoroutine());

    private IEnumerator SuckJapamanCoroutine()
    {
        RectTransform mouth = friendsMouthTarget.GetComponent<RectTransform>();
        RectTransform container = plateContainer.GetComponent<RectTransform>();
        if (mouth == null || container == null)
        {
            Debug.LogError("RectTransformが見つかりません");
            yield break;
        }

        Vector2 localMouthPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container,
            RectTransformUtility.WorldToScreenPoint(null, mouth.position),
            null,
            out localMouthPos
        );

        // 残り時間に応じた速度調整
        float remainingTime = GetRemainingTime();
        float speedMultiplier = CalculateSuckSpeedMultiplier(remainingTime);

        // キャラクターの飲み込みアニメーション開始（速度調整付き）
        if (CharacterSwallowAnimator.Instance != null)
        {
            // 速度に応じて飲み込み設定を調整
            float swallowSpeed = 3f * speedMultiplier;
            int swallowCount = Mathf.Max(1, Mathf.RoundToInt(5f / speedMultiplier));
            CharacterSwallowAnimator.Instance.SetSwallowSettings(20f, swallowSpeed, swallowCount);
            CharacterSwallowAnimator.Instance.StartSwallowAnimation();
        }

        List<Coroutine> suckCoroutines = new List<Coroutine>();

        // 🆕 吸い込み間隔をさらに短縮
        float suckInterval = 0.03f / speedMultiplier;

        foreach (var j in plateJapamanList)
        {
            if (j != null)
            {
                // 🆕 フェードアウト対応版のSuckSingleJapamanを呼び出し
                Coroutine suckCoroutine = StartCoroutine(SuckSingleJapaman(j, localMouthPos, speedMultiplier));
                suckCoroutines.Add(suckCoroutine);
                yield return new WaitForSeconds(suckInterval);
            }
        }

        // 🆕 待機時間も短縮（フェードアウトが早いため）
        float waitTime = 0.2f / speedMultiplier; // さらに短縮
        yield return new WaitForSeconds(waitTime);

        // キャラクターの飲み込みアニメーション終了
        if (CharacterSwallowAnimator.Instance != null)
        {
            CharacterSwallowAnimator.Instance.StopSwallowAnimation();
        }

        plateJapamanList.Clear();
        japamanOnPlate = 0;
    }





    // ClickManager.cs の吸い込み処理を移動中フェードアウト対応に修正

    /// <summary>
    /// 個別のジャパまんを吸い込む処理（移動中フェードアウト版）
    /// </summary>
    private IEnumerator SuckSingleJapaman(PlateJapamanAnimator japaman, Vector2 targetPos, float speedMultiplier = 1f)
    {
        if (japaman == null || japaman.gameObject == null)
        {
            yield break;
        }

        // 🔥 吸い込み開始を通知
        japaman.StartConsumption();

        float moveDuration = 0.4f / speedMultiplier;

        // 🆕 移動中フェードアウト対応版の軌道移動を呼び出し
        yield return StartCoroutine(RandomTrajectoryToPositionWithFade(japaman, targetPos, moveDuration));

        // 🔥 移動完了後はほぼ透明なので、即座に削除
        if (japaman != null && japaman.gameObject != null)
        {
            japaman.OnConsumptionComplete();
        }
    }

    /// <summary>
    /// ランダム軌道で目標位置まで移動（移動中フェードアウト版）
    /// </summary>
    private IEnumerator RandomTrajectoryToPositionWithFade(PlateJapamanAnimator japaman, Vector2 targetPos, float duration)
    {
        if (japaman == null || japaman.gameObject == null) yield break;

        RectTransform japamanRect = japaman.GetComponent<RectTransform>();
        Image japamanImage = japaman.GetComponent<Image>(); // フェード用のImage取得

        if (japamanRect == null) yield break;

        Vector2 startPos = japamanRect.anchoredPosition;
        int trajectoryType = Random.Range(0, 5);

        Vector2 controlPoint1, controlPoint2;
        CalculateControlPoints(startPos, targetPos, trajectoryType, out controlPoint1, out controlPoint2);

        float elapsed = 0f;
        Vector2 lastPosition = startPos;
        float rotationSpeed = Random.Range(180f, 720f);
        bool clockwise = Random.Range(0, 2) == 0;
        if (!clockwise) rotationSpeed = -rotationSpeed;

        // 🆕 初期色を保存
        Color originalColor = japamanImage != null ? japamanImage.color : Color.white;
        Vector3 originalScale = japamanRect.localScale;

        while (elapsed < duration)
        {
            // 🔥 毎フレーム null チェックを追加
            if (japaman == null || japaman.gameObject == null || japamanRect == null)
            {
                Debug.Log("RandomTrajectoryToPositionWithFade: オブジェクトが削除されたため中断");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = ApplyEasing(t, trajectoryType);

            Vector2 currentPos = CalculateTrajectoryPosition(startPos, targetPos, controlPoint1, controlPoint2, easedT, trajectoryType);

            // 🔥 設定前にもう一度 null チェック
            if (japamanRect != null)
            {
                japamanRect.anchoredPosition = currentPos;

                Vector2 direction = (currentPos - lastPosition).normalized;
                float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float spinAngle = elapsed * rotationSpeed;
                japamanRect.rotation = Quaternion.Euler(0, 0, baseAngle + spinAngle);

                lastPosition = currentPos;
            }

            // 🆕 移動中のフェードアウト処理
            if (japamanImage != null)
            {
                // フェードアウト開始タイミングを調整
                float fadeStartPoint = 0.3f; // 移動の30%地点からフェード開始

                if (t >= fadeStartPoint)
                {
                    // フェードアウト進行度を計算
                    float fadeProgress = (t - fadeStartPoint) / (1f - fadeStartPoint);

                    // 🎯 より急速なフェードアウト（3乗カーブ）
                    float fadeAmount = 1f - Mathf.Pow(1f - fadeProgress, 3f);

                    Color newColor = originalColor;
                    newColor.a = Mathf.Lerp(originalColor.a, 0.05f, fadeAmount); // 完全に0ではなく0.05に
                    japamanImage.color = newColor;

                    // スケールも同時に縮小
                    float scaleAmount = 1f - (fadeProgress * 0.3f); // 最大30%縮小
                    japamanRect.localScale = originalScale * scaleAmount;
                }
            }

            yield return null;
        }

        // 最終位置設定（null チェック付き）
        if (japamanRect != null)
        {
            japamanRect.anchoredPosition = targetPos;
        }

        // 最終的にほぼ透明に
        if (japamanImage != null)
        {
            Color finalColor = originalColor;
            finalColor.a = 0.02f; // ほぼ透明
            japamanImage.color = finalColor;
        }
    }

    /// <summary>
    /// フェードアウトとオブジェクト削除処理（即座削除版）
    /// </summary>
    private IEnumerator FadeOutAndDestroy(GameObject obj, float speedMultiplier = 1f)
    {
        if (obj == null) yield break;

        // 🔥 PlateJapamanAnimatorがある場合は即座に削除
        PlateJapamanAnimator japamanAnimator = obj.GetComponent<PlateJapamanAnimator>();
        if (japamanAnimator != null)
        {
            // 移動中に既にフェードアウトしているため、即座に削除
            japamanAnimator.OnConsumptionComplete();
            yield break;
        }

        // 通常のフェードアウト処理（PlateJapamanAnimatorがない場合のみ）
        Image img = obj.GetComponent<Image>();
        if (img == null)
        {
            Destroy(obj);
            yield break;
        }

        // 既にフェードアウトしている可能性があるため、短時間で削除
        float quickFadeDuration = 0.1f / speedMultiplier;
        float elapsed = 0f;
        Color startColor = img.color;

        while (elapsed < quickFadeDuration && obj != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / quickFadeDuration;

            if (img != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(startColor.a, 0f, progress);
                img.color = newColor;
            }

            obj.transform.localScale = Vector3.Lerp(obj.transform.localScale, Vector3.zero, progress);
            yield return null;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// 🆕 フェードアウトタイミングを調整可能な設定
    /// </summary>
    [Header("フェードアウト設定")]
    [Range(0.1f, 0.8f)]
    public float fadeStartPoint = 0.3f; // フェード開始地点（移動の何%から開始するか）
    [Range(1f, 5f)]
    public float fadeSpeed = 3f; // フェード速度（指数）
    [Range(0.01f, 0.2f)]
    public float minAlpha = 0.05f; // 最小透明度

    /// <summary>
    /// 🧪 デバッグ用：フェードアウト設定テスト
    /// </summary>
    [ContextMenu("🧪 フェードアウト設定テスト")]
    public void TestFadeSettings()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("プレイモードでのみ実行可能です");
            return;
        }

        Debug.Log($"🧪 現在のフェード設定:");
        Debug.Log($"  開始地点: 移動の{fadeStartPoint * 100:F0}%から");
        Debug.Log($"  フェード速度: {fadeSpeed}乗カーブ");
        Debug.Log($"  最小透明度: {minAlpha:F2}");

        if (plateJapamanList.Count > 0)
        {
            Debug.Log("🧪 設定適用でテスト実行");
            StartSuckAllJapamanToMouth();
        }
        else
        {
            Debug.Log("🧪 皿にジャパまんがありません");
        }
    }

    /// <summary>
    /// 🧪 デバッグ用：フェード速度プリセット
    /// </summary>
    [ContextMenu("🧪 高速フェード設定")]
    public void SetFastFadePreset()
    {
        fadeStartPoint = 0.2f; // 移動の20%からフェード開始
        fadeSpeed = 4f;        // 4乗カーブで急速フェード
        minAlpha = 0.02f;      // ほぼ完全透明
        Debug.Log("🧪 高速フェード設定を適用しました");
    }

    [ContextMenu("🧪 標準フェード設定")]
    public void SetNormalFadePreset()
    {
        fadeStartPoint = 0.3f; // 移動の30%からフェード開始
        fadeSpeed = 3f;        // 3乗カーブ
        minAlpha = 0.05f;      // 少し透明度を残す
        Debug.Log("🧪 標準フェード設定を適用しました");
    }


    /// <summary>
    /// ランダム軌道で目標位置まで移動（null チェック強化版）
    /// </summary>
    private IEnumerator RandomTrajectoryToPosition(PlateJapamanAnimator japaman, Vector2 targetPos, float duration)
    {
        if (japaman == null || japaman.gameObject == null) yield break;

        RectTransform japamanRect = japaman.GetComponent<RectTransform>();
        if (japamanRect == null) yield break;

        Vector2 startPos = japamanRect.anchoredPosition;
        int trajectoryType = Random.Range(0, 5);

        Vector2 controlPoint1, controlPoint2;
        CalculateControlPoints(startPos, targetPos, trajectoryType, out controlPoint1, out controlPoint2);

        float elapsed = 0f;
        Vector2 lastPosition = startPos;
        float rotationSpeed = Random.Range(180f, 720f);
        bool clockwise = Random.Range(0, 2) == 0;
        if (!clockwise) rotationSpeed = -rotationSpeed;

        while (elapsed < duration)
        {
            // 🔥 毎フレーム null チェックを追加
            if (japaman == null || japaman.gameObject == null || japamanRect == null)
            {
                Debug.Log("RandomTrajectoryToPosition: オブジェクトが削除されたため中断");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = ApplyEasing(t, trajectoryType);

            Vector2 currentPos = CalculateTrajectoryPosition(startPos, targetPos, controlPoint1, controlPoint2, easedT, trajectoryType);

            // 🔥 設定前にもう一度 null チェック
            if (japamanRect != null)
            {
                japamanRect.anchoredPosition = currentPos;

                Vector2 direction = (currentPos - lastPosition).normalized;
                float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float spinAngle = elapsed * rotationSpeed;
                japamanRect.rotation = Quaternion.Euler(0, 0, baseAngle + spinAngle);

                lastPosition = currentPos;
            }

            yield return null;
        }

        // 最終位置設定（null チェック付き）
        if (japamanRect != null)
        {
            japamanRect.anchoredPosition = targetPos;
        }
    }

   

    /// <summary>
    /// 軌道タイプに応じて制御点を計算
    /// </summary>
    private void CalculateControlPoints(Vector2 start, Vector2 target, int trajectoryType, out Vector2 control1, out Vector2 control2)
    {
        Vector2 direction = target - start;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized;

        switch (trajectoryType)
        {
            case 0: // 直線的吸い込み
                control1 = Vector2.Lerp(start, target, 0.33f);
                control2 = Vector2.Lerp(start, target, 0.66f);
                break;

            case 1: // S字カーブ
                {
                    float curveMagnitude = Random.Range(30f, 80f);
                    control1 = Vector2.Lerp(start, target, 0.33f) + perpendicular * curveMagnitude;
                    control2 = Vector2.Lerp(start, target, 0.66f) - perpendicular * curveMagnitude;
                }
                break;

            case 2: // 大きく迂回
                {
                    float detourMagnitude = Random.Range(50f, 120f);
                    Vector2 midPoint = Vector2.Lerp(start, target, 0.5f);
                    Vector2 detourDirection = Random.Range(0, 2) == 0 ? perpendicular : -perpendicular;
                    control1 = midPoint + detourDirection * detourMagnitude;
                    control2 = Vector2.Lerp(control1, target, 0.7f);
                }
                break;

            case 3: // 螺旋状
                {
                    float spiralRadius = Random.Range(40f, 90f);
                    Vector2 spiralCenter = Vector2.Lerp(start, target, 0.5f);
                    control1 = spiralCenter + perpendicular * spiralRadius;
                    control2 = spiralCenter - perpendicular * spiralRadius;
                }
                break;

            case 4: // 放物線
                {
                    float arcHeight = Random.Range(60f, 150f);
                    Vector2 arcDirection = direction.y > 0 ? Vector2.up : Vector2.up; // 常に上向きの弧
                    control1 = Vector2.Lerp(start, target, 0.3f) + arcDirection * arcHeight;
                    control2 = Vector2.Lerp(start, target, 0.7f) + arcDirection * arcHeight * 0.5f;
                }
                break;

            default:
                control1 = Vector2.Lerp(start, target, 0.33f);
                control2 = Vector2.Lerp(start, target, 0.66f);
                break;
        }
    }

    /// <summary>
    /// 軌道タイプに応じた位置計算
    /// </summary>
    private Vector2 CalculateTrajectoryPosition(Vector2 start, Vector2 target, Vector2 control1, Vector2 control2, float t, int trajectoryType)
    {
        switch (trajectoryType)
        {
            case 0: // 直線
                return Vector2.Lerp(start, target, t);

            case 1: // S字カーブ（3次ベジェ曲線）
                return CalculateCubicBezier(start, control1, control2, target, t);

            case 2: // 大きく迂回（2次ベジェ曲線）
                return CalculateQuadraticBezier(start, control1, target, t);

            case 3: // 螺旋状
                {
                    Vector2 basePos = CalculateQuadraticBezier(start, control1, target, t);
                    float spiralOffset = Mathf.Sin(t * Mathf.PI * 3f) * 20f * (1f - t); // 徐々に収束
                    Vector2 perpendicular = new Vector2(-(target - start).y, (target - start).x).normalized;
                    return basePos + perpendicular * spiralOffset;
                }

            case 4: // 放物線（2次ベジェ曲線）
                return CalculateQuadraticBezier(start, control1, target, t);

            default:
                return Vector2.Lerp(start, target, t);
        }
    }

    /// <summary>
    /// 軌道タイプに応じたイージング関数
    /// </summary>
    private float ApplyEasing(float t, int trajectoryType)
    {
        switch (trajectoryType)
        {
            case 0: // 直線 - 急加速
                return 1f - Mathf.Pow(1f - t, 3f);

            case 1: // S字 - スムーズ
                return t * t * (3f - 2f * t);

            case 2: // 迂回 - 最初ゆっくり、後半急加速
                return t * t;

            case 3: // 螺旋 - 一定速度
                return t;

            case 4: // 放物線 - イーズインアウト
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            default:
                return t;
        }
    }

    /// <summary>
    /// 2次ベジェ曲線の計算
    /// </summary>
    private Vector2 CalculateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// 3次ベジェ曲線の計算
    /// </summary>
    private Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t2 = t * t;
        float t3 = t2 * t;

        return u3 * p0 + 3f * u2 * t * p1 + 3f * u * t2 * p2 + t3 * p3;
    }



    /// <summary>
    /// 新しいステージ開始時のリセット処理
    /// </summary>
    public void ResetForNewStage()
    {
        Debug.Log("🔧 ClickManager: 新ステージ用リセット開始");

        // 🔥 カウンターを確実に0に設定
        japamanCount = 0;
        extraJapamanCount = 0;
        japamanOnPlate = 0;
        totalJapamanProduced = 0; // 既存フィールド

        // 🔥 既存の皿のジャパまんを全て削除
        ClearAllPlateJapaman();

        // 🔥 UIを0で更新
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateJapamanText(0);
            UIManager.Instance.UpdateExtraJapamanText(0);

            // extraJapamanTextを非表示にする
            if (UIManager.Instance.extraJapamanText != null)
            {
                UIManager.Instance.extraJapamanText.gameObject.SetActive(false);
            }
        }

        Debug.Log("🔧 ClickManager: 新ステージ用にリセット完了 - japamanCount=0");
    }


    private void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateJapamanText(japamanCount);
            UIManager.Instance.UpdateExtraJapamanText(extraJapamanCount);
        }
    }


    /// <summary>
    /// ResetCountersOnly メソッド（ログ削減版）
    /// </summary>
    public void ResetCountersOnly()
    {
        // 🔥 現在のアップグレード効果を保存
        int savedClickMultiplier = clickMultiplier;
        float savedAutoProductionRate = autoProductionRate;
        float savedAutoClickRate = autoClickRate;

        // カウンターをリセット
        japamanCount = 0;
        //plateJapamanCount = 0;
        extraJapamanCount = 0;
        totalJapamanProduced = 0;
        japamanOnPlate = 0;

        // 既存の皿のジャパまんを全て削除
        ClearAllPlateJapaman();

        // 🔥 アップグレード効果を復元
        clickMultiplier = savedClickMultiplier;
        autoProductionRate = savedAutoProductionRate;
        autoClickRate = savedAutoClickRate;

        // UIを更新
        UpdateUI();

        Debug.Log($"🔧 カウンターリセット完了 - 効果保持: クリック×{clickMultiplier}, 自動生産{autoProductionRate}/秒");
    }

    // 🔥 デバッグ用：現在のアップグレード効果状態を確認
    [ContextMenu("🔍 ClickManager状態確認")]
    /// <summary>
    /// 🔥 DebugClickManagerState メソッド（修正版・重複なし）
    /// </summary>
    [ContextMenu("🔍 ClickManager状態確認")]
    public void DebugClickManagerState()
    {
        Debug.Log("=== ClickManager状態確認 ===");
        Debug.Log($"japamanCount: {japamanCount}");
        Debug.Log($"goalCount: {goalCount}");
        Debug.Log($"clickMultiplier: {clickMultiplier}");
        Debug.Log($"autoProductionRate: {autoProductionRate}");
        Debug.Log($"autoClickRate: {autoClickRate}");
        Debug.Log($"clickEnabled: {clickEnabled}");

        // 対応するアップグレードの確認
        if (UpgradeManager.Instance != null)
        {
            var clickPower = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.ClickPower);
            var factory = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.Factory);
            var helper = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.HelperFriend);

            Debug.Log($"対応アップグレード:");
            Debug.Log($"  クリック強化: {(clickPower != null ? $"Lv.{clickPower.currentLevel}" : "なし")}");
            Debug.Log($"  工場: {(factory != null ? $"Lv.{factory.currentLevel}" : "なし")}");
            Debug.Log($"  ヘルパー: {(helper != null ? $"Lv.{helper.currentLevel}" : "なし")}");
        }
    }

    // 🔥 アップグレード効果を強制的に再適用（外部から呼び出し可能）
    /// <summary>
    /// 🔥 ForceApplyUpgradeEffects メソッド（修正版・重複なし）
    /// </summary>
    public void ForceApplyUpgradeEffects()
    {
        Debug.Log("🔧 ClickManager: アップグレード効果を強制再適用");

        if (UpgradeManager.Instance == null)
        {
            Debug.LogWarning("UpgradeManager.Instance が null です");
            return;
        }

        // クリック強化
        var clickPower = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.ClickPower);
        if (clickPower != null)
        {
            clickMultiplier = (int)clickPower.GetCurrentEffect();
            Debug.Log($"クリック強化適用: x{clickMultiplier}");
        }
        else
        {
            clickMultiplier = 1;
            Debug.Log("クリック強化: 初期値に設定");
        }

        // 工場（自動生産）
        var factory = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.Factory);
        if (factory != null)
        {
            autoProductionRate = factory.GetCurrentEffect();
            Debug.Log($"工場適用: {autoProductionRate}/秒");
        }
        else
        {
            autoProductionRate = 0f;
            Debug.Log("工場: 初期値に設定");
        }

        // ヘルパーフレンズ（自動クリック）
        var helper = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.HelperFriend);
        if (helper != null)
        {
            autoClickRate = helper.GetCurrentEffect();
            Debug.Log($"ヘルパー適用: {autoClickRate}/秒");
        }
        else
        {
            autoClickRate = 0f;
            Debug.Log("ヘルパー: 初期値に設定");
        }
    }

    /// <summary>
    /// ClearAllPlateJapaman メソッド（ログ削減版）
    /// </summary>
    private void ClearAllPlateJapaman()
    {
        // リストのジャパまんを全て削除
        foreach (var japaman in plateJapamanList)
        {
            if (japaman != null && japaman.gameObject != null)
            {
                Destroy(japaman.gameObject);
            }
        }

        // リストをクリア
        plateJapamanList.Clear();

        // 念のため、plateContainer配下の全子オブジェクトも削除
        if (plateContainer != null)
        {
            for (int i = plateContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = plateContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // ログ削減：必要な時のみ
        // Debug.Log("皿のジャパまんを全て削除しました");  // コメントアウト
    }

    /// <summary>
    /// AutoProduceJapaman メソッド（ログ削減版）
    /// </summary>
    private void AutoProduceJapaman()
    {
        // 🆕 ゲーム状態チェックを追加
        if (GameManager.Instance != null && !GameManager.Instance.CanAutoProduction())
        {
            return;
        }
        japamanCount++;
        UIManager.Instance.UpdateJapamanText(japamanCount);

        // ログは5秒間隔程度に削減
        if (Time.frameCount % 300 == 0) // 60fps * 5秒
        {
            Debug.Log($"🏭 自動生産中: {japamanCount}個");
        }
    }

  
    /// <summary>
    /// AutoClick メソッド（まとめる係対応修正版）
    /// </summary>
    private void AutoClick()
    {
        // ゲーム状態チェック
        if (GameManager.Instance != null && !GameManager.Instance.CanAutoProduction())
        {
            return;
        }

        // 🔥 まとめる係に【自動クリック】通知（OnPlayerClickは呼ばない）
        if (organizerManager != null)
        {
            organizerManager.OnAutoClick(); // 自動クリック（まとめる係に影響しない）
        }

        // 🎨 UIエフェクトをトリガー
        if (UpgradeSidePanelUI.Instance != null)
        {
            UpgradeSidePanelUI.Instance.TriggerItemActivationEffect(UpgradeType.HelperFriend);
        }

       
        // ログは10秒間隔程度に削減
        if (Time.frameCount % 600 == 0) // 60fps * 10秒
        {
            Debug.Log($"🤖 自動クリック中");
        }
    }

   

    /// <summary>
    /// クリック実行の共通処理（エフェクト対応版）
    /// </summary>
    /// <param name="isPlayerClick">プレイヤーによるクリックかどうか</param>
    private void ExecuteClick(bool isPlayerClick)
    {
        // 🔥 既存の状態チェック方法を使用
        if (!clickEnabled)
        {
            return;
        }

        // UIManagerのカウントダウン中はクリック無効
        if (UIManager.Instance != null && UIManager.Instance.IsCountdownActive())
        {
            return;
        }

        // 時間切れ後や食べ終わり待機中はクリック無効
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && (gameManager.IsTimeUp() || gameManager.IsWaitingForEating()))
        {
            return;
        }

        // 🔥 クリック前の状態を記録
        long previousCount = japamanCount;

        // クリック倍率を適用
        for (int i = 0; i < clickMultiplier; i++)
        {
            japamanCount++;
        }

        // UI更新
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateJapamanText(japamanCount);
        }

        // 🆕 クリック位置を設定（手動/自動で区別）
        if (isPlayerClick)
        {
            RecordClickPosition(); // プレイヤークリック時は実際の位置を記録
        }
        else
        {
            // 🆕 自動クリック時はボタン位置をベースにランダム位置を生成
            RecordAutoClickPosition();
        }

        // 🔥 皿に追加すべき個数を計算
        long plateAddCount = 0;

        if (japamanCount <= goalCount)
        {
            // 全て皿に追加
            plateAddCount = clickMultiplier;
        }
        else if (previousCount < goalCount)
        {
            // 一部を皿に、残りは直接投げ込み
            plateAddCount = goalCount - previousCount;
            long extraCount = japamanCount - goalCount;

            // 直接投げ込み処理
            extraJapamanCount += extraCount;
            float remainingTime = GetRemainingTime();
            float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);

            // 複数個の投げ込みアニメーション
            StartCoroutine(MultipleThrowToFriendsMouth(extraCount, speedMultiplier));
            UIManager.Instance.UpdateExtraJapamanText(extraJapamanCount);
        }
        else
        {
            // 全て直接投げ込み
            plateAddCount = 0;
            extraJapamanCount += clickMultiplier;
            float remainingTime = GetRemainingTime();
            float speedMultiplier = CalculateThrowSpeedMultiplier(remainingTime);

            StartCoroutine(MultipleThrowToFriendsMouth(clickMultiplier, speedMultiplier));
            UIManager.Instance.UpdateExtraJapamanText(extraJapamanCount);
        }

        // 🔥 皿にジャパまんを複数個追加（手動/自動両方でエフェクト発生）
        if (plateAddCount > 0)
        {
            // 🆕 エフェクト種類を指定
            ClickEffectType effectType = isPlayerClick ? ClickEffectType.PlayerClick : ClickEffectType.AutoClick;
            StartCoroutine(MultipleFlyJapamanFromClick(plateAddCount, effectType));
            AddMultipleJapamanToPlate((int)plateAddCount);
        }

        // 🔥 ノルマ達成判定
        if (previousCount < goalCount && japamanCount >= goalCount)
        {
            Debug.Log("ノルマ超過！吸い込み処理を開始");
            StartCoroutine(DelayedSuck());
        }

        // 音の判定
        if (audioSource)
        {
            bool shouldUsePlateSound = plateAddCount > 0;
            audioSource.PlayOneShot(shouldUsePlateSound ? clickSound : mouthFeedSound);
        }

        // 🆕 プレイヤークリック時のみボタン揺れ
        if (isPlayerClick && clickButton != null)
        {
            StartCoroutine(ShakeButton());
        }

        // UpgradeManagerに通知（統計用）
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnClick();
        }

        // デバッグログ（頻繁になりすぎないよう制限）
        if (isPlayerClick || Time.frameCount % 180 == 0) // 自動クリックは3秒に1回ログ
        {
            string clickType = isPlayerClick ? "🖱️プレイヤー" : "🤖自動";
            Debug.Log($"{clickType}クリック: +{clickMultiplier} (総計: {japamanCount})");
        }
    }




    /// <summary>
    /// ゴール達成チェック（既存処理ベース）
    /// </summary>
    private void CheckGoalAchievement()
    {
        // 既存のOnClickJapaman()と同じロジックを使用
        if (japamanCount >= goalCount)
        {
            // ノルマ達成の処理は既にOnClickJapaman()にあるため、
            // ここでは簡単なログのみ
            Debug.Log($"ゴール状態: {japamanCount}/{goalCount}");
        }
    }

    /// <summary>
    /// 指定位置にジャパまん生成演出（簡易版）
    /// </summary>
    private void CreateJapamanAtPosition(Vector3 position)
    {
        // 簡易実装: クリック位置を記録するだけ
        RecordClickPosition();
        Debug.Log($"ジャパまん生成演出: {position}");
    }

    /// <summary>
    /// まとめる係マネージャーを設定（UpgradeManagerから呼び出し用）
    /// </summary>
    public void SetOrganizerManager(OrganizerManager manager)
    {
        organizerManager = manager;
        Debug.Log("🔧 ClickManager: OrganizerManagerが設定されました");
    }

    /// <summary>
    /// デバッグ用: まとめる係状態確認
    /// </summary>
    [ContextMenu("🔍 まとめる係連携状態確認")]
    public void DebugOrganizerIntegration()
    {
        Debug.Log("=== ClickManager まとめる係連携状態 ===");
        Debug.Log($"organizerManager: {(organizerManager != null ? "設定済み" : "未設定")}");
        Debug.Log($"autoClickRate: {autoClickRate}/秒");
        Debug.Log($"clickMultiplier: {clickMultiplier}x");

        if (organizerManager != null)
        {
            var status = organizerManager.GetStatus();
            Debug.Log($"まとめる係アクティブ: {status.isActive}");
            Debug.Log($"まとめる係ブースト中: {status.isBoostActive}");
            Debug.Log($"現在のブースト倍率: {status.boostMultiplier:F1}x");
        }
    }

    /// <summary>
    /// デバッグ用: 手動プレイヤークリック
    /// </summary>
    [ContextMenu("🧪 手動プレイヤークリック")]
    public void DebugManualPlayerClick()
    {
        if (Application.isPlaying)
        {
            OnPlayerClick();
            Debug.Log("🧪 デバッグ: 手動プレイヤークリック実行");
        }
    }



    // 残り時間を取得
    private float GetRemainingTime()
    {
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            return gameManager.GetRemainingTime();
        }
        return 30f; // デフォルト値
    }

    // 投げ込み速度の倍率計算
    private float CalculateThrowSpeedMultiplier(float remainingTime)
    {
        if (remainingTime <= 2f)
        {
            return 2f;    // 残り2秒以下：2倍速
        }
        else if (remainingTime <= 5f)
        {
            return 1.5f;    // 残り5秒以下：1倍速
        }
        else if (remainingTime <= 10f)
        {
            return 1f;    // 残り10秒以下：1倍速
        }
        else
        {
            return 1f;    // 通常速度
        }
    }

    // 吸い込み速度の倍率計算
    private float CalculateSuckSpeedMultiplier(float remainingTime)
    {
        if (remainingTime <= 1f)
        {
            return 20f;    // 残り1秒以下：20倍速
        }
        else if (remainingTime <= 3f)
        {
            return 10f;    // 残り3秒以下：10倍速
        }
        else if (remainingTime <= 5f)
        {
            return 5f;    // 残り5秒以下：5倍速
        }
        else if (remainingTime <= 10f)
        {
            return 2f;    // 残り10秒以下：2倍速
        }
        else
        {
            return 1f;    // 通常速度
        }
    }

    // 器変更時の処理
    public void OnContainerChanged()
    {
        var data = ContainerSettings.Instance?.GetCurrentContainerData();
        if (data != null)
        {
            Debug.Log("器が" + data.containerName + "に変更されました");
        }
    }

    public long GetTotalJapamanCount()
    {
        return japamanCount;
    }

    public long GetPlateJapamanCount()
    {
        return japamanOnPlate;
    }

    public long GetExtraJapamanCount()
    {
        return extraJapamanCount;
    }

    // ClickManager.cs に追加するメソッド群
    // 既存のClickManagerクラスに以下のメソッドを追加してください

    /// <summary>
    /// ジャパリパンからじゃぱまんを追加（修正版）
    /// </summary>
    public void AddJapamanFromPan(float panValue)
    {
        // 🔥 直接整数に切り上げ
        long addedValue = (long)Mathf.Ceil(panValue);

        // 🔥 クリック前の状態を記録
        long previousCount = japamanCount;

        // 🔥 実際の変数名を使用
        japamanCount += addedValue;
        totalJapamanProduced += addedValue;

        // 🔥 実際のUI更新メソッドを使用
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateJapamanText(japamanCount);
        }

        // 🔥 既存のサウンド再生
        if (audioSource && clickSound)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // 🔥 ノルマ達成判定（ジャパリパンでも有効）
        if (previousCount < goalCount && japamanCount >= goalCount)
        {
            Debug.Log("🍞 ジャパリパンでノルマ超過！吸い込み処理を開始");

            // 🔥 GameManagerを通じてノルマ達成状態を更新
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null && !gameManager.IsCleared())
            {
                Debug.Log("🔥 GameManagerの状態更新開始");
                gameManager.SetGoalAchieved();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetPhaseUI(false);
                    UIManager.Instance.ShowPhaseChangeMessage("ノルマ達成！\nフレンズにジャパまんをあげよう！");
                }
            }

            TriggerDelayedSuck();
        }
        else
        {
            Debug.Log($"🔍 ノルマ判定: {japamanCount} >= {goalCount} ? {japamanCount >= goalCount} (前回: {previousCount})");
        }

        // 統計更新
        UpdatePanStatistics(addedValue);
    }

    /// <summary>
    /// パン関連統計更新
    /// </summary>
    private void UpdatePanStatistics(long panValue)
    {
        // パン収集回数をカウント（新しい統計項目）
        if (!PlayerPrefs.HasKey("TotalPansCollected"))
            PlayerPrefs.SetInt("TotalPansCollected", 0);

        int totalPans = PlayerPrefs.GetInt("TotalPansCollected") + 1;
        PlayerPrefs.SetInt("TotalPansCollected", totalPans);

        // パンから得た総価値（新しい統計項目）
        if (!PlayerPrefs.HasKey("TotalPanValue"))
            PlayerPrefs.SetString("TotalPanValue", "0");

        long totalPanValue = long.Parse(PlayerPrefs.GetString("TotalPanValue")) + panValue;
        PlayerPrefs.SetString("TotalPanValue", totalPanValue.ToString());

        PlayerPrefs.Save();
    }

    /// <summary>
    /// パン統計情報取得
    /// </summary>
    public PanStatistics GetPanStatistics()
    {
        return new PanStatistics
        {
            totalPansCollected = PlayerPrefs.GetInt("TotalPansCollected", 0),
            totalPanValue = long.Parse(PlayerPrefs.GetString("TotalPanValue", "0")),
            donkeyBakeryLevel = GetDonkeyBakeryLevel()
        };
    }

    /// <summary>
    /// ロバのパン屋レベル取得
    /// </summary>
    private int GetDonkeyBakeryLevel()
    {
        if (UpgradeManager.Instance != null)
        {
            var donkeyUpgrade = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.DonkeyBakery);
            var robaUpgrade = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.RobaBakery); // 互換性
            var factoryUpgrade = UpgradeManager.Instance.GetActiveUpgrade(UpgradeType.Factory); // 互換性

            var activeUpgrade = donkeyUpgrade ?? robaUpgrade ?? factoryUpgrade;
            return activeUpgrade?.currentLevel ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// クリックエフェクトの種類を定義
    /// </summary>
    public enum ClickEffectType
    {
        /// <summary>
        /// プレイヤーによる手動クリック
        /// - より派手なエフェクト
        /// - ボタン揺れあり
        /// - ダイナミックな飛行パターン
        /// </summary>
        PlayerClick,

        /// <summary>
        /// お手伝いフレンズによる自動クリック
        /// - 控えめだが分かりやすいエフェクト
        /// - ボタン揺れなし
        /// - 規則的な飛行パターン
        /// </summary>
        AutoClick,

        /// <summary>
        /// 工場による自動生産
        /// - 最も控えめなエフェクト
        /// - 継続的な演出
        /// </summary>
        AutoProduction,

        /// <summary>
        /// ジャパリパンからの変換
        /// - 特別なエフェクト
        /// - パン専用の演出
        /// </summary>
        PanConversion
    }


    /// <summary>
    /// デバッグ用：パン統計表示
    /// </summary>
    [ContextMenu("📊 パン統計表示")]
    public void DebugShowPanStatistics()
    {
        var stats = GetPanStatistics();
        Debug.Log("=== パン統計情報 ===");
        Debug.Log($"収集したパン総数: {stats.totalPansCollected}個");
        Debug.Log($"パンから得た総価値: {stats.totalPanValue}じゃぱまん");
        Debug.Log($"ロバのパン屋レベル: {stats.donkeyBakeryLevel}");

        if (stats.totalPansCollected > 0)
        {
            float averageValue = (float)stats.totalPanValue / stats.totalPansCollected;
            Debug.Log($"パン1個あたりの平均価値: {averageValue:F1}じゃぱまん");
        }



    }

    /// <summary>
    /// 🆕 自動クリック用の位置記録
    /// </summary>
    private void RecordAutoClickPosition()
    {
        if (clickButton != null)
        {
            lastClickPosition = clickButton.transform.position;

            // ボタンの範囲内でより大きくランダムに位置を調整（自動クリック感を演出）
            RectTransform buttonRect = clickButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-buttonRect.rect.width * 0.4f, buttonRect.rect.width * 0.4f),
                    Random.Range(-buttonRect.rect.height * 0.4f, buttonRect.rect.height * 0.4f),
                    0
                );
                lastClickPosition += randomOffset;
            }
        }
        else
        {
            // フォールバック：spawnPointを使用
            if (spawnPoint != null)
            {
                lastClickPosition = spawnPoint.position;
            }
            else
            {
                lastClickPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 🆕 エフェクト種類を指定した複数個飛行アニメーション
    /// </summary>
    private IEnumerator MultipleFlyJapamanFromClick(long count, ClickEffectType effectType)
    {
        int actualCount = Mathf.Min((int)count, 8); // 最大8個まで

        for (int i = 0; i < actualCount; i++)
        {
            StartCoroutine(FlyJapamanFromClick(i, actualCount, effectType));

            // 🆕 エフェクト種類に応じて発射間隔を調整
            float interval = effectType == ClickEffectType.PlayerClick ? 0.05f : 0.08f;
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// 🆕 エフェクト種類対応版の飛行アニメーション
    /// </summary>
    private IEnumerator FlyJapamanFromClick(int index, int totalCount, ClickEffectType effectType)
    {
        // クリック位置から開始
        Vector3 startPosition = lastClickPosition;

        // フォールバック：spawnPointがある場合はそれを使用
        if (startPosition == Vector3.zero && spawnPoint != null)
        {
            startPosition = spawnPoint.position;
        }

        var obj = Instantiate(flyingJapamanPrefab, startPosition, Quaternion.identity, spawnPoint?.parent);
        var rt = obj.GetComponent<Image>();

        Vector3 start = startPosition;

        // 🆕 エフェクト種類に応じて飛行パターンを調整
        float angle, distance, duration;

        if (effectType == ClickEffectType.PlayerClick)
        {
            // プレイヤークリック：よりダイナミックな演出
            angle = Random.Range(0f, 360f);
            distance = Random.Range(120f, 200f);
            duration = Random.Range(0.5f, 0.8f);
        }
        else
        {
            // 自動クリック：控えめだが分かりやすい演出
            angle = Random.Range(-45f, 45f) + (index * 15f); // より規則的
            distance = Random.Range(80f, 150f);
            duration = Random.Range(0.6f, 0.9f); // 少し長め
        }

        Vector3 end = start + (Quaternion.Euler(0, 0, angle) * Vector3.right) * distance;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // 🔥 シンプルな直線移動
            obj.transform.position = Vector3.Lerp(start, end, progress);

            // 🆕 エフェクト種類に応じて回転速度を調整
            float rotationSpeed = effectType == ClickEffectType.PlayerClick ? 360f : 180f;
            obj.transform.rotation = Quaternion.Euler(0, 0, t * rotationSpeed);

            // フェードアウト
            rt.color = Color.Lerp(rt.color, new Color(1, 1, 1, 0), t / duration);

            yield return null;
        }

        Destroy(obj);
    }

    /// <summary>
    /// プレイヤークリック処理の統合版
    /// まとめる係との連携を含む
    /// </summary>
    public void OnPlayerClick()
    {
        // ゲーム状態チェック
        if (GameManager.Instance == null || !GameManager.Instance.CanAutoProduction())
            return;

        // 🔥 まとめる係にクリック通知（機能は中断されない）
        var organizerManager = FindFirstObjectByType<OrganizerManager>();
        if (organizerManager != null)
        {
            organizerManager.OnPlayerClick();
        }

        // 共通クリック処理を実行（プレイヤークリックフラグ = true）
        ExecuteClick(true);
    }

    /// <summary>
    /// 自動クリック処理の修正版（エラー解決）
    /// </summary>
    private void ProcessAutoClick()
    {
        if (autoClickRate <= 0) return;

        autoClickTimer += Time.deltaTime;
        float interval = 1f / autoClickRate;

        if (autoClickTimer >= interval)
        {
            autoClickTimer = 0f;

            // まとめる係に自動クリック通知（プレイヤークリックとは区別）
            if (organizerManager != null)
            {
                organizerManager.OnAutoClick();
            }

            // 🎨 お手伝いフレンズのエフェクト通知（修正版）
            if (UpgradeSidePanelUI.Instance != null)
            {
                UpgradeSidePanelUI.Instance.TriggerItemActivationEffect(UpgradeType.HelperFriend);
            }

            // 🆕 共通処理を呼び出し（isPlayerClick = false）
            ExecuteClick(false);
        }
    }

    /// <summary>
    /// 🆕 OnClickJapaman()の修正版（重複処理排除、プレイヤークリック専用）
    /// </summary>
    public void OnClickJapaman()
    {
        // クリック無効時は処理しない
        if (!clickEnabled)
        {
            Debug.Log("クリックが無効状態のため処理をスキップ");
            return;
        }

        // UIManagerのカウントダウン中はクリック無効
        if (UIManager.Instance != null && UIManager.Instance.IsCountdownActive())
        {
            Debug.Log("カウントダウン中のためクリックをスキップ");
            return;
        }

        // 時間切れ後や食べ終わり待機中はクリック無効
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && (gameManager.IsTimeUp() || gameManager.IsWaitingForEating()))
        {
            Debug.Log("時間切れまたは食べ終わり待機中のためクリックをスキップ");
            return;
        }

        // 🆕 プレイヤークリック処理を呼び出し
        OnPlayerClick();
    }

    // パン統計情報クラス
    [System.Serializable]
    public class PanStatistics
    {
        public int totalPansCollected;      // 収集したパン総数
        public long totalPanValue;          // パンから得た総価値
        public int donkeyBakeryLevel;       // ロバのパン屋レベル
    }


}