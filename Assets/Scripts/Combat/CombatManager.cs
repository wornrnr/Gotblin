using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자동 전투 구역 내의 아군(고블린) 및 적군 유닛 리스트를 상시 관리하고,
/// 최단 거리 타겟을 2D anchoredPosition 기준으로 정확히 추적해주는 전역 전투 관리자입니다.
/// </summary>
[DisallowMultipleComponent]
public class CombatManager : MonoBehaviour
{
    // 전역 전투 관리 싱글톤 인스턴스
    public static CombatManager Instance { get; private set; }

    [Header("전투 참가 유닛 리스트")]
    public List<BaseCombatUnit> playerUnits = new List<BaseCombatUnit>();
    public List<BaseCombatUnit> enemyUnits = new List<BaseCombatUnit>();

    private void Awake()
    {
        // [초기화 타이밍 개선]: Awake 단계에서 싱글톤 확보하여 각 유닛의 Start() 등록 널 예외 사전 예방
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
    /// 요청 유닛의 피아 식별 기준에 따른 최단 거리 생존 유닛 타겟을 반환합니다.
    /// 리스트 역순 순회로 널(Null) 및 사망 대상을 즉시 정리합니다.
    /// </summary>
    /// <param name="requestUnit">조회를 요청한 유닛 본인</param>
    public BaseCombatUnit GetClosestTarget(BaseCombatUnit requestUnit)
    {
        if (requestUnit == null) return null;

        // [피아식별 조건문 수정]: 요청 유닛이 적군(true)이면 아군 리스트를, 아군(false)이면 적군 리스트를 탐색
        List<BaseCombatUnit> targetList = requestUnit.isEnemy ? playerUnits : enemyUnits;

        if (targetList == null || targetList.Count == 0) return null;

        BaseCombatUnit closestTarget = null;
        float closestDistance = Mathf.Infinity;

        // [부모 계층 좌표 불일치 해결]: UGUI 계층 구조가 서로 다를 때 오차를 방지하기 위해 World Position 기준으로 거리 연산 수행
        Vector3 myPos = requestUnit.transform.position;

        // [리스트 무결성 정제]: 역순 순회하며 사망(HP <= 0)했거나 참조가 파괴(Null)된 요소를 실시간 제외
        for (int i = targetList.Count - 1; i >= 0; i--)
        {
            if (targetList[i] == null || targetList[i].currentHP <= 0)
            {
                targetList.RemoveAt(i);
                continue;
            }

            Vector3 targetPos = targetList[i].transform.position;
            float distance = Vector3.Distance(myPos, targetPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetList[i];
            }
        }

        return closestTarget;
    }

    /// <summary>
    /// 요청 유닛의 사거리 내/전장 기준 최단 거리 N명의 생존 유닛 타겟 리스트를 반환합니다.
    /// 기존 타겟 유지 및 다중 타겟(targetCount) 공격 시스템 지원용입니다.
    /// </summary>
    public List<BaseCombatUnit> GetClosestTargets(BaseCombatUnit requestUnit, int count)
    {
        List<BaseCombatUnit> result = new List<BaseCombatUnit>();
        if (requestUnit == null || count <= 0) return result;

        List<BaseCombatUnit> targetList = requestUnit.isEnemy ? playerUnits : enemyUnits;
        if (targetList == null || targetList.Count == 0) return result;

        Vector3 myPos = requestUnit.transform.position;

        // 사망 및 null 유닛 정제
        List<BaseCombatUnit> validTargets = new List<BaseCombatUnit>();
        for (int i = targetList.Count - 1; i >= 0; i--)
        {
            if (targetList[i] == null || targetList[i].currentHP <= 0)
            {
                targetList.RemoveAt(i);
                continue;
            }
            validTargets.Add(targetList[i]);
        }

        // 월드 거리 순 정렬 후 상위 count개 타겟 추출
        validTargets.Sort((a, b) => Vector3.Distance(myPos, a.transform.position).CompareTo(Vector3.Distance(myPos, b.transform.position)));

        int fetchCount = Mathf.Min(count, validTargets.Count);
        for (int i = 0; i < fetchCount; i++)
        {
            result.Add(validTargets[i]);
        }

        return result;
    }

    // -----------------------------------------------------------------------------------
    // 유닛 등록/해제 편의 메서드군
    // -----------------------------------------------------------------------------------
    public void RegisterUnit(BaseCombatUnit unit)
    {
        if (unit == null) return;

        if (unit.isEnemy)
        {
            if (!enemyUnits.Contains(unit)) enemyUnits.Add(unit);
        }
        else
        {
            if (!playerUnits.Contains(unit)) playerUnits.Add(unit);
        }
    }

    public void UnregisterUnit(BaseCombatUnit unit)
    {
        if (unit == null) return;

        if (unit.isEnemy)
        {
            enemyUnits.Remove(unit);
        }
        else
        {
            playerUnits.Remove(unit);
        }
    }
}
