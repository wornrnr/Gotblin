using TMPro;
using UnityEngine;

/// <summary>
/// 전투 화면(CombatPanel) 중상단에 현재 스테이지(Stage n)와 
/// 더 큰 폰트의 챕터 이름(Chapter_Name_m)을 출력하고 관리하는 UI 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public class CombatStageHeaderUI : MonoBehaviour
{
    [Header("UI 텍스트 참조")]
    [Tooltip("상단에 표시할 대형 폰트 챕터 이름 텍스트입니다.")]
    public TextMeshProUGUI chapterText;

    [Tooltip("하단에 표시할 'Stage n' 텍스트입니다.")]
    public TextMeshProUGUI stageText;

    private int cachedStage = 1;
    private int cachedStagesPerChapter = 20;

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged()
    {
        RefreshUI();
    }

    /// <summary>
    /// 매개변수 없이 현재 캐시된 정보로 헤더 UI를 갱신합니다.
    /// </summary>
    public void RefreshUI()
    {
        if (CombatStageManager.Instance != null)
        {
            RefreshHeader(CombatStageManager.Instance.currentStage, CombatStageManager.Instance.stagesPerChapter);
        }
        else
        {
            RefreshHeader(cachedStage, cachedStagesPerChapter);
        }
    }

    /// <summary>
    /// 스테이지 번호와 챕터당 스테이지 간격 수치에 맞춰 UI 텍스트를 실시간 연산/갱신합니다.
    /// </summary>
    /// <param name="currentStage">현재 진행 중인 스테이지 번호 (1-indexed)</param>
    /// <param name="stagesPerChapter">챕터가 전환될 스테이지 단위 간격 (기본 20, 30/50 지원)</param>
    public void RefreshHeader(int currentStage, int stagesPerChapter)
    {
        cachedStage = Mathf.Max(1, currentStage);
        cachedStagesPerChapter = Mathf.Max(1, stagesPerChapter);

        // 1. Stage n 텍스트 적용
        if (stageText != null)
        {
            stageText.text = $"Stage {cachedStage}";
        }

        // 2. Chapter_Name_m 텍스트 연산 및 Localization 참조
        if (chapterText != null)
        {
            int targetIndex = (cachedStage - 1) / cachedStagesPerChapter; // 0-indexed 챕터 번호
            string targetKey = $"Chapter_Name_{targetIndex}";

            string chapterTitle = string.Empty;
            LocalizationManager locMgr = LocalizationManager.Instance != null ? LocalizationManager.Instance : Object.FindFirstObjectByType<LocalizationManager>();

            if (locMgr != null)
            {
                if (locMgr.HasKey(targetKey))
                {
                    // 정확한 챕터 번역 키가 존재하는 경우
                    chapterTitle = locMgr.GetLocalizedString(targetKey);
                }
                else
                {
                    // [m-max Fallback]: m보다 큰 번호가 없는 경우 존재하는 가장 큰 m번째 키(Chapter_Name_m) 탐색
                    int maxM = -1;
                    int searchM = 0;
                    while (true)
                    {
                        string checkKey = $"Chapter_Name_{searchM}";
                        if (locMgr.HasKey(checkKey))
                        {
                            maxM = searchM;
                            searchM++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (maxM >= 0)
                    {
                        string maxKey = $"Chapter_Name_{maxM}";
                        chapterTitle = locMgr.GetLocalizedString(maxKey);
                    }
                    else
                    {
                        // 0번 키조차 없는 경우 기본 표기 반환
                        chapterTitle = $"Chapter {targetIndex + 1}";
                    }
                }
            }
            else
            {
                chapterTitle = $"Chapter {targetIndex + 1}";
            }

            chapterText.text = chapterTitle;
        }
    }
}
