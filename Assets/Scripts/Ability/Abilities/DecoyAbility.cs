using UnityEngine;

/// <summary>
/// 미끼 능력: 사망 마크를 같은 구역의 다음 우선순위(오름차순 CharacterId) 캐릭터에게 전달합니다.
/// 같은 구역에 자신보다 높은 ID의 캐릭터가 없으면 면역입니다.
/// 실제 로직은 GameState.ConfirmDeaths()에 구현되어 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "Ability_Decoy", menuName = "MafiaGame/Ability/Decoy")]
public class DecoyAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState) { }
}
