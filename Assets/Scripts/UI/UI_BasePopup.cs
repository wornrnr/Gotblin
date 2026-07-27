using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로젝트의 모든 팝업 UI의 공통 ID, 열기/닫기 동작, 닫기 버튼,
/// 그리고 배경 딤드(Dimmed) 이미지 및 딤드 터치 닫기 이벤트를 전역 처리하는 베이스 클래스입니다.
/// </summary>
public abstract class UI_BasePopup : MonoBehaviour
{
    [Header("팝업 공통 설정")]
    [Tooltip("이 팝업이 연결될 건물의 고유 식별 ID입니다. (예: Blacksmith, HQ, TownHall, Barracks)")]
    public string popupID;

    [Tooltip("팝업 창을 닫을 X 또는 닫기 버튼 컴포넌트입니다.")]
    [SerializeField] protected Button closeButton;

    [Header("팝업 배경 딤드 오버레이 설정")]
    [Tooltip("팝업 뒤 화면 전체를 덮는 반투명 딤드 배경 버튼입니다. 터치/클릭 시 팝업이 닫힙니다.")]
    [SerializeField] protected Button dimmedBackgroundButton;

    protected virtual void Awake()
    {
        EnsurePopupID();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }

        EnsureDimmedBackground();
    }

    protected virtual void OnValidate()
    {
        EnsurePopupID();
    }

    /// <summary>
    /// 팝업 뒤 딤드(Dimmed) 배경을 검사 및 자동 생성하고 클릭 닫기 이벤트를 연결합니다.
    /// </summary>
    protected virtual void EnsureDimmedBackground()
    {
        if (dimmedBackgroundButton == null)
        {
            Transform dimmedTr = transform.Find("DimmedBackground");
            if (dimmedTr != null)
            {
                dimmedBackgroundButton = dimmedTr.GetComponent<Button>();
            }

            // 딤드 배경이 없으면 동적으로 자동 생성하여 최하위 레이어에 배치
            if (dimmedBackgroundButton == null)
            {
                GameObject dimmedObj = new GameObject("DimmedBackground", typeof(RectTransform), typeof(Image), typeof(Button));
                dimmedObj.transform.SetParent(transform, false);
                dimmedObj.transform.SetAsFirstSibling();

                RectTransform rt = dimmedObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = new Vector2(4000, 4000); // 캔버스 화면 전역 커버

                Image img = dimmedObj.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.65f); // 65% 반투명 검은색 딤드
                img.raycastTarget = true;

                dimmedBackgroundButton = dimmedObj.GetComponent<Button>();
            }
        }

        if (dimmedBackgroundButton != null)
        {
            dimmedBackgroundButton.onClick.RemoveAllListeners();
            dimmedBackgroundButton.onClick.AddListener(ClosePopup);
        }
    }

    /// <summary>
    /// popupID가 비어있을 경우 클래스 이름(UI_BlacksmithPanel -> Blacksmith)에서 고유 ID를 자동 도출합니다.
    /// </summary>
    public string EnsurePopupID()
    {
        if (string.IsNullOrEmpty(popupID))
        {
            string className = GetType().Name;
            if (className.StartsWith("UI_")) className = className.Substring(3);
            if (className.EndsWith("Panel")) className = className.Substring(0, className.Length - 5);
            if (className.EndsWith("Popup")) className = className.Substring(0, className.Length - 5);
            popupID = className;
        }
        return popupID;
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
