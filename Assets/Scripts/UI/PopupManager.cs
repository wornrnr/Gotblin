using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내의 모든 팝업 UI들(대장간 팝업, 고블린 본부 팝업, 훈련소 팝업 등)을 총괄 등록 및 관리하는
/// 전역 팝업 메인 매니저 싱글톤 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("팝업 UI 레이어 참조")]
    [Tooltip("씬 캔버스 하위에 위치하는 팝업 전용 부모 레이어 Transform입니다.")]
    [SerializeField] private Transform popupLayerParent;

    // popupID -> UI_BasePopup 딕셔너리 레지스트리
    private Dictionary<string, UI_BasePopup> registeredPopups = new Dictionary<string, UI_BasePopup>();

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

        RegisterAllPopupsInScene();
    }

    private void Start()
    {
        RegisterAllPopupsInScene();
    }

    /// <summary>
    /// 씬 상의 모든 UI_BasePopup 컴포넌트들을 탐색하여 popupID 기준으로 레지스트리에 자동 등록합니다.
    /// </summary>
    public void RegisterAllPopupsInScene()
    {
        registeredPopups.Clear();
        UI_BasePopup[] popups = Object.FindObjectsByType<UI_BasePopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var popup in popups)
        {
            if (popup != null && !string.IsNullOrEmpty(popup.popupID))
            {
                registeredPopups[popup.popupID] = popup;

                // 2차 별칭 등록 (예: HQ <-> TownHall)
                if (popup.popupID == "HQ") registeredPopups["TownHall"] = popup;
                if (popup.popupID == "TownHall") registeredPopups["HQ"] = popup;

                Debug.Log($"<color=cyan>[PopupManager] 팝업 '{popup.popupID}' ({popup.gameObject.name}) 레지스트리 등록 완료.</color>");
            }
        }
    }

    /// <summary>
    /// 지정된 popupID(예: Blacksmith, HQ, TownHall)에 해당하는 팝업 UI를 활성화하고 리프레시합니다.
    /// </summary>
    public bool OpenPopup(string popupID)
    {
        if (string.IsNullOrEmpty(popupID)) return false;

        if (!registeredPopups.ContainsKey(popupID))
        {
            RegisterAllPopupsInScene();
        }

        if (registeredPopups.TryGetValue(popupID, out var targetPopup) && targetPopup != null)
        {
            // 다른 활성 팝업 닫기 (단일 모달 팝업 정책)
            CloseAllPopups();

            targetPopup.OpenPopup();
            Debug.Log($"<color=green>[PopupManager] 팝업 '{popupID}' 오픈 성공!</color>");
            return true;
        }

        Debug.LogWarning($"[PopupManager] '{popupID}' 식별자를 가진 팝업 UI를 찾을 수 없습니다.");
        return false;
    }

    /// <summary>
    /// 지정된 popupID 팝업을 비활성화합니다.
    /// </summary>
    public void ClosePopup(string popupID)
    {
        if (registeredPopups.TryGetValue(popupID, out var popup) && popup != null)
        {
            popup.ClosePopup();
        }
    }

    /// <summary>
    /// 현재 활성화된 모든 팝업 창을 비활성화 닫기 처리합니다.
    /// </summary>
    public void CloseAllPopups()
    {
        foreach (var kvp in registeredPopups)
        {
            if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
            {
                kvp.Value.ClosePopup();
            }
        }
    }

    /// <summary>
    /// 지정된 popupID 팝업이 등록되어 있는지 확인합니다.
    /// </summary>
    public bool HasPopup(string popupID)
    {
        if (string.IsNullOrEmpty(popupID)) return false;
        return registeredPopups.ContainsKey(popupID);
    }
}
