using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro; // TextMeshPro 관련 컴포넌트 접근용

/// <summary>
/// 소셜 그래프 미니게임의 BannerSubTxt에 긴장감과 기대감을 극대화하기 위한 Juice 연출 컨트롤러입니다.
/// - 1초~5초: 각도 변경 없이 X좌표가 빠르게 [n-1, n, n+1] 지그재그 패턴으로 진동 (1초부터 시작)
/// - 5초 이후: 진동 수준 고정 및 텍스트 주변에서 사방으로 튀는 파편 파티클 연출 구동
/// - 부모 패널의 LayoutGroup(Horizontal/Vertical Layout) 강제 좌표 고정을 완벽 우회하기 위해 TMP 텍스트 Margin 제어 방식 도입
/// </summary>
[DisallowMultipleComponent]
public class BannerSubTxtJuiceController : MonoBehaviour
{
    [Header("UI 대상 타깃 설정")]
    [Tooltip("진동 및 파편 효과를 적용할 BannerSubTxt의 RectTransform입니다. 미할당 시 자동 탐색합니다.")]
    [SerializeField] private RectTransform targetTextRect;

    [Tooltip("파편 파티클들이 생성될 최상위 부모 컨테이너 RectTransform입니다. LayoutGroup 영향을 안 받는 최상위 Canvas 권장.")]
    [SerializeField] private RectTransform particleContainer;

    [Header("진동 (Shake) 세부 설정")]
    [Tooltip("진동 연출이 시작되는 라운드 시간(초)입니다. (기획 요구사항: 1.0초)")]
    [SerializeField] private float shakeStartTime = 1.0f;

    [Tooltip("진동 연출이 최고조에 달하는 라운드 시간(초)입니다.")]
    [SerializeField] private float shakeMaxTime = 5.0f;

    [Tooltip("★[X좌표 이동 폭 조절]★ 최고조(5초 이후) 시점의 X좌표 최대 이동 흔들림 오프셋(px)입니다. (예: 10~100)")]
    [SerializeField] private float maxShakeOffsetX = 20.0f;

    [Tooltip("진동 고속 스위칭 속도(초)입니다. 작을수록 빠르게 [n-1, n, n+1]로 진동합니다.")]
    [SerializeField] private float shakeStepInterval = 0.03f;

    [Header("파편 파티클 (Debris Particle) 세부 설정")]
    [Tooltip("BannerSubTxt의 중심을 기준으로 파티클이 생성될 중심 위치(X, Y) 오프셋(px)입니다.")]
    [SerializeField] private Vector2 debrisCenterOffset = Vector2.zero;

    [Tooltip("5초 이후 파편 파티클 생성 간격(초)입니다.")]
    [SerializeField] private float debrisSpawnInterval = 0.06f;

    [Tooltip("기본 파편 파티클 크기(px)입니다.")]
    [SerializeField] private Vector2 defaultParticleSize = new Vector2(24f, 24f);

    [Tooltip("파편 파티클 프리팹입니다. 미지정 시 런타임에 동적으로 UI 파티클을 자동 생성합니다.")]
    [SerializeField] private GameObject debrisParticlePrefab;

    [Tooltip("파편 파티클에 적용될 선명한 스파크 컬러 모음입니다.")]
    [SerializeField] private Color[] debrisColors = new Color[]
    {
        new Color(1.0f, 0.9f, 0.2f, 1.0f),  // 선명한 황금빛 골드
        new Color(1.0f, 0.55f, 0.1f, 1.0f), // 비주얼 강한 주황빛
        new Color(1.0f, 1.0f, 0.6f, 1.0f),  // 눈부신 백황색 스파크
        new Color(1.0f, 0.35f, 0.2f, 1.0f)  // 화염 스파크 주황
    };

    // 부모 LayoutGroup의 덮어쓰기를 완벽 우회하기 위한 텍스트 마진(Margin) 조작용 변수
    private TextMeshProUGUI targetTMPText;
    private Vector4 originalMargin;
    private Vector3 originalLocalPosition;
    private Quaternion originalRotation;
    private bool isPositionSaved = false;

    // [n-1, n, n+1] 지그재그 패턴용 제어 변수
    private float shakeStepTimer = 0f;
    private int currentStepIndex = 0;
    private static readonly float[] StepPattern = new float[] { -1.0f, 0.0f, 1.0f, 0.0f, -0.7f, 0.7f, 1.0f, -1.0f };

    // 파티클 스폰 제어 변수
    private float debrisTimer = 0f;

