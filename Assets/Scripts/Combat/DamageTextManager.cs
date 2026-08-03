using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 전투 피해량(데미지 수치) 연출 오브젝트 풀을 관리하고, 
/// 피격 지점 상단에 떠오르는 데미지 텍스트 생성을 총괄하는 전역 매니저 싱글톤입니다.
/// </summary>
[DisallowMultipleComponent]
public class DamageTextManager : MonoBehaviour
{
    private static DamageTextManager instance;
    public static DamageTextManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<DamageTextManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DamageTextManager", typeof(DamageTextManager));
                    instance = go.GetComponent<DamageTextManager>();
                }
            }
            return instance;
        }
    }

    [Header("폰트 에셋 설정")]
    [Tooltip("데미지 텍스트에 적용할 TMP Font Asset입니다. 지정되지 않으면 기본 폰트를 탐색합니다.")]
    [SerializeField] private TMP_FontAsset fontAsset;

    private Transform textContainer;
    private Queue<DamageTextFX> textPool = new Queue<DamageTextFX>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitContainerAndFont();
    }

    /// <summary>
    /// UI 텍스트 부모 컨테이너 및 폰트 에셋을 확보합니다.
    /// </summary>
    private void InitContainerAndFont()
    {
        // 1. 폰트 에셋 자동 탐색 (미지정 시)
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("NeoDunggeunmoPro-Regular SDF");
            if (fontAsset == null)
            {
                fontAsset = TMPro.TMP_Settings.defaultFontAsset;
            }
            if (fontAsset == null)
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts != null && fonts.Length > 0)
                {
                    fontAsset = fonts[0];
                }
            }
        }

        // 2. 텍스트 부모 컨테이너(Transform) 정렬
        if (CombatStageManager.Instance != null && CombatStageManager.Instance.battleBackground != null)
        {
            textContainer = CombatStageManager.Instance.battleBackground.parent;
        }

        if (textContainer == null)
        {
            Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                textContainer = mainCanvas.transform;
            }
            else
            {
                textContainer = transform;
            }
        }
    }

    /// <summary>
    /// 외부에서 데미지 수치를 피격 위치 상단에 출력할 때 호출하는 전역 API입니다.
    /// </summary>
    /// <param name="amount">피해량 수치</param>
    /// <param name="isHeroAttacking">true: 히어로 ➡️ 적 (오렌지/대형), false: 적 ➡️ 히어로 (레드/소형)</param>
    /// <param name="targetWorldPos">피격당한 유닛의 위치 좌표</param>
    public void ShowDamageText(int amount, bool isHeroAttacking, Vector3 targetWorldPos)
    {
        // 최적화: 전투 탭 화면이 활성화되지 않은 상태에서는 보이지 않으므로 팝업 연산을 생략합니다.
        if (MainScreenManager.Instance != null && !MainScreenManager.Instance.IsCombatPanelActive)
        {
            return;
        }

        if (textContainer == null)
        {
            InitContainerAndFont();
        }

        DamageTextFX textFX = GetTextFromPool();
        if (textFX != null)
        {
            textFX.gameObject.SetActive(true);
            textFX.Play(amount, isHeroAttacking, targetWorldPos, ReturnTextToPool);
        }
    }

    /// <summary>
    /// 풀에서 FX 컴포넌트를 가져오거나 없으면 새로 생성합니다.
    /// </summary>
    private DamageTextFX GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            DamageTextFX text = textPool.Dequeue();
            if (text != null)
            {
                return text;
            }
        }

        // 새 텍스트 오브젝트 생성
        GameObject obj = new GameObject("DamageTextFX", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(DamageTextFX));
        obj.transform.SetParent(textContainer, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 60f);

        DamageTextFX newFX = obj.GetComponent<DamageTextFX>();
        if (fontAsset != null)
        {
            newFX.SetFontAsset(fontAsset);
        }

        return newFX;
    }

    /// <summary>
    /// 연출이 완료된 FX 오브젝트를 풀로 반납합니다.
    /// </summary>
    private void ReturnTextToPool(DamageTextFX textFX)
    {
        if (textFX == null) return;

        textFX.gameObject.SetActive(false);
        if (!textPool.Contains(textFX))
        {
            textPool.Enqueue(textFX);
        }
    }
}
