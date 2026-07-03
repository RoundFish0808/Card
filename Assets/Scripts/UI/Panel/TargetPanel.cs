using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetPanel : BasePanel
{
    public Text targetScore;
    public Text curScore;
    public Text vigScore;
    public override void Init()
    {
        targetScore.text="目标:";//[DRAIVORY]这里需要你+一个计算系统的结果
        curScore.text="得分:";
        vigScore.text="利率";
    }
}
