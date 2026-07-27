using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로젝트의 모든 건물이 가진 팝업 UI (대장간 팝업, 고블린 본부 팝업, 훈련소 팝업 등)의
/// 공통 식별 ID, 열기/닫기 동작, 닫기 버튼 이벤트를 정의하는 추상 베이스 클래스입니다.
/// </summary>
public abstract class UI_BasePopup : MonoBehaviour
{
    [Header("팝업 공통 설정")]
    [Tooltip("이 팝업이 연결될 건물의 고유 식별 ID입니다. (예: Blacksmith, HQ, TownHall, Barracks)")]
    public string popupID;

    [Tooltip("팝업 창을 닫을 X 또는 닫기 버튼 컴포넌트입니다.")]
    [SerializeField] protected Button closeButton;

    protected virtual void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    /// <summary>
    /// 팝업 창을 엽니다.
    /// </summary>
    public virtual void OpenPopup()
    {
        gameObject.SetActive(true);
        RefreshAllUI();
    }

    /// <summary>
    /// 팝업 창을 닫습니다.
    /// </summary>
    public virtual void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 팝업 내부의 최신 정보 및 데이터 수치를 갱신합니다.
    /// </summary>
    public abstract void RefreshAllUI();
}
