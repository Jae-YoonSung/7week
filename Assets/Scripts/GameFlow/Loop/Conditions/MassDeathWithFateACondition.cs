using UnityEngine;

/// <summary>
/// 루프 강제 종료 조건: 한 턴에 2명 이상 사망 AND 운명A의 사망이 운명B에 의한 것이 아닌 경우.
///
/// 판정 규칙:
///   - DeathsThisTurn < 2           → 미발동
///   - 운명A가 이번 턴에 사망하지 않음  → 미발동
///   - 운명A가 운명B에 의해 사망       → 미발동 (운명A+B 상호 사망은 정상 시나리오)
///   - 위 예외를 모두 통과              → 루프 강제 종료
/// </summary>
[CreateAssetMenu(fileName = "Condition_MassDeathWithFateA",
                 menuName  = "MafiaGame/LoopCondition/MassDeathWithFateA")]
public class MassDeathWithFateACondition : LoopConditionConfig
{
    [Tooltip("이번 턴 최소 사망자 수. 기본값 2.")]
    [SerializeField] private int _minDeaths = 2;

    public override bool ShouldLoop(GameState gameState)
    {
        if (gameState.DeathsThisTurn < _minDeaths)
            return false;

        bool fateADiedThisTurn = false;
        bool fateAKilledByFateB = false;

        foreach (var record in gameState.GetAllDeathMarks())
        {
            if (gameState.GetRole(record.TargetCharacterId) != RoleType.FateA)
                continue;

            fateADiedThisTurn  = true;
            fateAKilledByFateB = record.CauseRole == RoleType.FateB;
            break;
        }

        if (!fateADiedThisTurn)
        {
            Debug.Log("[LoopCondition] 미발동 — 2명 이상 사망이나 운명A 생존");
            return false;
        }

        if (fateAKilledByFateB)
        {
            Debug.Log("[LoopCondition] 미발동 — 운명A 사망이지만 운명B에 의한 것");
            return false;
        }

        Debug.Log($"[LoopCondition] 루프 종료 — {gameState.DeathsThisTurn}명 사망, 운명A가 운명B 이외 원인으로 사망");
        return true;
    }
}
