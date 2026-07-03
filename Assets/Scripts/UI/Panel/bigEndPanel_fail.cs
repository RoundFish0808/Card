using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bigEndPanel_fail : BasePanel
{
    public Button reStartBtn;
    public override void Init()
    {
        reStartBtn.onClick.AddListener(() =>
        {
            //重新开始游戏
        });
    }
}
