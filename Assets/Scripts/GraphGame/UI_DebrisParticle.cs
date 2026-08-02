using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BannerSubTxt 진동 시 5초 이후부터 주변으로 튀어나가는 UI 파편 파티클 컴포넌트입니다.
/// LayoutGroup(Horizontal/Vertical Layout)의 크기 계산을 교란하지 않도록 LayoutElement(ignoreLayout=true)를 내장합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class UI_DebrisParticle : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image debrisImage;
    private LayoutElement layoutElement;
    private Action<UI_DebrisParticle> onReleaseAction;
    private Coroutine activeCoroutine;

    private static Sprite whiteSquareSprite;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        debrisImage = GetComponent<Image>();
        layoutElement = GetComponent<LayoutElement>();

        // [핵심] 부모 패널의 LayoutGroup이 이 파티클을 UI 요소로 인식해 다른 UI(BannerSubTxt 등)의 크기를 줄이는 현상 원천 차단
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        EnsureSprite();
    }

    private void EnsureSprite()
    {
        if (debrisImage != null && debrisImage.sprite == null)
        {
            if (whiteSquareSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSquareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            debrisImage.sprite = whiteSquareSprite;
        }
    }

    /// <summary>
    /// 지정된 위치에서 무작위 방향으로 파편 파티클을 방출합니다.
    /// </summary>
    /// <param name="startWorldPosition">파편이 발생할 텍스트 주변 월드 좌표</param>
    /// <param name="color">파편의 컬러</param>
    /// <param name="onRelease">파티클 수명 종료 시 풀에 반환할 콜백</param>
    public void LaunchWorld(Vector3 startWorldPosition, Color color, Action<UI_DebrisParticle> onRelease)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (debrisImage == null) debrisImage = GetComponent<Image>();
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();

        if (layoutElement != null) layoutElement.ignoreLayout = true;
        EnsureSprite();

        // 텍스트 패널 최상단에 렌더링되도록 처리
        transform.SetAsLastSibling();

        // 월드 좌표 기준 배치 (부모 RectTransform의 Local 좌표로 변환)
        rectTransform.position = startWorldPosition;
        onReleaseAction = onRelease;

        // 확연히 보이는 눈에 띄는 크기
        float randomScale = UnityEngine.Random.Range(0.9f, 1.6f);
        rectTransform.localScale = new Vector3(randomScale, randomScale, 1f);
        rectTransform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));

        if (debrisImage != null)
        {
            debrisImage.color = color;
            debrisImage.raycastTarget = false;
        }

        // 360도 무작위 사방 방출 속도
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = UnityEngine.Random.Range(200f, 480f);
        Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

        float angularVelocity = UnityEngine.Random.Range(-720f, 720f);

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(AnimateDebris(velocity, angularVelocity));
    }

    private IEnumerator AnimateDebris(Vector2 velocity, float angularVelocity)
    {
        float lifetime = UnityEngine.Random.Range(0.35f, 0.6f);
        float elapsed = 0f;
        Vector3 initialScale = rectTransform.localScale;
        Color initialColor = debrisImage != null ? debrisImage.color : Color.white;

        float drag = 2.5f;
        float gravity = -200f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            velocity -= velocity * (drag * Time.deltaTime);
            velocity.y += gravity * Time.deltaTime;

            rectTransform.anchoredPosition += velocity * Time.deltaTime;
            rectTransform.Rotate(0, 0, angularVelocity * Time.deltaTime);

            rectTransform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t * t);
            if (debrisImage != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(initialColor.a, 0f, t);
                debrisImage.color = c;
            }

            yield return null;
        }

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        onReleaseAction?.Invoke(this);
        onReleaseAction = null;
    }
}
