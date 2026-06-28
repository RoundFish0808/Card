using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class HandController : MonoBehaviour
{
    [Header("样条曲线引用")]
    [Tooltip("场景中的样条曲线容器。如果为空，会在运行时自动创建一条弧形样条曲线")]
    public SplineContainer splineContainer;

    [Header("自动生成弧形设置（splineContainer为空时生效）")]
    [Tooltip("弧形的宽度")]
    public float autoArcWidth = 800f;

    [Tooltip("弧形的高度（弯曲深度）")]
    public float autoArcHeight = 100f;

    [Tooltip("弧形向下弯曲还是向上弯曲")]
    public bool arcDownward = true;

    [Header("曲线位置设置")]
    [Tooltip("卡牌在曲线上的起始归一化位置（0~1）")]
    [Range(0f, 1f)]
    public float startNormalizedPos = 0.05f;

    [Tooltip("卡牌在曲线上的结束归一化位置（0~1）")]
    [Range(0f, 1f)]
    public float endNormalizedPos = 0.95f;

    [Tooltip("是否让卡牌朝向曲线切线方向")]
    public bool orientToTangent = true;

    [Tooltip("切线方向的额外旋转偏移")]
    public float tangentRotationOffset = 0f;

    [Header("悬停设置")]
    [Tooltip("悬停时卡牌上移距离")]
    public float hoverYOffset = 60f;

    [Tooltip("悬停时卡牌放大倍数")]
    public float hoverScale = 1.15f;

    [Tooltip("悬停动画时长")]
    public float hoverDuration = 0.15f;

    [Tooltip("悬停动画缓动")]
    public Ease hoverEase = Ease.OutCubic;

    [Header("布局动画设置")]
    [Tooltip("是否启用布局动画")]
    public bool useAnimation = true;

    [Tooltip("布局动画时长（秒）")]
    public float layoutAnimationDuration = 0.3f;

    [Tooltip("每一张牌的动画延迟（秒）")]
    public float cardAnimationDelay = 0.05f;

    [Tooltip("布局动画缓动类型")]
    public Ease layoutEase = Ease.OutCubic;

    private readonly List<GameObject> cards = new List<GameObject>();
    private readonly Dictionary<GameObject, Sequence> layoutSequences = new Dictionary<GameObject, Sequence>();
    private GameObject hoveredCard = null;
    private SplineContainer runtimeSpline;

    public IReadOnlyList<GameObject> Cards => cards.AsReadOnly();
    public int CardCount => cards.Count;

    private void Awake()
    {
        EnsureSplineContainer();
    }

    private void EnsureSplineContainer()
    {
        if (splineContainer != null) return;

        GameObject splineObj = new GameObject("HandSpline");
        splineObj.transform.SetParent(transform, false);
        splineObj.transform.localPosition = Vector3.zero;

        runtimeSpline = splineObj.AddComponent<SplineContainer>();
        splineContainer = runtimeSpline;

        CreateArcSpline(runtimeSpline.Spline);
    }

    private void CreateArcSpline(Spline spline)
    {
        spline.Clear();

        float width = autoArcWidth;
        float height = arcDownward ? -autoArcHeight : autoArcHeight;
        float halfWidth = width * 0.5f;

        BezierKnot startKnot = new BezierKnot(
            position: new float3(-halfWidth, 0f, 0f),
            tangentIn: new float3(-halfWidth * 0.4f, 0f, 0f),
            tangentOut: new float3(halfWidth * 0.4f, height * 0.6f, 0f),
            rotation: quaternion.identity
        );

        BezierKnot midKnot = new BezierKnot(
            position: new float3(0f, height, 0f),
            tangentIn: new float3(-halfWidth * 0.4f, 0f, 0f),
            tangentOut: new float3(halfWidth * 0.4f, 0f, 0f),
            rotation: quaternion.identity
        );

        BezierKnot endKnot = new BezierKnot(
            position: new float3(halfWidth, 0f, 0f),
            tangentIn: new float3(-halfWidth * 0.4f, height * 0.6f, 0f),
            tangentOut: new float3(halfWidth * 0.4f, 0f, 0f),
            rotation: quaternion.identity
        );

        spline.Add(startKnot);
        spline.Add(midKnot);
        spline.Add(endKnot);

        spline.Closed = false;
    }

    public void AddCard(GameObject cardObj)
    {
        if (cardObj == null) return;

        cardObj.transform.SetParent(transform, false);
        cards.Add(cardObj);

        SetupCardEvents(cardObj);
        UpdateCardLayout();
    }

    public void RemoveCard(GameObject cardObj)
    {
        if (cardObj == null) return;

        if (hoveredCard == cardObj)
            hoveredCard = null;

        KillCardLayoutTween(cardObj);
        layoutSequences.Remove(cardObj);

        cards.Remove(cardObj);
        UpdateCardLayout();
    }

    public void ClearCards()
    {
        foreach (var kvp in layoutSequences)
        {
            if (kvp.Value != null)
                kvp.Value.Kill(false);
        }
        layoutSequences.Clear();

        foreach (var card in cards)
        {
            if (card != null)
                Destroy(card);
        }
        cards.Clear();
        hoveredCard = null;
    }

    public void UpdateCardLayout()
    {
        int count = cards.Count;
        if (count == 0) return;

        EnsureSplineContainer();

        for (int i = 0; i < count; i++)
        {
            GameObject card = cards[i];
            if (card == null) continue;

            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect == null) continue;

            float t = count == 1
                ? Mathf.Lerp(startNormalizedPos, endNormalizedPos, 0.5f)
                : Mathf.Lerp(startNormalizedPos, endNormalizedPos, (float)i / (count - 1));

            Vector3 targetPos = EvaluateSplinePosition(t);
            Quaternion targetRot = orientToTangent ? EvaluateSplineRotation(t) : Quaternion.identity;

            bool isHovered = hoveredCard == card;
            if (isHovered)
            {
                targetPos.y += hoverYOffset;
            }
            Vector3 targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;

            int siblingIndex = isHovered ? count : i;
            rect.SetSiblingIndex(siblingIndex);

            if (useAnimation && Application.isPlaying)
            {
                float delay = i * cardAnimationDelay;
                StartLayoutAnimation(card, rect, targetPos, targetRot, targetScale, delay);
            }
            else
            {
                rect.localPosition = targetPos;
                rect.localRotation = targetRot;
                rect.localScale = targetScale;
            }
        }
    }

    private Vector3 EvaluateSplinePosition(float t)
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return Vector3.zero;

        SplineUtility.Evaluate(splineContainer.Spline, t, out var position, out _, out _);
        return new Vector3(position.x, position.y, position.z);
    }

    private Quaternion EvaluateSplineRotation(float t)
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return Quaternion.identity;

        SplineUtility.Evaluate(splineContainer.Spline, t, out _, out var tangent, out _);
        if (tangent.Equals(float3.zero))
            return Quaternion.identity;

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, -angle + tangentRotationOffset);
    }

    private void SetupCardEvents(GameObject cardObj)
    {
        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = cardObj.AddComponent<EventTrigger>();

        bool hasEnter = false;
        bool hasExit = false;
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter) hasEnter = true;
            if (entry.eventID == EventTriggerType.PointerExit) hasExit = true;
        }

        if (!hasEnter)
        {
            EventTrigger.Entry enterEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((data) => OnCardPointerEnter(cardObj));
            trigger.triggers.Add(enterEntry);
        }

        if (!hasExit)
        {
            EventTrigger.Entry exitEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) => OnCardPointerExit(cardObj));
            trigger.triggers.Add(exitEntry);
        }
    }

    private void OnCardPointerEnter(GameObject cardObj)
    {
        if (hoveredCard == cardObj) return;

        hoveredCard = cardObj;
        UpdateCardLayout();
    }

    private void OnCardPointerExit(GameObject cardObj)
    {
        if (hoveredCard != cardObj) return;

        hoveredCard = null;
        UpdateCardLayout();
    }

    private void StartLayoutAnimation(GameObject cardObj, RectTransform rect, Vector3 targetPos, Quaternion targetRot, Vector3 targetScale, float delay)
    {
        if (layoutSequences.TryGetValue(cardObj, out var existing))
        {
            if (existing != null)
                existing.Kill(false);
        }

        Vector3 targetEuler = targetRot.eulerAngles;

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);

        seq.Join(rect.DOLocalMove(targetPos, layoutAnimationDuration).SetEase(layoutEase));
        seq.Join(rect.DOLocalRotate(targetEuler, layoutAnimationDuration, RotateMode.FastBeyond360).SetEase(layoutEase));
        seq.Join(rect.DOScale(targetScale, layoutAnimationDuration).SetEase(layoutEase));

        seq.OnKill(() =>
        {
            if (layoutSequences.ContainsKey(cardObj))
                layoutSequences[cardObj] = null;
        });

        layoutSequences[cardObj] = seq;
    }

    private void KillCardLayoutTween(GameObject cardObj)
    {
        if (layoutSequences.TryGetValue(cardObj, out var layoutSeq) && layoutSeq != null)
        {
            layoutSeq.Kill(false);
        }
    }

    public void RebuildAutoSpline()
    {
        if (runtimeSpline == null && splineContainer == null)
        {
            EnsureSplineContainer();
            return;
        }

        if (runtimeSpline != null)
        {
            CreateArcSpline(runtimeSpline.Spline);
            UpdateCardLayout();
        }
    }

    private void OnDestroy()
    {
        ClearCards();
        DOTween.Kill(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);

        int segments = 50;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 worldPos = transform.TransformPoint(EvaluateSplinePosition(t));

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, worldPos);
            }
            prevPoint = worldPos;
        }
    }
}
