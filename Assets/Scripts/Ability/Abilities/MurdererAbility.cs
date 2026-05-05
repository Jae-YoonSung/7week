using System.Linq;
using UnityEngine;

/// <summary>
/// 살인자 능력: 같은 구역에 다른 캐릭터가 있으면 1명 사망 지정 (알파벳 순 우선)
/// - 같은 구역에 생존 캐릭터가 없으면 발동 없음
/// </summary>
[CreateAssetMenu(fileName = "AbilityConfig_Murderer", menuName = "MafiaGame/Ability/Murderer")]
public class MurdererAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int zone = gameState.GetZone(ownerId);

        var candidates = gameState.GetCharactersInZone(zone)
            .Where(c => c.CharacterId != ownerId && c.IsAlive)
            .OrderBy(c => c.CharacterName) // 알파벳 순
            .ToList();

        var punisher = gameState.GetCharacterByRole(RoleType.Punisher);
        // 응징자는 이미 마크돼도 공격 대상 포함 (반격 트리거를 위해)
        var target = candidates.FirstOrDefault(c =>
            !gameState.IsMarkedForDeath(c.CharacterId) ||
            (punisher != null && c.CharacterId == punisher.CharacterId));
        if (target == null) return;

        gameState.MarkForDeath(target.CharacterId, RoleType.Murderer, ownerId);
    }
}
