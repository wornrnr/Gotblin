using UnityEngine;

/// <summary>
/// 씬에 정적으로 영구 저장 배치된 Building_Blacksmith 오브젝트와 대장간 팝업 UI 간의 
/// 데이터 바인딩을 보장하는 헬퍼 컴포넌트입니다.
/// 유저가 씬 에디터(Scene View)에서 직접 마우스로 배치하고 수정한 위치(RectTransform)를 100% 보존합니다.
/// </summary>
[DisallowMultipleComponent]
public class TownBuildingBlacksmithSetup : MonoBehaviour
{
    private void Start()
    {
        EnsureBlacksmithBuilding();
    }

    private void OnEnable()
    {
        EnsureBlacksmithBuilding();
    }

    /// <summary>
    /// 씬 상의 Building_Blacksmith 오브젝트 식별자 바인딩을 점검합니다.
    /// </summary>
    public void EnsureBlacksmithBuilding()
    {
        var worldObjs = Object.FindObjectsByType<UI_WorldBuildingObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var wo in worldObjs)
        {
            if (wo.gameObject.name == "Building_Blacksmith" || wo.buildingID == "Blacksmith")
            {
                wo.buildingID = "Blacksmith";

                var slotScript = wo.GetComponent<UI_BuildingSlot>();
                if (slotScript != null)
                {
                    slotScript.SetupSlot("Blacksmith");
                }
                break;
            }
        }
    }
}
