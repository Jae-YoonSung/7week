using UnityEngine;
/// <summary>
/// 구역 유령 능력: 같은 구역에 있는 캐릭터 중 CharacterId가 가장 낮은 1명을 사망 마킹합니다.
/// ZonePhantom은 GetCharactersInZone()에서 제외되므로 자신은 후보에 포함되지 않습니다.
/// </summary>
[CreateAssetMenu(fileName = "Ability_ZonePhantom", menuName = "MafiaGame/Ability/ZonePhantom")]
public class ZonePhantomAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int myZone = gameState.GetZone(ownerId);
        var targets = gameState.GetCharactersInZone(myZone);

        ICharacterStatus lowestTarget = null;
        foreach (var c in targets)
        {
            if (lowestTarget == null || c.CharacterId < lowestTarget.CharacterId)
                lowestTarget = c;
        }

        if (lowestTarget == null) return;

        gameState.MarkForDeath(lowestTarget.CharacterId, RoleType.ZonePhantom, ownerId);
    }
}
