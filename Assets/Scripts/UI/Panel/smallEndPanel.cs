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

        SetText(title, string.Format("\u7b2c{0}\u5c0f\u5c40", data.SmallIndex));
        SetText(curScore, string.Format("\u5c0f\u5c40\u5f97\u5206:{0}", data.PlayerSmallScore));
        SetText(curVigScore, string.Format("\u5c0f\u5c40\u5229\u7387:{0}", data.SmallInterestRate.ToString("P0")));
        SetText(cardPatten, string.Format("\u5c0f\u5c40\u724c\u578b:{0}", GetCurrentHandTypeName()));
    }

    private string GetCurrentHandTypeName()
    {
        if (data == null || data.RoundResults == null || data.RoundResults.Count <= 0)
            return "-";

        string handTypeName = data.RoundResults[data.RoundResults.Count - 1].HandTypeName;
        return string.IsNullOrEmpty(handTypeName) ? "-" : handTypeName;
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
