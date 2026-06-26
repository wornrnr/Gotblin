using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ScrollRect 컴포넌트를 사용하지 않고 모바일 터치 및 마우스 드래그를 직접 감지하여
/// 관성(Inertia) 감속이 적용된 UGUI 부락 필드 드래그 핸들러 클래스입니다.
/// 화면(Viewport) 밖으로 필드가 완전히 이탈하지 않도록 정밀한 바운더리 락을 구현합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UI_FieldDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("기준 연동 트랜스폼")]
    [Tooltip("이동 및 드래그 처리를 진행할 최하단 근경/필드 루트 RectTransform입니다.")]
    [SerializeField] private RectTransform targetFieldRoot;

    [Tooltip("화면에 보여지는 뷰포트 기준 틀 RectTransform입니다 (일반적으로 TownBuildingPanel 본체).")]
    [SerializeField] private RectTransform viewportBounds;

    [Header("드래그 & 관성 설정")]
    [Tooltip("드래그 반응 감도입니다.")]
    [SerializeField] private float dragSensitivity = 1.0f;

    [Tooltip("드래그 해제 시 관성 속도가 0에 가깝게 멈추는 감속도 비율입니다. (클수록 빨리 멈춤)")]
    [SerializeField] private float decelerationRate = 9f;

    private RectTransform myRectTransform;
    private Vector2 velocity = Vector2.zero;
    private bool isDragging = false;

    // 실시간 클램프 바운더리 한계값
    private float minX, maxX;
    private float minY, maxY;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 레퍼런스 누락 시 자기 자신 또는 부모를 대입하여 작동 유연성 확보
        if (targetFieldRoot == null) targetFieldRoot = myRectTransform;
        if (viewportBounds == null && transform.parent != null)
        {
            viewportBounds = transform.parent.GetComponent<RectTransform>();
        }

        if (viewportBounds == null)
        {
            Debug.LogError("[UI_FieldDragController] viewportBounds 레퍼런스를 지정하지 못했습니다!");
            enabled = false;
            return;
        }

        // 초기 바운더리 영역 계산
        CalculateBoundaries();
    }

    private void Update()
    {
        // 1. 드래그 중이 아닐 때는 계산된 마지막 속도(관성)에 따라 부드럽게 감속 이동합니다.
        if (!isDragging && velocity.sqrMagnitude > 0.001f)
        {
            Vector2 pos = targetFieldRoot.anchoredPosition;
            pos += velocity * Time.deltaTime;

            // 이동 범위를 뷰포트 내로 실시간 한정 클램핑
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            targetFieldRoot.anchoredPosition = pos;

            // 속도 감속 처리 (Linear Interpolation Decay)
            velocity = Vector2.Lerp(velocity, Vector2.zero, decelerationRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// 근경(targetFieldRoot)과 뷰포트(viewportBounds)의 크기 차이를 계산하여 드래그 제한 범위를 정의합니다.
    /// (이 계산은 targetFieldRoot와 viewportBounds가 둘 다 Pivot(0.5, 0.5)이고 앵커가 정중앙인 환경에서 최적화됩니다)
    /// </summary>
    public void CalculateBoundaries()
    {
        if (targetFieldRoot == null || viewportBounds == null) return;

        // 두 UI의 가로/세로 길이 차이를 기반으로 대칭 한계 영역 계산
        float limitX = Mathf.Max(0f, (targetFieldRoot.rect.width - viewportBounds.rect.width) / 2f);
        float limitY = Mathf.Max(0f, (targetFieldRoot.rect.height - viewportBounds.rect.height) / 2f);

        minX = -limitX;
        maxX = limitX;
        minY = -limitY;
        maxY = limitY;
    }

    #region EventSystems Drag Interfaces

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        velocity = Vector2.zero; // 드래그 시작 시 관성 초기화
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetFieldRoot == null) return;

        // PointerEventData의 델타값은 화면 해상도 대비 마우스/터치 변위량이므로 감도 비례 연산
        Vector2 delta = eventData.delta * dragSensitivity;

        // 실시간 속도 누적 계산 (관성 이동에 쓰임)
        if (Time.deltaTime > 0f)
        {
            velocity = delta / Time.deltaTime;
        }

        Vector2 pos = targetFieldRoot.anchoredPosition;
        pos += delta;

        // 화면 밖으로 탈출 방지 클램핑 적용
        CalculateBoundaries(); // 런타임 해상도/크기 변경 대응을 위해 드래그 시 실시간 재계산
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        targetFieldRoot.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // 드래그를 딱 놓았을 때의 속도가 너무 약하면 즉시 멈추도록 예외 방어
        if (velocity.sqrMagnitude < 100f)
        {
            velocity = Vector2.zero;
        }
    }

    #endregion
}
