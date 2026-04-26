/// <summary>
/// 게임에 등장하는 직업 종류를 정의합니다.
/// 스테이지별로 등장 여부가 달라질 수 있으며, StageRoleConfig를 통해 관리됩니다.
/// 새 직업 추가 시 이 enum에만 항목을 추가하면 됩니다.
/// </summary>
public enum RoleType
{
    Murderer,    // 살인자
    Deputy,      // 대리자
    Fanatic,     // 광신도
    Protagonist, // 주인공
    FriendA,     // 친구A
    FriendB,     // 친구B
    Variable,    // 변수
    WaterGhost,  // 물귀신
    Martyr,      // 순교자
    LoverC,      // 연인C
    LoverD,      // 연인D
    Punisher,    // 응징자
    ZonePhantom,   // 구역 유령 — 존 슬롯 비점유, 타겟 불가, 존 색 변경
    AbilityBlocker, // 능력 봉인자 — 같은 구역 모든 캐릭터 능력 봉인
    Decoy,          // 미끼 — 같은 구역 타겟 불가, _disableAbilities 효과 받음
    PhantomShifter,  // 유령 이동자 — 대기 시 ZonePhantom을 반시계(−1) 이동
    FateA,           // 운명 A — 운명 B와 다른 칸이면 같은 칸 오름차순 1명 사망
    FateB,           // 운명 B — 운명 A와 같은 칸이면 운명 A·B 모두 사망
    Persister,       // 영속자 — 생존 시 강제 루프 차단
    Bomber,          // 폭탄마 — 대기 시 자신의 칸으로 이동한 캐릭터 사망
    Timid            // 심약자 — 전 루프에 사망자가 있던 턴에 자살
}
