using UnityEngine;

/// <summary>
/// 기준 필드(UI_FieldDragController의 targetFieldRoot)가 움직일 때,
/// 지정한 배율 가중치(parallaxMultiplier)를 곱하여 독립적인 속도로 미끄러지듯 따라 움직이는
/// UGUI 전용 다방향 시차(Parallax) 레이어 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UI_ParallaxLayer : MonoBehaviour
{
    [Header("시차 기준 연동")]
    [Tooltip("기준점이 될 UI_FieldDragController의 targetFieldRoot를 인스펙터에서 연결해 줍니다.")]
    public RectTransform referenceField;

    [Header("시차 연출 설정")]
    [Tooltip("기준 필드가 움직일 때 이 레이어가 따라 움직일 가중치 배율 벡터입니다 (X, Y). 원경일수록 작은 값(예: 0.1), 근경에 가까운 중경일수록 큰 값(예: 0.5)을 줍니다.")]
    public Vector2 parallaxMultiplier = new Vector2(0.5f, 0.5f);

    private RectTransform myRectTransform;
    
    // 초기 기준 오프셋 보존용 필드
    private Vector2 initialPos;
    private Vector2 initialRefPos;
    private bool isInitialized = false;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        InitializeOffset();
    }

    /// <summary>
    /// 로컬 캔버스 상에서 자신의 시작 위치와 기준 필드의 시작 위치를 캐싱합니다.
    /// </summary>
    public void InitializeOffset()
    {
        if (referenceField != null)
        {
            initialPos = myRectTransform.anchoredPosition;
            initialRefPos = referenceField.anchoredPosition;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning($"[UI_ParallaxLayer] {gameObject.name}에 referenceField(기준 필드)가 설정되어 있지 않아 오프셋 측정을 건너뜁니다. 유니티 인스펙터에서 연결이 필요합니다.");
        }
    }

    private void LateUpdate()
    {
        if (!isInitialized || referenceField == null) return;

        // 1. 기준 필드가 최초 배치 위치에서 현재까지 이동한 델타 변화량을 구합니다.
        Vector2 currentRefPos = referenceField.anchoredPosition;
        Vector2 referenceDelta = currentRefPos - initialRefPos;

        // 2. 델타 변화량에 설정된 시차 비율 가중치를 곱하여 자신의 anchoredPosition에 무손실 갱신합니다.
        myRectTransform.anchoredPosition = initialPos + (referenceDelta * parallaxMultiplier);
    }
}
