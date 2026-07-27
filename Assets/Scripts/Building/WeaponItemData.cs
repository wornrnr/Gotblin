using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 추가 옵션의 종류를 정의하는 Enum입니다.
/// </summary>
public enum WeaponOptionType
{
    ATKPercent,    // 공격력 (%)
    TargetCount,   // 공격 타겟 수
    AttackSpeed,   // 공격 속도
    HPPercent,     // 생명력 (%)
    MoveSpeed      // 이동 속도
}

/// <summary>
/// 무기에 부여되는 단일 추가 옵션 정보 구조체입니다.
/// </summary>[System.Serializable]
public struct ItemOption
{
    [Tooltip("추가 옵션의 종류입니다.")]
    public WeaponOptionType optionType;

    [Tooltip("옵션의 수치입니다. (퍼센트 옵션은 0.1 = 10% 단위로 설정)")]
    public float value;
}

/// <summary>
/// 대장간 무기의 기본 정보, 기본/추가 옵션, visual 에셋 및 강화 밸런스를 정의하는 ScriptableObject 에셋 템플릿입니다.
/// </summary>
[CreateAssetMenu(fileName = "WeaponItemData", menuName = "Gotblin/Blacksmith/Weapon Item Data", order = 1)]
public class WeaponItemData : ScriptableObject
{
    [Header("무기 기본 정보")]
    [Tooltip("무기의 고유 식별 ID입니다. (예: sword_a_1)")]
    public string weaponID;

    [Tooltip("유저에게 표기될 무기의 한글 이름입니다.")]
    public string weaponName;

    [Tooltip("무기의 계열 종류입니다. (예: 단검, 양손검, 도끼 등)")]
    public string weaponType;

    [Tooltip("무기의 등급 수치입니다. (1-indexed)")]
    public int grade = 1;

    [Header("비주얼 스프라이트 설정")]
    [Tooltip("히어로 고블린 손에 장착되어 코어3 전투 씬에 렌더링될 weapon_visual 스프라이트입니다.")]
    public Sprite visualSprite;

    [Tooltip("대장간 UI 및 인벤토리 슬롯에 표시될 아이콘 스프라이트입니다.")]
    public Sprite iconSprite;

    [Header("옵션 구성")]
    [Tooltip("[기본 옵션] 모든 종류 및 등급 무기에 공통 적용되는 기본 공격력 수치입니다.")]
    public int baseATK = 10;

    [Tooltip("[추가 옵션] 등급 및 종류에 따라 추가 부여되는 옵션 목록입니다. (공격력%, 타겟수, 공속, 생명력%, 이동속도)")]
    public List<ItemOption> additionalOptions = new List<ItemOption>();

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

    [Tooltip("강화 시 소모되는 '철 주괴' 개수입니다.")]
    public int requiredIronIngot = 5;

    [Tooltip("강화 성공 시 변경될 다음 단계/등급의 무기 데이터입니다.")]
    public WeaponItemData nextGradeWeapon;
}
