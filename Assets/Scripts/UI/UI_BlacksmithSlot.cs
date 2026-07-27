using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대장간 UI 인벤토리 5x3 그리드 내 개별 아이템 슬롯 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private GameObject highlightBorder;
    [SerializeField] private Button slotButton;

    public WeaponItemData BoundWeapon { get; private set; }
    public GemItemData BoundGem { get; private set; }

    private Action<UI_BlacksmithSlot> onClickCallback;

    private void Awake()
    {
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// 무기 데이터 바인딩
    /// </summary>
    public void Setup(WeaponItemData weapon, bool isSelected, Action<UI_BlacksmithSlot> onClick)
    {
        BoundWeapon = weapon;
        BoundGem = null;
        onClickCallback = onClick;

        if (weapon != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = weapon.iconSprite;
                iconImage.gameObject.SetActive(weapon.iconSprite != null);
            }
            if (gradeText != null)
            {
                gradeText.text = $"+{weapon.grade}";
                gradeText.gameObject.SetActive(weapon.grade > 0);
            }
        }
        else
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (gradeText != null) gradeText.gameObject.SetActive(false);
        }

        SetSelected(isSelected);
    }

    /// <summary>
    /// 보석 데이터 바인딩
    /// </summary>
    public void SetupGem(GemItemData gem, bool isSelected, Action<UI_BlacksmithSlot> onClick)
    {
        BoundGem = gem;
        BoundWeapon = null;
        onClickCallback = onClick;

        if (gem != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = gem.iconSprite;
                iconImage.gameObject.SetActive(gem.iconSprite != null);
            }
            if (gradeText != null)
            {
                gradeText.text = $"Lv.{gem.level}";
                gradeText.gameObject.SetActive(gem.level > 0);
            }
        }
        else
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (gradeText != null) gradeText.gameObject.SetActive(false);
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }
    }

    private void OnSlotClicked()
    {
        onClickCallback?.Invoke(this);
    }
}
