using UnityEngine;

/// <summary>
/// 대장간에서 강화 및 판매가 가능한 보석 아이템의 스펙 및 밸런스 데이터를 정의하는 ScriptableObject 에셋 템플릿입니다.
/// </summary>
[CreateAssetMenu(fileName = "GemItemData", menuName = "Gotblin/Blacksmith/Gem Item Data", order = 2)]
public class GemItemData : ScriptableObject
{
    [Header("보석 기본 정보")]
    [Tooltip("보석의 고유 식별 ID입니다. (예: ruby_lvl1)")]
    public string gemID;

    [Tooltip("유저에게 표시될 보석의 명칭입니다. (예: 루비 1단계)")]
    public string gemName;

    [Tooltip("보석의 강화 단계 레벨입니다.")]
    public int level = 1;

    [Tooltip("보석의 외형 비주얼 아이콘 스프라이트입니다.")]
    public Sprite iconSprite;

    [Header("경제 및 강화 밸런스")]
    [Tooltip("이 보석을 판매했을 때 획득하는 골드 판매가입니다.")]
    public int sellPrice = 500;

    [Tooltip("보석 강화 성공 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float upgradeSuccessRate = 0.5f;

    [Tooltip("보석 강화 실패 시 무변화(유지)될 확률 (0.0 ~ 1.0). 나머지는 파괴 소멸 처리")]
    [Range(0f, 1f)]
    public float upgradeKeepRate = 0.3f;

    [Tooltip("보석 강화 성공 시 변경될 다음 단계의 보석 템플릿입니다.")]
    public GemItemData nextLevelGem;
}
