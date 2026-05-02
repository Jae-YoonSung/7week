using UnityEngine;

/// <summary>
/// 운명 A 능력: 운명 B와 다른 칸에 있을 경우, 자신의 칸에서 CharacterId 오름차순으로 1명을 사망시킵니다.
/// </summary>
[CreateAssetMenu(fileName = "Ability_FateA", menuName = "MafiaGame/Ability/FateA")]
public class FateAAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int myZone = gameState.GetZone(ownerId);

        var fateB = gameState.GetCharacterByRole(RoleType.FateB);
        // FateB가 같은 칸에 있고 마킹되지 않았으면 발동 안 함
        // FateB가 마킹된 상태라면 순교자가 없었다는 의미 → FateA 능력 발동
        if (fateB != null && gameState.GetZone(fateB.CharacterId) == myZone
            && !gameState.IsMarkedForDeath(fateB.CharacterId))
            return;

        var targets = gameState.GetCharactersInZone(myZone);
        ICharacterStatus lowestTarget = null;
        foreach (var c in targets)
        {
            if (c.CharacterId == ownerId) continue;
            if (gameState.IsMarkedForDeath(c.CharacterId)) continue;
            if (lowestTarget == null || c.CharacterId < lowestTarget.CharacterId)
                lowestTarget = c;
        }

        if (lowestTarget == null) return;
        gameState.MarkForDeath(lowestTarget.CharacterId, RoleType.FateA, ownerId);
    }
}
