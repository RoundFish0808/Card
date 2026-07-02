using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartPanel : BasePanel
{
    public Button strBtn;
    public override void Init()
    {
        strBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Beta_2");
        });
    }

}