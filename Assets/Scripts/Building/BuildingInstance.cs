/// <summary>
/// 유저가 소유한 건물의 실시간 런타임 상태를 기억하는 데이터 모델 클래스입니다.
/// 인스펙터 뷰 실시간 모니터링을 위해 직렬화가 가능하도록 설계되었습니다.
/// </summary>
[System.Serializable]
public class BuildingInstance
{
    public string buildingID;
    public int currentLevel;      // 0이면 미건설 상태, 1 이상이면 건설된 상태
    public bool isConstructing;   // 현재 건설 또는 업그레이드 중인지 여부
    public float remainingTime;   // 남은 건설 소요 시간 (초 단위)

    /// <summary>
    /// 새로운 건물 인스턴스를 지정된 ID로 초기화합니다.
    /// </summary>
    public BuildingInstance(string id)
    {
        buildingID = id;
        currentLevel = 0;
        isConstructing = false;
        remainingTime = 0f;
    }
}
