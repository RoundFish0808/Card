using UnityEngine;
using UnityEngine.UI;

public class bigEndPanel_suc : BasePanel, IBigEndPanelDataReceiver
{
    public Button nextLevelBtn;

    public BigPanelResultData Data { get; private set; }

    private Text titleText;
    private Text totalScoreText;
    private Text baseInterestText;
    private Text cheatInterestText;
    private Text finalSettlementText;

    public override void Init()
    {
        EnsureRuntimeTexts();

        if (nextLevelBtn != null)
        {
            nextLevelBtn.onClick.RemoveAllListeners();
            nextLevelBtn.onClick.AddListener(() =>
            {
                CardManager.Instance?.StartNextLevel();
                UIManager.Instance.HidePanel<bigEndPanel_suc>();
            });
        }

        if (Data == null && GameEconomyBridge.Instance != null)
            Data = GameEconomyBridge.Instance.LatestBigPanelResult;

        RefreshRuntimeTexts();
    }

    public void SetData(BigPanelResultData data)
    {
        Data = data;
        EnsureRuntimeTexts();
        RefreshRuntimeTexts();
    }

    private void EnsureRuntimeTexts()
    {
        if (titleText != null)
            return;

        Transform oldRoot = transform.Find("TempBigEndPanelContent");
        GameObject root = oldRoot != null ? oldRoot.gameObject : new GameObject("TempBigEndPanelContent", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(980f, 460f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = root.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateLine(root.transform, "Title", 48, 74f);
        totalScoreText = CreateLine(root.transform, "TotalScore", 30, 46f);
        baseInterestText = CreateLine(root.transform, "BaseInterest", 30, 46f);
        cheatInterestText = CreateLine(root.transform, "CheatInterest", 30, 46f);
        finalSettlementText = CreateLine(root.transform, "FinalSettlement", 30, 56f);
    }

    private Text CreateLine(Transform parent, string objectName, int fontSize, float preferredHeight)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.raycastTarget = false;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;

        return text;
    }

    private void RefreshRuntimeTexts()
    {
        if (titleText == null)
            return;

        titleText.text = "\u672c\u5c40\u80dc\u5229";

        if (Data == null)
        {
            totalScoreText.text = "\u603b\u91d1\u989d:";
            baseInterestText.text = "\u57fa\u7840\u5229\u7387:";
            cheatInterestText.text = "\u51fa\u5343\u5229\u7387:";
            finalSettlementText.text = "\u6700\u7ec8\u7ed3\u7b97\u91d1\u989d:";
            return;
        }

        totalScoreText.text = "\u603b\u91d1\u989d:" + Data.BigRoundScore;
        baseInterestText.text = "\u57fa\u7840\u5229\u7387:" + Data.BaseInterestRate.ToString("P0");
        cheatInterestText.text = "\u51fa\u5343\u5229\u7387:" + Data.CheatInterestRate.ToString("P0");
        finalSettlementText.text = "\u6700\u7ec8\u7ed3\u7b97\u91d1\u989d:" + Data.FinalSettlementAmount;
    }
}
