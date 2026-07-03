using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class smallEndPanel : BasePanel
{
    public Text title;
    public Text curScore;
    public Text curVigScore;
    public Text cardPatten;
    public Button continueBtn;
    public override void Init()
    {
        continueBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<smallEndPanel>();
        });
        //[DRAIVORY]这里需要你+一个计算系统的结果2
        title.text="";
        curScore.text="";
        curVigScore.text="";
        cardPatten.text="";
    }
}
