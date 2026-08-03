using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 버튼 터치/클릭 시 톡톡 튀는 펀치 스케일 트위닝 애니메이션 연출을 제공하는 범용 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_ButtonTweenEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("트위닝 설정")]
    [SerializeField] private float compressScaleMultiplier = 0.88f; // 터치 시 작아지는 비율
    [SerializeField] private float compressDuration = 0.06f;
    [SerializeField] private float returnDuration = 0.12f;

    private Button targetButton;
    private Vector3 originalScale = Vector3.one;
    private Coroutine currentTweenCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetButton = GetComponent<Button>();

        if (targetButton != null)
        {
            targetButton.onClick.AddListener(PlayPopScaleTween);
        }
    }

    private void OnDisable()
    {
        if (currentTweenCoroutine != null)
        {
            StopCoroutine(currentTweenCoroutine);
            currentTweenCoroutine = null;
        }
        transform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetButton != null && !targetButton.interactable) return;

        // 눌린 순간 작아짐
        if (currentTweenCoroutine != null) StopCoroutine(currentTweenCoroutine);
        currentTweenCoroutine = StartCoroutine(CoScaleTo(originalScale * compressScaleMultiplier, compressDuration));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 떼었을 때 원래 크기로 복원
        if (targetButton != null && !targetButton.interactable) return;
        if (currentTweenCoroutine != null) StopCoroutine(currentTweenCoroutine);
        currentTweenCoroutine = StartCoroutine(CoScaleTo(originalScale, returnDuration));
    }

    /// <summary>
    /// 버튼 클릭/입력 시 작아졌다가 원래 크기로 돌아오는 트위닝 애니메이션을 실행합니다.
    /// </summary>
    public void PlayPopScaleTween()
    {
        if (!gameObject.activeInHierarchy) return;

        if (currentTweenCoroutine != null)
        {
            StopCoroutine(currentTweenCoroutine);
        }
        currentTweenCoroutine = StartCoroutine(CoPopScaleSequence());
    }

    private IEnumerator CoPopScaleSequence()
    {
        Vector3 compressScale = originalScale * compressScaleMultiplier;
        float elapsed = 0f;

        // 1. 0.88배로 축소 (EaseOutQuad)
        while (elapsed < compressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / compressDuration);
            float easeT = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(transform.localScale, compressScale, easeT);
            yield return null;
        }

        transform.localScale = compressScale;
        elapsed = 0f;

        // 2. originalScale(1.0배)로 복원 (SmoothStep)
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float easeT = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(compressScale, originalScale, easeT);
            yield return null;
        }

        transform.localScale = originalScale;
        currentTweenCoroutine = null;
    }

    private IEnumerator CoScaleTo(Vector3 target, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeT = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(startScale, target, easeT);
            yield return null;
        }
        transform.localScale = target;
    }
}
