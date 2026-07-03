using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bigEndPanel_suc : BasePanel
{
    public Button nextLevelBtn;
    public override void Init()
    {
        nextLevelBtn.onClick.AddListener(() =>
        {
            //进入下一关
        });
    }
}