    // 유니티 내장 ObjectPool
    private ObjectPool<UI_DebrisParticle> particlePool;
    private readonly List<UI_DebrisParticle> activeParticles = new List<UI_DebrisParticle>();

    private void Awake()
    {
        InitializePool();
    }

    private void Start()
    {
        EnsureTargetReferences();
        GraphGameManager.OnStateChanged += HandleStateChanged;

        if (GraphGameManager.Instance != null)
        {
            HandleStateChanged(GraphGameManager.Instance.CurrentState);
        }
    }

    private void EnsureTargetReferences()
    {
        if (targetTextRect == null)
        {
            GameObject foundObj = GameObject.Find("BannerSubTxt");
            if (foundObj != null)
            {
                targetTextRect = foundObj.GetComponent<RectTransform>();
            }
            else
            {
                targetTextRect = GetComponent<RectTransform>();
            }
        }

        // TMP 컴포넌트를 가져와 레이아웃 독립적인 흔들림을 준비
        if (targetTextRect != null)
        {
            targetTMPText = targetTextRect.GetComponent<TextMeshProUGUI>();
        }

        FindSafeParticleContainer();
    }

    private void SaveOriginalTransform()
    {
        if (targetTextRect != null)
        {
            originalLocalPosition = targetTextRect.localPosition;
            originalRotation = targetTextRect.localRotation;
            
            if (targetTMPText != null)
            {
                originalMargin = targetTMPText.margin;
            }
            
            isPositionSaved = true;
        }
    }

