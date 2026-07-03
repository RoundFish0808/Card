using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class cheatPanel : BasePanel
{
    private CardDataSO SeletCard;

    public Button checkButton;
    public override void Init()
    {
        //调用作弊方法,具体为生成五张牌并显示到界面上
        checkButton.onClick.AddListener(()=>{

        });

    }
}
