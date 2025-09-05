using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanelsManager : MonoBehaviour
{
    [Header("情報パネル")]
    public GameObject howToPlayPanel;     // あそびかたパネル
    public GameObject aboutKemonoPanel;   // けもフレ説明パネル  
    public GameObject creditsPanel;       // クレジットパネル

    [Header("あそびかたパネル")]
    public TextMeshProUGUI howToPlayText;
    public Button howToPlayBackButton;

    [Header("けもフレ説明パネル")]
    public TextMeshProUGUI aboutKemonoText;
    public Button aboutKemonoBackButton;

    [Header("クレジットパネル")]
    public TextMeshProUGUI creditsText;
    public Button creditsBackButton;

    void Start()
    {
        InitializeInfoPanels();
        SetupEventListeners();
    }

    void InitializeInfoPanels()
    {
        // 全パネルを非表示にする
        HideAllPanels();

        // テキスト内容を設定
        SetupTexts();
    }

    void SetupEventListeners()
    {
        // 戻るボタンのイベント設定
        if (howToPlayBackButton != null)
            howToPlayBackButton.onClick.AddListener(HideAllPanels);

        if (aboutKemonoBackButton != null)
            aboutKemonoBackButton.onClick.AddListener(HideAllPanels);

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(HideAllPanels);
    }

    void SetupTexts()
    {
        // あそびかたテキスト
        if (howToPlayText != null)
        {
            howToPlayText.text = "ジャパまん手作りキットを使って、心を込めてフレンズにジャパまんをプレゼントしよう。\n" +
                                "腹ペコなフレンズをお腹いっぱいにできたらノルマクリア！\n" +
                                "お腹を満たせなければゲームオーバーです。\n" +
                                "要は、おっきな愛で、でっかく育てよう！";
        }
        // けもフレ説明テキスト  
        if (aboutKemonoText != null)
        {
            aboutKemonoText.text = "けものフレンズを知らない人へ\n" +
                                  "このゲームは、けものフレンズの二次創作です。この機会に「けもフレ」をぜひお見知り置きください。\n" +
                                  "この子の名前は『ホワイトライオン』です。動物のホワイトライオンが、なんやかんやで女の子（フレンズ）になりました。\n" +
                                  "立派なたてがみかありますが女の子です。かわいいですね。\n" +
                                  "ジャパまんとは、フレンズたちの主食のおまんじゅうです。お肉はそんなに好まないらしいです。\n" +
                                  "そしてこのゲームでめっちゃ叩かされるのは『ジャパまん手作りキット』です。\n" +
                                  "アプリゲームにおいては好感度上限突破のための貴重なアイテムです。\n" +
                                  "懇意のフレンズと一緒にジャパまんを手作りすることで、すごく仲が深まるんです。\n" +
                                  "本来は叩いたら無限にジャパまんが出てくるものではありません。";
        }
        // クレジットテキスト
        if (creditsText != null)
        {
            creditsText.text = "開発・企画：八雲吉祥　音楽・効果音：ヴォストク\n" +
                              "プログラミング支援：Claude Sonnet4";
        }
    }

    // === 公開メソッド（TitleManagerから呼び出される） ===

    public void ShowHowToPlay()
    {
        Debug.Log("あそびかたパネル表示");
        HideAllPanels();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void ShowAboutKemono()
    {
        Debug.Log("けもフレ説明パネル表示");
        HideAllPanels();
        if (aboutKemonoPanel != null)
            aboutKemonoPanel.SetActive(true);
    }

    public void ShowCredits()
    {
        Debug.Log("クレジットパネル表示");
        HideAllPanels();
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void HideAllPanels()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (aboutKemonoPanel != null)
            aboutKemonoPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }
}