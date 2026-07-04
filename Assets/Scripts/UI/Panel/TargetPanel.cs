using UnityEngine;
using UnityEngine.UI;

public class TargetPanel : BasePanel
{
    public Text targetScore;
    public Text curScore;
    public Text vigScore;

    private bool subscribed;

    public override void Init()
    {
        TrySubscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribed && GameEconomyBridge.Instance != null)
            GameEconomyBridge.Instance.OnEconomyChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (subscribed)
            return;

        if (TrySubscribe())
            Refresh();
    }

    private bool TrySubscribe()
    {
        if (subscribed || GameEconomyBridge.Instance == null)
            return subscribed;

        GameEconomyBridge.Instance.OnEconomyChanged += Refresh;
        subscribed = true;
        return true;
    }

    private void Refresh()
    {
        GameEconomyBridge economy = GameEconomyBridge.Instance;
        if (economy == null)
        {
            SetText(targetScore, "\u76ee\u6807:");
            SetText(curScore, "\u5f97\u5206:");
            SetText(vigScore, "\u5229\u7387:");
            return;
        }

        SetText(targetScore, "\u76ee\u6807:" + economy.TargetScore);
        SetText(curScore, "\u5f97\u5206:" + economy.PlayerTotalScore);
        SetText(vigScore, "\u5229\u7387:" + economy.CurrentInterestRate.ToString("P0"));
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
