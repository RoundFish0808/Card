using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TargetPanel : BasePanel
{
    public Text targetScore;
    public Text curScore;
    public Text vigScore;

    private bool subscribed;

    public override void Init()
    {
        StartCoroutine(WaitForEconomyAndInit());
    }

    private IEnumerator WaitForEconomyAndInit()
    {
        float waitTime = 0f;
        Debug.Log("[TargetPanel] 开始等待 GameEconomyBridge 初始化...");

        while (GameEconomyBridge.Instance == null)
        {
            waitTime += Time.deltaTime;
            if (waitTime > 5f)
            {
                Debug.LogError("[TargetPanel] 等待 5 秒了 GameEconomyBridge.Instance 还是 null！经济系统没初始化成功！");
                yield break;
            }
            yield return null;
        }

        Debug.Log($"[TargetPanel] 等到了 GameEconomyBridge，等了 {waitTime:F2} 秒，TargetScore={GameEconomyBridge.Instance.TargetScore}");
        TrySubscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribed && GameEconomyBridge.Instance != null)
            GameEconomyBridge.Instance.OnEconomyChanged -= Refresh;
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
        if (target == null)
        {
            Debug.LogError($"[TargetPanel] SetText 失败：target Text 为空！value={value}");
            return;
        }
        target.text = value;
        Debug.Log($"[TargetPanel] SetText 成功：{target.name} = {value}");
    }
}
