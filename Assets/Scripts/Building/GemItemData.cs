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

    [Header("강화 가중치 밸런스 설정 (성공:실패:파괴)")]
    [Tooltip("강화 성공 가중치 (기본: 3 -> 60%)")]
    public int successWeight = 3;

    [Tooltip("강화 실패(단계 유지) 가중치 (기본: 1 -> 20%)")]
    public int keepWeight = 1;

    [Tooltip("강화 실패(아이템 파괴) 가중치 (기본: 1 -> 20%)")]
    public int destroyWeight = 1;

    /// <summary>
    /// 성공/실패/파괴 가중치의 총합입니다. (3 + 1 + 1 = 5)
    /// </summary>
    public int TotalWeight => Mathf.Max(1, successWeight + keepWeight + destroyWeight);

    /// <summary>
    /// 가중치 기반 성공 확률 비율 (0.0 ~ 1.0)
    /// </summary>
    public float upgradeSuccessRate => (float)successWeight / TotalWeight;

    /// <summary>
    /// 가중치 기반 실패(단계 유지) 확률 비율 (0.0 ~ 1.0)
    /// </summary>
    public float upgradeKeepRate => (float)keepWeight / TotalWeight;

    /// <summary>
    /// 가중치 기반 파괴 확률 비율 (0.0 ~ 1.0)
    /// </summary>
    public float upgradeDestroyRate => (float)destroyWeight / TotalWeight;

    [Tooltip("보석 강화 성공 시 변경될 다음 단계의 보석 템플릿입니다.")]
    public GemItemData nextLevelGem;
}
