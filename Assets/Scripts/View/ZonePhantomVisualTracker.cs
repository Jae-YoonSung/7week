using UnityEngine;

/// <summary>
/// ZonePhantom 캐릭터가 있는 구역의 색상 인디케이터를 갱신합니다.
/// GameFlowController와 같은 씬에 배치하고 ZoneLayout을 연결하세요.
/// </summary>
public class ZonePhantomVisualTracker : MonoBehaviour
{
    [SerializeField] private ZoneLayout _zoneLayout;

    private GameFlowController _gfc;
    private int _lastPhantomZone = -1;

    private void Start()
    {
        _gfc = GameFlowController.Instance;
        if (_gfc == null) return;

        _gfc.OnLoopReset += Refresh;

        var turnSM = _gfc.GetTurnSM();
        if (turnSM != null)
            turnSM.OnPlayerActionStarted += Refresh;

        Refresh(); // 첫 턴 이벤트를 놓치지 않도록 직접 호출
    }

    private void OnDestroy()
    {
        if (_gfc == null) return;

        _gfc.OnLoopReset -= Refresh;

        var turnSM = _gfc.GetTurnSM();
        if (turnSM != null)
            turnSM.OnPlayerActionStarted -= Refresh;
    }

    private void Refresh()
    {
        var gameState = _gfc.GameState;
        if (gameState == null || _zoneLayout == null) return;

        // 모든 캐릭터 중 ZonePhantom 역할을 찾아 현재 존 확인
        int phantomZone = -1;
        foreach (int id in gameState.GetAllCharacterIds())
        {
            var status = gameState.GetCharacter(id);
            if (status == null || !status.IsAlive) continue;
            if (gameState.GetRole(id) == RoleType.ZonePhantom)
            {
                phantomZone = gameState.GetZone(id);
                break;
            }
        }

        if (phantomZone == _lastPhantomZone) return;

        // 이전 존 비활성화
        if (_lastPhantomZone >= 0)
        {
            var prev = _zoneLayout.GetZonePoint(_lastPhantomZone);
            if (prev != null) prev.SetPhantomPresent(false);
        }

        // 새 존 활성화
        if (phantomZone >= 0)
        {
            var next = _zoneLayout.GetZonePoint(phantomZone);
            if (next != null) next.SetPhantomPresent(true);
        }

        _lastPhantomZone = phantomZone;
    }
}
