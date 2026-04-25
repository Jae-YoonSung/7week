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
        if (fateB != null && gameState.GetZone(fateB.CharacterId) == myZone)
            return;

        var targets = gameState.GetCharactersInZone(myZone);
        ICharacterStatus lowestTarget = null;
        foreach (var c in targets)
        {
            if (c.CharacterId == ownerId) continue;
            if (lowestTarget == null || c.CharacterId < lowestTarget.CharacterId)
                lowestTarget = c;
        }

        if (lowestTarget == null) return;
        gameState.MarkForDeath(lowestTarget.CharacterId, RoleType.FateA, ownerId);
    }
}
