using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// CSV 다국어 번역 테이블을 로드하고 게임 내 텍스트 번역 요청을 처리하는 싱글톤 매니저 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("언어 설정")]
    [Tooltip("현재 설정된 언어 코드입니다. (예: KO, EN, JA)")]
    public string currentLanguage = "KO";

    [Tooltip("사용할 CSV 번역 리소스 파일 이름입니다 (확장자 제외, Resources 폴더 산하)")]
    [SerializeField] private string csvFileName = "LocalizationTable";

    // 파싱 완료된 번역 데이터를 캐싱해둘 딕셔너리
    private Dictionary<string, string> localizedDictionary = new Dictionary<string, string>();

    // 언어가 변경되었을 때 등록된 UI 텍스트들을 일괄 리프레시하기 위한 전역 이벤트
    public static event Action OnLanguageChanged;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalizationTable();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 현재 언어 코드를 변경하고 등록된 UI 텍스트들을 일괄 갱신합니다.
    /// </summary>
    public void ChangeLanguage(string langCode)
    {
        if (currentLanguage.Equals(langCode, StringComparison.OrdinalIgnoreCase)) return;

        currentLanguage = langCode.ToUpper();
        LoadLocalizationTable();

        // 언어 변경 이벤트 통지
        OnLanguageChanged?.Invoke();
        Debug.Log($"[LocalizationManager] 게임 언어가 변경되었습니다: <color=yellow><b>{currentLanguage}</b></color>");
    }

    /// <summary>
    /// 외부에서 번역 요청을 할 때 호출하는 핵심 메서드입니다.
    /// </summary>
    /// <param name="key">번역 키값 (예: ui_start_btn)</param>
    /// <returns>번역된 텍스트. 키가 없으면 [Key] 형태로 에러 없이 안전하게 자체 반환</returns>
    public string GetLocalizedString(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        if (localizedDictionary != null && localizedDictionary.TryGetValue(key, out string translatedText))
        {
            return translatedText;
        }

        // 키가 번역 테이블에 없는 경우 기획자가 식별하기 쉽도록 대괄호를 감싸서 키 자체를 리턴
        return $"[{key}]";
    }

    /// <summary>
    /// Resources 폴더로부터 CSV 파일을 동적으로 로드하고 파싱하여 메모리에 적재합니다.
    /// </summary>
    private void LoadLocalizationTable()
    {
        localizedDictionary.Clear();

        // 1. Resources 로드 시도
        TextAsset csvAsset = Resources.Load<TextAsset>(csvFileName);
        if (csvAsset == null)
        {
            Debug.LogError($"[LocalizationManager] Resources 폴더에서 '{csvFileName}' CSV 파일을 찾을 수 없습니다! 경로를 확인해 주세요.");
            return;
        }

        // 2. 텍스트 라인 분할 (OS별 개행 규칙 완벽 호환)
        string[] lines = csvAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0)
        {
            Debug.LogError($"[LocalizationManager] '{csvFileName}' 번역 파일이 비어 있습니다.");
            return;
        }

        // 3. 헤더 파싱 (첫 번째 줄에서 언어 인덱스 맵핑)
        string[] headers = ParseCSVLine(lines[0]);
        int keyIndex = -1;
        int targetLangIndex = -1;

        for (int i = 0; i < headers.Length; i++)
        {
            string colName = headers[i].Trim();
            if (colName.Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                keyIndex = i;
            }
            else if (colName.Equals(currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                targetLangIndex = i;
            }
        }

        // 예외 검증
        if (keyIndex == -1)
        {
            Debug.LogError("[LocalizationManager] CSV 헤더에 'Key' 컬럼이 존재하지 않습니다!");
            return;
        }

        if (targetLangIndex == -1)
        {
            Debug.LogWarning($"[LocalizationManager] 헤더에서 현재 설정 언어 '{currentLanguage}' 컬럼을 찾을 수 없습니다. 기본값으로 첫 번째 번역 컬럼을 탐색합니다.");
            // Key를 제외하고 가장 처음에 정의된 언어 컬럼으로 자동 대체 처리
            for (int i = 0; i < headers.Length; i++)
            {
                if (i != keyIndex && !string.IsNullOrEmpty(headers[i]))
                {
                    targetLangIndex = i;
                    break;
                }
            }

            // 대체 언어조차 찾을 수 없는 경우
            if (targetLangIndex == -1)
            {
                Debug.LogError("[LocalizationManager] 유효한 번역 언어 컬럼이 CSV 헤더에 없습니다.");
                return;
            }
        }

        // 4. 레코드 행 파싱 진행 (두 번째 줄부터 끝까지)
        int parsedCount = 0;
        for (int row = 1; row < lines.Length; row++)
        {
            string line = lines[row];
            if (string.IsNullOrWhiteSpace(line)) continue; // 빈 줄 스킵

            string[] fields = ParseCSVLine(line);

            // 유효성 체크
            if (fields.Length <= keyIndex || fields.Length <= targetLangIndex) continue;

            string key = fields[keyIndex].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            string localizedValue = fields[targetLangIndex];

            // 딕셔너리 중복 방지 및 적재
            if (!localizedDictionary.ContainsKey(key))
            {
                localizedDictionary.Add(key, localizedValue);
                parsedCount++;
            }
            else
            {
                Debug.LogWarning($"[LocalizationManager] 중복된 번역 키가 발견되어 첫 번째 키를 우선하여 보존합니다: {key}");
            }
        }

        Debug.Log($"[LocalizationManager] 번역 테이블 로드 완료: <color=yellow><b>{currentLanguage}</b></color> (총 {parsedCount}개의 다국어 키 적재 완료)");
    }

    /// <summary>
    /// RFC 4180 기준에 부합하여, 큰따옴표 내부에 쉼표(,)나 따옴표가 삽입된 형태도 안전하게 추출하는 고성능 CSV 라인 파서입니다.
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        // 쉼표로 분할하되, 큰따옴표로 감싸진 영역 안의 쉼표는 스킵하는 정규식 수식입니다.
        string splitPattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
        string[] fields = Regex.Split(line, splitPattern);

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].Trim();
            
            // 필드의 앞뒤 큰따옴표(")가 있다면 탈착 처리
            if (fields[i].StartsWith("\"") && fields[i].EndsWith("\""))
            {
                if (fields[i].Length >= 2)
                {
                    fields[i] = fields[i].Substring(1, fields[i].Length - 2);
                }
            }
            
            // CSV 표준 포맷상 쌍따옴표 두 개("")로 표현된 데이터는 하나의 단일 따옴표(")로 치환
            fields[i] = fields[i].Replace("\"\"", "\"");
            
            // CSV 이스케이프 문자(예: \n)를 실제 줄바꿈 문자로 변환 처리하여 줄바꿈 기획 지원
            fields[i] = fields[i].Replace("\\n", "\n");
        }

        return fields;
    }

    // -----------------------------------------------------------------------------------
    // 에디터 테스트 편의용 ContextMenu 디버그 기능군
    // -----------------------------------------------------------------------------------
    [ContextMenu("Language/Switch To KO")]
    private void SwitchToKO()
    {
        ChangeLanguage("KO");
    }

    [ContextMenu("Language/Switch To EN")]
    private void SwitchToEN()
    {
        ChangeLanguage("EN");
    }
}
