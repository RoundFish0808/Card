using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class smallEndPanel : BasePanel, ISmallEndPanelDataReceiver
{
    public Text title;
    public Text curScore;
    public Text curVigScore;
    public Text cardPatten;
    public Button continueBtn;

    private SmallPanelResultData data;
    public SmallPanelResultData Data => data;

    public override void Init()
    {
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() =>
            {
                CardManager.Instance?.ContinueAfterSmallRound();
                UIManager.Instance.HidePanel<smallEndPanel>();
            });
        }

        if (data == null && GameEconomyBridge.Instance != null)
            data = GameEconomyBridge.Instance.LatestSmallPanelResult;

        ApplyData();
    }

    public void SetData(SmallPanelResultData resultData)
    {
        data = resultData;
        ApplyData();
    }

    private void ApplyData()
    {
        if (data == null)
        {
            SetText(title, "");
            SetText(curScore, "");
            SetText(curVigScore, "");
            SetText(cardPatten, "");
            return;
        }

        SetText(title, "\u7b2c" + data.SmallIndex + "\u5c0f\u5c40");
        SetText(curScore, "\u5c0f\u5c40\u5f97\u5206:" + data.PlayerSmallScore);
        SetText(curVigScore, "\u5c0f\u5c40\u5229\u7387:" + data.SmallInterestRate.ToString("P0"));
        SetText(cardPatten, BuildCardPatternText(data));
    }

    private string BuildCardPatternText(SmallPanelResultData resultData)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("\u5c0f\u5c40\u724c\u578b:");

        foreach (EconomyRoundResultData round in resultData.RoundResults)
        {
            builder.AppendLine();
            builder.Append("\u7b2c");
            builder.Append(round.RoundIndex);
            builder.Append("\u56de\u5408 ");
            builder.Append(string.IsNullOrEmpty(round.HandTypeName) ? "-" : round.HandTypeName);
            builder.Append(" ");
            builder.Append(round.PlayerScore);
            builder.Append(" (");
            builder.Append(round.PrincipalBeforeRound);
            builder.Append("x");
            builder.Append(round.FormulaBase);
            builder.Append("x");
            builder.Append(round.HandMultiplier.ToString("0.##"));
            builder.Append(", diff ");
            builder.Append(round.WinDiffScore);
            builder.Append(")");
        }

        return builder.ToString();
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
