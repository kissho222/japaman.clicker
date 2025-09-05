using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameOverDialogManager : MonoBehaviour
{
    public static GameOverDialogManager Instance { get; private set; }

    [Header("吹き出しUI")]
    public GameObject speechBubble;                // 吹き出し全体
    public TextMeshProUGUI speechText;            // 発言テキスト
    public Image characterImage;                   // フレンズの画像（表情変更用）

    [Header("アニメーション設定")]
    public float bubbleShowDuration = 0.3f;       // 吹き出し表示時間
    public float textTypeDuration = 1.5f;         // テキスト表示時間（短縮）
    public float bubbleHideDuration = 0.2f;       // 吹き出し非表示時間
    public float pauseBetweenDialogs = 0.8f;      // 発言間の間隔（調整）

    [Header("フレンズ表情")]
    public Sprite normalExpression;               // 通常表情
    public Sprite sadExpression;                  // 悲しい表情
    public Sprite encouragingExpression;          // 励ましの表情

    private System.Action onDialogComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 初期状態では吹き出しを非表示
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
    }

    /// <summary>
    /// ゲームオーバー時のダイアログシーケンスを開始
    /// </summary>
    public void StartGameOverDialog(int stage, long japamanCount, long goalCount, System.Action onComplete)
    {
        onDialogComplete = onComplete;

        Debug.Log("=== ゲームオーバーダイアログ開始 ===");

        // ステージに応じたセリフを生成
        List<DialogData> dialogs = GenerateGameOverDialogs(stage, japamanCount, goalCount);

        StartCoroutine(PlayDialogSequence(dialogs));
    }
    /// <summary>
    /// ステージ15クリア時のダイアログシーケンスを開始
    /// </summary>
    public void StartStage15ClearDialog(System.Action onComplete)
    {
        onDialogComplete = onComplete;

        Debug.Log("=== ステージ15クリアダイアログ開始 ===");

        List<DialogData> dialogs = GenerateStage15ClearDialogs();

        StartCoroutine(PlayDialogSequence(dialogs));
    }

    /// <summary>
    /// ステージ15クリア時のセリフを生成
    /// </summary>
    private List<DialogData> GenerateStage15ClearDialogs()
    {
        List<DialogData> dialogs = new List<DialogData>();

        dialogs.Add(new DialogData("ふぅ～！　もう食べられません～！", encouragingExpression));
        dialogs.Add(new DialogData("こんなに毎日お腹いっぱいになるなんて初めてです！", normalExpression));
        dialogs.Add(new DialogData("お空の上って、こんな風になってるんですねぇ。", normalExpression));
        dialogs.Add(new DialogData("たくさんじゃぱまんを作ってくれた隊長ちゃんのおかげで", encouragingExpression));
        dialogs.Add(new DialogData("こんなに大きくなれました！", encouragingExpression));
        dialogs.Add(new DialogData("でも、もっともっと大きくなれば、", normalExpression));
        dialogs.Add(new DialogData("宇宙の猫さんに会えるかもしれませんよ！", encouragingExpression));
        dialogs.Add(new DialogData("ん～？ウサギでしたっけ？", normalExpression));
        dialogs.Add(new DialogData("まあ、またいつか、一緒に目指しましょうね！", encouragingExpression));

        return dialogs;
    }

    /// <summary>
    /// ゲームオーバー時のセリフを生成
    /// </summary>
    private List<DialogData> GenerateGameOverDialogs(int stage, long japamanCount, long goalCount)
    {
        List<DialogData> dialogs = new List<DialogData>();

        float achievementRate = (float)japamanCount / goalCount;

        // ステージ数と達成率に基づいてセリフを選択
        if (stage <= 5)
        {
            if (achievementRate >= 0.9f)
            {
                dialogs.Add(new DialogData("うーん、あとちょっと物足りないですねぇ。", normalExpression));
                dialogs.Add(new DialogData("デザートはまだですかぁ？", normalExpression));
            }
            else if (achievementRate >= 0.5f) // 🔥 0.7f → 0.5f に変更
            {
                dialogs.Add(new DialogData("たくさん作ってくれましたけど、まだまだ食べられますよ～！", normalExpression));
                dialogs.Add(new DialogData("え？もう終わりなんですか？ざんねん……。", sadExpression));
            }
            else if (achievementRate > 0.0f) // 🔥 50%未満だが何か作った場合
            {
                dialogs.Add(new DialogData("……ジャパまんお腹いっぱいくれるって言ったじゃないですか！", sadExpression));
                dialogs.Add(new DialogData("ぜんぜん物足りないので、ギンギツネちゃんにもっと貰いに行きますねぇ～", encouragingExpression));
            }
            else // 🔥 0%の場合（何も作れなかった場合）
            {
                dialogs.Add(new DialogData("あれ？ジャパまんは？", normalExpression));
                dialogs.Add(new DialogData("何も作れなかったんですか？", sadExpression));
            }
        }
        else if (stage <= 10)
        {
            if (achievementRate >= 0.9f)
            {
                dialogs.Add(new DialogData("あとちょっと、食べさせてくれませんか？", normalExpression));
                dialogs.Add(new DialogData("もうちょっとでお腹いっぱいなんです～……。", sadExpression));
            }
            else if (achievementRate >= 0.5f) // 🔥 0.7f → 0.5f に変更
            {
                dialogs.Add(new DialogData("はらはちぶんめってやつですかね？まだまだ食べられますよ～！", normalExpression));
                dialogs.Add(new DialogData("え？終わりなんですか？ざんねん……。", sadExpression));
            }
            else // 🔥 50%未満の場合
            {
                dialogs.Add(new DialogData("まだまだお腹すいてるんですけど、もうジャパまん無いんですか？", normalExpression));
                dialogs.Add(new DialogData("うーん、残念です……", sadExpression));
            }
        }
        else if (stage <= 14)
        {
            if (achievementRate >= 0.9f)
            {
                dialogs.Add(new DialogData("ジャパまんが、お腹にたーくさん！ですけど……", normalExpression));
                dialogs.Add(new DialogData("満腹にはもう一息でしたねぇ。", sadExpression));
            }
            else if (achievementRate >= 0.5f) // 🔥 0.7f → 0.5f に変更
            {
                dialogs.Add(new DialogData("まだ、大きくなれますよ！でも、もうおしまいですかぁ…", encouragingExpression));
                dialogs.Add(new DialogData("仕方がないので、ふわふわの雲を食べましょう！", encouragingExpression));
                dialogs.Add(new DialogData("……甘くなくてクリーミィじゃないただの空気の味ですぅ～……。", sadExpression));
            }
            else // 🔥 50%未満の場合
            {
                dialogs.Add(new DialogData("どうしましたか？もう作れなくなっちゃいましたか？", normalExpression));
                dialogs.Add(new DialogData("それとも……", normalExpression));
                dialogs.Add(new DialogData("これ以上大きくなるのが、怖くなっちゃいましたか？うふふ❤", encouragingExpression));
            }
        }
        else if (stage == 15 && achievementRate >= 0.9f)
        {
            dialogs.Add(new DialogData("あれ？あれ！？", normalExpression));
            dialogs.Add(new DialogData("ここまで来たのにおしまいですかぁ！？", sadExpression));
            dialogs.Add(new DialogData("うぅ～　あんまりです～……", sadExpression));
            dialogs.Add(new DialogData("せっかくこんなに大きくなって、", normalExpression));
            dialogs.Add(new DialogData("もっとジャパまんが食べられるとおもったのに……", sadExpression));
            dialogs.Add(new DialogData("……。", sadExpression));
            dialogs.Add(new DialogData("あの、よかったら……", normalExpression));
            dialogs.Add(new DialogData("また、ここまで連れてきてくださいね？", encouragingExpression));
        }

        // フォールバック（上記の条件に当てはまらない場合）
        if (dialogs.Count == 0)
        {
            dialogs.Add(new DialogData("お疲れさまでした！", normalExpression));
            dialogs.Add(new DialogData("また挑戦してくださいね！", encouragingExpression));
        }

        return dialogs;
    }

    /// <summary>
    /// ダイアログシーケンスを再生
    /// </summary>
    private IEnumerator PlayDialogSequence(List<DialogData> dialogs)
    {
        foreach (var dialog in dialogs)
        {
            // 🔥 各ダイアログ前にテキストクリア
            if (speechText != null)
            {
                speechText.text = "";
            }

            yield return StartCoroutine(ShowSingleDialog(dialog));
            yield return new WaitForSeconds(pauseBetweenDialogs);
        }

        // 🔥 全ダイアログ完了時にも最終クリア
        if (speechText != null)
        {
            speechText.text = "";
        }

        // 全ダイアログ完了
        Debug.Log("ゲームオーバーダイアログ完了");
        onDialogComplete?.Invoke();
    }

    /// <summary>
    /// 単一のダイアログを表示
    /// </summary>
    private IEnumerator ShowSingleDialog(DialogData dialog)
    {
        // 🔥 最初にテキストをクリア
        if (speechText != null)
        {
            speechText.text = "";
        }

        // フレンズの表情を変更
        if (characterImage != null && dialog.expression != null)
        {
            characterImage.sprite = dialog.expression;
        }

        // 吹き出し表示アニメーション
        yield return StartCoroutine(ShowBubble());

        // 🔥 表示アニメーション完了後にテキストをタイプライター風に表示
        yield return StartCoroutine(TypeText(dialog.text));

        // テキスト表示時間
        yield return new WaitForSeconds(textTypeDuration);

        // 🔥 非表示前にテキストをクリア
        if (speechText != null)
        {
            speechText.text = "";
        }

        // 吹き出し非表示アニメーション
        yield return StartCoroutine(HideBubble());
    }

    /// <summary>
    /// 吹き出し表示アニメーション
    /// </summary>
    private IEnumerator ShowBubble()
    {
        if (speechBubble == null) yield break;

        // 🔥 表示前にテキストを確実にクリア
        if (speechText != null)
        {
            speechText.text = "";
        }

        speechBubble.SetActive(true);
        speechBubble.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < bubbleShowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bubbleShowDuration;

            // イージング効果
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            speechBubble.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easedT);

            yield return null;
        }

        speechBubble.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 吹き出し非表示アニメーション
    /// </summary>
    private IEnumerator HideBubble()
    {
        if (speechBubble == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = speechBubble.transform.localScale;

        while (elapsed < bubbleHideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bubbleHideDuration;

            speechBubble.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        speechBubble.SetActive(false);
    }

    /// <summary>
    /// テキストをタイプライター風に表示
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        if (speechText == null) yield break;

        // 🔥 開始前に確実にクリア
        speechText.text = "";

        // 少し待ってからタイピング開始
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i <= text.Length; i++)
        {
            if (speechText != null) // null チェック追加
            {
                speechText.text = text.Substring(0, i);
            }
            yield return new WaitForSeconds(0.1f); // 1文字あたり0.1秒
        }
    }

    /// <summary>
    /// 強制的にダイアログを終了
    /// </summary>
    public void ForceEndDialog()
    {
        StopAllCoroutines();

        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }

        onDialogComplete?.Invoke();
    }
}

/// <summary>
/// ダイアログデータクラス
/// </summary>
[System.Serializable]
public class DialogData
{
    public string text;
    public Sprite expression;

    public DialogData(string text, Sprite expression)
    {
        this.text = text;
        this.expression = expression;
    }
    
}