    private void FindSafeParticleContainer()
    {
        if (particleContainer != null) return;

        if (targetTextRect != null)
        {
            Canvas canvas = targetTextRect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                particleContainer = canvas.transform as RectTransform;
            }
            else
            {
                particleContainer = targetTextRect.parent as RectTransform;
            }
        }
    }

    private void OnDestroy()
    {
        GraphGameManager.OnStateChanged -= HandleStateChanged;
        ResetJuiceEffect();
    }

    private void Update()
    {
        if (GraphGameManager.Instance == null) return;
        if (targetTextRect == null) return;

        if (GraphGameManager.Instance.CurrentState == GraphGameState.Running)
        {
            float currentTimer = GraphGameManager.Instance.CurrentTimer;

            // 라운드 시작 직후 원래 위치와 마진 값을 저장
            if (!isPositionSaved)
            {
                SaveOriginalTransform();
            }

            // 1. 진동 강도 연산 (1.0초~5초: 0->1 smooth, 5초~: 1 고정)
            float intensity = 0f;
            if (currentTimer >= shakeStartTime)
            {
                if (currentTimer >= shakeMaxTime)
                {
                    intensity = 1.0f;
                }
                else
                {
                    intensity = (currentTimer - shakeStartTime) / (shakeMaxTime - shakeStartTime);
                    intensity = Mathf.Clamp01(intensity);
                    intensity = intensity * intensity;
                }
            }

            // 2. 흔들림 오프셋 계산
            float currentOffsetX = 0f;
            if (intensity > 0f)
            {
                shakeStepTimer += Time.deltaTime;
                if (shakeStepTimer >= shakeStepInterval)
                {
                    shakeStepTimer = 0f;
                    currentStepIndex = (currentStepIndex + 1) % StepPattern.Length;
                }

                float stepMultiplier = StepPattern[currentStepIndex];
                currentOffsetX = stepMultiplier * (maxShakeOffsetX * intensity);
            }

            // 3. 레이아웃 덮어쓰기 무력화 (TMP 텍스트의 Margin 조작)
            // 매 프레임 UI LayoutRebuilder가 부모 LayoutGroup의 명령에 따라 위치(anchoredPosition, localPosition)를 복구해버리므로,
            // 텍스트 컴포넌트 자체의 내부 렌더링 마진을 움직여 LayoutGroup과 무관하게 글자를 이동시킵니다!
            if (targetTMPText != null && isPositionSaved)
            {
                // margin은 (left, top, right, bottom) 구조입니다.
                // X 오프셋만큼 좌우 마진을 동시에 밀어주어 텍스트 렌더링 위치만 흔들리게 만듭니다.
                Vector4 newMargin = originalMargin;
                newMargin.x += currentOffsetX; // left
                newMargin.z -= currentOffsetX; // right
                targetTMPText.margin = newMargin;
                
                // 회전은 유지
                targetTextRect.localRotation = originalRotation;
            }
            else
            {
                // TMP가 없는 만약의 경우를 위한 백업 (위치 기반 이동)
                targetTextRect.localPosition = originalLocalPosition + new Vector3(currentOffsetX, 0f, 0f);
                targetTextRect.localRotation = originalRotation;
            }

            // 4. 5초 이후 파편 파티클 연출 구동
            if (currentTimer >= shakeMaxTime)
            {
                debrisTimer += Time.deltaTime;
                if (debrisTimer >= debrisSpawnInterval)
                {
                    debrisTimer = 0f;
                    int spawnCount = UnityEngine.Random.Range(1, 3);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnDebrisParticle();
                    }
                }
            }
        }
    }

    private void HandleStateChanged(GraphGameState newState)
    {
        if (newState != GraphGameState.Running)
        {
            ResetJuiceEffect();
        }
        else
        {
            debrisTimer = 0f;
            shakeStepTimer = 0f;
            currentStepIndex = 0;
            isPositionSaved = false; // 새로운 라운드 시작 시 원위치 재저장 유도
        }
    }

    public void ResetJuiceEffect()
    {
        debrisTimer = 0f;
        shakeStepTimer = 0f;
        currentStepIndex = 0;

        if (targetTextRect != null && isPositionSaved)
        {
            if (targetTMPText != null)
            {
                targetTMPText.margin = originalMargin;
            }
            targetTextRect.localPosition = originalLocalPosition;
            targetTextRect.localRotation = originalRotation;
        }

        isPositionSaved = false;

        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            if (activeParticles[i] != null)
            {
                activeParticles[i].ReturnToPool();
            }
        }
        activeParticles.Clear();
    }

    private void SpawnDebrisParticle()
    {
        if (particlePool == null) return;
        if (targetTextRect == null) return;

        UI_DebrisParticle particle = particlePool.Get();
        if (particle == null) return;

        Vector2 rectSize = targetTextRect.rect.size;
        if (rectSize.x < 50f) rectSize.x = 280f;
        if (rectSize.y < 20f) rectSize.y = 50f;

        Vector3 localOffset = new Vector3(
            debrisCenterOffset.x + UnityEngine.Random.Range(-rectSize.x * 0.48f, rectSize.x * 0.48f),
            debrisCenterOffset.y + UnityEngine.Random.Range(-rectSize.y * 0.45f, rectSize.y * 0.45f),
            0f
        );

        Vector3 worldSpawnPos = targetTextRect.TransformPoint(localOffset);

        Color particleColor = Color.yellow;
        if (debrisColors != null && debrisColors.Length > 0)
        {
            particleColor = debrisColors[UnityEngine.Random.Range(0, debrisColors.Length)];
        }

        particle.LaunchWorld(worldSpawnPos, particleColor, (p) => particlePool.Release(p));
    }

    #region ObjectPool Implementation
    private void InitializePool()
    {
        particlePool = new ObjectPool<UI_DebrisParticle>(
            createFunc: CreateParticleInstance,
            actionOnGet: OnGetParticle,
            actionOnRelease: OnReleaseParticle,
            actionOnDestroy: OnDestroyParticle,
            collectionCheck: true,
            defaultCapacity: 20,
            maxSize: 80
        );
    }

    private UI_DebrisParticle CreateParticleInstance()
    {
        FindSafeParticleContainer();

        GameObject go;
        if (debrisParticlePrefab != null)
        {
            go = Instantiate(debrisParticlePrefab, particleContainer != null ? particleContainer : transform);
        }
        else
        {
            go = new GameObject("Dynamic_DebrisParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(UI_DebrisParticle));
            go.transform.SetParent(particleContainer != null ? particleContainer : transform, false);

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = defaultParticleSize;

            LayoutElement layoutElem = go.GetComponent<LayoutElement>();
            if (layoutElem != null)
            {
                layoutElem.ignoreLayout = true;
            }
        }

        UI_DebrisParticle particle = go.GetComponent<UI_DebrisParticle>();
        if (particle == null)
        {
            particle = go.AddComponent<UI_DebrisParticle>();
        }

        return particle;
    }

    private void OnGetParticle(UI_DebrisParticle particle)
    {
        particle.gameObject.SetActive(true);
        activeParticles.Add(particle);
    }

    private void OnReleaseParticle(UI_DebrisParticle particle)
    {
        particle.gameObject.SetActive(false);
        activeParticles.Remove(particle);
    }

    private void OnDestroyParticle(UI_DebrisParticle particle)
    {
        if (particle != null)
        {
            Destroy(particle.gameObject);
        }
    }
    #endregion
}
