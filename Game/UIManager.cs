using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("基本UI")]
    public TMP_Text JapamanText;
    public TMP_Text timeText;
    public TMP_Text goalText;
    public TMP_Text stageText; // ステージ表示用に追加

    [Header("追加UI")]
    public TMP_Text extraJapamanText;
    public GameObject gameOverPanel;
    public GameObject gameClearPanel;
    public GameObject roundClearPanel; // ラウンドクリア用パネルを追加
    public TMP_Text finalResultText;
    public TMP_Text roundClearText; // ラウンドクリア用テキストを追加
    public UnityEngine.UI.Button nextDayButton; // 次の日ボタンを追加

    [Header("演出表示")]
    public GameObject platePhaseUI;
    public GameObject mouthPhaseUI;
    public GameObject transitionPanel; // ステージ遷移用パネルを追加
    public TMP_Text transitionText; // ステージ遷移用テキストを追加

    [Header("カウントダウンUI")]
    public GameObject countdownPanel; // カウントダウン用パネル
    public TMP_Text countdownText; // カウントダウン用テキスト

    [Header("ステージクリア選択UI")]
    public GameObject stageCompletePanel;  // 新しいパネル
    public UnityEngine.UI.Button continueStageButton;  // 次のステージボタン
    public UnityEngine.UI.Button suspendSaveButton;    // 中断セーブボタン
    public TMPro.TextMeshProUGUI stageCompleteMessage; // メッセージ表示


    // ボタン制御用フラグ
    private bool isButtonProcessing = false;
    private bool isButtonClickable = false;

    // カウントダウン制御用フラグ
    private bool isCountdownActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        Debug.Log("UIManager Instance設定: ゲームオブジェクト=" + gameObject.name);
    }

    private void Start()
    {
        SetPhaseUI(true);

        if (extraJapamanText != null)
        {
            extraJapamanText.gameObject.SetActive(false);
        }

        // 全ての結果画面パネルを確実に非表示
        if (roundClearPanel != null)
        {
            roundClearPanel.SetActive(false);
            Debug.Log("ラウンドクリアパネルを非表示に設定しました");
        }

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
            Debug.Log("ステージ遷移パネルを非表示に設定しました");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(false);
        }

        // カウントダウンパネルの初期化のメモ（非表示にしない）
        // カウントダウンが始まる前の状態確認のみ
        if (countdownPanel != null)
        {
            // 最初は非表示だが、強制的に非表示にはしない
            // カウントダウンシステムに制御を任せる
            Debug.Log("カウントダウンパネル発見済み（制御はカウントダウンシステムに委譲）");
        }
        else
        {
            Debug.LogWarning("カウントダウンパネルが設定されていません！Inspector で設定してください");
        }

        // 次の日ボタンのイベントを設定
        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayButtonClicked);
            nextDayButton.gameObject.SetActive(false);
            nextDayButton.interactable = false;
            isButtonClickable = false;
            Debug.Log("次の日ボタンを非表示に設定しました");
        }
        else
        {
            Debug.LogWarning("nextDayButtonが設定されていません！");
        }
    }

    /// <summary>
    /// ラウンド開始カウントダウンを表示
    /// </summary>
    // UIManager.cs のカウントダウン関連メソッドを修正

    public void StartRoundCountdown()
    {
        Debug.Log("🎬 StartRoundCountdown呼び出し");

        // UI要素の存在確認（より詳細）
        if (countdownPanel == null)
        {
            Debug.LogError("❌ countdownPanel が null です！Inspectorで設定してください");
            // 自動検索を試行
            countdownPanel = GameObject.Find("CountdownPanel");
            if (countdownPanel != null)
            {
                Debug.Log("✅ CountdownPanelを自動検索で発見");
            }
            else
            {
                Debug.LogError("❌ CountdownPanelが見つかりません");
                return;
            }
        }

        if (countdownText == null)
        {
            Debug.LogError("❌ countdownText が null です！Inspectorで設定してください");
            // countdownPanel内からTextコンポーネントを検索
            if (countdownPanel != null)
            {
                countdownText = countdownPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (countdownText != null)
                {
                    Debug.Log("✅ CountdownTextを自動検索で発見");
                }
            }

            if (countdownText == null)
            {
                Debug.LogError("❌ CountdownTextが見つかりません");
                return;
            }
        }

        Debug.Log("🎬 カウントダウンUI要素確認完了");

        // 既存のコルーチンを停止
        StopAllCoroutines();
        Debug.Log("🎬 既存のカウントダウン停止");

        // 新しいカウントダウンを開始
        StartCoroutine(CountdownCoroutine());
        Debug.Log("🎬 新しいカウントダウン開始");
    }


    private IEnumerator CountdownCoroutine()
    {
        Debug.Log("🎬 CountdownCoroutine開始");

        // カウントダウン開始時にフラグを設定
        isCountdownActive = true;

        // UI要素の最終確認
        if (countdownPanel == null || countdownText == null)
        {
            Debug.LogError("❌ CountdownCoroutine: UI要素が見つかりません");
            isCountdownActive = false; // エラー時はfalseに
            yield break;
        }

        // カウントダウンパネルを表示
        countdownPanel.SetActive(true);
        Debug.Log("🎬 カウントダウンパネル表示");

        // 3, 2, 1 のカウントダウン
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            Debug.Log($"🎬 カウントダウン: {i}");

            if (countdownText != null)
            {
                StartCoroutine(SimpleScaleAnimation(countdownText.transform));
            }
            yield return new WaitForSeconds(1f);
        }

        // スタート表示
        countdownText.text = "すたーと！";
        Debug.Log("🎬 スタート表示");

        if (countdownText != null)
        {
            StartCoroutine(SimpleScaleAnimation(countdownText.transform, 1.5f));
        }
        yield return new WaitForSeconds(1f);

        // カウントダウンパネルを非表示
        countdownPanel.SetActive(false);
        isCountdownActive = false; // カウントダウン終了
        Debug.Log("🎬 カウントダウンパネル非表示");

        // ゲーム開始通知
        if (GameManager.Instance != null)
        {
            Debug.Log("🎬 GameManager.OnCountdownComplete呼び出し");
            GameManager.Instance.OnCountdownComplete();
        }
        else
        {
            Debug.LogError("❌ GameManager.Instance が null です！");
        }

        Debug.Log("🎬 CountdownCoroutine完了");
    }

    private IEnumerator SimpleScaleAnimation(Transform target, float maxScale = 1.3f)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        // 拡大フェーズ
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration / 2f));
            float scale = Mathf.Lerp(1f, maxScale, progress);
            target.localScale = originalScale * scale;
            yield return null;
        }

        // 縮小フェーズ
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration / 2f));
            float scale = Mathf.Lerp(maxScale, 1f, progress);
            target.localScale = originalScale * scale;
            yield return null;
        }

        // 元のサイズに確実に戻す
        target.localScale = originalScale;
    }

    private IEnumerator StartTextAnimation(TMP_Text countdownText)
    {
        throw new NotImplementedException();
    }

    private string CountdownTextAnimation(TMP_Text countdownText)
    {
        throw new NotImplementedException();
    }

    public bool IsCountdownActive()
    {
        
        isCountdownActive = countdownPanel != null && countdownPanel.activeSelf;
        return isCountdownActive;
    }

    /// <summary>
    /// カウントダウン数字のアニメーション
    /// </summary>
    private System.Collections.IEnumerator CountdownNumberAnimation()
    {
        if (countdownText == null) yield break;

        // 大きく表示してから小さくなる
        Vector3 originalScale = countdownText.transform.localScale;
        countdownText.transform.localScale = originalScale * 2f;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // イージング：最初大きく、徐々に縮む
            float scale = Mathf.Lerp(2f, 1f, t);
            countdownText.transform.localScale = originalScale * scale;

            yield return null;
        }

        countdownText.transform.localScale = originalScale;
    }

    /// <summary>
    /// 「すたーと！」のアニメーション
    /// </summary>
    private System.Collections.IEnumerator StartTextAnimation()
    {
        if (countdownText == null) yield break;

        Vector3 originalScale = countdownText.transform.localScale;

        // パルス効果：大きく→小さく→大きく
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // サイン波でパルス効果
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.3f;
            countdownText.transform.localScale = originalScale * pulse;

            yield return null;
        }

        countdownText.transform.localScale = originalScale;
    }

    

    public void UpdateJapamanText(long count)
    {
        if (JapamanText != null)
        {
            JapamanText.text = "じゃぱまん：" + count;
        }
        else
        {
            Debug.LogError("JapamanTextが設定されていません");
        }
    }

    public void UpdateExtraJapamanText(long extraCount)
    {
        if (extraJapamanText != null)
        {
            extraJapamanText.gameObject.SetActive(true);
            extraJapamanText.text = "追加：" + extraCount + "個";
            StartCoroutine(TextScaleAnimation(extraJapamanText));
        }
    }

    public void UpdateTimeText(float seconds)
    {
        if (timeText != null)
        {
            timeText.text = "のこり時間：" + Mathf.CeilToInt(seconds) + "秒";
        }
        else
        {
            Debug.LogError("timeTextが設定されていません");
        }
    }

    public void UpdateGoalText(long goal)
    {
        if (goalText != null)
            goalText.text = "目標：" + goal + "個";
        else
            Debug.LogError("goalTextが設定されていません");
    }

    public void UpdateStageText(int stage)
    {
        if (stageText != null)
            stageText.text = stage + "日目";
        else
            Debug.LogError("stageTextが設定されていません");
    }

    public void SetPhaseUI(bool isPlatePhase)
    {
        if (platePhaseUI != null)
            platePhaseUI.SetActive(isPlatePhase);

        if (mouthPhaseUI != null)
            mouthPhaseUI.SetActive(!isPlatePhase);
    }

    public void ShowFinalResult(long totalCount, long plateCount, long extraCount, float timeUsed)
    {
        if (finalResultText != null)
        {
            string resultMessage = "最終結果\n\n";
            resultMessage += "合計じゃぱまん数：" + totalCount + "個\n";
            resultMessage += "皿のじゃぱまん：" + plateCount + "個\n";

            if (extraCount > 0)
            {
                resultMessage += "追加じゃぱまん：" + extraCount + "個\n";
                resultMessage += "フレンズに" + extraCount + "個あげました！\n";
            }

            resultMessage += "\n使用時間：" + timeUsed.ToString("F1") + "秒";

            finalResultText.text = resultMessage;
        }

        // GameManagerから現在の目標値を取得
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && plateCount >= gameManager.GetCurrentStageGoal())
        {
            ShowGameClearPanel();
        }
        else
        {
            ShowGameOverPanel();
        }
    }

    public void ShowGameClearPanel()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
            StartCoroutine(PanelScaleAnimation(gameClearPanel));
        }
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            StartCoroutine(PanelScaleAnimation(gameOverPanel));
        }
    }

    /// <summary>
    /// ラウンドクリア演出を表示
    /// </summary>
    public void ShowRoundClear(int stage, long roundTotal, long roundExtra, long lifetimeTotal, float goalTime, float totalTime, bool showNextDayButton = false)
    {
        Debug.Log("ShowRoundClear呼出し: stage=" + stage + ", showNextDayButton=" + showNextDayButton);

        if (roundClearPanel != null && roundClearText != null)
        {
            // メッセージ設定
            string message = stage + "日目クリア！\n";
            message += "今日のじゃぱまん生産数：" + roundTotal + "個\n";
            message += "ノルマ達成時間：" + goalTime.ToString("F1") + "秒\n";
            message += "経過時間：" + totalTime.ToString("F1") + "秒";
            roundClearText.text = message;

            roundClearPanel.SetActive(true);
            StartCoroutine(PanelScaleAnimation(roundClearPanel));

            // 既存のNextDayButtonは使用しない（新システム移行のため）
            if (nextDayButton != null)
            {
                nextDayButton.gameObject.SetActive(false);
                nextDayButton.interactable = false;
            }
            // NextDayButtonがnullでもエラーを出さない

            Debug.Log("ラウンドクリア表示完了（新システム対応）");
        }
        else
        {
            Debug.LogError("roundClearPanel または roundClearText がnullです！");
        }
    }


    /// <summary>
    /// ステージクリア後の選択画面を表示
    /// </summary>
    public void ShowStageCompleteChoice(int stage, System.Action onContinue, System.Action onSuspendSave)
    {
        if (stageCompletePanel != null)
        {
            // メッセージ設定
            if (stageCompleteMessage != null)
            {
                stageCompleteMessage.text = $"ステージ{stage}をクリアしました！\n\n次はどうしますか？";
            }

            // 次のステージボタン設定
            if (continueStageButton != null)
            {
                continueStageButton.onClick.RemoveAllListeners();
                continueStageButton.onClick.AddListener(() => {
                    Debug.Log("次のステージが選択されました");
                    HideStageCompleteChoice();
                    onContinue?.Invoke();
                });
                continueStageButton.interactable = true;
            }

            // 中断セーブボタン設定
            if (suspendSaveButton != null)
            {
                suspendSaveButton.onClick.RemoveAllListeners();
                suspendSaveButton.onClick.AddListener(() => {
                    Debug.Log("中断セーブが選択されました");
                    HideStageCompleteChoice();
                    onSuspendSave?.Invoke();
                });
                suspendSaveButton.interactable = true;
            }

            // パネル表示
            stageCompletePanel.SetActive(true);
            StartCoroutine(PanelScaleAnimation(stageCompletePanel));

            Debug.Log("ステージクリア選択画面を表示");
        }
        else
        {
            Debug.LogError("stageCompletePanel が設定されていません - フォールバック実行");
            onContinue?.Invoke();
        }
    }

    /// <summary>
    /// ステージクリア選択画面を非表示
    /// </summary>
    public void HideStageCompleteChoice()
    {
        if (stageCompletePanel != null)
        {
            stageCompletePanel.SetActive(false);
        }
    }

    /// <summary>
    /// ステージ遷移演出を表示
    /// </summary>
    public void ShowStageTransition(int nextStage, long nextGoal)
    {
        if (transitionPanel != null && transitionText != null)
        {
            string message = "NEXT STAGE\n\n";
            message += nextStage + "日目\n";
            message += "目標: " + nextGoal + "個\n\n";
            message += "がんばって！";

            transitionText.text = message;
            transitionPanel.SetActive(true);
            StartCoroutine(PanelScaleAnimation(transitionPanel));

            // 一定時間後に自動で非表示
            StartCoroutine(HideTransitionPanel());
        }
    }

    private System.Collections.IEnumerator HideTransitionPanel()
    {
        yield return new WaitForSeconds(2f);

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ゲームオーバー表示（累積集計付き）
    /// </summary>
    public void ShowGameOver(long roundTotal, long plateCount, long roundExtra,
                           long lifetimeTotal, int currentStage, float timeUsed)
    {
        if (finalResultText != null)
        {
            string resultMessage = "GAME OVER\n\n";
            resultMessage += currentStage + "日目で終了！\n\n";
            resultMessage += "今回の結果:\n";
            resultMessage += "合計じゃぱまん数：" + roundTotal + "個\n";
            resultMessage += "皿のじゃぱまん：" + plateCount + "個\n";

            if (roundExtra > 0)
            {
                resultMessage += "追加じゃぱまん：" + roundExtra + "個\n";
            }

            resultMessage += "\n使用時間：" + timeUsed.ToString("F1") + "秒\n";
            resultMessage += "累積総生産数: " + lifetimeTotal + "個";

            finalResultText.text = resultMessage;
        }

        ShowGameOverPanel();
    }

    /// <summary>
    /// ラウンドクリア画面を非表示
    /// </summary>
    public void HideRoundClear()
    {
        if (roundClearPanel != null)
        {
            roundClearPanel.SetActive(false);
        }

        if (nextDayButton != null)
        {
            nextDayButton.gameObject.SetActive(false);
            nextDayButton.interactable = false;
        }

        // フラグもリセット
        isButtonClickable = false;
        isButtonProcessing = false;

        Debug.Log("ラウンドクリア画面を非表示にしました");
    }

    /// <summary>
    /// 全ての結果画面パネルを非表示
    /// </summary>
    public void HideAllResultPanels()
    {
        if (roundClearPanel != null)
        {
            roundClearPanel.SetActive(false);
        }
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(false);
        }
        if (nextDayButton != null)
        {
            nextDayButton.gameObject.SetActive(false);
            nextDayButton.interactable = false;
        }

        // カウントダウンパネルも非表示
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        // フラグもリセット
        isButtonClickable = false;
        isButtonProcessing = false;
        isCountdownActive = false; // フラグを実際に使用

        Debug.Log("全ての結果画面パネルを非表示にしました");
    }


    /// <summary>
    /// 次の日ボタンが押された時の処理
    /// </summary>
    private void OnNextDayButtonClicked()
    {
        // 重複クリック防止ガード
        if (isButtonProcessing)
        {
            Debug.Log("ボタン処理中のため無効");
            return;
        }

        if (!isButtonClickable)
        {
            Debug.Log("ボタンがクリック不可状態");
            return;
        }

        isButtonProcessing = true;
        isButtonClickable = false;

        Debug.Log("次の日ボタンがクリックされました");

        // シンプルなボタンアニメーション
        if (nextDayButton != null)
        {
            StartCoroutine(ButtonClickAnimation(nextDayButton.gameObject));
        }

        // GameManagerに通知
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnNextDayButtonPressed();
        }

        // 少し遅延してからフラグをリセット
        StartCoroutine(ResetButtonFlags());
    }

    /// <summary>
    /// ボタンの遅延有効化
    /// </summary>
    private System.Collections.IEnumerator DelayedButtonActivation()
    {
        yield return new WaitForSeconds(1.5f); // 1.5秒待ってから表示・有効化

        if (nextDayButton != null)
        {
            // 表示と有効化を同時実行
            nextDayButton.gameObject.SetActive(true);
            nextDayButton.interactable = true;
            isButtonClickable = true;

            // 出現アニメーション実行
            StartCoroutine(SimpleButtonAnimation(nextDayButton.gameObject));
            Debug.Log("nextDayButtonを表示・有効化しました");
        }
    }

    /// <summary>
    /// ボタンフラグリセット用コルーチン
    /// </summary>
    private System.Collections.IEnumerator ResetButtonFlags()
    {
        yield return new WaitForSeconds(0.5f);
        isButtonProcessing = false;
        Debug.Log("ボタン処理フラグをリセット");
    }

    /// <summary>
    /// ボタンの遅延表示アニメーション
    /// </summary>
    private System.Collections.IEnumerator DelayedButtonAnimation()
    {
        yield return new WaitForSeconds(0.5f); // 0.5秒遅れて表示

        if (nextDayButton != null)
        {
            StartCoroutine(SimpleButtonAnimation(nextDayButton.gameObject));
        }
    }

    /// <summary>
    /// シンプルなボタンアニメーション
    /// </summary>
    private System.Collections.IEnumerator SimpleButtonAnimation(GameObject button)
    {
        button.transform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            button.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        button.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// ボタンクリック時のシンプルアニメーション
    /// </summary>
    private System.Collections.IEnumerator ButtonClickAnimation(GameObject button)
    {
        Vector3 originalScale = button.transform.localScale;

        // 押し込み
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            button.transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.9f, t);
            yield return null;
        }

        // 戻り
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            button.transform.localScale = Vector3.Lerp(originalScale * 0.9f, originalScale, t);
            yield return null;
        }

        button.transform.localScale = originalScale;
    }

    private System.Collections.IEnumerator TextScaleAnimation(TMP_Text text)
    {
        Vector3 originalScale = text.transform.localScale;
        text.transform.localScale = originalScale * 1.2f;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            text.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }

        text.transform.localScale = originalScale;
    }

    private System.Collections.IEnumerator PanelScaleAnimation(GameObject panel)
    {
        panel.transform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easedT);
            yield return null;
        }

        panel.transform.localScale = Vector3.one;
    }

   
    public void ShowPhaseChangeMessage(string message)
    {
        StartCoroutine(ShowTemporaryMessage(message, 2f));
    }

    private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        yield return new WaitForSeconds(duration);
    }
}