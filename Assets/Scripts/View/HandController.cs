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

    [Header("测试/调试")]
    [Tooltip("调试用卡牌预制体。在编辑器中点击「生成测试卡牌」时会实例化此预制体")]
    public GameObject debugCardPrefab;

    private readonly List<GameObject> cards = new List<GameObject>();
    private readonly Dictionary<GameObject, Sequence> layoutSequences = new Dictionary<GameObject, Sequence>();
    private readonly Dictionary<GameObject, CardView> cardViewCache = new Dictionary<GameObject, CardView>();
    private GameObject hoveredCard = null;
    private SplineContainer runtimeSpline;

    public IReadOnlyList<GameObject> Cards => cards.AsReadOnly();
    public int CardCount => cards.Count;

    public event System.Action<CardView> OnCardClicked;
    public event System.Action<CardView> OnCardPointerEnter;
    public event System.Action<CardView> OnCardPointerExit;

    private void Awake()
    {
        EnsureSplineContainer();
    }

    //确保样条曲线容器存在,不存在则创建一条
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

    //初始化失败创造弧形曲线
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

    //添加一张卡牌到手牌布局,并为新的卡牌设置监听,更新布局
    public void AddCard(GameObject cardObj)
    {
        AddCard(cardObj, true);
    }

    public void AddCard(GameObject cardObj, bool refreshLayout = true)
    {
        if (cardObj == null) return;

        cardObj.transform.SetParent(transform, false);
        cards.Add(cardObj);

        SetupCardEvents(cardObj);
        SetupCardViewEvents(cardObj);

        if (refreshLayout)
            UpdateCardLayout();
    }

    public void RemoveCard(GameObject cardObj)
    {
        if (cardObj == null) return;

        if (hoveredCard == cardObj)
            hoveredCard = null;

        KillCardLayoutTween(cardObj);
        layoutSequences.Remove(cardObj);//清楚字典动画序列

        UnregisterCardViewEvents(cardObj);
        cardViewCache.Remove(cardObj);

        cards.Remove(cardObj);//清除列表卡牌对象
        UpdateCardLayout();
    }

    public void RefreshLayout()
    {
        UpdateCardLayout();
    }

    //清除所有卡牌,并销毁所有卡牌对象
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

    //更新布局
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

            //计算当前卡牌在样条曲线上的位置比例,一张卡牌在中间
            float t = count == 1
                ? Mathf.Lerp(startNormalizedPos, endNormalizedPos, 0.5f)
                : Mathf.Lerp(startNormalizedPos, endNormalizedPos, (float)i / (count - 1));
            
            //获取当前卡牌目标位置
            Vector3 targetPos = EvaluateSplinePosition(t);
            Quaternion targetRot = orientToTangent ? EvaluateSplineRotation(t) : Quaternion.identity;

            //判断是否悬停在当前卡牌上,悬停卡牌有一个向上位置偏移
            bool isHovered = hoveredCard == card;
            if (isHovered)
            {
                targetPos.y += hoverYOffset;
            }
            Vector3 targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;

            //设置卡牌兄弟索引,悬停卡牌停留最后,SiblingIndex = 在父物体下面排列的位置,原理:选中的话就停留在最后保持视觉最上层,如果没选中就按顺序
            int siblingIndex = isHovered ? count : i;
            //Hover 的牌永远在最上层
            rect.SetSiblingIndex(siblingIndex);
            
            //如果有动画优先动画效果
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

    //根据t值计算样条曲线上的位置
    private Vector3 EvaluateSplinePosition(float t)
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return Vector3.zero;

        SplineUtility.Evaluate(splineContainer.Spline, t, out var position, out _, out _);//这里只要pos其他的不要
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

    //设置卡牌事件监听
    private void SetupCardEvents(GameObject cardObj)
    {
        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = cardObj.AddComponent<EventTrigger>();

        bool hasEnter = false;
        bool hasExit = false;
        //检查这个卡牌的 EventTrigger 里，PointerEnter / PointerExit 事件有没有已经被绑定过，避免重复添加
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter) hasEnter = true;
            if (entry.eventID == EventTriggerType.PointerExit) hasExit = true;
        }

        //设置监听类型和回调方法
        if (!hasEnter)
        {
            EventTrigger.Entry enterEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((data) => {HandleHoverEnter(cardObj);
            Debug.Log($"PointerEnter");
            });
            trigger.triggers.Add(enterEntry);
        }

        if (!hasExit)
        {
            EventTrigger.Entry exitEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) => {HandleHoverExit(cardObj);
            Debug.Log($"PointerExit");
            });
            trigger.triggers.Add(exitEntry);
        }
    }

    private void SetupCardViewEvents(GameObject cardObj)
    {
        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView == null)
            cardView = cardObj.GetComponentInChildren<CardView>();

        if (cardView == null) return;

        cardViewCache[cardObj] = cardView;
        cardView.OnClicked += HandleCardViewClicked;
        cardView.OnPointerEnterEvent += HandleCardViewPointerEnter;
        cardView.OnPointerExitEvent += HandleCardViewPointerExit;
    }

    private void UnregisterCardViewEvents(GameObject cardObj)
    {
        if (cardViewCache.TryGetValue(cardObj, out var cardView) && cardView != null)
        {
            cardView.OnClicked -= HandleCardViewClicked;
            cardView.OnPointerEnterEvent -= HandleCardViewPointerEnter;
            cardView.OnPointerExitEvent -= HandleCardViewPointerExit;
        }
    }

    private void HandleCardViewClicked(CardView card)
    {
        OnCardClicked?.Invoke(card);
    }

    private void HandleCardViewPointerEnter(CardView card)
    {
        if (card != null && card.gameObject != null)
            OnCardPointerEnter?.Invoke(card);
    }

    private void HandleCardViewPointerExit(CardView card)
    {
        if (card != null && card.gameObject != null)
            OnCardPointerExit?.Invoke(card);
    }

    private void HandleHoverEnter(GameObject cardObj)
    {
        if (hoveredCard == cardObj) return;

        hoveredCard = cardObj;
        UpdateCardLayout();
    }

    private void HandleHoverExit(GameObject cardObj)
    {
        if (hoveredCard != cardObj) return;

        hoveredCard = null;
        UpdateCardLayout();
    }

    //用 DOTween 给“卡牌 UI”做一个带延迟的布局动画
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

    //取消卡牌布局动画
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

    //在 Scene视图绘制Spline曲线
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
