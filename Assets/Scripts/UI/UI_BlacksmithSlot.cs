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
    public int SlotIndex { get; private set; } = -1;

    private Action<UI_BlacksmithSlot> onClickCallback;
    private Coroutine scaleTweenCoroutine;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnDisable()
    {
        if (scaleTweenCoroutine != null)
        {
            StopCoroutine(scaleTweenCoroutine);
            scaleTweenCoroutine = null;
        }
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 무기 데이터 바인딩
    /// </summary>
    public void Setup(WeaponItemData weapon, int slotIndex, bool isSelected, Action<UI_BlacksmithSlot> onClick)
    {
        BoundWeapon = weapon;
        BoundGem = null;
        SlotIndex = slotIndex;
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
    public void SetupGem(GemItemData gem, int slotIndex, bool isSelected, Action<UI_BlacksmithSlot> onClick)
    {
        BoundGem = gem;
        BoundWeapon = null;
        SlotIndex = slotIndex;
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
        PlayTouchScaleTween();
        onClickCallback?.Invoke(this);
    }

    /// <summary>
    /// 터치/클릭 시 0.88배로 작아졌다가 원래 크기로 돌아오는 트위닝 연출
    /// </summary>
    public void PlayTouchScaleTween()
    {
        if (!gameObject.activeInHierarchy) return;

        if (scaleTweenCoroutine != null)
        {
            StopCoroutine(scaleTweenCoroutine);
        }
        scaleTweenCoroutine = StartCoroutine(CoTouchScaleTween());
    }

    private System.Collections.IEnumerator CoTouchScaleTween()
    {
        Vector3 targetCompressScale = originalScale * 0.88f; // 작아짐
        float compressDuration = 0.06f; // 0.06초 축소
        float returnDuration = 0.12f;   // 0.12초 복원
        float elapsed = 0f;

        // 1. 1.0 -> 0.88 축소
        while (elapsed < compressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / compressDuration);
            float easeT = 1f - (1f - t) * (1f - t); // EaseOutQuad
            transform.localScale = Vector3.Lerp(originalScale, targetCompressScale, easeT);
            yield return null;
        }

        transform.localScale = targetCompressScale;
        elapsed = 0f;

        // 2. 0.88 -> 1.0 원래 크기로 복원
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float easeT = t * t * (3f - 2f * t); // SmoothStep
            transform.localScale = Vector3.Lerp(targetCompressScale, originalScale, easeT);
            yield return null;
        }

        transform.localScale = originalScale;
        scaleTweenCoroutine = null;
    }
}
