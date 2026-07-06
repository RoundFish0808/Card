using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DeclarationView : MonoBehaviour
{
    [Header("气泡背景")]
    public GameObject bubbleRoot;

    [Header("宣言文本")]
    public Text declarationText;

    [Header("NPC头像/立绘 (可选)")]
    public Image npcPortrait;

    [Header("动画参数")]
    public float showDuration = 0.3f;
    public float hideDuration = 0.2f;
    public float defaultDisplayTime = 2.5f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;

    [Header("打字机效果")]
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.05f;

    private Sequence currentSeq;

    private void Awake()
    {
        AutoFindReferences();

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private void AutoFindReferences()
    {
        if (bubbleRoot == null)
            bubbleRoot = gameObject;

        if (declarationText == null)
            declarationText = GetComponentInChildren<Text>();
    }

    public void ShowDeclaration(string text, float displayTime = -1f)
    {
        bool keepVisible = Mathf.Approximately(displayTime, 0f);

        if (displayTime < 0f)
            displayTime = defaultDisplayTime;

        KillCurrent();

        bubbleRoot.SetActive(true);
        bubbleRoot.transform.localScale = Vector3.zero;

        currentSeq = DOTween.Sequence();

        currentSeq.Append(bubbleRoot.transform.DOScale(Vector3.one, showDuration)
            .SetEase(showEase));

        if (useTypewriter && declarationText != null)
        {
            currentSeq.AppendCallback(() =>
            {
                StartCoroutine(TypewriterCoroutine(text, () =>
                {
                    if (!keepVisible)
                        DOVirtual.DelayedCall(displayTime, Hide);
                }));
            });
        }
        else
        {
            if (declarationText != null)
                declarationText.text = text;

            if (!keepVisible)
            {
                currentSeq.AppendInterval(displayTime);
                currentSeq.AppendCallback(Hide);
            }
        }
    }

    public void Hide()
    {
        KillCurrent();

        if (bubbleRoot == null) return;

        currentSeq = DOTween.Sequence();
        currentSeq.Append(bubbleRoot.transform.DOScale(Vector3.zero, hideDuration)
            .SetEase(hideEase));
        currentSeq.AppendCallback(() =>
        {
            if (bubbleRoot != null)
                bubbleRoot.SetActive(false);
        });
    }

    private System.Collections.IEnumerator TypewriterCoroutine(string text, System.Action onComplete)
    {
        if (declarationText == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        declarationText.text = "";
        foreach (char c in text)
        {
            declarationText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        onComplete?.Invoke();
    }

    private void KillCurrent()
    {
        if (currentSeq != null && currentSeq.IsActive())
        {
            currentSeq.Kill();
            currentSeq = null;
        }
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        KillCurrent();
    }
}
