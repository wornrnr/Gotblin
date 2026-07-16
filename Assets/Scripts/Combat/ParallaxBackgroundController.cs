using UnityEngine;

/// <summary>
/// 사이드 스크롤러 전투 진행 시, 카메라/배경 이동에 맞춰 
/// 원경(Far Background)과 근경(Near Background)을 차등 속도로 패럴랙스 스크롤링 해주는 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxBackgroundController : MonoBehaviour
{
    [Header("패럴랙스 배경 레이어 참조")]
    [Tooltip("가장 먼 거리의 원경 배경 RectTransform입니다. (가장 천천히 이동)")]
    public RectTransform farBackground;

    [Tooltip("중간 거리의 근경/중경 배경 RectTransform입니다. (중간 속도 이동)")]
    public RectTransform nearBackground;

    [Header("패럴랙스 이동 속도 비율 (Parallax Multipliers)")]
    [Tooltip("원경 패럴랙스 속도 비율입니다. (0.25 = 카메라 이동량의 25%만 이동)")]
    [Range(0f, 1f)]
    public float farMultiplier = 0.25f;

    [Tooltip("근경 패럴랙스 속도 비율입니다. (0.55 = 카메라 이동량의 55% 이동)")]
    [Range(0f, 1f)]
    public float nearMultiplier = 0.55f;

    private RectTransform fieldRect;
    private Vector2 initialFarPos;
    private Vector2 initialNearPos;

    private void Awake()
    {
        fieldRect = GetComponent<RectTransform>();

        if (farBackground != null)
        {
            initialFarPos = farBackground.anchoredPosition;
        }
        if (nearBackground != null)
        {
            initialNearPos = nearBackground.anchoredPosition;
        }
    }

    private void Start()
    {
        // 초기 포지션 셋업
        if (fieldRect == null) fieldRect = GetComponent<RectTransform>();
        UpdateParallax();
    }

    private void LateUpdate()
    {
        UpdateParallax();
    }

    /// <summary>
    /// combatFieldContext의 앵커 X 좌표 변화량을 기반으로 원경/근경의 로컬 오프셋을 역산 패닝시킵니다.
    /// </summary>
    public void UpdateParallax()
    {
        if (fieldRect == null) return;

        float contextX = fieldRect.anchoredPosition.x;

        // 원경 (Far Background): 1.0 - farMultiplier 만큼 로컬 오프셋을 역보정하여 0.25x 효과 완성
        if (farBackground != null)
        {
            float farOffset = -contextX * (1f - farMultiplier);
            farBackground.anchoredPosition = new Vector2(initialFarPos.x + farOffset, initialFarPos.y);
        }

        // 근경 (Near Background): 1.0 - nearMultiplier 만큼 로컬 오프셋을 역보정하여 0.55x 효과 완성
        if (nearBackground != null)
        {
            float nearOffset = -contextX * (1f - nearMultiplier);
            nearBackground.anchoredPosition = new Vector2(initialNearPos.x + nearOffset, initialNearPos.y);
        }
    }
}
