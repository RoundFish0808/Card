using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class teachPanel_2 : BasePanel
{
   public Button nextBtn;
   public Button lastBtn;

   public Button closeBtn;
    public override void Init()
    {
        nextBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<teachPanel_2>();
            UIManager.Instance.ShowPanel<teachPanel_3>();
        });

        lastBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<teachPanel_2>();
            UIManager.Instance.ShowPanel<teachPanel_1>();
        });

        closeBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<teachPanel_2>();
        });
    }

}
