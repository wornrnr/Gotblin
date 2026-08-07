using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Figma 대장간 UI (UI_Blacksmith) 와이어프레임 시안 기반의 개편된 대장간 메인 UI 컨트롤러입니다.
/// 상단 Split 2컬럼 패널(비주얼 & 선택 항목/3색 확률 인디케이터), 중앙 5x3 인벤토리 그리드,
/// 하단 Sell & Forge 액션 버튼(비용 뱃지 포함) 및 최하단 탭바를 지원합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithPanel : UI_BasePopup
{
    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(popupID)) popupID = "Blacksmith";
    }

    [Header("해금 및 가림막 UI")]
    [SerializeField] private GameObject lockedOverlayPanel;
    [SerializeField] private TextMeshProUGUI lockNoticeText;

    [Header("Figma Header & Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Upper Split Section (선택 항목 정보 및 3색 확률)")]
    [SerializeField] private RectTransform leftVisualPanel;
    [SerializeField] private Image equippedWeaponIcon;
    [SerializeField] private TextMeshProUGUI equippedWeaponNameText;
    [SerializeField] private TextMeshProUGUI weaponGradeLevelText; // 예: +7

    [Header("3-Color Rate Indicators (🟢, 🟡, 🔴)")]
    [Tooltip("성공 확률 텍스트 (🟢 초록)")]
    [SerializeField] private TextMeshProUGUI successRateText;

    [Tooltip("유지 확률 텍스트 (🟡 노랑)")]
    [SerializeField] private TextMeshProUGUI keepRateText;

    [Tooltip("파괴 확률 텍스트 (🔴 빨강)")]
    [SerializeField] private TextMeshProUGUI destroyRateText;

    [Header("Middle Inventory Grid Section (5x3 Grid)")]
    [SerializeField] private Transform inventoryGridContainer;
    [SerializeField] private UI_BlacksmithSlot slotPrefab;
    [SerializeField] private TextMeshProUGUI emptyNoticeText;

    [Header("Lower Action Buttons (Sell, Forge & Equip)")]
    [SerializeField] private Button sellBtn;
    [SerializeField] private TextMeshProUGUI sellBtnLabelText;
    [SerializeField] private TextMeshProUGUI sellGoldText;
    [SerializeField] private Button forgeBtn;
    [SerializeField] private TextMeshProUGUI forgeBtnLabelText;
    [SerializeField] private TextMeshProUGUI forgeGoldText;
    [SerializeField] private Button equipBtn;
    [SerializeField] private TextMeshProUGUI equipBtnLabelText;

    [Header("Bottom Navigation Tabs (Tab 1: Weapon / Tab 2: Gem)")]
    [SerializeField] private Button tab1Btn; // 무기 탭
    [SerializeField] private Button tab2Btn; // 보석 탭
    [SerializeField] private Image tab1Highlight;
    [SerializeField] private Image tab2Highlight;

    [Header("기타 파티클 및 옵션")]
    [SerializeField] private UI_BlacksmithEmberFX emberFX;
    [SerializeField] private UI_BlacksmithVisualController visualController;
    [SerializeField] private Toggle useProtectionToggle;

    [Header("치트 버튼 참조 (선택 사항)")]
    [SerializeField] private Button cheatAllWeaponsBtn;
    [SerializeField] private Button cheatAllGemsBtn;

    // 내부 관리 변수
    private WeaponItemData selectedWeapon;
    private GemItemData selectedGem;
    private int selectedWeaponIndex = 0;
    private int selectedGemIndex = 0;
    private bool isWeaponTab = true;

    private readonly List<UI_BlacksmithSlot> activeSlots = new List<UI_BlacksmithSlot>();

    private void OnEnable()
    {
        BlacksmithManager.OnInventoryUpdated += RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged += RefreshAllUI;

        if (tab1Btn != null) tab1Btn.onClick.AddListener(SwitchToWeaponTab);
        if (tab2Btn != null) tab2Btn.onClick.AddListener(SwitchToGemTab);
        if (forgeBtn != null) forgeBtn.onClick.AddListener(OnClickForge);
        if (sellBtn != null) sellBtn.onClick.AddListener(OnClickSell);
        if (equipBtn != null) equipBtn.onClick.AddListener(OnClickEquip);

        if (cheatAllWeaponsBtn != null) cheatAllWeaponsBtn.onClick.AddListener(OnCheatAllWeaponsClicked);
        if (cheatAllGemsBtn != null) cheatAllGemsBtn.onClick.AddListener(OnCheatAllGemsClicked);

        InitButtonLabels();
        AttachButtonTweenEffects();
        SetupGridScrollAndMask();

        if (visualController == null && leftVisualPanel != null)
        {
            visualController = leftVisualPanel.GetComponent<UI_BlacksmithVisualController>();
            if (visualController == null)
            {
                visualController = leftVisualPanel.gameObject.AddComponent<UI_BlacksmithVisualController>();
            }
        }
        if (visualController != null)
        {
            visualController.LoadSpritesIfNull();
            visualController.StartIdleAnimation();
        }

        // UI가 새로 켜지면 아무 슬롯도 선택하지 않은 상태
        selectedWeapon = null;
        selectedGem = null;
        selectedWeaponIndex = -1;
        selectedGemIndex = -1;

        SwitchToWeaponTab();
        RefreshAllUI();
    }

    /// <summary>
    /// InventoryGridSection 및 InventoryGridContainer에 RectMask2D, ScrollRect, ContentSizeFitter를 자동 세팅하여
    /// 영역 이탈 마스킹 및 상하 스크롤 기능을 보장합니다.
    /// </summary>
    private void SetupGridScrollAndMask()
    {
        if (inventoryGridContainer == null) return;

        RectTransform containerRect = inventoryGridContainer as RectTransform;
        if (containerRect == null) containerRect = inventoryGridContainer.GetComponent<RectTransform>();

        Transform sectionTransform = inventoryGridContainer.parent;
        if (sectionTransform == null) return;

        RectTransform sectionRect = sectionTransform as RectTransform;

        // 1. InventoryGridSection (부모 영역)에 RectMask2D 및 ScrollRect 추가/설정
        RectMask2D mask = sectionTransform.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = sectionTransform.gameObject.AddComponent<RectMask2D>();
        }

        ScrollRect scrollRect = sectionTransform.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = sectionTransform.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.content = containerRect;
        scrollRect.viewport = sectionRect;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.scrollSensitivity = 25f;

        // 2. InventoryGridContainer (자식 컨텐츠) Anchor & Pivot 및 너비 설정 (상단 중앙 기준)
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0f, 0f);

            if (sectionRect != null && sectionRect.rect.width > 0)
            {
                containerRect.sizeDelta = new Vector2(sectionRect.rect.width, containerRect.sizeDelta.y);
            }
        }

        // 3. GridLayoutGroup 및 ContentSizeFitter 설정 (상단 중앙 정렬 보장)
        GridLayoutGroup gridLayout = inventoryGridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5; // 5열 고정
            gridLayout.childAlignment = TextAnchor.UpperLeft; // 슬롯 좌측 상단 정렬
        }

        ContentSizeFitter csf = inventoryGridContainer.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = inventoryGridContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>
    /// SellButton, ForgeButton, Tab1Button, Tab2Button에 클릭/터치 스케일 트위닝 연출 컴포넌트를 부착합니다.
    /// </summary>
    private void AttachButtonTweenEffects()
    {
        EnsureTweenEffect(sellBtn);
        EnsureTweenEffect(forgeBtn);
        EnsureTweenEffect(tab1Btn);
        EnsureTweenEffect(tab2Btn);
    }

    private void EnsureTweenEffect(Button btn)
    {
        if (btn != null && btn.GetComponent<UI_ButtonTweenEffect>() == null)
        {
            btn.gameObject.AddComponent<UI_ButtonTweenEffect>();
        }
    }

    /// <summary>
    /// SellButton 및 ForgeButton의 LabelText에 각각 "ui_sell_btn"(판매), "ui_forge_btn"(강화) 다국어를 바인딩하고 GoldText 위치를 탐색합니다.
    /// </summary>
    private void InitButtonLabels()
    {
        // 1. Sell Button Label ("ui_sell_btn" -> "판매") 및 GoldText 바인딩
        if (sellBtn != null)
        {
            if (sellBtnLabelText == null)
            {
                Transform labelTr = sellBtn.transform.Find("LabelText");
                if (labelTr != null) sellBtnLabelText = labelTr.GetComponent<TextMeshProUGUI>();
            }

            if (sellGoldText == null)
            {
                Transform goldBadgeTr = sellBtn.transform.Find("GoldBadge");
                if (goldBadgeTr != null)
                {
                    Transform goldTextTr = goldBadgeTr.Find("GoldText");
                    if (goldTextTr != null) sellGoldText = goldTextTr.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (sellBtnLabelText != null)
        {
            string sellText = "판매";
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasKey("ui_sell_btn"))
            {
                sellText = LocalizationManager.Instance.GetLocalizedString("ui_sell_btn");
            }
            sellBtnLabelText.text = sellText;
        }

        // 2. Forge Button Label ("ui_forge_btn" -> "강화") 및 GoldText 바인딩
        if (forgeBtn != null)
        {
            if (forgeBtnLabelText == null)
            {
                Transform labelTr = forgeBtn.transform.Find("LabelText");
                if (labelTr != null) forgeBtnLabelText = labelTr.GetComponent<TextMeshProUGUI>();
            }

            if (forgeGoldText == null)
            {
                Transform goldBadgeTr = forgeBtn.transform.Find("GoldBadge");
                if (goldBadgeTr != null)
                {
                    Transform goldTextTr = goldBadgeTr.Find("GoldText");
                    if (goldTextTr != null) forgeGoldText = goldTextTr.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (forgeBtnLabelText != null)
        {
            string forgeText = "강화";
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasKey("ui_forge_btn"))
            {
                forgeText = LocalizationManager.Instance.GetLocalizedString("ui_forge_btn");
            }
            forgeBtnLabelText.text = forgeText;
        }
    }

    private void OnDisable()
    {
        BlacksmithManager.OnInventoryUpdated -= RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged -= RefreshAllUI;

        if (tab1Btn != null) tab1Btn.onClick.RemoveListener(SwitchToWeaponTab);
        if (tab2Btn != null) tab2Btn.onClick.RemoveListener(SwitchToGemTab);
        if (forgeBtn != null) forgeBtn.onClick.RemoveListener(OnClickForge);
        if (sellBtn != null) sellBtn.onClick.RemoveListener(OnClickSell);
        if (equipBtn != null) equipBtn.onClick.RemoveListener(OnClickEquip);

        if (cheatAllWeaponsBtn != null) cheatAllWeaponsBtn.onClick.RemoveListener(OnCheatAllWeaponsClicked);
        if (cheatAllGemsBtn != null) cheatAllGemsBtn.onClick.RemoveListener(OnCheatAllGemsClicked);
    }

    private void OnCheatAllWeaponsClicked()
    {
        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllWeaponsCheat();
        }
    }

    private void OnCheatAllGemsClicked()
    {
        if (BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.AddAllGemsCheat();
        }
    }

    /// <summary>
    /// 대장간 해금 검증 및 전체 UI 갱신
    /// </summary>
    public override void RefreshAllUI()
    {
        bool isUnlocked = BlacksmithManager.Instance != null && BlacksmithManager.Instance.IsBlacksmithUnlocked();

        if (lockedOverlayPanel != null)
        {
            lockedOverlayPanel.SetActive(!isUnlocked);
        }

        if (lockNoticeText != null)
        {
            lockNoticeText.text = isUnlocked ? string.Empty : "대장간 건물을 먼저 건설해야 합니다!\n(코어2 대장간 건물 Lv 1 이상 필요)";
        }

        if (BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            BlacksmithManager.Instance.SortWeapons();
            var weapons = BlacksmithManager.Instance.ownedWeapons;
            if (weapons.Count == 0 || selectedWeaponIndex < 0 || selectedWeaponIndex >= weapons.Count)
            {
                selectedWeaponIndex = -1;
                selectedWeapon = null;
            }
            else
            {
                selectedWeapon = weapons[selectedWeaponIndex];
            }
            RefreshWeaponDetail(selectedWeapon);
            BuildWeaponGrid();
        }
        else
        {
            BlacksmithManager.Instance.SortGems();
            var gems = BlacksmithManager.Instance.ownedGems;
            if (gems.Count == 0 || selectedGemIndex < 0 || selectedGemIndex >= gems.Count)
            {
                selectedGemIndex = -1;
                selectedGem = null;
            }
            else
            {
                selectedGem = gems[selectedGemIndex];
            }
            RefreshGemDetail(selectedGem);
            BuildGemGrid();
        }

        UpdateActionButtonsState();
    }

    /// <summary>
    /// 선택 무기 상세 정보 및 3색 확률 인디케이터 (🟢, 🟡, 🔴) 갱신
    /// </summary>
    private void RefreshWeaponDetail(WeaponItemData weapon)
    {
        if (weapon == null)
        {
            if (equippedWeaponIcon != null)
            {
                equippedWeaponIcon.sprite = null;
                equippedWeaponIcon.gameObject.SetActive(false);
            }
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "선택된 무기 없음";
            if (weaponGradeLevelText != null) weaponGradeLevelText.text = "+0";
            if (successRateText != null) successRateText.text = "0%";
            if (keepRateText != null) keepRateText.text = "0%";
            if (destroyRateText != null) destroyRateText.text = "0%";
            if (forgeGoldText != null) forgeGoldText.text = "0";
            if (sellGoldText != null) sellGoldText.text = "0";
            return;
        }

        if (equippedWeaponIcon != null)
        {
            equippedWeaponIcon.sprite = weapon.iconSprite;
            equippedWeaponIcon.gameObject.SetActive(weapon.iconSprite != null);
        }
        if (equippedWeaponNameText != null)
        {
            equippedWeaponNameText.text = weapon.weaponName;
        }
        if (weaponGradeLevelText != null)
        {
            weaponGradeLevelText.text = $"+{weapon.grade}";
        }

        // 3-Color Rates (🟢 초록=성공, 🟡 노랑=유지, 🔴 빨강=파괴)
        float successRatio = weapon.upgradeSuccessRate;
        float keepRatio = weapon.upgradeKeepRate;
        float destroyRatio = weapon.upgradeDestroyRate;

        if (successRateText != null) successRateText.text = $"{Mathf.RoundToInt(successRatio * 100f)}%";
        if (keepRateText != null) keepRateText.text = $"{Mathf.RoundToInt(keepRatio * 100f)}%";
        if (destroyRateText != null) destroyRateText.text = $"{Mathf.RoundToInt(destroyRatio * 100f)}%";

        // 버튼 비용 및 판매가 뱃지 갱신
        if (forgeGoldText != null) forgeGoldText.text = $"{weapon.enhanceCost:N0}";
        if (sellGoldText != null) sellGoldText.text = $"{weapon.sellPrice:N0}";
    }

    /// <summary>
    /// 선택 보석 상세 정보 갱신
    /// </summary>
    private void RefreshGemDetail(GemItemData gem)
    {
        if (gem == null)
        {
            if (equippedWeaponIcon != null)
            {
                equippedWeaponIcon.sprite = null;
                equippedWeaponIcon.gameObject.SetActive(false);
            }
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "선택된 보석 없음";
            if (weaponGradeLevelText != null) weaponGradeLevelText.text = "Lv.0";
            if (successRateText != null) successRateText.text = "0%";
            if (keepRateText != null) keepRateText.text = "0%";
            if (destroyRateText != null) destroyRateText.text = "0%";
            if (forgeGoldText != null) forgeGoldText.text = "0";
            if (sellGoldText != null) sellGoldText.text = "0";
            return;
        }

        if (equippedWeaponIcon != null)
        {
            equippedWeaponIcon.sprite = gem.iconSprite;
            equippedWeaponIcon.gameObject.SetActive(gem.iconSprite != null);
        }
        if (equippedWeaponNameText != null)
        {
            equippedWeaponNameText.text = gem.gemName;
        }
        if (weaponGradeLevelText != null)
        {
            weaponGradeLevelText.text = $"Lv.{gem.level}";
        }

        float successRatio = gem.upgradeSuccessRate;
        float keepRatio = gem.upgradeKeepRate;
        float destroyRatio = gem.upgradeDestroyRate;

        if (successRateText != null) successRateText.text = $"{Mathf.RoundToInt(successRatio * 100f)}%";
        if (keepRateText != null) keepRateText.text = $"{Mathf.RoundToInt(keepRatio * 100f)}%";
        if (destroyRateText != null) destroyRateText.text = $"{Mathf.RoundToInt(destroyRatio * 100f)}%";

        // 버튼 비용 및 판매가 뱃지 갱신
        if (forgeGoldText != null) forgeGoldText.text = $"{gem.enhanceCost:N0}";
        if (sellGoldText != null) sellGoldText.text = $"{gem.sellPrice:N0}";
    }

    /// <summary>
    /// 5x3 그리드 무기 인벤토리 슬롯 생성 및 갱신 (데이터가 없는 경우 슬롯 없이 안내 문구 출력)
    /// </summary>
    private void BuildWeaponGrid()
    {
        ClearGridSlots();
        if (inventoryGridContainer == null || BlacksmithManager.Instance == null) return;

        var weapons = BlacksmithManager.Instance.ownedWeapons;
        if (weapons == null || weapons.Count == 0)
        {
            UpdateEmptyNoticeText(true);
            return;
        }

        UpdateEmptyNoticeText(false);

        if (slotPrefab == null) return;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponItemData wData = weapons[i];
            UI_BlacksmithSlot slot = Instantiate(slotPrefab, inventoryGridContainer);
            bool isSelected = (wData != null && i == selectedWeaponIndex);
            slot.Setup(wData, i, isSelected, OnSlotSelected);
            activeSlots.Add(slot);
        }
    }

    /// <summary>
    /// 5x3 그리드 보석 인벤토리 슬롯 생성 및 갱신 (데이터가 없는 경우 슬롯 없이 안내 문구 출력)
    /// </summary>
    private void BuildGemGrid()
    {
        ClearGridSlots();
        if (inventoryGridContainer == null || BlacksmithManager.Instance == null) return;

        var gems = BlacksmithManager.Instance.ownedGems;
        if (gems == null || gems.Count == 0)
        {
            UpdateEmptyNoticeText(true);
            return;
        }

        UpdateEmptyNoticeText(false);

        if (slotPrefab == null) return;

        for (int i = 0; i < gems.Count; i++)
        {
            GemItemData gData = gems[i];
            UI_BlacksmithSlot slot = Instantiate(slotPrefab, inventoryGridContainer);
            bool isSelected = (gData != null && i == selectedGemIndex);
            slot.SetupGem(gData, i, isSelected, OnSlotSelected);
            activeSlots.Add(slot);
        }
    }

    /// <summary>
    /// 인벤토리가 비어있을 때 표시할 중앙 안내 텍스트("Notice_No_Items")의 상태를 갱신합니다.
    /// </summary>
    private void UpdateEmptyNoticeText(bool isEmpty)
    {
        if (emptyNoticeText == null && inventoryGridContainer != null)
        {
            Transform parentTransform = inventoryGridContainer.parent != null ? inventoryGridContainer.parent : inventoryGridContainer;
            Transform existingText = parentTransform.Find("EmptyNoticeText");
            if (existingText != null)
            {
                emptyNoticeText = existingText.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject textGO = new GameObject("EmptyNoticeText");
                textGO.transform.SetParent(parentTransform, false);

                RectTransform rectTransform = textGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(400f, 60f);

                emptyNoticeText = textGO.AddComponent<TextMeshProUGUI>();
                emptyNoticeText.alignment = TextAlignmentOptions.Center;
                emptyNoticeText.enableAutoSizing = true;
                emptyNoticeText.fontSizeMin = 14;
                emptyNoticeText.fontSizeMax = 32;
                emptyNoticeText.fontSize = 24;
                emptyNoticeText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    emptyNoticeText.font = TMPro.TMP_Settings.defaultFontAsset;
                }
            }
        }

        if (emptyNoticeText != null)
        {
            emptyNoticeText.enableAutoSizing = true;
            emptyNoticeText.fontSizeMin = 14;
            emptyNoticeText.fontSizeMax = 32;

            if (isEmpty)
            {
                string msg = "아이템이 없습니다.";
                if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasKey("Notice_No_Items"))
                {
                    msg = LocalizationManager.Instance.GetLocalizedString("Notice_No_Items");
                }
                emptyNoticeText.text = msg;
                emptyNoticeText.gameObject.SetActive(true);
            }
            else
            {
                emptyNoticeText.gameObject.SetActive(false);
            }
        }
    }

    private void ClearGridSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        activeSlots.Clear();
    }

    private void OnSlotSelected(UI_BlacksmithSlot slot)
    {
        if (isWeaponTab)
        {
            if (slot.BoundWeapon != null)
            {
                selectedWeaponIndex = slot.SlotIndex;
                selectedWeapon = slot.BoundWeapon;
                RefreshWeaponDetail(selectedWeapon);
                UpdateSlotHighlightBorders();
                UpdateActionButtonsState();
            }
        }
        else
        {
            if (slot.BoundGem != null)
            {
                selectedGemIndex = slot.SlotIndex;
                selectedGem = slot.BoundGem;
                RefreshGemDetail(selectedGem);
                UpdateSlotHighlightBorders();
                UpdateActionButtonsState();
            }
        }
    }

    private void UpdateSlotHighlightBorders()
    {
        int targetSelectedIndex = isWeaponTab ? selectedWeaponIndex : selectedGemIndex;
        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (activeSlots[i] != null)
            {
                activeSlots[i].SetSelected(activeSlots[i].SlotIndex == targetSelectedIndex);
            }
        }
    }

    private bool isEnhancing = false;

    private void UpdateActionButtonsState()
    {
        bool hasSelection = isWeaponTab ? (selectedWeapon != null) : (selectedGem != null);
        bool canInteract = hasSelection && !isEnhancing;

        if (isWeaponTab)
        {
            if (forgeBtn != null)
            {
                forgeBtn.gameObject.SetActive(true);
                forgeBtn.interactable = canInteract;
            }

            bool isAlreadyEquipped = (BlacksmithManager.Instance != null && selectedWeapon != null && BlacksmithManager.Instance.equippedWeapon == selectedWeapon);

            if (equipBtn != null)
            {
                equipBtn.gameObject.SetActive(true);
                equipBtn.interactable = canInteract && !isAlreadyEquipped;

                if (equipBtnLabelText != null)
                {
                    equipBtnLabelText.text = isAlreadyEquipped ? "장착중" : "장착";
                }
            }

            if (sellBtn != null)
            {
                sellBtn.gameObject.SetActive(true);
                sellBtn.interactable = canInteract;
            }
        }
        else
        {
            // Tab 2 (보석 탭): 장착 버튼만 숨김 처리 (보석도 강화가 가능하므로 강화 버튼은 유지)
            if (forgeBtn != null)
            {
                forgeBtn.gameObject.SetActive(true);
                forgeBtn.interactable = canInteract;
            }
            if (equipBtn != null) equipBtn.gameObject.SetActive(false);

            if (sellBtn != null)
            {
                sellBtn.gameObject.SetActive(true);
                sellBtn.interactable = canInteract;
            }
        }
    }

    #region Tab & Action Handlers

    public void SwitchToWeaponTab()
    {
        if (isEnhancing) return;
        isWeaponTab = true;
        selectedWeapon = null;
        selectedWeaponIndex = -1;
        if (tab1Highlight != null) tab1Highlight.gameObject.SetActive(true);
        if (tab2Highlight != null) tab2Highlight.gameObject.SetActive(false);
        RefreshAllUI();
    }

    public void SwitchToGemTab()
    {
        if (isEnhancing) return;
        isWeaponTab = false;
        selectedGem = null;
        selectedGemIndex = -1;
        if (tab1Highlight != null) tab1Highlight.gameObject.SetActive(false);
        if (tab2Highlight != null) tab2Highlight.gameObject.SetActive(true);
        RefreshAllUI();
    }

    private void OnClickEquip()
    {
        if (isEnhancing) return;

        if (isWeaponTab && selectedWeapon != null)
        {
            if (BlacksmithManager.Instance != null)
            {
                BlacksmithManager.Instance.EquipWeapon(selectedWeapon);
                UI_ToastPopup.Show($"{selectedWeapon.weaponName} 장착 완료!");
                Debug.Log($"<color=green>[UI_BlacksmithPanel] 무기 장착 완료: {selectedWeapon.weaponName}</color>");
                RefreshAllUI();
            }
        }
    }
    private void OnClickForge()
    {
        if (isEnhancing || BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            if (selectedWeapon == null || selectedWeaponIndex < 0) return;
            if (selectedWeapon.nextGradeWeapon == null)
            {
                UI_ToastPopup.Show("Notice_Max_Upgrade");
                return;
            }
            
            // 재화 사전 체크 및 차감
            if (!BlacksmithManager.Instance.TryConsumeWeaponEnhanceCost(selectedWeaponIndex))
            {
                UI_ToastPopup.Show("Notice_No_Currency");
                return;
            }
        }
        else
        {
            if (selectedGem == null || selectedGemIndex < 0) return;
            if (selectedGem.nextLevelGem == null)
            {
                UI_ToastPopup.Show("Notice_Max_Upgrade");
                return;
            }

            // 재화 사전 체크 및 차감
            if (!BlacksmithManager.Instance.TryConsumeGemEnhanceCost(selectedGemIndex))
            {
                UI_ToastPopup.Show("Notice_No_Currency");
                return;
            }
        }

        if (visualController == null && leftVisualPanel != null)
        {
            visualController = leftVisualPanel.GetComponent<UI_BlacksmithVisualController>();
        }

        isEnhancing = true;
        UpdateActionButtonsState();

        System.Action onStrike = () =>
        {
            if (emberFX != null) emberFX.TriggerEnhanceSparkFX();
        };

        System.Action onComplete = () =>
        {
            // 연출 종료 후 실제 강화 확률 굴림 및 인벤토리 갱신
            if (isWeaponTab)
            {
                WeaponItemData nextWeapon = selectedWeapon.nextGradeWeapon;
                bool useProtection = useProtectionToggle != null && useProtectionToggle.isOn;
                WeaponEnhanceResult weaponResult = BlacksmithManager.Instance.ExecuteWeaponEnhance(selectedWeaponIndex, useProtection);

                if (weaponResult == WeaponEnhanceResult.Success)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Success");
                    int newIndex = BlacksmithManager.Instance.ownedWeapons.IndexOf(nextWeapon);
                    selectedWeaponIndex = newIndex;
                    selectedWeapon = (newIndex >= 0) ? nextWeapon : null;
                }
                else if (weaponResult == WeaponEnhanceResult.Keep || weaponResult == WeaponEnhanceResult.ProtectedFailure)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Fail");
                }
                else if (weaponResult == WeaponEnhanceResult.DestroyedFailure)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Destroy");
                    selectedWeapon = null;
                    selectedWeaponIndex = -1;
                }
            }
            else
            {
                GemItemData nextGem = selectedGem.nextLevelGem;
                GemEnhanceResult gemResult = BlacksmithManager.Instance.ExecuteGemEnhance(selectedGemIndex);

                if (gemResult == GemEnhanceResult.Success)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Success");
                    int newIndex = BlacksmithManager.Instance.ownedGems.IndexOf(nextGem);
                    selectedGemIndex = newIndex;
                    selectedGem = (newIndex >= 0) ? nextGem : null;
                }
                else if (gemResult == GemEnhanceResult.Keep)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Fail");
                }
                else if (gemResult == GemEnhanceResult.Destroyed)
                {
                    UI_ToastPopup.Show("Notice_Enhance_Destroy");
                    selectedGem = null;
                    selectedGemIndex = -1;
                }
            }

            isEnhancing = false;
            RefreshAllUI();
        };

        if (visualController != null)
        {
            visualController.PlayEnhanceHammerSequence(onStrike, onComplete);
        }
        else
        {
            onStrike();
            onComplete();
        }
    }

    private void OnClickSell()
    {
        if (BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            if (selectedWeapon == null || selectedWeaponIndex < 0) return;
            BlacksmithManager.Instance.SellWeaponAtIndex(selectedWeaponIndex);
            selectedWeapon = null;
            selectedWeaponIndex = -1;
        }
        else
        {
            if (selectedGem == null || selectedGemIndex < 0) return;
            BlacksmithManager.Instance.SellGemAtIndex(selectedGemIndex);
            selectedGem = null;
            selectedGemIndex = -1;
        }

        RefreshAllUI();
    }

    #endregion
}
