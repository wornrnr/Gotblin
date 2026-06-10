/// <summary>
/// 소셜 그래프 미니게임의 진행 상태를 정의하는 열거형입니다.
/// </summary>
public enum GraphGameState
{
    /// <summary>
    /// 게임 시작 전 대기 상태
    /// </summary>
    Ready,

    /// <summary>
    /// 타이머가 작동하며 배수가 실시간으로 상승 중인 상태
    /// </summary>
    Running,

    /// <summary>
    /// 유저가 타이머 도달 전 안전하게 '그만(CashOut)'을 눌러 성공한 상태
    /// </summary>
    Success,

    /// <summary>
    /// 유저가 탈출하기 전에 정지 시간에 도달하여 실패(파산)한 상태
    /// </summary>
    Busted
}
