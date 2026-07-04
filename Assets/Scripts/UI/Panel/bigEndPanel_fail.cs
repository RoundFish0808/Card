using UnityEngine;
using UnityEngine.UI;

public class bigEndPanel_fail : BasePanel, IBigEndPanelDataReceiver
{
    public Button reStartBtn;
    public Text titleText;
    public Text totalScoreText;
    public Text baseInterestText;
    public Text cheatInterestText;
    public Text finalSettlementText;

    public BigPanelResultData Data { get; private set; }

    public override void Init()
    {
        if (reStartBtn != null)
        {
            reStartBtn.onClick.RemoveAllListeners();
            reStartBtn.onClick.AddListener(() =>
            {
                CardManager.Instance?.RestartGame();
                UIManager.Instance.HidePanel<bigEndPanel_fail>();
            });
        }

        if (Data == null && GameEconomyBridge.Instance != null)
            Data = GameEconomyBridge.Instance.LatestBigPanelResult;

        ApplyData();
    }

    public void SetData(BigPanelResultData data)
    {
        Data = data;
        ApplyData();
    }

    private void ApplyData()
    {
        SetText(titleText, "本局失败");

        if (Data == null)
        {
            SetText(totalScoreText, "总金额:");
            SetText(baseInterestText, "基础利率:");
            SetText(cheatInterestText, "出千利率:");
            SetText(finalSettlementText, "最终结算金额:");
            return;
        }

        SetText(totalScoreText, "总金额:" + Data.BigRoundScore);
        SetText(baseInterestText, "基础利率:" + Data.BaseInterestRate.ToString("P0"));
        SetText(cheatInterestText, "出千利率:" + Data.CheatInterestRate.ToString("P0"));
        SetText(finalSettlementText, "最终结算金额:" + Data.FinalSettlementAmount);
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}