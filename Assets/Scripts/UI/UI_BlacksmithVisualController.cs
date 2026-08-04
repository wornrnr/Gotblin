using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_BlacksmithPanel의 LeftVisualPanel 내부 대장간 배경, 모루, 고블린 대장장이의 
/// 비주얼 연출 및 애니메이션을 제어하는 컨트롤러입니다. (Static Prefab 방식)
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithVisualController : MonoBehaviour
{
    [Header("UI Image Elements (Prefab References)")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image anvilImage;
    [SerializeField] private Image goblinImage;

    [Header("Sprite References")]
    [SerializeField] private Sprite[] idleSprites;      // 4 frames
    [SerializeField] private Sprite[] hammeringSprites; // 4 frames (Ready, Windup, Strike, Recoil)

    private Coroutine activeAnimCoroutine;
    private bool isHammering = false;

    private void Awake()
    {
        LoadSpritesIfNull();
    }

    private void OnEnable()
    {
        LoadSpritesIfNull();
        StartIdleAnimation();
    }

    private void OnDisable()
    {
        if (activeAnimCoroutine != null)
        {
            StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = null;
        }
        isHammering = false;
    }

    /// <summary>
    /// 애니메이션을 위한 고블린 스프라이트 시트 슬라이싱 및 캐싱
    /// </summary>
    public void LoadSpritesIfNull()
    {
        // Goblin Smith Sheet Slicing (Idle & Hammering)
        if (idleSprites == null || idleSprites.Length < 4 || hammeringSprites == null || hammeringSprites.Length < 4)
        {
            Sprite[] sheetSprites = Resources.LoadAll<Sprite>("Sprite/Goblin_Blacksmith_Sheet");
            idleSprites = new Sprite[4];
            hammeringSprites = new Sprite[4];

            if (sheetSprites != null && sheetSprites.Length > 0)
            {
                foreach (var s in sheetSprites)
                {
                    if (s.name == "Goblin_Idle_0") idleSprites[0] = s;
                    else if (s.name == "Goblin_Idle_1") idleSprites[1] = s;
                    else if (s.name == "Goblin_Idle_2") idleSprites[2] = s;
                    else if (s.name == "Goblin_Idle_3") idleSprites[3] = s;
                    else if (s.name == "Goblin_Hammer_0") hammeringSprites[0] = s;
                    else if (s.name == "Goblin_Hammer_1") hammeringSprites[1] = s;
                    else if (s.name == "Goblin_Hammer_2") hammeringSprites[2] = s;
                    else if (s.name == "Goblin_Hammer_3") hammeringSprites[3] = s;
                }
            }

            // Fallback (메타데이터가 없어서 슬라이스가 안 된 경우)
            if (idleSprites[0] == null || hammeringSprites[0] == null)
            {
                Texture2D sheetTex = Resources.Load<Texture2D>("Sprite/Goblin_Blacksmith_Sheet");
                if (sheetTex != null)
                {
                    int frameW = 64;
                    int frameH = 64;
                    for (int f = 0; f < 4; f++)
                    {
                        idleSprites[f] = Sprite.Create(sheetTex, new Rect(f * frameW, frameH, frameW, frameH), new Vector2(0.5f, 0.5f), 16f);
                        hammeringSprites[f] = Sprite.Create(sheetTex, new Rect(f * frameW, 0, frameW, frameH), new Vector2(0.5f, 0.5f), 16f);
                    }
                }
            }
        }

        if (goblinImage != null && idleSprites != null && idleSprites.Length > 0 && goblinImage.sprite == null)
        {
            goblinImage.sprite = idleSprites[0];
        }
    }

    /// <summary>
    /// 대기(Idle) 숨쉬기 루프 애니메이션 시작
    /// </summary>
    public void StartIdleAnimation()
    {
        if (isHammering) return;
        if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
        activeAnimCoroutine = StartCoroutine(IdleRoutine());
    }

    private IEnumerator IdleRoutine()
    {
        int frameIndex = 0;
        float frameInterval = 0.25f;

        while (!isHammering)
        {
            if (idleSprites != null && idleSprites.Length > 0 && goblinImage != null)
            {
                goblinImage.sprite = idleSprites[frameIndex % idleSprites.Length];
                frameIndex++;
            }
            yield return new WaitForSeconds(frameInterval);
        }
    }

    /// <summary>
    /// 강화 망치질 연출 (2회 타격, 1초 이내 완료)
    /// </summary>
    public void PlayEnhanceHammerSequence(System.Action onStrike, System.Action onComplete)
    {
        if (isHammering) return;
        if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
        activeAnimCoroutine = StartCoroutine(HammerSequenceRoutine(onStrike, onComplete));
    }

    private IEnumerator HammerSequenceRoutine(System.Action onStrike, System.Action onComplete)
    {
        isHammering = true;
        LoadSpritesIfNull();

        bool hasHammerSprites = (hammeringSprites != null && hammeringSprites.Length >= 4 && goblinImage != null);

        // Strike 1
        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[0]; // Ready
        yield return new WaitForSeconds(0.12f);

        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[1]; // Windup
        yield return new WaitForSeconds(0.12f);

        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[2]; // STRIKE 1!
        onStrike?.Invoke();
        yield return new WaitForSeconds(0.15f);

        // Strike 2
        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[1]; // Windup 2
        yield return new WaitForSeconds(0.12f);

        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[2]; // STRIKE 2!
        onStrike?.Invoke();
        yield return new WaitForSeconds(0.18f);

        if (hasHammerSprites) goblinImage.sprite = hammeringSprites[3]; // Recoil
        yield return new WaitForSeconds(0.12f);

        onComplete?.Invoke();

        isHammering = false;
        StartIdleAnimation();
    }
}
