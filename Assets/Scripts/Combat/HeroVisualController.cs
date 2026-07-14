using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아군 히어로 고블린의 몸체 스킨(Body)과 장착된 무기(Weapon) 외형을 독립적으로 제어하고,
/// 애니메이터(Animator) 손 피벗 프레임 궤적을 무기가 실시간으로 추적하도록 돕는 모듈형 비주얼 컨트롤러입니다.
/// </summary>
public class HeroVisualController : MonoBehaviour
{
    [Header("[파츠 계층 구조 참조]")]
    [Tooltip("고블린 본체 외형 이미지(Image)가 포함된 자식 오브젝트의 트랜스폼입니다.")]
    public Transform bodyVisual;

    [Tooltip("본체 외형의 자식 오브젝트로 내장되어 애니메이션 궤적을 굽히는 빈 손 앵커 트랜스폼입니다.")]
    public Transform handAnchor;

    [Tooltip("장착된 무기 이미지(Image)가 포함된 자식 오브젝트의 트랜스폼입니다.")]
    public Transform weaponVisual;

    private Image bodyImage;
    private Image weaponImage;
    private Animator animator;

    // 애니메이터 내부 파라미터 존재 여부 안전 검사용 플래그
    private bool hasIsMovingParam = false;
    private bool hasAttackParam = false;

    private void Awake()
    {
        InitComponents();
    }

    /// <summary>
    /// 컴포넌트 참조 지연 획득 및 런타임 Null 에러 안전 방어용 초기화 메서드입니다.
    /// </summary>
    private void InitComponents()
    {
        if (bodyVisual != null)
        {
            if (bodyImage == null) bodyImage = bodyVisual.GetComponent<Image>();
            if (animator == null)
            {
                animator = bodyVisual.GetComponent<Animator>();
                CheckParameters();
            }
        }
        if (weaponVisual != null)
        {
            if (weaponImage == null) weaponImage = weaponVisual.GetComponent<Image>();
        }
    }

    /// <summary>
    /// 애니메이터 컨트롤러에 필요한 파라미터가 등록되어 있는지 안전 검사합니다.
    /// </summary>
    private void CheckParameters()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            hasIsMovingParam = false;
            hasAttackParam = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "isMoving") hasIsMovingParam = true;
                if (param.name == "Attack") hasAttackParam = true;
            }
        }
    }

    private void LateUpdate()
    {
        // [기획 핵심 규칙]: 프레임 밀림 현상(Lagging) 방지를 위해 LateUpdate 시점에 손 앵커의 위치/회전 복사
        if (handAnchor != null && weaponVisual != null)
        {
            weaponVisual.position = handAnchor.position;
            weaponVisual.rotation = handAnchor.rotation;
        }
    }

    /// <summary>
    /// 이동 애니메이션 상태를 조율합니다 (isMoving 파라미터).
    /// </summary>
    public void SetMoveAnimation(bool isMoving)
    {
        if (animator == null) InitComponents();

        // 파라미터가 실제 애니메이터에 등록되어 있을 때만 에러 없이 안전하게 호출
        if (animator != null && hasIsMovingParam)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }

    /// <summary>
    /// 공격 애니메이션 트리거를 발동시킵니다 (Attack 트리거).
    /// </summary>
    public void TriggerAttackAnimation()
    {
        if (animator == null) InitComponents();

        // 파라미터가 실제 애니메이터에 등록되어 있을 때만 에러 없이 안전하게 호출
        if (animator != null && hasAttackParam)
        {
            animator.SetTrigger("Attack");
            Debug.Log("<color=yellow>[HeroVisualController] Attack 트리거가 정상 가동되었습니다!</color>");
        }
    }

    /// <summary>
    /// [외형 스킨 변경용 독립 API] - 무기 설정에 영향을 미치지 않고 캐릭터의 몸체 스프라이트와 컨트롤러만 교체합니다.
    /// </summary>
    public void ChangeSkin(Sprite newSkinSprite, RuntimeAnimatorController newController)
    {
        InitComponents();
        if (bodyImage != null)
        {
            bodyImage.sprite = newSkinSprite;
        }

        if (animator != null && newController != null)
        {
            animator.runtimeAnimatorController = newController;
            CheckParameters(); // 새로운 컨트롤러 교체 시 파라미터 상태 다시 재검사
        }
    }

    /// <summary>
    /// [장비 무기 변경용 독립 API] - 외형 스킨에 영향을 미치지 않고 무기 스프라이트 이미지의 형태만 교체합니다.
    /// </summary>
    public void ChangeWeapon(Sprite newWeaponSprite)
    {
        InitComponents();
        if (weaponImage != null)
        {
            weaponImage.sprite = newWeaponSprite;
        }
    }

    /// <summary>
    /// 타겟 적의 위치에 맞게 고블린의 몸통 크기 스케일 부호를 유지하며 좌우 뒤집기(Flip) 처리를 가합니다.
    /// </summary>
    public void SetFacingDirection(bool lookLeft)
    {
        Vector3 scale = transform.localScale;
        scale.x = lookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
