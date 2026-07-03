using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class teachPanel_1 : BasePanel
{
    public Button nextBtn;

     public Button closeBtn;
    public override void Init()
    {
        nextBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<teachPanel_1>();
            UIManager.Instance.ShowPanel<teachPanel_2>();
        });

        closeBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<teachPanel_1>();
        });
    }
}
