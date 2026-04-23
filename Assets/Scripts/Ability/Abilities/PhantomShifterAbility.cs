using UnityEngine;
/// <summary>
/// 유령 이동자 능력: 이번 턴 이동하지 않았을 때 ZonePhantom을 반시계(−1) 방향으로 한 칸 이동시킵니다.
/// 이동 판정은 WaterGhostAbility와 동일 (CurrentZone == PreviousZone).
/// Zone 0에서 −1 하면 Zone 3이 됩니다.
/// </summary>
[CreateAssetMenu(fileName = "Ability_PhantomShifter", menuName = "MafiaGame/Ability/PhantomShifter")]
public class PhantomShifterAbility : AbilityConfig
{
    public override void Execute(int ownerId, IGameState gameState)
    {
        if (gameState.GetZone(ownerId) != gameState.GetPreviousZone(ownerId)) return;

        (gameState as GameState)?.MovePhantomCounterClockwise();
    }
}
