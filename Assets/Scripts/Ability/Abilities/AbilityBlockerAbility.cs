using UnityEngine;
/// <summary>
/// 능력 봉인자: 자신이 있는 구역의 모든 캐릭터 능력을 이번 턴 봉인합니다.
/// RoleActivationOrderConfig에서 가장 먼저 실행되도록 배치하세요.
/// </summary>
[CreateAssetMenu(fileName = "Ability_AbilityBlocker", menuName = "MafiaGame/Ability/AbilityBlocker")]
public class AbilityBlockerAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        int myZone = gameState.GetZone(ownerId);
        gameState.DisableAbilitiesInZone(myZone);
    }
}
