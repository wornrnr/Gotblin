using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛 크기 및 진영/보스 여부에 맞춰 머리 위에 차등 크기의 체력바를 표시하고,
/// 히어로 고블린 및 보스 몬스터의 경우 중앙에 남은 체력 퍼센트(%) 텍스트를 실시간 출력하는 UI 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public class UnitHealthBarUI : MonoBehaviour
{
    private BaseCombatUnit ownerUnit;

    private RectTransform rectTransform;
    private RectTransform fillRect;
    private Image bgImage;
    private Image fillImage;
    private TextMeshProUGUI percentText;

    // 진영/타입별 체력 바 fill 색상 정의
    private static readonly Color ColorHeroHP = new Color32(0x2E, 0xCC, 0x71, 0xFF);   // Emerald Green
    private static readonly Color ColorBossHP = new Color32(0x9B, 0x59, 0xB6, 0xFF);   // Purple/Magenta
    private static readonly Color ColorEnemyHP = new Color32(0xE7, 0x4C, 0x3C, 0xFF);  // Red

    /// <summary>
    /// 유닛 정보를 바탕으로 체력바 UI 구조와 크기, 텍스트 활성화 여부를 동적으로 셋업합니다.
    /// </summary>
    public void Init(BaseCombatUnit owner)
    {
        ownerUnit = owner;
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        EnsureComponents();
        ApplyStyleAndSizing();

        if (ownerUnit != null)
        {
            UpdateHealthBar(ownerUnit.currentHP, ownerUnit.maxHP);
        }
    }

    /// <summary>
    /// UGUI Image 및 TextMeshProUGUI 계층 구조를 동적으로 구축합니다.
    /// </summary>
    private void EnsureComponents()
    {
        // 1. Background Image
        bgImage = GetComponent<Image>();
        if (bgImage == null)
        {
            bgImage = gameObject.AddComponent<Image>();
        }
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgImage.raycastTarget = false;

        // 2. Fill Image Child
        Transform fillObj = transform.Find("Fill");
        if (fillObj == null)
        {
            GameObject go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            fillObj = go.transform;
        }

        fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        fillImage = fillObj.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;

        // 3. Percentage Text Child
        Transform textObj = transform.Find("PercentText");
        if (textObj == null)
        {
            GameObject go = new GameObject("PercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            textObj = go.transform;
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        percentText = textObj.GetComponent<TextMeshProUGUI>();
        percentText.alignment = TextAlignmentOptions.Center;
        percentText.fontStyle = FontStyles.Bold;
        percentText.color = Color.white;
        percentText.raycastTarget = false;
        percentText.enableWordWrapping = false;
    }

    /// <summary>
    /// 유닛 덩치 크기(bodyRadius) 및 진영/보스 타입에 맞춘 차등 크기 및 색상 적용
    /// </summary>
    private void ApplyStyleAndSizing()
    {
        if (ownerUnit == null) return;

        Vector2 barSize;
        float fontSize;
        Color hpColor;
        bool showText;
        float yOffset;

        if (ownerUnit.isBoss)
        {
            // 1. 보스 몬스터: 대형 체력 바 (180x20), 보라색 fill, 텍스트 표출
            barSize = new Vector2(180f, 20f);
            fontSize = 13f;
            hpColor = ColorBossHP;
            showText = true;
            yOffset = ownerUnit.bodyRadius + 40f;
        }
        else if (!ownerUnit.isEnemy)
        {
            // 2. 히어로 고블린: 중형 체력 바 (110x16), 에메랄드 초록 fill, 텍스트 표출
            barSize = new Vector2(110f, 16f);
            fontSize = 11f;
            hpColor = ColorHeroHP;
            showText = true;
            yOffset = ownerUnit.bodyRadius + 35f;
        }
        else
        {
            // 3. 일반 몬스터: 소형 체력 바 (80x10), 레드 fill, 텍스트 비활성화
            barSize = new Vector2(80f, 10f);
            fontSize = 9f;
            hpColor = ColorEnemyHP;
            showText = false; // 일반 몬스터는 텍스트 미표시
            yOffset = ownerUnit.bodyRadius + 25f;
        }

        rectTransform.sizeDelta = barSize;
        rectTransform.anchoredPosition = new Vector2(0f, yOffset);
        fillImage.color = hpColor;

        if (percentText != null)
        {
            percentText.fontSize = fontSize;
            percentText.gameObject.SetActive(showText);
        }
    }

    /// <summary>
    /// 체력 변경 시 fillAmount 및 anchorMax.x 수치를 실시간 연산하여 게이지 바 너비를 줄여줍니다.
    /// </summary>
    public void UpdateHealthBar(int currentHP, int maxHP)
    {
        if (maxHP <= 0) return;

        float fillRatio = Mathf.Clamp01((float)currentHP / maxHP);

        // 1. UGUI AnchorMax 기반 Fill 너비 픽셀 감소 연동
        if (fillRect == null && fillImage != null)
        {
            fillRect = fillImage.GetComponent<RectTransform>();
        }

        if (fillRect != null)
        {
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(fillRatio, 1f);
            fillRect.sizeDelta = Vector2.zero;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = fillRatio;
        }

        // 2. 남은 체력 비율 퍼센트 텍스트 갱신
        if (percentText != null && percentText.gameObject.activeSelf)
        {
            int percent = Mathf.Clamp(Mathf.RoundToInt(fillRatio * 100f), 0, 100);
            percentText.text = $"{percent}%";
        }
    }

    private void LateUpdate()
    {
        // 부모 유닛이 피격 펄스로 확대/축소되거나 바라보는 방향(X부호)이 바뀌더라도,
        // 체력 바 본체는 피격 펄스의 확장이 전달되지 않고 항시 정방향 1.0 크기를 유지하도록 스케일 역산 보정
        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.localScale;

            float parentAbsX = Mathf.Abs(parentScale.x);
            float parentAbsY = Mathf.Abs(parentScale.y);

            if (parentAbsX > 0.001f && parentAbsY > 0.001f)
            {
                float signX = parentScale.x < 0 ? -1f : 1f;
                float signY = parentScale.y < 0 ? -1f : 1f;

                // 부모의 확대/축소(펄스) 크기를 나눠주어 체력바의 실효 월드 스케일을 항시 1.0으로 고정
                transform.localScale = new Vector3(signX / parentAbsX, signY / parentAbsY, 1f);
            }
        }
    }
}
