using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponEnhanceResult
{
    Success,           // 강화 성공 (다음 단계로 변경)
    Keep,              // 변화 없음 (단계 유지)
    ProtectedFailure,  // 실패하였으나 파괴 방지권 사용으로 파괴 소멸 방지됨
    DestroyedFailure   // 실패하여 무기가 완전히 파괴 소멸됨
}

public enum GemEnhanceResult
{
    Success,   // 강화 성공
    Keep,      // 변화 없음
    Destroyed  // 파괴 소멸
}

/// <summary>
/// 무기 장착 옵션 합산 수치를 보관하는 구조체입니다.
/// </summary>
public struct CalculatedWeaponStats
{
    public int bonusBaseATK;
    public float bonusATKPercent;
    public int bonusTargetCount;
    public float bonusAttackSpeed;
    public float bonusHPPercent;
    public float bonusMoveSpeed;
}

/// <summary>
/// 대장간 해금 조건 검증, 무기 장착 및 외형/스탯 연동, 무기/보석 확률형 강화 및 파괴 방지권 처리,
/// 그리고 재화 소모 및 보석 판매를 총괄하는 전역 매니저 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class BlacksmithManager : MonoBehaviour
{
    public static BlacksmithManager Instance { get; private set; }

    [Header("건물 연동 식별 ID")]
    [Tooltip("BuildingManager에서 참조할 대장간 건물의 고유 ID입니다.")]
    public string blacksmithBuildingID = "Blacksmith";

    [Header("재화 및 수량 소지품")]
    [Tooltip("무기 강화 시 소모되는 '철 주괴' 개수입니다.")]
    public int ironIngotCount = 50;

    [Tooltip("무기 강화 실패 시 파괴를 방지해주는 '파괴 방지권' 아이템 개수입니다.")]
    public int protectionItemCount = 5;

    [Header("유저 보유 아이템 세션")]
    [Tooltip("현재 히어로 고블린이 장착 중인 무기 데이터입니다.")]
    public WeaponItemData equippedWeapon;

    [Tooltip("유저가 소유하고 있는 무기 리스트입니다.")]
    public List<WeaponItemData> ownedWeapons = new List<WeaponItemData>();

    [Tooltip("유저가 소유하고 있는 보석 리스트입니다.")]
    public List<GemItemData> ownedGems = new List<GemItemData>();

    // 무기/보석 상태 변경 이벤트
    public static event Action OnInventoryUpdated;
    public static event Action OnEquippedWeaponChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 대장간 건물이 건설 완료(Lv >= 1)되어 콘텐츠가 해금되었는지 여부를 확인합니다.
    /// </summary>
    public bool IsBlacksmithUnlocked()
    {
        if (BuildingManager.Instance != null)
        {
            var bInstance = BuildingManager.Instance.GetBuildingInstance(blacksmithBuildingID);
            return bInstance != null && bInstance.currentLevel >= 1 && !bInstance.isConstructing;
        }
        return false;
    }

    /// <summary>
    /// 무기를 장착하고 히어로 고블린의 외형(weapon_visual) 및 전투 스탯을 실시간 갱신합니다.
    /// </summary>
    public bool EquipWeapon(WeaponItemData weapon)
    {
        if (weapon != null && !ownedWeapons.Contains(weapon))
        {
            ownedWeapons.Add(weapon);
        }

        equippedWeapon = weapon;
        ApplyHeroStatsAndVisual();

        OnEquippedWeaponChanged?.Invoke();
        OnInventoryUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 무기를 대장간에서 강화 처리합니다 (철 주괴 소모 ➡️ 성공/유지/실패 확률 및 파괴 방지 연동)
    /// </summary>
    public WeaponEnhanceResult EnhanceWeapon(WeaponItemData weapon, bool useProtectionItem)
    {
        if (weapon == null) return WeaponEnhanceResult.Keep;

        // 1. 철 주괴 수량 검증
        if (ironIngotCount < weapon.requiredIronIngot)
        {
            Debug.LogWarning($"[BlacksmithManager] 철 주괴가 부족합니다! (필요: {weapon.requiredIronIngot}, 보유: {ironIngotCount})");
            return WeaponEnhanceResult.Keep;
        }

        // 2. 철 주괴 차감
        ironIngotCount -= weapon.requiredIronIngot;

        // 3. 강화 확률 연산
        float rnd = UnityEngine.Random.value;
        float successRatio = Mathf.Clamp01(weapon.upgradeSuccessRate);
        float keepRatio = Mathf.Clamp01(weapon.upgradeKeepRate);

        WeaponEnhanceResult result;

        if (rnd <= successRatio)
        {
            // [성공]: 다음 단계 무기로 인벤토리 교체
            result = WeaponEnhanceResult.Success;
            if (weapon.nextGradeWeapon != null)
            {
                WeaponItemData nextWeapon = weapon.nextGradeWeapon;
                int idx = ownedWeapons.IndexOf(weapon);
                if (idx >= 0)
                {
                    ownedWeapons[idx] = nextWeapon;
                }
                else
                {
                    ownedWeapons.Add(nextWeapon);
                }

                // 장착 중이던 무기였다면 자동으로 다음 단계 무기로 장착 동기화
                if (equippedWeapon == weapon)
                {
                    equippedWeapon = nextWeapon;
                    ApplyHeroStatsAndVisual();
                }
            }
        }
        else if (rnd <= successRatio + keepRatio)
        {
            // [유지]: 변화 없음
            result = WeaponEnhanceResult.Keep;
        }
        else
        {
            // [실패]: 파괴 방지권 사용 여부에 따른 분기
            if (useProtectionItem && protectionItemCount > 0)
            {
                protectionItemCount--;
                result = WeaponEnhanceResult.ProtectedFailure; // 파괴 방지 보존
            }
            else
            {
                result = WeaponEnhanceResult.DestroyedFailure; // 파괴 소멸
                ownedWeapons.Remove(weapon);

                if (equippedWeapon == weapon)
                {
                    equippedWeapon = null;
                    ApplyHeroStatsAndVisual();
                }
            }
        }

        OnInventoryUpdated?.Invoke();
        return result;
    }

    /// <summary>
    /// 보석을 대장간에서 강화 처리합니다. (성공 / 유지 / 파괴)
    /// </summary>
    public GemEnhanceResult EnhanceGem(GemItemData gem)
    {
        if (gem == null) return GemEnhanceResult.Keep;

        float rnd = UnityEngine.Random.value;
        float successRatio = Mathf.Clamp01(gem.upgradeSuccessRate);
        float keepRatio = Mathf.Clamp01(gem.upgradeKeepRate);

        GemEnhanceResult result;

        if (rnd <= successRatio)
        {
            result = GemEnhanceResult.Success;
            if (gem.nextLevelGem != null)
            {
                int idx = ownedGems.IndexOf(gem);
                if (idx >= 0)
                {
                    ownedGems[idx] = gem.nextLevelGem;
                }
                else
                {
                    ownedGems.Add(gem.nextLevelGem);
                }
            }
        }
        else if (rnd <= successRatio + keepRatio)
        {
            result = GemEnhanceResult.Keep;
        }
        else
        {
            result = GemEnhanceResult.Destroyed;
            ownedGems.Remove(gem);
        }

        OnInventoryUpdated?.Invoke();
        return result;
    }

    /// <summary>
    /// 보석을 지정된 판매가 골드로 판매하고 인벤토리에서 제거합니다.
    /// </summary>
    public bool SellGem(GemItemData gem)
    {
        if (gem == null || !ownedGems.Contains(gem)) return false;

        int earnGold = Mathf.Max(0, gem.sellPrice);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(earnGold);
        }

        ownedGems.Remove(gem);
        OnInventoryUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 현재 장착 중인 무기의 기본 옵션(공격력) 및 추가 옵션(공격력%, 타겟수, 공속, 생명력%, 이동속도) 수치를 합산 연산합니다.
    /// </summary>
    public CalculatedWeaponStats GetCalculatedHeroBonusStats()
    {
        CalculatedWeaponStats stats = new CalculatedWeaponStats();

        if (equippedWeapon == null) return stats;

        // 1. 기본 옵션: 공격력
        stats.bonusBaseATK += equippedWeapon.baseATK;

        // 2. 추가 옵션 리스트 합산 연산
        if (equippedWeapon.additionalOptions != null)
        {
            foreach (var opt in equippedWeapon.additionalOptions)
            {
                switch (opt.optionType)
                {
                    case WeaponOptionType.ATKPercent:
                        stats.bonusATKPercent += opt.value;
                        break;
                    case WeaponOptionType.TargetCount:
                        stats.bonusTargetCount += Mathf.RoundToInt(opt.value);
                        break;
                    case WeaponOptionType.AttackSpeed:
                        stats.bonusAttackSpeed += opt.value;
                        break;
                    case WeaponOptionType.HPPercent:
                        stats.bonusHPPercent += opt.value;
                        break;
                    case WeaponOptionType.MoveSpeed:
                        stats.bonusMoveSpeed += opt.value;
                        break;
                }
            }
        }

        return stats;
    }

    /// <summary>
    /// 히어로 고블린의 전투 유닛 스탯 및 weapon_visual 무기 외형을 전장에 갱신 적용합니다.
    /// </summary>
    public void ApplyHeroStatsAndVisual()
    {
        var combatUnits = UnityEngine.Object.FindObjectsByType<BaseCombatUnit>(FindObjectsSortMode.None);
        foreach (var unit in combatUnits)
        {
            if (unit != null && !unit.isEnemy)
            {
                unit.RefreshWeaponStatsAndVisual();
            }
        }
    }
}
