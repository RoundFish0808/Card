using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager instance = new UIManager();
    public static UIManager Instance => instance;

    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();
    private Transform canvasTrans;

    private UIManager() { }

    /// <summary>
    /// 获取 Canvas，为 null 时自动去场景里找
    /// </summary>
    private Transform CanvasTrans
    {
        get
        {
            if (canvasTrans == null)
            {
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null)
                {
                    canvasTrans = canvasObj.transform;
                }
                else
                {
                    Debug.LogError("UIManager 找不到场景中的 Canvas！");
                }
            }
            return canvasTrans;
        }
    }

    //显示面板
    public T ShowPanel<T>() where T:BasePanel
    {
        string panelName = typeof(T).Name;

        if (panelDic.ContainsKey(panelName))
            return panelDic[panelName] as T;

        GameObject panelPrefab = Resources.Load<GameObject>("UI/" + panelName);
        if (panelPrefab == null && !string.IsNullOrEmpty(panelName))
        {
            string upperFirstName = char.ToUpper(panelName[0]) + panelName.Substring(1);
            panelPrefab = Resources.Load<GameObject>("UI/" + upperFirstName);
        }

        if (panelPrefab == null)
        {
            Debug.LogError("UIManager 找不到面板预制体：Resources/UI/" + panelName);
            return null;
        }

        if (CanvasTrans == null)
        {
            Debug.LogError("UIManager Canvas 为 null，无法创建面板：" + panelName);
            return null;
        }

        GameObject panelObj = GameObject.Instantiate(panelPrefab);
        panelObj.transform.SetParent(CanvasTrans, false);

        T panel = panelObj.GetComponent<T>();
        panelDic.Add(panelName, panel);
        panel.ShowMe();

        return panel;
    }

    //隐藏面板
    public void HidePanel<T>( bool isFade = true ) where T:BasePanel
    {
        string panelName = typeof(T).Name;
        if( panelDic.ContainsKey(panelName) )
        {
            if( isFade )
            {
                panelDic[panelName].HideMe(() =>
                {
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                GameObject.Destroy(panelDic[panelName].gameObject);
                panelDic.Remove(panelName);
            }
        }
    }

    //获得面板
    public T GetPanel<T>() where T:BasePanel
    {
        string panelName = typeof(T).Name;
        if(panelDic.ContainsKey(panelName))
            return panelDic[panelName] as T;

        return null;
    }

    //判断面板是否显示
    public bool IsPanelShown<T>() where T:BasePanel
    {
        string panelName = typeof(T).Name;
        return panelDic.ContainsKey(panelName) && panelDic[panelName] != null;
    }

    //隐藏所有面板
    public void HideAllPanels(bool isFade = true)
    {
        List<string> keys = new List<string>(panelDic.Keys);
        foreach (string key in keys)
        {
            if (panelDic.ContainsKey(key) && panelDic[key] != null)
            {
                if (isFade)
                {
                    string panelKey = key;
                    panelDic[key].HideMe(() =>
                    {
                        if (panelDic.ContainsKey(panelKey))
                        {
                            GameObject.Destroy(panelDic[panelKey].gameObject);
                            panelDic.Remove(panelKey);
                        }
                    });
                }
                else
                {
                    GameObject.Destroy(panelDic[key].gameObject);
                    panelDic.Remove(key);
                }
            }
        }
    }
}
