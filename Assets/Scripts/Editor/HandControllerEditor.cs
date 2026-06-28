#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(HandController))]
public class HandControllerEditor : Editor
{
    private HandController handController;

    private void OnEnable()
    {
        handController = (HandController)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("调试工具", EditorStyles.boldLabel);

        if (GUILayout.Button("刷新布局"))
        {
            handController.UpdateCardLayout();
            EditorUtility.SetDirty(handController);
        }

        if (GUILayout.Button("重建弧形样条"))
        {
            handController.RebuildAutoSpline();
            EditorUtility.SetDirty(handController);
        }

        if (GUILayout.Button("生成测试卡牌 (3张)"))
        {
            GenerateTestCards(3);
        }

        if (GUILayout.Button("生成测试卡牌 (5张)"))
        {
            GenerateTestCards(5);
        }

        if (GUILayout.Button("生成测试卡牌 (10张)"))
        {
            GenerateTestCards(10);
        }

        if (GUILayout.Button("清除所有卡牌"))
        {
            handController.ClearCards();
            EditorUtility.SetDirty(handController);
        }

        EditorGUILayout.HelpBox(
            "弧形手牌（Splines + DOTween 版本）\n\n" +
            "使用方法：\n" +
            "1. 在 Canvas 下创建空物体，挂上 HandController\n" +
            "2. 可以手动指定 SplineContainer，也可以留空让它自动生成弧形样条\n" +
            "3. 运行时通过 AddCard / RemoveCard 动态管理手牌\n" +
            "4. 鼠标悬停时卡牌自动抬升并放大\n\n" +
            "核心原理：\n" +
            "- 用 Splines 包定义曲线形状\n" +
            "- 用 SplineUtility.Evaluate 取曲线上的点和切线\n" +
            "- 用 DOTween.Sequence + Join 做位置/旋转/缩放并行动画",
            MessageType.Info);
    }

    private void GenerateTestCards(int count)
    {
        handController.ClearCards();

        for (int i = 0; i < count; i++)
        {
            GameObject cardObj;

            if (handController.debugCardPrefab != null)
            {
                cardObj = Instantiate(handController.debugCardPrefab);
            }
            else
            {
                cardObj = new GameObject("Card_" + i);
                RectTransform rect = cardObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100, 150);

                Image img = cardObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.5f);

                Text txt = cardObj.AddComponent<Text>();
                txt.text = "Card " + i;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.black;

                cardObj.AddComponent<CardView>();
            }

            cardObj.name = "Card_" + i;
            handController.AddCard(cardObj);
        }

        EditorUtility.SetDirty(handController);
    }

    private void OnSceneGUI()
    {
        if (handController == null) return;

        if (handController.splineContainer != null && handController.splineContainer.Spline != null)
        {
            Handles.color = new Color(0.5f, 0.8f, 1f, 0.8f);

            int segments = 50;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 localPos = EvaluatePosition(t);
                Vector3 worldPos = handController.transform.TransformPoint(localPos);

                if (i > 0)
                {
                    Handles.DrawLine(prevPoint, worldPos);
                }
                prevPoint = worldPos;
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.fontSize = 12;
            Handles.Label(handController.transform.position + new Vector3(0, 50, 0),
                "手牌样条曲线（Splines 包）", style);
        }
    }

    private Vector3 EvaluatePosition(float t)
    {
        var spline = handController.splineContainer.Spline;
        if (spline == null) return Vector3.zero;

        UnityEngine.Splines.SplineUtility.Evaluate(spline, t, out var position, out _, out _);
        return new Vector3(position.x, position.y, position.z);
    }
}
#endif
