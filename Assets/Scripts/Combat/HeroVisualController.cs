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

    private void Awake()
    {
        // 런타임 Null 에러를 차단하기 위해 자식들의 이미지 컴포넌트 안전 획득
        if (bodyVisual != null) bodyImage = bodyVisual.GetComponent<Image>();
        if (weaponVisual != null) weaponImage = weaponVisual.GetComponent<Image>();
    }

    private void LateUpdate()
    {
        // [기획 핵심 규칙]: 프레임이 밀려 찢어지는 현상(Lagging)을 원천 방지하기 위해 
        // LateUpdate 타이밍에 애니메이터에 의해 움직이는 Hand_Anchor의 위치와 회전을 무기가 복사싱크함
        if (handAnchor != null && weaponVisual != null)
        {
            weaponVisual.position = handAnchor.position;
            weaponVisual.rotation = handAnchor.rotation;
        }
    }

    /// <summary>
    /// [외형 스킨 변경용 독립 API] - 무기 설정에 영향을 미치지 않고 캐릭터의 몸체 스프라이트와 컨트롤러만 교체합니다.
    /// </summary>
    public void ChangeSkin(Sprite newSkinSprite, RuntimeAnimatorController newController)
    {
        if (bodyImage != null)
        {
            bodyImage.sprite = newSkinSprite;
        }

        if (bodyVisual != null)
        {
            Animator animator = bodyVisual.GetComponent<Animator>();
            if (animator != null && newController != null)
            {
                animator.runtimeAnimatorController = newController;
            }
        }
    }

    /// <summary>
    /// [장비 무기 변경용 독립 API] - 외형 스킨에 영향을 미치지 않고 무기 스프라이트 이미지의 형태만 교체합니다.
    /// </summary>
    public void ChangeWeapon(Sprite newWeaponSprite)
    {
        if (weaponImage != null)
        {
            weaponImage.sprite = newWeaponSprite;
        }
    }

    /// <summary>
    /// 타겟 적의 위치에 맞게 고블린의 몸통 크기 스케일 부호를 유지하며 좌우 뒤집기(Flip) 처리를 가합니다.
    /// </summary>
    /// <param name="lookLeft">true 이면 왼쪽 방향을, false 이면 오른쪽 방향을 보게 뒤집음</param>
    public void SetFacingDirection(bool lookLeft)
    {
        Vector3 scale = transform.localScale;
        // X축 스케일 절댓값 부호만 조절하여 스케일 크기 변동 없이 방향만 대칭 반전
        scale.x = lookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
