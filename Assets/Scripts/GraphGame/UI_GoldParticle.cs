using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오브젝트 풀에서 꺼내져 사방으로 튕겨 나간 뒤 아래로 떨어지며 사라지는 개별 골드 연출용 컴포넌트입니다.
/// UGUI 환경에서 부모의 스케일과 해상도 변화에 영향받지 않도록 anchoredPosition을 제어합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UI_GoldParticle : MonoBehaviour
{
    [Header("골드 드롭 연출 설정")]
    [Tooltip("아래로 떨어지는 속도에 영향을 미치는 중력 가속도 수치입니다.")]
    [SerializeField] private float gravity = 800f;

    [Tooltip("투명화(Fade Out)되는 속도 계수입니다.")]
    [SerializeField] private float fadeSpeed = 1.5f;

    private RectTransform rectTransform;
    private Image goldImage;
    private Action<UI_GoldParticle> onReleaseAction;
    private Coroutine activeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        goldImage = GetComponent<Image>();
    }

    /// <summary>
    /// 고블린의 위치에서 골드를 공중으로 무작위 포물선을 그리며 투척합니다.
    /// </summary>
    /// <param name="startPosition">고블린의 실시간 anchoredPosition 좌표</param>
    /// <param name="onRelease">연출이 완료되었을 때 풀로 복귀시키기 위한 반환 콜백</param>
    /// <summary>
    /// 고블린의 위치에서 골드를 공중으로 무작위 포물선을 그리며 투척합니다.
    /// </summary>
    /// <param name="startPosition">고블린의 실시간 anchoredPosition 좌표</param>
    /// <param name="onRelease">연출이 완료되었을 때 풀로 복귀시키기 위한 반환 콜백</param>
    public void Launch(Vector2 startPosition, Action<UI_GoldParticle> onRelease)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (goldImage == null) goldImage = GetComponent<Image>();

        rectTransform.anchoredPosition = startPosition;
        onReleaseAction = onRelease;

        // 2D 분수 형태의 포물선 경로를 위한 좌우 및 수직 초기 무작위 속도값 설정
        Vector2 velocity = new Vector2(
            UnityEngine.Random.Range(-150f, 150f),
            UnityEngine.Random.Range(200f, 400f)
        );

        // 투명도 100% 원본으로 복구
        if (goldImage != null)
        {
            Color c = goldImage.color;
            c.a = 1f;
            goldImage.color = c;
        }

        // 혹시 작동 중인 연출 코루틴이 남아있다면 정지 후 재시작
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(AnimateParticle(velocity));
    }

    /// <summary>
    /// 시간 경과에 따른 중력 가속 포물선 이동 및 투명화 연출을 수행하는 코루틴입니다.
    /// </summary>
    private IEnumerator AnimateParticle(Vector2 velocity)
    {
        float alpha = 1f;
        
        while (alpha > 0f)
        {
            // Y축 방향으로 중력 가속도 차감 적용
            velocity.y -= gravity * Time.deltaTime;
            rectTransform.anchoredPosition += velocity * Time.deltaTime;

            // 서서히 페이드 아웃 처리
            alpha -= fadeSpeed * Time.deltaTime;
            if (goldImage != null)
            {
                Color c = goldImage.color;
                c.a = Mathf.Max(0f, alpha);
                goldImage.color = c;
            }

            yield return null;
        }

        // 연출 시간이 종료되면 자신을 풀에 반환합니다.
        ReturnToPool();
    }

    /// <summary>
    /// 강제 회수되거나 정상 연출이 끝나서 비활성화될 때 호출되어 풀에 개체를 돌려줍니다.
    /// </summary>
    public void ReturnToPool()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        gameObject.SetActive(false);
        onReleaseAction?.Invoke(this);
    }
}
