using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WeaponEnhanceResult
{
    Success,           // 강화 성공 (다음 단계로 진입)
    Keep,              // 강화 실패 (단계 유지)
    ProtectedFailure,  // 확률에 의해 파괴될 뻔 했으나 보호권으로 유지
    DestroyedFailure,  // 확률에 의해 파괴되어 소멸됨
    NotEnoughCurrency  // 재화 부족
}

public enum GemEnhanceResult
{
    Success,   // 강화 성공
    Keep,      // 강화 실패
    Destroyed, // 파괴되어 소멸됨
    NotEnoughCurrency // 재화 부족
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
    private static BlacksmithManager _instance;

    public static BlacksmithManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<BlacksmithManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BlacksmithManager");
                    _instance = go.AddComponent<BlacksmithManager>();
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitDefaultTestItems();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 테스트용: 게임 시작 시 기본적으로 Diamond_Lv1을 5개 소지하도록 초기화합니다.
    /// </summary>
    private void InitDefaultTestItems()
    {
        GemItemData diamondLv1 = Resources.Load<GemItemData>("Data/Gems/Diamond_Lv1");
        if (diamondLv1 != null)
        {
            for (int i = 0; i < 5; i++)
            {
                ownedGems.Add(diamondLv1);
            }
            SortGems();
            OnInventoryUpdated?.Invoke();
        }
        else
        {
            Debug.LogWarning("[BlacksmithManager] Resources/Data/Gems/Diamond_Lv1 에셋을 로드할 수 없습니다.");
        }
    }

    /// <summary>
    /// 보유 보석 리스트를 정렬 규칙에 맞춰 정렬합니다.
    /// (규칙: 1. 레벨 내림차순, 2. gemID 오름차순, 3. 기존 생성 순서 유지)
    /// </summary>
    public void SortGems()
    {
        if (ownedGems == null || ownedGems.Count <= 1) return;

        ownedGems = ownedGems
            .OrderByDescending(g => g != null ? g.level : -1)
            .ThenBy(g => g != null ? g.gemID : string.Empty)
            .ToList();
    }

    /// <summary>
    /// 보유 무기 리스트를 정렬 규칙에 맞춰 정렬합니다.
    /// (규칙: 1. 등급 내림차순, 2. weaponID 오름차순, 3. 기존 생성 순서 유지)
    /// </summary>
    public void SortWeapons()
    {
        if (ownedWeapons == null || ownedWeapons.Count <= 1) return;

        ownedWeapons = ownedWeapons
            .OrderByDescending(w => w != null ? w.grade : -1)
            .ThenBy(w => w != null ? w.weaponID : string.Empty)
            .ToList();
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
            SortWeapons();
        }

        equippedWeapon = weapon;
        ApplyHeroStatsAndVisual();

