using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 피격 유닛 상단에서 피해량(데미지 수치)이 떠오르며 페이드아웃으로 소멸하는 단일 UI FX 효과 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DamageTextFX : MonoBehaviour
{
    private RectTransform rectTransform;
    private TextMeshProUGUI textComponent;

    // 히어로 타격(#FF5D00) vs 적 타격(#FF0505) 색상 정의
    private static readonly Color ColorHeroDamage = new Color32(0xFF, 0x5D, 0x00, 0xFF);
    private static readonly Color ColorEnemyDamage = new Color32(0xFF, 0x05, 0x05, 0xFF);

    private const float LargeFontSize = 44f;
    private const float SmallFontSize = 30f;
    private const float Duration = 0.65f;
    private const float FloatDistance = 60f;

    private Coroutine animateCoroutine;
    private System.Action<DamageTextFX> onCompleteCallback;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent == null)
        {
            textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        }

        // 중앙 정렬 및 아웃라인 셋업
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false;
        textComponent.raycastTarget = false;
    }

    /// <summary>
    /// 폰트 에셋을 동적으로 할당합니다.
    /// </summary>
    public void SetFontAsset(TMP_FontAsset fontAsset)
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null && fontAsset != null)
        {
            textComponent.font = fontAsset;
        }
    }

    /// <summary>
    /// 데미지 연출을 시작합니다.
    /// </summary>
    /// <param name="amount">피해량 수치</param>
    /// <param name="isHeroAttacking">true: 히어로 ➡️ 적 (오렌지/대형), false: 적 ➡️ 히어로 (레드/소형)</param>
    /// <param name="targetPosition">타격된 위치 (월드/로컬 anchoredPosition)</param>
    /// <param name="onComplete">연출 완료 시 풀 반납 콜백</param>
    public void Play(int amount, bool isHeroAttacking, Vector3 targetPosition, System.Action<DamageTextFX> onComplete)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();

        onCompleteCallback = onComplete;

        // 1. 수치 및 스타일 지정
        textComponent.text = amount.ToString();

        Color baseColor;
        float fontSize;

        if (isHeroAttacking)
        {
            // 히어로가 적을 공격한 경우: #FF5D00, 대형 폰트
            baseColor = ColorHeroDamage;
            fontSize = LargeFontSize;
        }
        else
        {
            // 적이 히어로를 공격한 경우: #FF0505, 소형 폰트 (적 피해량 대비 작음)
            baseColor = ColorEnemyDamage;
            fontSize = SmallFontSize;
        }

        textComponent.color = baseColor;
        textComponent.fontSize = fontSize;

        // 2. 위치 및 무작위 산개 오프셋 적용 (Y축 상단 + 약간의 X 무작위 지터)
        float randomXOffset = Random.Range(-15f, 15f);
        float baseYOffset = isHeroAttacking ? 60f : 70f;

        rectTransform.position = targetPosition + new Vector3(randomXOffset, baseYOffset, 0f);

        // 3. 코루틴 재가동
        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }
        animateCoroutine = StartCoroutine(AnimateSequence(baseColor));
    }

    /// <summary>
    /// 위로 떠오르며 알파가 투명해지는 연출 코루틴
    /// </summary>
    private IEnumerator AnimateSequence(Color baseColor)
    {
        Vector3 startWorldPos = rectTransform.position;
        Vector3 targetWorldPos = startWorldPos + new Vector3(0f, FloatDistance, 0f);

        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;

            // SmoothStep 상승 운동
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            rectTransform.position = Vector3.Lerp(startWorldPos, targetWorldPos, smoothT);

            // 후반부 40% 지점부터 알파 페이드아웃 (Alpha 1.0 -> 0.0)
            float alpha = 1.0f;
            if (t > 0.4f)
            {
                alpha = Mathf.Lerp(1.0f, 0.0f, (t - 0.4f) / 0.6f);
            }

            Color currentColor = baseColor;
            currentColor.a = alpha;
            textComponent.color = currentColor;

            yield return null;
        }

        // 완벽 투명 처리 및 콜백 호출
        Color finalColor = baseColor;
        finalColor.a = 0f;
        textComponent.color = finalColor;

        animateCoroutine = null;
        onCompleteCallback?.Invoke(this);
    }
}
