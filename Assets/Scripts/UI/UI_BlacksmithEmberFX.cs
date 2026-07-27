using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stitch 대장간 UI의 마이크로 인터랙션을 구현하는 파티클 및 애니메이션 컨트롤러입니다.
/// 배경의 불꽃 입자(Ember Particles) 지속 생성 및 강화 클릭 시 안빌 충격 바운스 & 스파크(Spark) 튀는 효과를 연출합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithEmberFX : MonoBehaviour
{
    [Header("불꽃 입자(Ember) 연출 설정")]
    [Tooltip("불꽃 입자가 생성될 RectTransform 컨테이너 영역입니다.")]
    [SerializeField] private RectTransform emberContainer;

    [Tooltip("불꽃 입자로 사용할 UI Image 템플릿/프리팹입니다. (생성 시 동적 복제됨)")]
    [SerializeField] private Image emberTemplate;

    [Tooltip("입자 생성 간격 (초 단위)")]
    [SerializeField] private float spawnInterval = 0.35f;

    [Tooltip("불꽃 색상")]
    [SerializeField] private Color emberColor = new Color(1f, 0.36f, 0f, 0.9f); // #ff5c00

    [Header("강화 스파크(Spark) 연출 설정")]
    [Tooltip("강화 시 안빌/무기 연출 대상 RectTransform 입니다.")]
    [SerializeField] private RectTransform anvilRect;

    private readonly List<Image> activeEmbers = new List<Image>();
    private Coroutine emberSpawnCoroutine;

    private void OnEnable()
    {
        if (emberTemplate != null)
        {
            emberTemplate.gameObject.SetActive(false);
        }

        if (emberContainer != null && emberTemplate != null)
        {
            emberSpawnCoroutine = StartCoroutine(EmberSpawnLoop());
        }
    }

    private void OnDisable()
    {
        if (emberSpawnCoroutine != null)
        {
            StopCoroutine(emberSpawnCoroutine);
            emberSpawnCoroutine = null;
        }

        ClearEmbers();
    }

    /// <summary>
    /// 지속적으로 불꽃 입자를 상승시키는 루프 코루틴
    /// </summary>
    private IEnumerator EmberSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSingleEmber();
        }
    }

    private void SpawnSingleEmber()
    {
        if (emberContainer == null || emberTemplate == null) return;

        Image emberInstance = Instantiate(emberTemplate, emberContainer);
        emberInstance.gameObject.SetActive(true);

        RectTransform rect = emberInstance.GetComponent<RectTransform>();
        float containerWidth = emberContainer.rect.width;
        float randomX = Random.Range(-containerWidth * 0.45f, containerWidth * 0.45f);

        rect.anchoredPosition = new Vector2(randomX, -emberContainer.rect.height * 0.4f);
        float size = Random.Range(3f, 6f);
        rect.sizeDelta = new Vector2(size, size);
        emberInstance.color = emberColor;

        activeEmbers.Add(emberInstance);
        StartCoroutine(AnimateEmber(emberInstance, rect));
    }

    private IEnumerator AnimateEmber(Image ember, RectTransform rect)
    {
        float duration = Random.Range(2.0f, 3.5f);
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        float riseDistance = Random.Range(120f, 220f);

        while (elapsed < duration)
        {
            if (ember == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float y = Mathf.Lerp(startPos.y, startPos.y + riseDistance, t);
            float xOffset = Mathf.Sin(t * Mathf.PI * 3f) * 15f;
            rect.anchoredPosition = new Vector2(startPos.x + xOffset, y);

            // Fade in and out
            float alpha = t < 0.2f ? (t / 0.2f) : (1f - (t - 0.2f) / 0.8f);
            Color c = emberColor;
            c.a = Mathf.Clamp01(alpha);
            ember.color = c;

            yield return null;
        }

        if (ember != null)
        {
            activeEmbers.Remove(ember);
            Destroy(ember.gameObject);
        }
    }

    /// <summary>
    /// 강화 버튼 클릭 시 안빌 충격 바운스 및 스파크 튀는 이펙트 연출을 트리거합니다.
    /// </summary>
    public void TriggerEnhanceSparkFX()
    {
        if (anvilRect != null)
        {
            StartCoroutine(AnvilBounceRoutine());
        }

        // 15개 스파크 흩뿌리기
        if (emberContainer != null && emberTemplate != null)
        {
            Vector2 origin = anvilRect != null ? anvilRect.anchoredPosition : Vector2.zero;
            for (int i = 0; i < 15; i++)
            {
                SpawnSpark(origin);
            }
        }
    }

    private IEnumerator AnvilBounceRoutine()
    {
        Vector3 originalScale = anvilRect.localScale;
        anvilRect.localScale = originalScale * 1.12f;
        yield return new WaitForSeconds(0.1f);
        anvilRect.localScale = originalScale;
    }

    private void SpawnSpark(Vector2 origin)
    {
        Image spark = Instantiate(emberTemplate, emberContainer);
        spark.gameObject.SetActive(true);

        RectTransform rect = spark.GetComponent<RectTransform>();
        rect.anchoredPosition = origin;
        rect.sizeDelta = new Vector2(6f, 6f);
        spark.color = new Color(1f, 0.85f, 0.2f, 1f);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float speed = Random.Range(100f, 250f);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        StartCoroutine(AnimateSpark(spark, rect, dir, speed));
    }

    private IEnumerator AnimateSpark(Image spark, RectTransform rect, Vector2 dir, float speed)
    {
        float duration = Random.Range(0.3f, 0.6f);
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;

        while (elapsed < duration)
        {
            if (spark == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rect.anchoredPosition = startPos + dir * (speed * elapsed);

            Color c = spark.color;
            c.a = 1f - t;
            spark.color = c;

            yield return null;
        }

        if (spark != null)
        {
            Destroy(spark.gameObject);
        }
    }

    private void ClearEmbers()
    {
        foreach (var ember in activeEmbers)
        {
            if (ember != null) Destroy(ember.gameObject);
        }
        activeEmbers.Clear();
    }
}
