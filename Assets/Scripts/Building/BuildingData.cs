using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 건물 레벨에 따른 소모 비용, 소요 시간 및 비주얼 스프라이트를 보관하는 구조체입니다.
/// </summary>
[System.Serializable]
public struct BuildingLevelEnv
{
    [Tooltip("레벨 값입니다 (0 = 미건설, 1 이상 = 건설 상태)")]
    public int level;

    [Tooltip("이 레벨에서 다음 레벨로 업그레이드할 때 필요한 골드 비용입니다. (최고 레벨에서는 사용되지 않음)")]
    public int upgradeCost;

    [Tooltip("이 레벨에서 다음 레벨로 업그레이드하는 데 걸리는 시간(초 단위)입니다. (최고 레벨에서는 사용되지 않음)")]
    public float buildDuration;

    [Tooltip("이 레벨에 도달했을 때 월드 맵 및 UI에 노출될 건물의 도트 이미지 스프라이트입니다.")]
    public Sprite visualSprite;
}

/// <summary>
/// 각 건물의 기획 정보를 테이블 레벨별 리스트 데이터 기반으로 정의하는 ScriptableObject 에셋 템플릿입니다.
/// </summary>
[CreateAssetMenu(fileName = "BuildingData", menuName = "Gotblin/Building/Building Data", order = 1)]
public class BuildingData : ScriptableObject
{
    [Header("건물 기본 설정")]
    [Tooltip("건물의 고유 식별 ID입니다. (예: TownHall, Barracks)")]
    [SerializeField] private string buildingID;

    [Tooltip("유저에게 보여줄 건물의 한글 명칭입니다.")]
    [SerializeField] private string buildingName;

    [Header("레벨별 가변 스펙 설정 (테이블)")]
    [Tooltip("레벨별 스프라이트, 비용, 빌드 시간 등을 설정하는 리스트입니다. (0번 인덱스는 미건설 상태 스펙)")]
    public List<BuildingLevelEnv> levelSettings = new List<BuildingLevelEnv>();

    // 외부 노출 Read-Only 프로퍼티
    public string BuildingID => buildingID;
    public string BuildingName => buildingName;

    /// <summary>
    /// 이 건물이 도달할 수 있는 최고 레벨 한계치입니다. (인덱스의 최대값)
    /// </summary>
    public int MaxLevel => levelSettings != null ? Mathf.Max(0, levelSettings.Count - 1) : 0;
}