        OnEquippedWeaponChanged?.Invoke();
        OnInventoryUpdated?.Invoke();
        return true;
    }

    public WeaponEnhanceResult EnhanceWeapon(WeaponItemData weapon, bool useProtectionItem)
    {
        int idx = ownedWeapons.IndexOf(weapon);
        if (idx >= 0) return EnhanceWeaponAtIndex(idx, useProtectionItem);
        return WeaponEnhanceResult.Keep;
    }

    public WeaponEnhanceResult EnhanceWeaponAtIndex(int index, bool useProtectionItem)
    {
        if (index < 0 || index >= ownedWeapons.Count) return WeaponEnhanceResult.Keep;
        WeaponItemData weapon = ownedWeapons[index];
        if (weapon == null) return WeaponEnhanceResult.Keep;

        if (CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.ConsumeGold(weapon.enhanceCost))
            {
                return WeaponEnhanceResult.NotEnoughCurrency;
            }
        }

        if (ironIngotCount < weapon.requiredIronIngot)
        {
            Debug.LogWarning($"[BlacksmithManager] 철 주괴가 부족합니다! (필요: {weapon.requiredIronIngot}, 현재: {ironIngotCount})");
            return WeaponEnhanceResult.NotEnoughCurrency;
        }

        ironIngotCount -= weapon.requiredIronIngot;

        int totalWeight = weapon.TotalWeight;
        int rnd = UnityEngine.Random.Range(0, totalWeight);

        WeaponEnhanceResult result;

        if (rnd < weapon.successWeight)
        {
            result = WeaponEnhanceResult.Success;
            if (weapon.nextGradeWeapon != null)
            {
                WeaponItemData nextWeapon = weapon.nextGradeWeapon;
                ownedWeapons[index] = nextWeapon;

                if (equippedWeapon == weapon)
                {
                    equippedWeapon = nextWeapon;
                    ApplyHeroStatsAndVisual();
                }
            }
        }
        else if (rnd < weapon.successWeight + weapon.keepWeight)
        {
            result = WeaponEnhanceResult.Keep;
        }
        else
        {
            if (useProtectionItem && protectionItemCount > 0)
            {
                protectionItemCount--;
                result = WeaponEnhanceResult.ProtectedFailure;
            }
            else
            {
                result = WeaponEnhanceResult.DestroyedFailure;
                ownedWeapons.RemoveAt(index);

                if (equippedWeapon == weapon)
                {
                    equippedWeapon = null;
                    ApplyHeroStatsAndVisual();
                }
            }
        }

        SortWeapons();
        OnInventoryUpdated?.Invoke();
        return result;
    }

    public GemEnhanceResult EnhanceGem(GemItemData gem)
    {
        int idx = ownedGems.IndexOf(gem);
        if (idx >= 0) return EnhanceGemAtIndex(idx);
        return GemEnhanceResult.Keep;
    }

    public GemEnhanceResult EnhanceGemAtIndex(int index)
    {
        if (index < 0 || index >= ownedGems.Count) return GemEnhanceResult.Keep;
        GemItemData gem = ownedGems[index];
        if (gem == null) return GemEnhanceResult.Keep;

        if (CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.ConsumeGold(gem.enhanceCost))
            {
                return GemEnhanceResult.NotEnoughCurrency;
            }
        }

        int totalWeight = gem.TotalWeight;
        int rnd = UnityEngine.Random.Range(0, totalWeight);

        GemEnhanceResult result;

        if (rnd < gem.successWeight)
        {
            result = GemEnhanceResult.Success;
            if (gem.nextLevelGem != null)
            {
                ownedGems[index] = gem.nextLevelGem;
            }
        }
        else if (rnd < gem.successWeight + gem.keepWeight)
        {
            result = GemEnhanceResult.Keep;
        }
        else
        {
            result = GemEnhanceResult.Destroyed;
            ownedGems.RemoveAt(index);
        }

        SortGems();
        OnInventoryUpdated?.Invoke();
        return result;
    }

    public bool SellWeapon(WeaponItemData weapon)
    {
        int idx = ownedWeapons.IndexOf(weapon);
        if (idx >= 0) return SellWeaponAtIndex(idx);
        return false;
    }

    public bool SellWeaponAtIndex(int index)
    {
        if (index < 0 || index >= ownedWeapons.Count) return false;
        WeaponItemData weapon = ownedWeapons[index];
        if (weapon == null) return false;

        int earnGold = Mathf.Max(0, weapon.sellPrice);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(earnGold);
        }

        if (equippedWeapon == weapon)
        {
            equippedWeapon = null;
            ApplyHeroStatsAndVisual();
            OnEquippedWeaponChanged?.Invoke();
        }

        ownedWeapons.RemoveAt(index);
        SortWeapons();
        OnInventoryUpdated?.Invoke();
        return true;
    }

    public bool SellGem(GemItemData gem)
    {
        int idx = ownedGems.IndexOf(gem);
        if (idx >= 0) return SellGemAtIndex(idx);
        return false;
    }

    public bool SellGemAtIndex(int index)
    {
        if (index < 0 || index >= ownedGems.Count) return false;
        GemItemData gem = ownedGems[index];
        if (gem == null) return false;

        int earnGold = Mathf.Max(0, gem.sellPrice);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(earnGold);
        }

        ownedGems.RemoveAt(index);
        SortGems();
        OnInventoryUpdated?.Invoke();
        return true;
    }

    #region Cheat Functions

    /// <summary>
    /// [치트] Resources/Data/Weapons 경로의 모든 무기 아이템을 1개씩 보유 목록에 추가합니다.
    /// </summary>
    public void AddAllWeaponsCheat()
    {
        WeaponItemData[] weapons = Resources.LoadAll<WeaponItemData>("Data/Weapons");
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("[BlacksmithManager] Data/Weapons 경로에서 무기 데이터를 찾을 수 없습니다.");
            UI_ToastPopup.Show("무기 데이터를 찾지 못했습니다.");
            return;
        }

        foreach (var w in weapons)
        {
            if (w != null)
            {
                ownedWeapons.Add(w);
            }
        }

        SortWeapons();
        OnInventoryUpdated?.Invoke();
        UI_ToastPopup.Show($"모든 장비 획득 완료! ({weapons.Length}종)");
        Debug.Log($"<color=cyan>[Cheat] 모든 장비 {weapons.Length}종을 1개씩 획득했습니다.</color>");
    }

    /// <summary>
    /// [치트] Resources/Data/Gems 경로의 모든 보석 아이템을 1개씩 보유 목록에 추가합니다.
    /// </summary>
    public void AddAllGemsCheat()
    {
        GemItemData[] gems = Resources.LoadAll<GemItemData>("Data/Gems");
        if (gems == null || gems.Length == 0)
        {
            Debug.LogWarning("[BlacksmithManager] Data/Gems 경로에서 보석 데이터를 찾을 수 없습니다.");
            UI_ToastPopup.Show("보석 데이터를 찾지 못했습니다.");
            return;
        }

        foreach (var g in gems)
        {
            if (g != null)
            {
                ownedGems.Add(g);
            }
        }

        SortGems();
        OnInventoryUpdated?.Invoke();
        UI_ToastPopup.Show($"모든 보석 획득 완료! ({gems.Length}종)");
        Debug.Log($"<color=cyan>[Cheat] 모든 보석 {gems.Length}종을 1개씩 획득했습니다.</color>");
    }

    #endregion

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
