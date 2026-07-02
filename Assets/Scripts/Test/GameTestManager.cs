using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 可复用的游戏功能测试管理器
/// 支持通过Inspector配置和UI按钮触发各种测试
/// 按 F3 键可以显示/隐藏测试面板
/// 如果需要添加新的测试功能，只需在 RegisterTestFunctions() 方法中添加
                                /*
                                testFunctions["分类: 测试名称"] = () => {
                                // 你的测试代码
                                GameTestManager.Instance?.Log("测试日志");
                                };*/
/// 
/// </summary>

public class GameTestManager : MonoBehaviour
{
    public static GameTestManager Instance { get; private set; }

    [Header("测试面板设置")]
    public bool showTestPanel = true;
    public KeyCode toggleKey = KeyCode.F3;
    public float panelWidth = 300f;
    public float panelHeight = 400f;

    [Header("测试数据")]
    public List<CardDataSO> testCards = new List<CardDataSO>();
    public AIDifficulty testDifficulty = AIDifficulty.Normal;

    // 测试功能列表
    private Dictionary<string, System.Action> testFunctions = new Dictionary<string, System.Action>();
    private Vector2 scrollPosition;
    private bool panelVisible = true;
    private string testOutput = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 只在播放模式下使用 DontDestroyOnLoad
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);

        RegisterTestFunctions();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            panelVisible = !panelVisible;
    }

    private void OnGUI()
    {
        if (!showTestPanel || !panelVisible) return;

        float x = Screen.width - panelWidth - 10f;
        float y = 10f;
        Rect rect = new Rect(x, y, panelWidth, panelHeight);

        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(rect);
        GUILayout.Space(10);

        // 标题
        GUI.skin.label.fontSize = 18;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Game Test Panel", GUILayout.Height(30));
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        GUI.skin.label.fontSize = 12;

        // 测试功能列表
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(panelHeight - 180));

        foreach (var test in testFunctions)
        {
            if (GUILayout.Button(test.Key, GUILayout.Height(35)))
            {
                Log("Executing: " + test.Key, Color.yellow);
                try
                {
                    test.Value?.Invoke();
                    Log("Success: " + test.Key, Color.green);
                }
                catch (System.Exception ex)
                {
                    Log("Error: " + ex.Message, Color.red);
                    Debug.LogException(ex);
                }
            }
        }

        GUILayout.EndScrollView();

        // 输出日志
        GUILayout.Space(5);
        GUI.skin.textArea.fontSize = 10;
        Color oldColor = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f);
        Rect logRect = GUILayoutUtility.GetRect(panelWidth - 20, 60);
        GUI.TextArea(logRect, testOutput.Length > 500 ? testOutput.Substring(testOutput.Length - 500) : testOutput);
        GUI.color = oldColor;

        GUILayout.EndArea();
    }

    private void Log(string message, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        testOutput = System.DateTime.Now.ToString("HH:mm:ss") + " " + message + "\n" + testOutput;
        Debug.Log("[Test] " + message);
    }

    private void Log(string message)
    {
        Log(message, Color.white);
    }

    /// <summary>
    /// 注册测试功能
    /// </summary>
    private void RegisterTestFunctions()
    {
        // ===== NPC系统测试 =====
        testFunctions["NPC: Initialize Hand"] = Test_NPC_InitializeHand;
        testFunctions["NPC: Generate Declaration"] = Test_NPC_GenerateDeclaration;
        testFunctions["NPC: Show Declaration"] = Test_NPC_ShowDeclaration;
        testFunctions["NPC: Full Flow (Init + Declare)"] = Test_NPC_FullFlow;
        testFunctions["NPC: Show Cards (Debug)"] = Test_NPC_ShowCardsDebug;
        testFunctions["NPC: Hide Cards (Debug)"] = Test_NPC_HideCardsDebug;
        testFunctions["NPC: Toggle Cards (Debug)"] = Test_NPC_ToggleCardsDebug;

        // ===== 玩家系统测试 =====
        testFunctions["Player: Draw 5 Cards"] = Test_Player_DrawCards;
        testFunctions["Player: Clear Hand"] = Test_Player_ClearHand;

        // ===== 战斗系统测试 =====
        testFunctions["Battle: Enter Attack Mode"] = Test_Battle_EnterAttackMode;
        testFunctions["Battle: Simulate Attack"] = Test_Battle_SimulateAttack;

        // ===== 工具测试 =====
        testFunctions["Utils: Refresh Layout"] = Test_Utils_RefreshLayout;
        testFunctions["Utils: Clear Console"] = Test_Utils_ClearConsole;
        testFunctions["Utils: List Scene Objects"] = Test_Utils_ListSceneObjects;
    }

    #region NPC System Tests

    private void Test_NPC_InitializeHand()
    {
        var npc = FindObjectOfType<NPCPresenter>();
        if (npc == null) { Log("No NPCPresenter found!", Color.red); return; }

        if (testCards.Count >= 3)
        {
            npc.InitializeHand(testCards.Take(3).ToList());
            Log("NPC initialized with 3 cards");
        }
        else
        {
            Log("Not enough test cards! Need 3, have " + testCards.Count, Color.yellow);
        }
    }

    private void Test_NPC_GenerateDeclaration()
    {
        var npc = FindObjectOfType<NPCPresenter>();
        if (npc == null) { Log("No NPCPresenter found!", Color.red); return; }

        // 先初始化手牌
        if (testCards.Count >= 3)
        {
            npc.InitializeHand(testCards.Take(3).ToList());
        }

        npc.GenerateAndShowDeclaration();
        Log("Declaration generated and shown");
    }

    private void Test_NPC_ShowDeclaration()
    {
        var declView = FindObjectOfType<DeclarationView>();
        if (declView == null) { Log("No DeclarationView found!", Color.red); return; }

        string testText = GetTestTextByDifficulty();
        declView.ShowDeclaration(testText);
        Log("Showing: " + testText);
    }

    private string GetTestTextByDifficulty()
    {
        switch (testDifficulty)
        {
            case AIDifficulty.Easy: return "左中右，中间最强，我手里有一张黑桃A";
            case AIDifficulty.Normal: return "看好我的中间！";
            case AIDifficulty.Hard: return "让我看看你的实力。";
            case AIDifficulty.Nightmare: return "……";
            default: return "测试宣言文本";
        }
    }

    private void Test_NPC_FullFlow()
    {
        Test_NPC_InitializeHand();
        Test_NPC_GenerateDeclaration();
    }

    private void Test_NPC_ShowCardsDebug()
    {
        // 仅用于调试：显示 NPC 所有手牌/槽位牌正面，不改变实际战斗结算。
        SetNpcCardsDebugVisible(true);
    }

    private void Test_NPC_HideCardsDebug()
    {
        // 回到正式玩法显示：NPC 未被攻击的牌保持盖住。
        SetNpcCardsDebugVisible(false);
    }

    private void Test_NPC_ToggleCardsDebug()
    {
        var cardManager = FindObjectOfType<CardManager>();
        var npc = FindObjectOfType<NPCPresenter>();
        if (cardManager == null && npc == null) { Log("No NPC cards found to toggle", Color.red); return; }

        bool currentState = cardManager != null ? cardManager.debugShowNpcCards : npc.DebugShowFront;
        SetNpcCardsDebugVisible(!currentState);
    }

    private void SetNpcCardsDebugVisible(bool visible)
    {
        var cardManager = FindObjectOfType<CardManager>();
        if (cardManager != null)
        {
            // 通过 CardManager 设置，后续 NPC 补牌/新小局也会沿用当前调试显示状态。
            cardManager.SetNPCDebugCardsVisible(visible);
            Log("NPC cards debug view: " + (visible ? "SHOW" : "HIDE"), Color.cyan);
            return;
        }

        var npc = FindObjectOfType<NPCPresenter>();
        if (npc == null) { Log("No NPCPresenter found!", Color.red); return; }

        npc.SetDebugShowFront(visible);
        Log("NPC cards debug view: " + (visible ? "SHOW" : "HIDE"), Color.cyan);
    }

    #endregion

    #region Player System Tests

    private void Test_Player_DrawCards()
    {
        // 获取玩家的 HandController
        var handCtrl = FindObjectOfType<HandController>();
        if (handCtrl == null) { Log("No HandController found!", Color.red); return; }

        // 通过 HandPresenter 添加卡牌，或者直接实例化
        var handPresenter = FindObjectOfType<HandPresenter>();
        if (handPresenter != null && handPresenter.cardPrefab != null)
        {
            for (int i = 0; i < 5 && i < testCards.Count; i++)
            {
                var cardObj = Instantiate(handPresenter.cardPrefab, handCtrl.transform);
                var cardView = cardObj.GetComponent<CardView>();
                if (cardView != null)
                    cardView.Init(testCards[i]);
                handCtrl.AddCard(cardObj, false);
            }
            handCtrl.UpdateCardLayout();
            Log("Added " + Mathf.Min(5, testCards.Count) + " cards to player hand");
        }
        else
        {
            Log("No HandPresenter or CardPrefab configured", Color.yellow);
        }
    }

    private void Test_Player_ClearHand()
    {
        var handCtrl = FindObjectOfType<HandController>();
        if (handCtrl == null) { Log("No HandController found!", Color.red); return; }

        handCtrl.ClearCards();
        Log("Player hand cleared");
    }

    #endregion

    #region Battle System Tests

    private void Test_Battle_EnterAttackMode()
    {
        var battleField = FindObjectOfType<BattleFieldView>();
        if (battleField == null) { Log("No BattleFieldView found!", Color.red); return; }

        // 切换到攻击模式（如果有这个功能的话）
        // battleField.SetBattleMode(true);
        // 或者直接设置状态
        battleField.isPlayerTurn = false;
        Log("Set to enemy turn / attack mode");
    }

    private void Test_Battle_SimulateAttack()
    {
        var attackSys = FindObjectOfType<AttackSystem>();
        if (attackSys == null) { Log("No AttackSystem found!", Color.red); return; }

        var battleField = FindObjectOfType<BattleFieldView>();
        if (battleField == null) { Log("No BattleFieldView!", Color.red); return; }

        var playerSlots = battleField.playerSlots.Where(s => s != null && s.IsOccupied).ToList();
        var npcSlots = battleField.npcSlots.Where(s => s != null && s.IsOccupied).ToList();

        if (playerSlots.Count == 0 || npcSlots.Count == 0)
        {
            Log("Need cards on both sides for attack test!", Color.yellow);
            return;
        }

        var attacker = playerSlots[0].CurrentCard;
        var target = npcSlots[0].CurrentCard;

        string attackerName = attacker?.CardData != null ? attacker.CardData.cardName : "Unknown";
        string targetName = target?.CardData != null ? target.CardData.cardName : "Unknown";

        Log("Simulating attack: " + attackerName + " vs " + targetName);
    }

    #endregion

    #region Utility Tests

    private void Test_Utils_RefreshLayout()
    {
        var handCtrls = FindObjectsOfType<HandController>();
        foreach (var hc in handCtrls)
        {
            hc.UpdateCardLayout();
        }
        Log("Refreshed " + handCtrls.Length + " hand layouts");
    }

    private void Test_Utils_ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
        if (assembly == null) { Log("Cannot access UnityEditor", Color.yellow); return; }

        var type = assembly.GetType("UnityEditor.LogEntries");
        if (type != null)
        {
            var method = type.GetMethod("Clear");
            method?.Invoke(null, null);
        }

        testOutput = "";
        Log("Console cleared");
    }

    private void Test_Utils_ListSceneObjects()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Log("Scene has " + roots.Length + " root objects:", Color.cyan);
        foreach (var root in roots)
        {
            Log("  - " + root.name, Color.gray);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 外部调用：显示宣言
    /// </summary>
    public void ShowDeclaration(string text)
    {
        var declView = FindObjectOfType<DeclarationView>();
        declView?.ShowDeclaration(text);
    }

    /// <summary>
    /// 外部调用：注册自定义测试功能
    /// </summary>
    public void RegisterTest(string name, System.Action action)
    {
        if (!testFunctions.ContainsKey(name))
        {
            testFunctions[name] = action;
            Log("Registered test: " + name, Color.cyan);
        }
        else
        {
            Log("Test already exists: " + name, Color.yellow);
        }
    }

    /// <summary>
    /// 外部调用：输出日志
    /// </summary>
    public void LogTest(string message, Color? color = null)
    {
        Log(message, color ?? Color.white);
    }

    #endregion
}
