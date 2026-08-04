using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BlacksmithPrefabBuilder
{
    [MenuItem("Gotblin/Build Blacksmith Popup Prefab")]
    public static void BuildPrefab()
    {
        BlacksmithVisualSpriteGenerator.GenerateAssets();

        // 1. Root GameObject 생성 (전체 화면 Stretch Canvas Overlay)
        GameObject root = new GameObject("UI_BlacksmithPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(UI_BlacksmithPanel));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetFullStretch(rootRect);

        UI_BlacksmithPanel blacksmithPanel = root.GetComponent<UI_BlacksmithPanel>();
        SerializedObject serializedPanel = new SerializedObject(blacksmithPanel);
        serializedPanel.FindProperty("popupID").stringValue = "Blacksmith";

        // 2. Dimmed Background Image & Button (화면 전체 반투명 검은색 딤드 - 터치 시 팝업 닫힘)
        GameObject dimmedObj = CreateUIElement("DimmedBackground", root.transform);
        RectTransform dimmedRect = dimmedObj.GetComponent<RectTransform>();
        SetFullStretch(dimmedRect);
        dimmedRect.sizeDelta = new Vector2(4000, 4000); // 캔버스 화면 전역 커버

        Image dimmedImg = dimmedObj.AddComponent<Image>();
        dimmedImg.color = new Color(0f, 0f, 0f, 0.65f); // 65% 반투명 딤드
        dimmedImg.raycastTarget = true;
        Button dimmedBtn = dimmedObj.AddComponent<Button>();

        // 3. Content Panel (440 x 850 중앙 팝업 본체 패널)
        GameObject contentObj = CreateUIElement("ContentPanel", root.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(440, 850);
        contentRect.anchoredPosition = Vector2.zero;

        // 4. Main Background Panel
        GameObject bgObj = CreateUIElement("Background", contentObj.transform);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.18f, 1f); // #2e2e2e
        SetFullStretch(bgObj.GetComponent<RectTransform>());

        // 5. Top Header Bar (연두색 헤더)
        GameObject headerObj = CreateUIElement("HeaderBar", contentObj.transform);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -10);
        headerRect.sizeDelta = new Vector2(-20, 60);

        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(0.72f, 0.95f, 0.42f, 1f); // 연두색 #b8f26b

        TextMeshProUGUI titleText = CreateTextElement("TitleText", headerObj.transform, "UI_Blacksmith", Color.black, new Vector2(15, 0));
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;

        // 6. Upper Split Section (Left: Visual Panel, Right: Selected Item & Rates)
        GameObject upperSplitObj = CreateUIElement("UpperSplitSection", contentObj.transform);
        RectTransform upperRect = upperSplitObj.GetComponent<RectTransform>();
        upperRect.anchorMin = new Vector2(0, 1);
        upperRect.anchorMax = new Vector2(1, 1);
        upperRect.pivot = new Vector2(0.5f, 1);
        upperRect.anchoredPosition = new Vector2(0, -80);
        upperRect.sizeDelta = new Vector2(-20, 230);

        // Left Visual Panel (Ember FX & Large Preview & Goblin Smith Visual)
        GameObject leftVisualObj = CreateUIElement("LeftVisualPanel", upperSplitObj.transform);
        RectTransform leftRect = leftVisualObj.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.48f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        Image leftBg = leftVisualObj.AddComponent<Image>();
        leftBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        leftVisualObj.AddComponent<RectMask2D>();

        // Load default sprites for the builder
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprite/Blacksmith_Interior_BG.png");
        Sprite anvilSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprite/Blacksmith_Anvil.png");
        
        Object[] goblinSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Sprite/Goblin_Blacksmith_Sheet.png");
        Sprite goblinIdle0 = null;
        if (goblinSprites != null)
        {
            foreach (var obj in goblinSprites)
            {
                if (obj is Sprite s && s.name.EndsWith("_0")) { goblinIdle0 = s; break; }
                else if (obj is Sprite s2 && goblinIdle0 == null) goblinIdle0 = s2; // fallback
            }
        }

        // Visual Background Image
        GameObject visualBgObj = CreateUIElement("VisualBG", leftVisualObj.transform);
        SetFullStretch(visualBgObj.GetComponent<RectTransform>());
        Image visualBgImg = visualBgObj.AddComponent<Image>();
        visualBgImg.color = Color.white;
        visualBgImg.sprite = bgSprite;

        // Visual Anvil Image
        GameObject visualAnvilObj = CreateUIElement("VisualAnvil", leftVisualObj.transform);
        RectTransform anvilRect = visualAnvilObj.GetComponent<RectTransform>();
        anvilRect.anchorMin = new Vector2(0.6f, 0.1f);
        anvilRect.anchorMax = new Vector2(0.6f, 0.1f);
        anvilRect.pivot = new Vector2(0.5f, 0f);
        anvilRect.sizeDelta = new Vector2(56, 56);
        anvilRect.anchoredPosition = Vector2.zero;
        Image visualAnvilImg = visualAnvilObj.AddComponent<Image>();
        visualAnvilImg.color = Color.white;
        visualAnvilImg.sprite = anvilSprite;

        // Visual Goblin Image
        GameObject visualGoblinObj = CreateUIElement("VisualGoblin", leftVisualObj.transform);
        RectTransform goblinRect = visualGoblinObj.GetComponent<RectTransform>();
        goblinRect.anchorMin = new Vector2(0.25f, 0.1f);
        goblinRect.anchorMax = new Vector2(0.25f, 0.1f);
        goblinRect.pivot = new Vector2(0.5f, 0f);
        goblinRect.sizeDelta = new Vector2(72, 72);
        goblinRect.anchoredPosition = Vector2.zero;
        Image visualGoblinImg = visualGoblinObj.AddComponent<Image>();
        visualGoblinImg.color = Color.white;
        visualGoblinImg.sprite = goblinIdle0;

        // Visual Controller Component
        UI_BlacksmithVisualController visualCtrl = leftVisualObj.AddComponent<UI_BlacksmithVisualController>();
        SerializedObject serializedVC = new SerializedObject(visualCtrl);
        serializedVC.FindProperty("bgImage").objectReferenceValue = visualBgImg;
        serializedVC.FindProperty("anvilImage").objectReferenceValue = visualAnvilImg;
        serializedVC.FindProperty("goblinImage").objectReferenceValue = visualGoblinImg;
        serializedVC.ApplyModifiedProperties();

        serializedPanel.FindProperty("leftVisualPanel").objectReferenceValue = leftRect;
        serializedPanel.FindProperty("visualController").objectReferenceValue = visualCtrl;

        // Ember Container & FX
        GameObject emberContainerObj = CreateUIElement("EmberContainer", leftVisualObj.transform);
        SetFullStretch(emberContainerObj.GetComponent<RectTransform>());
        UI_BlacksmithEmberFX emberFX = emberContainerObj.AddComponent<UI_BlacksmithEmberFX>();

        GameObject emberTemplateObj = CreateUIElement("EmberTemplate", emberContainerObj.transform);
        Image emberImg = emberTemplateObj.AddComponent<Image>();
        emberImg.color = new Color(1f, 0.36f, 0f, 0.9f);
        emberTemplateObj.SetActive(false);

        SerializedObject serializedFX = new SerializedObject(emberFX);
        serializedFX.FindProperty("emberContainer").objectReferenceValue = emberContainerObj.GetComponent<RectTransform>();
        serializedFX.FindProperty("emberTemplate").objectReferenceValue = emberImg;

        // Right Detail Panel (Selected Item + 3-Color Rates)
        GameObject rightDetailObj = CreateUIElement("RightDetailPanel", upperSplitObj.transform);
        RectTransform rightRect = rightDetailObj.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.52f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;

        // Selected Item Slot Image (+7 Badge)
        GameObject iconSlotObj = CreateUIElement("SelectedIconSlot", rightDetailObj.transform);
        RectTransform slotRect = iconSlotObj.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 1);
        slotRect.anchorMax = new Vector2(0.5f, 1);
        slotRect.pivot = new Vector2(0.5f, 1);
        slotRect.anchoredPosition = new Vector2(0, -10);
        slotRect.sizeDelta = new Vector2(130, 130);
        Image slotBg = iconSlotObj.AddComponent<Image>();
        slotBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        serializedFX.FindProperty("anvilRect").objectReferenceValue = slotRect;
        serializedFX.ApplyModifiedProperties();

        GameObject weaponIconObj = CreateUIElement("WeaponIcon", iconSlotObj.transform);
        Image weaponIcon = weaponIconObj.AddComponent<Image>();
        SetFullStretch(weaponIconObj.GetComponent<RectTransform>());

        // +7 Grade Badge
        GameObject gradeBadgeObj = CreateUIElement("GradeBadge", iconSlotObj.transform);
        RectTransform gradeRect = gradeBadgeObj.GetComponent<RectTransform>();
        gradeRect.anchorMin = new Vector2(1, 1);
        gradeRect.anchorMax = new Vector2(1, 1);
        gradeRect.pivot = new Vector2(1, 1);
        gradeRect.anchoredPosition = new Vector2(-6, -6);
        gradeRect.sizeDelta = new Vector2(34, 26);
        Image gradeBg = gradeBadgeObj.AddComponent<Image>();
        gradeBg.color = new Color(0.24f, 0.24f, 0.24f, 0.9f);

        TextMeshProUGUI gradeText = CreateTextElement("Text", gradeBadgeObj.transform, "+7", Color.white, Vector2.zero);
        gradeText.fontSize = 14;
        gradeText.fontStyle = FontStyles.Bold;
        SetFullStretch(gradeText.GetComponent<RectTransform>());

        // Item Name Text
        GameObject nameObj = CreateUIElement("Selected_Image_Name", rightDetailObj.transform);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -148);
        nameRect.sizeDelta = new Vector2(0, 24);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Selected_Image_Name";
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.black;

        // 3-Color Rate Indicators (🟢 100%, 🟡 100%, 🔴 100%)
        GameObject rateGroupObj = CreateUIElement("RateGroupPanel", rightDetailObj.transform);
        RectTransform rateGroupRect = rateGroupObj.GetComponent<RectTransform>();
        rateGroupRect.anchorMin = new Vector2(0, 0);
        rateGroupRect.anchorMax = new Vector2(1, 0);
        rateGroupRect.pivot = new Vector2(0.5f, 0);
        rateGroupRect.anchoredPosition = new Vector2(0, 10);
        rateGroupRect.sizeDelta = new Vector2(0, 30);

        HorizontalLayoutGroup rateLayout = rateGroupObj.AddComponent<HorizontalLayoutGroup>();
        rateLayout.childAlignment = TextAnchor.MiddleCenter;
        rateLayout.spacing = 6;
        rateLayout.childControlWidth = false;
        rateLayout.childControlHeight = false;

        TextMeshProUGUI successText = CreateRateIndicator("SuccessRate", rateGroupObj.transform, new Color(0.15f, 0.95f, 0.2f), "100%");
        TextMeshProUGUI keepText = CreateRateIndicator("KeepRate", rateGroupObj.transform, new Color(0.95f, 0.85f, 0.15f), "100%");
        TextMeshProUGUI destroyText = CreateRateIndicator("DestroyRate", rateGroupObj.transform, new Color(0.95f, 0.2f, 0.15f), "100%");

        // 7. Middle Inventory Grid Section (5x3 Grid)
        GameObject gridSectionObj = CreateUIElement("InventoryGridSection", contentObj.transform);
        RectTransform gridSectionRect = gridSectionObj.GetComponent<RectTransform>();
        gridSectionRect.anchorMin = new Vector2(0, 0.26f);
        gridSectionRect.anchorMax = new Vector2(1, 0.62f);
        gridSectionRect.offsetMin = new Vector2(15, 0);
        gridSectionRect.offsetMax = new Vector2(-15, 0);

        Image gridSectionBg = gridSectionObj.AddComponent<Image>();
        gridSectionBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        GameObject gridContainerObj = CreateUIElement("InventoryGridContainer", gridSectionObj.transform);
        RectTransform gridContainerRect = gridContainerObj.GetComponent<RectTransform>();
        SetFullStretch(gridContainerRect);
        gridContainerRect.offsetMin = new Vector2(10, 10);
        gridContainerRect.offsetMax = new Vector2(-10, -10);

        GridLayoutGroup gridLayout = gridContainerObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(70, 70);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;

        // Slot Template
        GameObject slotTemplateObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI_BlacksmithSlot.prefab");

        // 8. Action Bar Section (Sell & Forge Buttons)
        GameObject actionBarObj = CreateUIElement("ActionBarSection", contentObj.transform);
        RectTransform actionRect = actionBarObj.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0, 0.13f);
        actionRect.anchorMax = new Vector2(1, 0.24f);
        actionRect.offsetMin = new Vector2(15, 0);
        actionRect.offsetMax = new Vector2(-15, 0);

        GameObject sellBtnObj = CreateActionButton("SellButton", actionBarObj.transform, "Sell", new Color(0.24f, 0.55f, 0.62f), new Color(0.05f, 0.28f, 0.33f), new Vector2(-102, 0));
        Button sellBtn = sellBtnObj.GetComponent<Button>();
        TextMeshProUGUI sellGoldText = sellBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        GameObject forgeBtnObj = CreateActionButton("ForgeButton", actionBarObj.transform, "Forge", new Color(0.68f, 0.42f, 0.22f), new Color(0.35f, 0.20f, 0.08f), new Vector2(102, 0));
        Button forgeBtn = forgeBtnObj.GetComponent<Button>();
        TextMeshProUGUI forgeGoldText = forgeBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // 9. Bottom Navigation Tab Bar (Tab 1 | Tab 2)
        GameObject bottomTabObj = CreateUIElement("BottomTabBarSection", contentObj.transform);
        RectTransform bottomTabRect = bottomTabObj.GetComponent<RectTransform>();
        bottomTabRect.anchorMin = new Vector2(0, 0);
        bottomTabRect.anchorMax = new Vector2(1, 0.11f);
        bottomTabRect.offsetMin = new Vector2(15, 10);
        bottomTabRect.offsetMax = new Vector2(-15, -10);

        GameObject tab1Obj = CreateTabButton("Tab1Button", bottomTabObj.transform, "Tab1", new Vector2(-102, 0));
        Button tab1Btn = tab1Obj.GetComponent<Button>();
        Image tab1Highlight = tab1Obj.transform.Find("Highlight").GetComponent<Image>();

        GameObject tab2Obj = CreateTabButton("Tab2Button", bottomTabObj.transform, "Tab1", new Vector2(102, 0));
        Button tab2Btn = tab2Obj.GetComponent<Button>();
        Image tab2Highlight = tab2Obj.transform.Find("Highlight").GetComponent<Image>();
        tab2Highlight.gameObject.SetActive(false);

        // Close Button (Top-Right of Content Panel)
        GameObject closeBtnObj = CreateUIElement("CloseButton", contentObj.transform);
        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.anchoredPosition = new Vector2(-15, -15);
        closeBtnRect.sizeDelta = new Vector2(36, 36);
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f);
        Button closeBtn = closeBtnObj.AddComponent<Button>();

        // 10. Serialized Binding
        serializedPanel.FindProperty("dimmedBackgroundButton").objectReferenceValue = dimmedBtn;
        serializedPanel.FindProperty("closeButton").objectReferenceValue = closeBtn;
        serializedPanel.FindProperty("titleText").objectReferenceValue = titleText;
        serializedPanel.FindProperty("leftVisualPanel").objectReferenceValue = leftRect;
        serializedPanel.FindProperty("equippedWeaponIcon").objectReferenceValue = weaponIcon;
        serializedPanel.FindProperty("equippedWeaponNameText").objectReferenceValue = nameText;
        serializedPanel.FindProperty("weaponGradeLevelText").objectReferenceValue = gradeText;
        serializedPanel.FindProperty("successRateText").objectReferenceValue = successText;
        serializedPanel.FindProperty("keepRateText").objectReferenceValue = keepText;
        serializedPanel.FindProperty("destroyRateText").objectReferenceValue = destroyText;
        serializedPanel.FindProperty("inventoryGridContainer").objectReferenceValue = gridContainerObj.transform;
        if (slotTemplateObj != null)
        {
            serializedPanel.FindProperty("slotPrefab").objectReferenceValue = slotTemplateObj.GetComponent<UI_BlacksmithSlot>();
        }
        serializedPanel.FindProperty("sellBtn").objectReferenceValue = sellBtn;
        serializedPanel.FindProperty("sellGoldText").objectReferenceValue = sellGoldText;
        serializedPanel.FindProperty("forgeBtn").objectReferenceValue = forgeBtn;
        serializedPanel.FindProperty("forgeGoldText").objectReferenceValue = forgeGoldText;
        serializedPanel.FindProperty("tab1Btn").objectReferenceValue = tab1Btn;
        serializedPanel.FindProperty("tab2Btn").objectReferenceValue = tab2Btn;
        serializedPanel.FindProperty("tab1Highlight").objectReferenceValue = tab1Highlight;
        serializedPanel.FindProperty("tab2Highlight").objectReferenceValue = tab2Highlight;
        serializedPanel.FindProperty("emberFX").objectReferenceValue = emberFX;
        serializedPanel.ApplyModifiedProperties();

        // 11. Save Prefabs
        if (!AssetDatabase.IsValidFolder("Assets/Prefab")) AssetDatabase.CreateFolder("Assets", "Prefab");
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        string prefabPath1 = "Assets/Prefab/UI_BlacksmithPanel.prefab";
        string prefabPath2 = "Assets/Resources/UI_BlacksmithPanel.prefab";

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath1);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath2);

        Object.DestroyImmediate(root);

        Debug.Log($"<color=green>[BlacksmithPrefabBuilder] UI_BlacksmithPanel 딤드(Dimmed) 팝업 프리팹 빌드 완료!\n- {prefabPath1}\n- {prefabPath2}</color>");
    }

    private static TextMeshProUGUI CreateRateIndicator(string name, Transform parent, Color dotColor, string defaultText)
    {
        GameObject group = CreateUIElement(name, parent);
        RectTransform rect = group.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(56, 24);

        HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 3;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        GameObject dot = CreateUIElement("Dot", group.transform);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(10, 10);
        Image dotImg = dot.AddComponent<Image>();
        dotImg.color = dotColor;

        GameObject textObj = CreateUIElement("Text", group.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(40, 20);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Left;

        return tmp;
    }

    private static GameObject CreateActionButton(string name, Transform parent, string labelText, Color btnColor, Color badgeBgColor, Vector2 pos)
    {
        GameObject btnObj = CreateUIElement(name, parent);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(190, 70);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = btnColor;
        btnObj.AddComponent<Button>();

        GameObject labelObj = CreateUIElement("LabelText", btnObj.transform);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(0, -5);

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 16;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.Center;

        GameObject badgeObj = CreateUIElement("GoldBadge", btnObj.transform);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.1f, 0.1f);
        badgeRect.anchorMax = new Vector2(0.9f, 0.45f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;

        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.color = badgeBgColor;

        HorizontalLayoutGroup badgeLayout = badgeObj.AddComponent<HorizontalLayoutGroup>();
        badgeLayout.childAlignment = TextAnchor.MiddleCenter;
        badgeLayout.spacing = 4;
        badgeLayout.childControlWidth = false;
        badgeLayout.childControlHeight = false;

        GameObject goldIcon = CreateUIElement("GoldIcon", badgeObj.transform);
        RectTransform goldRect = goldIcon.GetComponent<RectTransform>();
        goldRect.sizeDelta = new Vector2(10, 10);
        Image goldImg = goldIcon.AddComponent<Image>();
        goldImg.color = new Color(0.95f, 0.85f, 0.15f);

        GameObject goldTextObj = CreateUIElement("GoldText", badgeObj.transform);
        RectTransform textRect = goldTextObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(60, 20);
        TextMeshProUGUI goldTmp = goldTextObj.AddComponent<TextMeshProUGUI>();
        goldTmp.text = "9,999";
        goldTmp.fontSize = 12;
        goldTmp.color = Color.white;
        goldTmp.alignment = TextAlignmentOptions.Left;

        return btnObj;
    }

    private static GameObject CreateTabButton(string name, Transform parent, string labelText, Vector2 pos)
    {
        GameObject tabObj = CreateUIElement(name, parent);
        RectTransform tabRect = tabObj.GetComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0.5f, 0.5f);
        tabRect.anchorMax = new Vector2(0.5f, 0.5f);
        tabRect.anchoredPosition = pos;
        tabRect.sizeDelta = new Vector2(190, 55);

        Image tabImg = tabObj.AddComponent<Image>();
        tabImg.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        tabObj.AddComponent<Button>();

        GameObject hlObj = CreateUIElement("Highlight", tabObj.transform);
        RectTransform hlRect = hlObj.GetComponent<RectTransform>();
        hlRect.anchorMin = new Vector2(0, 0);
        hlRect.anchorMax = new Vector2(1, 0.08f);
        hlRect.offsetMin = Vector2.zero;
        hlRect.offsetMax = Vector2.zero;
        Image hlImg = hlObj.AddComponent<Image>();
        hlImg.color = new Color(0.72f, 0.95f, 0.42f);

        TextMeshProUGUI tmp = CreateTextElement("Text", tabObj.transform, labelText, Color.white, Vector2.zero);
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        SetFullStretch(tmp.GetComponent<RectTransform>());

        return tabObj;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateTextElement(string name, Transform parent, string text, Color color, Vector2 pos)
    {
        GameObject go = CreateUIElement(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120, 30);
        return tmp;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
