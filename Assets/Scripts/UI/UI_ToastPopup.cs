using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 중앙에 표시된 후 3초간 유지되다가 부드럽게 페이드 아웃되는 공용 토스트 팝업 메인 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_ToastPopup : MonoBehaviour
{
    private static UI_ToastPopup _instance;

    public static UI_ToastPopup Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<UI_ToastPopup>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    CreateToastPopupCanvas();
                }
            }
            return _instance;
        }
    }

    [Header("UI 구성 요소")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI toastText;
    [SerializeField] private Image backgroundImage;

    private Coroutine currentToastCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    /// <summary>
    /// 지정된 다국어 키(Notice_Max_Upgrade 등) 또는 직접 작성한 텍스트를 토스트 팝업으로 출력합니다.
    /// </summary>
    /// <param name="messageKeyOrText">다국어 키 또는 직접 표시할 메세지</param>
    /// <param name="duration">유지 시간 (기본값: 1.0초)</param>
    public static void Show(string messageKeyOrText, float duration = 1.0f)
    {
        if (Instance != null)
        {
            Instance.ShowToastMessage(messageKeyOrText, duration);
        }
    }

    public void ShowToastMessage(string messageKeyOrText, float duration = 1.0f)
    {
        string localizedString = messageKeyOrText;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasKey(messageKeyOrText))
        {
            localizedString = LocalizationManager.Instance.GetLocalizedString(messageKeyOrText);
        }

        if (toastText != null)
        {
            toastText.text = localizedString;
        }

        gameObject.SetActive(true);

        if (currentToastCoroutine != null)
        {
            StopCoroutine(currentToastCoroutine);
        }
        currentToastCoroutine = StartCoroutine(CoShowAndFadeOut(duration));
    }

    private IEnumerator CoShowAndFadeOut(float duration)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 3초간 유지
        yield return new WaitForSeconds(duration);

        // 0.5초 동안 부드럽게 페이드 아웃
        float fadeTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        currentToastCoroutine = null;
    }

    /// <summary>
    /// 씬 상에 UI_ToastPopup 개체가 존재하지 않는 경우 런타임에 동적으로 캔버스 및 팝업을 자동 생성합니다.
    /// </summary>
    private static void CreateToastPopupCanvas()
    {
        Canvas targetCanvas = null;

        // 씬 상의 기존 Canvas 탐색 (SortingOrder가 가장 높은 Canvas 선호)
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("ToastCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 9999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Toast Panel 생성 (화면 중앙 위치)
        GameObject toastGO = new GameObject("UI_ToastPopup");
        toastGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform rectTransform = toastGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero; // 화면 중앙
        rectTransform.sizeDelta = new Vector2(500f, 100f);

        Image bgImage = toastGO.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f); // 고급스러운 어두운 반투명 패널

        CanvasGroup cg = toastGO.AddComponent<CanvasGroup>();

        // TextMeshProUGUI 생성
        GameObject textGO = new GameObject("ToastText");
        textGO.transform.SetParent(toastGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-40f, -20f);
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 28;
        tmpText.color = Color.white;
        tmpText.enableAutoSizing = false;

        _instance = toastGO.AddComponent<UI_ToastPopup>();
        _instance.canvasGroup = cg;
        _instance.toastText = tmpText;
        _instance.backgroundImage = bgImage;

        toastGO.SetActive(false);
    }
}
