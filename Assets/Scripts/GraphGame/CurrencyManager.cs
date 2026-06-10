using System;
using UnityEngine;

/// <summary>
/// 게임 내의 전역 재화(Gold)를 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class CurrencyManager : MonoBehaviour
{
    private static CurrencyManager instance;
    public static CurrencyManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 유니티 6 권장 API인 FindFirstObjectByType을 사용하여 싱글톤 검색
                instance = FindFirstObjectByType<CurrencyManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CurrencyManager");
                    instance = go.AddComponent<CurrencyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("보유 재화 설정")]
    [Tooltip("기본 지급 골드 수량입니다.")]
    [SerializeField] private int gold = 1000;

    /// <summary>
    /// 현재 보유 골드 잔액입니다.
    /// </summary>
    public int Gold => gold;

    /// <summary>
    /// 골드가 변동될 때 UI나 타 시스템이 즉시 반영할 수 있도록 하는 이벤트입니다.
    /// </summary>
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 씬 시작 시 구독 중인 UI를 동기화하기 위한 이벤트 최초 1회 브로드캐스팅
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// 지정된 양의 골드를 획득(추가)합니다.
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        gold += amount;
        Debug.Log($"<color=#FFD700><b>[CurrencyManager] Gold 획득!</b></color> +{amount:N0} Gold (보유 Gold: {gold:N0})");
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// 지정된 양의 골드를 소모(차감)합니다. 소모 성공 여부를 반환합니다.
    /// </summary>
    public bool ConsumeGold(int amount)
    {
        if (amount <= 0) return false;

        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"<color=#FF7A7A><b>[CurrencyManager] Gold 소모!</b></color> -{amount:N0} Gold (보유 Gold: {gold:N0})");
            OnGoldChanged?.Invoke(gold);
            return true;
        }
        else
        {
            Debug.LogWarning($"[CurrencyManager] 골드가 부족합니다! (요구량: {amount:N0} / 보유량: {gold:N0})");
            return false;
        }
    }
}
