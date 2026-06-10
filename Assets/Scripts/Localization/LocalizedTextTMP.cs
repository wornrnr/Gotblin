using UnityEngine;
using TMPro;

/// <summary>
/// TextMeshProUGUI 컴포넌트가 부착된 게임 오브젝트에 추가되어, 지정된 키값에 맞춰 다국어 번역을 자동 반영해 주는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[DisallowMultipleComponent]
public class LocalizedTextTMP : MonoBehaviour
{
    [Header("다국어 키 설정")]
    [Tooltip("LocalizationTable CSV 파일에 기재된 고유 Key 이름입니다. (예: ui_start_btn)")]
    [SerializeField] private string localizationKey;

    private TextMeshProUGUI textMeshPro;

    /// <summary>
    /// 외부에서 키값을 변경하고 즉각 번역 갱신을 트리거할 수 있는 속성입니다.
    /// </summary>
    public string LocalizationKey
    {
        get => localizationKey;
        set
        {
            localizationKey = value;
            ApplyLocalization();
        }
    }

    private void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // 1. 활성화 시점에 번역 즉시 적용
        ApplyLocalization();

        // 2. 언어 변경 전역 이벤트 구독 (언어 전환 시 실시간 동기화)
        LocalizationManager.OnLanguageChanged += ApplyLocalization;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 해제 처리
        LocalizationManager.OnLanguageChanged -= ApplyLocalization;
    }

    private void Start()
    {
        // Start 시점에도 최종 적용 보장
        ApplyLocalization();
    }

    /// <summary>
    /// LocalizationManager로부터 현재 활성화된 언어의 번역 텍스트를 공급받아 출력합니다.
    /// </summary>
    [ContextMenu("Apply Localization Now")]
    public void ApplyLocalization()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        if (textMeshPro == null) return;

        if (string.IsNullOrEmpty(localizationKey))
        {
            // 키가 비어있는 상태일 경우 빈 문자열 반환 방어
            return;
        }

        if (LocalizationManager.Instance != null)
        {
            string translatedText = LocalizationManager.Instance.GetLocalizedString(localizationKey);
            textMeshPro.text = translatedText;
        }
        else
        {
            // 매니저가 씬에 없거나 빌드 순서 싱크 문제 대비용 안전 방어
            textMeshPro.text = $"[{localizationKey}]";
        }
    }
}
