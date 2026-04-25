using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 폭탄마 능력: 이동하지 않았을 시, 자신의 칸으로 이동해 온 캐릭터를 모두 사망시킵니다.
/// 파도 등 강제 이동(PreviousZone 미갱신)은 이동으로 간주하지 않습니다.
/// 대상이 여럿일 때 CharacterId 내림차순으로 사망 마크합니다 (대리자 개입 순서 결정).
/// </summary>
[CreateAssetMenu(fileName = "Ability_Bomber", menuName = "MafiaGame/Ability/Bomber")]
public class BomberAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int myZone     = gameState.GetZone(ownerId);
        int myPrevZone = gameState.GetPreviousZone(ownerId);

        if (myZone != myPrevZone) return;

        var targets = new List<ICharacterStatus>();
        foreach (var c in gameState.GetCharactersInZone(myZone))
        {
            if (c.CharacterId == ownerId) continue;
            if (gameState.GetPreviousZone(c.CharacterId) != myZone)
                targets.Add(c);
        }

        targets.Sort((a, b) => b.CharacterId.CompareTo(a.CharacterId));

        if (targets.Count > 0)
            gameState.MarkForDeath(targets[0].CharacterId, RoleType.Bomber, ownerId);
    }
}
