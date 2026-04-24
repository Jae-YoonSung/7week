using UnityEngine;

/// <summary>
/// 루프 종료 조건: 이번 턴 아무도 죽지 않음 OR 배회자(광신도) 사망 시 루프를 리셋합니다.
/// </summary>
[CreateAssetMenu(fileName = "Condition_NoDeathOrFanaticDeath", menuName = "MafiaGame/LoopCondition/NoDeathOrFanaticDeath")]
public class NoDeathOrFanaticDeathCondition : LoopConditionConfig
{
    public override bool ShouldLoop(GameState gameState)
    {
        if (gameState.DeathsThisTurn == 0)
        {
            Debug.Log("[LoopCondition] 루프 종료 — 이번 턴 사망자 없음");
            return true;
        }

        bool fanaticDead = gameState.GetCharacterByRole(RoleType.Fanatic) == null;
        if (fanaticDead)
        {
            Debug.Log("[LoopCondition] 루프 종료 — 배회자(광신도) 사망");
            return true;
        }

        return false;
    }
}
