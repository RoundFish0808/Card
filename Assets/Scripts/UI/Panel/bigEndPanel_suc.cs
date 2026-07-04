using UnityEngine;
using UnityEngine.UI;

public class bigEndPanel_suc : BasePanel, IBigEndPanelDataReceiver
{
    private const string SuccessAmountColor = "#2ECC71";

    public Button nextLevelBtn;
    public Text titleText;
    public Text totalScoreText;
    public Text baseInterestText;
    public Text cheatInterestText;
    public Text finalSettlementText;

    public BigPanelResultData Data { get; private set; }

    public override void Init()
    {
        ResolveExternalComponents();
        RegisterButton();

        if (Data == null && GameEconomyBridge.Instance != null)
            Data = GameEconomyBridge.Instance.LatestBigPanelResult;

        ApplyData();
    }

    public void SetData(BigPanelResultData data)
    {
        Data = data;
        ResolveExternalComponents();
        RegisterButton();
        ApplyData();
    }

    private void RegisterButton()
    {
        if (nextLevelBtn == null)
            return;

        nextLevelBtn.onClick.RemoveAllListeners();
        nextLevelBtn.onClick.AddListener(() =>
        {
            CardManager.Instance?.StartNextLevel();
            UIManager.Instance.HidePanel<bigEndPanel_suc>();
        });
    }

    private void ApplyData()
    {
        SetText(titleText, "本局成功");

        if (Data == null)
        {
            SetText(totalScoreText, "本局金额:");
            SetText(baseInterestText, "基础利率:");
            SetText(cheatInterestText, "出千利率:");
            SetText(finalSettlementText, FormatColoredValue("结算金额:", "", SuccessAmountColor));
            return;
        }

        SetText(totalScoreText, "本局金额:" + Data.BigRoundScore);
        SetText(baseInterestText, "基础利率:" + Data.BaseInterestRate.ToString("P0"));
        SetText(cheatInterestText, "出千利率:" + Data.CheatInterestRate.ToString("P0"));
        SetText(finalSettlementText, FormatColoredValue("结算金额:", Data.FinalSettlementAmount.ToString(), SuccessAmountColor));
    }

    private void ResolveExternalComponents()
    {
        if (nextLevelBtn == null)
            nextLevelBtn = GetComponentInChildren<Button>(true);

        if (titleText == null)
            titleText = FindTextByObjectName("Title");
        if (totalScoreText == null)
            totalScoreText = FindChildTextByContainerName("curScore");
        if (baseInterestText == null)
            baseInterestText = FindChildTextByContainerName("baseInterest");
        if (cheatInterestText == null)
            cheatInterestText = FindChildTextByContainerName("cheatInterest");
        if (finalSettlementText == null)
            finalSettlementText = FindChildTextByContainerName("lastScore");
    }

    private Text FindChildTextByContainerName(string objectName)
    {
        Transform target = FindTransformByName(transform, objectName);
        return target != null ? target.GetComponentInChildren<Text>(true) : null;
    }

    private Text FindTextByObjectName(string objectName)
    {
        Transform target = FindTransformByName(transform, objectName);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private Transform FindTransformByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindTransformByName(root.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private string FormatColoredValue(string label, string value, string color)
    {
        return label + "<color=" + color + ">" + value + "</color>";
    }

    private void SetText(Text target, string value)
    {
        if (target == null)
            return;

        target.supportRichText = true;
        target.text = value;
    }
}
