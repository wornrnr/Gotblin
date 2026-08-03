using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 테스트 및 기능 검증을 위한 개발용 치트 툴 패널 컨트롤러입니다.
/// 모든 장비 획득, 모든 보석 획득, 골드 획득 치트 기능을 제공하며 UI 토글 및 단축키(F12)를 지원합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_CheatPanel : MonoBehaviour
{
    private static UI_CheatPanel _instance;

    public static UI_CheatPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<UI_CheatPanel>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    CreateCheatPanelCanvas();
                }
            }
            return _instance;
        }
    }

    [Header("UI 연결 참조")]
    [SerializeField] private GameObject cheatContentPanel;
    [SerializeField] private Button toggleBtn;
    [SerializeField] private Button addWeaponsBtn;
    [SerializeField] private Button addGemsBtn;
    [SerializeField] private Button addGoldBtn;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        SetupButtonEvents();
    }

    private void Start()
    {
        SetupButtonEvents();
    }

    private void Update()
    {
        // F12 키 입력 시 치트 패널 토글
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleCheatPanel();
        }
    }

    private void SetupButtonEvents()
    {
        if (toggleBtn != null)
        {
            toggleBtn.onClick.RemoveAllListeners();
            toggleBtn.onClick.AddListener(ToggleCheatPanel);
        }

        if (addWeaponsBtn != null)
        {
            addWeaponsBtn.onClick.RemoveAllListeners();
            addWeaponsBtn.onClick.AddListener(OnAddAllWeaponsClicked);
        }

        if (addGemsBtn != null)
        {
            addGemsBtn.onClick.RemoveAllListeners();
            addGemsBtn.onClick.AddListener(OnAddAllGemsClicked);
        }

        if (addGoldBtn != null)
        {
            addGoldBtn.onClick.RemoveAllListeners();
            addGoldBtn.onClick.AddListener(OnAddGoldClicked);
        }
    }

    public void ToggleCheatPanel()
    {
        if (cheatContentPanel != null)
        {
            bool nextState = !cheatContentPanel.activeSelf;
            cheatContentPanel.SetActive(nextState);
        }
    }

    public void OnAddAllWeaponsClicked()
    {
        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllWeaponsCheat();
        }
        else
        {
            Debug.LogWarning("[CheatTool] BlacksmithManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    public void OnAddAllGemsClicked()
    {
        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllGemsCheat();
        }
        else
        {
            Debug.LogWarning("[CheatTool] BlacksmithManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    public void OnAddGoldClicked()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(100000);
            UI_ToastPopup.Show("+100,000 Gold 획득!");
        }
    }

    /// <summary>
    /// 씬 상에 UI_CheatPanel이 없을 경우 런타임에 동적으로 UI 캔버스 패널을 생성합니다.
    /// </summary>
    private static void CreateCheatPanelCanvas()
    {
        Canvas targetCanvas = null;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("CheatCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Root Object
        GameObject rootGO = new GameObject("UI_CheatPanel");
        rootGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRect = rootGO.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(20f, -20f);
        rootRect.sizeDelta = new Vector2(240f, 260f);

        // Toggle Button (⚡ CHEAT TOOL)
        GameObject toggleGO = new GameObject("ToggleBtn");
        toggleGO.transform.SetParent(rootGO.transform, false);

        RectTransform toggleRect = toggleGO.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(160f, 40f);

        Image toggleImg = toggleGO.AddComponent<Image>();
        toggleImg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        Button toggleBtnComp = toggleGO.AddComponent<Button>();

        GameObject toggleTxtGO = new GameObject("Text");
        toggleTxtGO.transform.SetParent(toggleGO.transform, false);
        RectTransform tRect = toggleTxtGO.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI toggleTxt = toggleTxtGO.AddComponent<TextMeshProUGUI>();
        toggleTxt.alignment = TextAlignmentOptions.Center;
        toggleTxt.fontSize = 18;
        toggleTxt.text = "⚡ CHEAT (F12)";
        toggleTxt.color = Color.yellow;

        // Content Panel
        GameObject contentGO = new GameObject("ContentPanel");
        contentGO.transform.SetParent(rootGO.transform, false);

        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -45f);
        contentRect.sizeDelta = new Vector2(240f, 200f);

        Image contentImg = contentGO.AddComponent<Image>();
        contentImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Button 1: Add All Weapons
        Button bWeapons = CreateCheatButton(contentGO.transform, "⚔️ 모든 장비 획득", new Color(0.3f, 0.5f, 0.8f));
        // Button 2: Add All Gems
        Button bGems = CreateCheatButton(contentGO.transform, "💎 모든 보석 획득", new Color(0.8f, 0.3f, 0.6f));
        // Button 3: Add Gold
        Button bGold = CreateCheatButton(contentGO.transform, "💰 +100,000 Gold", new Color(0.8f, 0.6f, 0.2f));

        _instance = rootGO.AddComponent<UI_CheatPanel>();
        _instance.cheatContentPanel = contentGO;
        _instance.toggleBtn = toggleBtnComp;
        _instance.addWeaponsBtn = bWeapons;
        _instance.addGemsBtn = bGems;
        _instance.addGoldBtn = bGold;

        contentGO.SetActive(false); // 기본 닫힘 상태
    }

    private static Button CreateCheatButton(Transform parent, string labelText, Color bgColor)
    {
        GameObject bGO = new GameObject("CheatBtn_" + labelText);
        bGO.transform.SetParent(parent, false);

        Image img = bGO.AddComponent<Image>();
        img.color = bgColor;
        Button btn = bGO.AddComponent<Button>();

        GameObject tGO = new GameObject("Text");
        tGO.transform.SetParent(bGO.transform, false);
        RectTransform tr = tGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 16;
        txt.text = labelText;
        txt.color = Color.white;

        return btn;
    }
}
