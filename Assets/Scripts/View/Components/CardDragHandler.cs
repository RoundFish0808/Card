using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using System.Collections;

/// <summary>
/// 卡牌拖拽组件
/// 完全仿造 Example/Scripts/Card.cs 实现：
/// - Update 中用 transform.Translate 平滑跟随鼠标
/// - 拖拽时记录 offset（鼠标位置 - 卡牌位置），保持相对偏移
/// - 拖拽时禁用 GraphicRaycaster 和 Image.raycastTarget
/// - 结束时找最近的空槽位吸附，否则回弹
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CardView))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("移动设置")]
    [Tooltip("瞬移模式（无延迟跟手）")]
    public bool teleportMode = true;

    [Header("吸附设置")]
    [Tooltip("吸附判定距离（像素）")]
    public float snapDistance = 150f;

    [Header("回弹设置")]
    [Tooltip("回弹动画时间")]
    public float returnDuration = 0.3f;

    [Header("吸附设置")]
    [Tooltip("吸附动画时间")]
    public float snapDuration = 0.25f;

    [Header("拖拽视觉")]
    [Tooltip("拖拽中的缩放倍数")]
    public float dragScale = 1.2f;
    [Tooltip("缩放动画时间")]
    public float scaleTransition = 0.15f;
    [Tooltip("缩放缓动")]
    public Ease scaleEase = Ease.OutBack;

    [Header("引用")]
    public RectTransform rectTransform;
    public Canvas canvas;

    private CardView cardView;
    private Image imageComponent;
    private CanvasGroup canvasGroup;

    private Vector3 offset;
    private bool isDragging = false;
    private bool wasDragged = false;
    private HandController cachedHandController;
    private SlotView currentHighlightedSlot;

    public bool IsDragging => isDragging;
    public bool WasDragged => wasDragged;
    public CardView CardView => cardView;

    public event Action<CardDragHandler> OnDragStarted;
    public event Action<CardDragHandler> OnDragEnded;
    public event Action<CardDragHandler, SlotView> OnPlacedToSlot;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (cardView == null)
            cardView = GetComponent<CardView>();

        imageComponent = GetComponent<Image>();
        if (imageComponent == null)
            imageComponent = GetComponentInChildren<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (!isDragging) return;
        if (rectTransform == null) return;
        if (canvas == null) return;

        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            null,
            out worldPos))
        {
            transform.position = worldPos + offset;
        }

        // 实时检测并高亮最近的空槽位
        UpdateSlotHighlight();
    }

    /// <summary>
    /// 实时检测并高亮最近的空槽位
    /// </summary>
    private void UpdateSlotHighlight()
    {
        SlotView nearestSlot = FindNearestEmptySlot();

        // 如果最近的槽位变了，取消之前的高亮
        if (currentHighlightedSlot != null && currentHighlightedSlot != nearestSlot)
        {
            currentHighlightedSlot.SetHighlighted(false);
            currentHighlightedSlot = null;
        }

        // 高亮新的最近槽位
        if (nearestSlot != null && nearestSlot != currentHighlightedSlot)
        {
            nearestSlot.SetHighlighted(true);
            currentHighlightedSlot = nearestSlot;
        }
    }

    /// <summary>
    /// 取消所有槽位高亮
    /// </summary>
    private void ClearAllHighlights()
    {
        if (currentHighlightedSlot != null)
        {
            currentHighlightedSlot.SetHighlighted(false);
            currentHighlightedSlot = null;
        }

        // 取消所有槽位的高亮（清除 BattleFieldView 中可能的手动高亮）
        SlotView[] allSlots = FindObjectsOfType<SlotView>();
        foreach (var slot in allSlots)
        {
            slot.SetHighlighted(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardView == null) return;
        if (cardView.CurrentState != CardState.InHand) return;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null) return;

        // 开始拖拽时清除所有槽位高亮
        ClearAllHighlights();

        // 缓存 HandController 引用（拖拽过程中父物体可能变化，之后再找就找不到了）
        cachedHandController = GetComponentInParent<HandController>();

        // 暂停手牌布局刷新（防止布局覆盖拖拽位置，导致卡牌乱飞）
        if (cachedHandController != null)
            cachedHandController.PauseLayout();

        // 计算鼠标位置与卡牌位置的偏移（保持相对位置不跳变）
        Vector3 mouseWorldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out mouseWorldPos))
        {
            offset = transform.position - mouseWorldPos;
        }

        isDragging = true;
        wasDragged = true;

        // 禁用 Canvas 的 GraphicRaycaster，避免阻挡射线
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;

        // 禁用 Image 的 raycastTarget，避免卡牌自己挡住射线
        if (imageComponent != null)
            imageComponent.raycastTarget = false;

        // 拖拽时放大
        rectTransform.DOScale(GetCurrentTargetScale() * dragScale, scaleTransition)
            .SetEase(scaleEase);

        // 把卡牌提升到最上层（避免被其他 UI 遮挡）
        transform.SetAsLastSibling();

        OnDragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 实际移动在 Update 中处理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        // 结束拖拽时清除所有槽位高亮
        ClearAllHighlights();

        // 恢复 GraphicRaycaster
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;
        }

        // 恢复 Image 的 raycastTarget
        if (imageComponent != null)
            imageComponent.raycastTarget = true;

        // 找最近的空槽位吸附
        SlotView nearestSlot = FindNearestEmptySlot();

        bool placed = false;
        if (nearestSlot != null)
        {
            PlaceToSlot(nearestSlot);
            placed = true;
        }
        else
        {
            ReturnToHand();
        }

        // 恢复手牌布局刷新（使用缓存的引用，因为放置后父物体变了就找不到了）
        if (cachedHandController != null)
        {
            if (placed)
            {
                // 放置成功：先解除暂停，再从手牌列表移除该卡牌（RemoveCard内部会刷新布局）
                cachedHandController.ResumeLayout(false);
                cachedHandController.RemoveCard(gameObject, true);
            }
            else
            {
                // 回弹：卡牌还在手里，直接刷新布局
                cachedHandController.ResumeLayout(true);
            }
            cachedHandController = null;
        }

        OnDragEnded?.Invoke(this);

        // 等待一帧后再重置 wasDragged（避免 click 事件误触发）
        StartCoroutine(ResetWasDraggedNextFrame());
    }

    private IEnumerator ResetWasDraggedNextFrame()
    {
        yield return new WaitForEndOfFrame();
        wasDragged = false;
    }

    /// <summary>
    /// 找最近的空槽位
    /// </summary>
    private SlotView FindNearestEmptySlot()
    {
        SlotView[] allSlots = FindObjectsOfType<SlotView>();
        if (allSlots == null || allSlots.Length == 0) return null;

        SlotView nearest = null;
        float minDist = float.MaxValue;
        Vector3 cardPos = transform.position;

        foreach (var slot in allSlots)
        {
            if (slot.IsOccupied) continue;
            if (!slot.interactable) continue;
            if (!slot.isPlayerSlot) continue;

            float dist = Vector3.Distance(cardPos, slot.transform.position);
            if (dist < minDist && dist <= snapDistance)
            {
                minDist = dist;
                nearest = slot;
            }
        }

        return nearest;
    }

    private void PlaceToSlot(SlotView slot)
    {
        slot.PlaceCard(cardView, true);
        cardView.SetState(CardState.Placed);
        OnPlacedToSlot?.Invoke(this, slot);
    }

    public void ReturnToHand()
    {
        if (rectTransform == null) return;

        // 重置锚点为中心（手牌里都是中心锚点）
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;

        // 缩放回原大小
        rectTransform.DOScale(GetCurrentTargetScale(), returnDuration)
            .SetEase(scaleEase);
    }

    /// <summary>
    /// 获取当前应该的目标缩放（手牌里 1，放置后可能被再次选中改过）
    /// </summary>
    private float GetCurrentTargetScale()
    {
        if (cardView != null && cardView.CurrentState == CardState.Placed)
        {
            return cardView.PlacedScale;
        }
        return 1f;
    }

    private void OnDestroy()
    {
        DOTween.Kill(rectTransform);
    }
}
