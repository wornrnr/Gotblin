using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소셜 그래프 미니게임의 정지 시간 후보군과 가중치를 설정하는 테이블 데이터 ScriptableObject입니다.
/// 기획자가 절대적 PPM에 얽매이지 않고 자유롭게 상대 가중치를 수정할 수 있도록 설계되었습니다.
/// </summary>
[CreateAssetMenu(fileName = "GraphGameTableData", menuName = "Gotblin/GraphGame/Table Data", order = 1)]
public class GraphGameTableData : ScriptableObject
{
    /// <summary>
    /// 정지 시간 후보군의 개별 행 데이터 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct GraphGameRow
    {
        [Tooltip("행을 구분하는 고유 인덱스입니다.")]
        public int index;

        [Tooltip("정지 시간 (초 단위, 예: 1.55, 12.0)")]
        public float stopTime;

        [Tooltip("상대 가중치 (정수 형태, 예: 1, 5, 10 등 자유롭게 입력)")]
        public int weight;
    }

    [Header("게임 정지 시간 후보군 설정")]
    [Tooltip("기획자가 에디터에서 설정하는 그래프 정지 시간 및 상대 가중치 목록입니다.")]
    [SerializeField] private List<GraphGameRow> dataRows = new List<GraphGameRow>();

    /// <summary>
    /// 설정된 모든 데이터 행의 읽기 전용 리스트를 제공합니다.
    /// </summary>
    public IReadOnlyList<GraphGameRow> DataRows => dataRows;

    /// <summary>
    /// 등록된 데이터 행의 개수를 반환합니다.
    /// </summary>
    public int Count => dataRows != null ? dataRows.Count : 0;

    /// <summary>
    /// 모든 항목의 상대 가중치(weight) 총합을 계산하여 반환합니다.
    /// </summary>
    /// <returns>상대 가중치 누적 총합</returns>
    public int GetTotalWeight()
    {
        if (dataRows == null) return 0;

        int total = 0;
        for (int i = 0; i < dataRows.Count; i++)
        {
            // 가중치 음수 입력 방어 처리
            total += Mathf.Max(0, dataRows[i].weight);
        }
        return total;
    }

    /// <summary>
    /// 기획 데이터 검증 및 에디터 경고 표시 기능
    /// </summary>
    private void OnValidate()
    {
        if (dataRows == null) return;

        int total = GetTotalWeight();
        if (total == 0 && dataRows.Count > 0)
        {
            Debug.LogError("[GraphGameTableData] 등록된 데이터 행이 있으나 가중치 총합이 0입니다! 가중치가 제대로 설정되었는지 확인해 주세요.");
        }

        for (int i = 0; i < dataRows.Count; i++)
        {
            if (dataRows[i].weight < 0)
            {
                Debug.LogWarning($"[GraphGameTableData] Index {dataRows[i].index}의 가중치가 음수({dataRows[i].weight})로 설정되었습니다. 계산 시에는 0으로 취급됩니다.");
            }
            if (dataRows[i].stopTime < 0)
            {
                Debug.LogWarning($"[GraphGameTableData] Index {dataRows[i].index}의 정지 시간이 음수({dataRows[i].stopTime})로 설정되었습니다. 0초 미만은 오동작을 유발할 수 있습니다.");
            }
        }
    }
}
