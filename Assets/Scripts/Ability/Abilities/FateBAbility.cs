using UnityEngine;

/// <summary>
/// 운명 B 능력: 운명 A와 같은 칸에 있을 경우, 운명 A와 운명 B 모두 사망시킵니다.
/// </summary>
[CreateAssetMenu(fileName = "Ability_FateB", menuName = "MafiaGame/Ability/FateB")]
public class FateBAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int myZone = gameState.GetZone(ownerId);

        var fateA = gameState.GetCharacterByRole(RoleType.FateA);
        if (fateA == null || gameState.GetZone(fateA.CharacterId) != myZone)
            return;

        gameState.MarkForDeath(fateA.CharacterId, RoleType.FateB, ownerId);
        gameState.MarkForDeath(ownerId, RoleType.FateB, ownerId);
    }
}
