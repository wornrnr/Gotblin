using UnityEngine;

/// <summary>
/// 각 건물의 기본 기획 스펙을 정의하는 ScriptableObject 데이터 파일입니다.
/// </summary>
[CreateAssetMenu(fileName = "BuildingData", menuName = "Gotblin/Building/Building Data", order = 1)]
public class BuildingData : ScriptableObject
{
    [Header("건물 스펙 설정")]
    [Tooltip("건물의 고유 식별자 ID입니다. (예: TownHall, Barracks)")]
    [SerializeField] private string buildingID;

    [Tooltip("유저에게 표시될 건물의 한글 이름입니다.")]
    [SerializeField] private string buildingName;

    [Tooltip("1레벨 건설 시 필요한 기본 골드 비용입니다.")]
    [SerializeField] private int baseCost = 200;

    [Tooltip("1레벨 건설 시 소요되는 기본 건설 시간(초 단위)입니다.")]
    [SerializeField] private float baseBuildTime = 10f;

    [Tooltip("건물이 도달할 수 있는 최대 레벨입니다.")]
    [SerializeField] private int maxLevel = 5;

    // 외부 노출 Read-Only 프로퍼티
    public string BuildingID => buildingID;
    public string BuildingName => buildingName;
    public int BaseCost => baseCost;
    public float BaseBuildTime => baseBuildTime;
    public int MaxLevel => maxLevel;
}
