using System;
using UnityEngine;

/// <summary>
/// 게임 이벤트를 GameLogger 파일에 기록하는 컴포넌트입니다.
/// GameFlowController와 같은 씬에 배치하세요.
/// </summary>
[DefaultExecutionOrder(-15)]
public class GameEventLogger : MonoBehaviour
{
    private GameFlowController _gfc;
    private TurnStateMachine   _turnSM;
    private DateTime           _levelStart;
    private DateTime           _loopStart;
    private DateTime           _turnStart;
    private int                _currentLoop;

    private void Start()
    {
        if (GameLogger.Instance == null)
        {
            Debug.LogWarning("[GameEventLogger] GameLogger가 없습니다.");
            enabled = false;
            return;
        }

        _gfc = GameFlowController.Instance;
        if (_gfc == null)
        {
            Debug.LogWarning("[GameEventLogger] GameFlowController가 없습니다.");
            enabled = false;
            return;
        }

        _levelStart = DateTime.Now;

        _gfc.OnLoopReset                += HandleLoopReset;
        _gfc.OnGameEndDialogueRequested += HandleFinalDecision;

        _turnSM = _gfc.GetTurnSM();
        if (_turnSM != null)
        {
            _turnSM.OnPlayerActionStarted    += HandlePlayerActionStarted;
            _turnSM.OnTurnEndEntered         += HandleTurnEndEntered;
            _turnSM.OnLoopConditionTriggered += HandleLoopConditionTriggered;
        }
    }

    private void OnDestroy()
    {
        if (_gfc != null)
        {
            _gfc.OnLoopReset                -= HandleLoopReset;
            _gfc.OnGameEndDialogueRequested -= HandleFinalDecision;
        }
        if (_turnSM != null)
        {
            _turnSM.OnPlayerActionStarted    -= HandlePlayerActionStarted;
            _turnSM.OnTurnEndEntered         -= HandleTurnEndEntered;
            _turnSM.OnLoopConditionTriggered -= HandleLoopConditionTriggered;
        }
    }

    // 각 턴의 플레이어 행동 단계 시작
    private void HandlePlayerActionStarted()
    {
        if (_gfc.TurnCount == 1)
        {
            if (_gfc.LoopCount == 1)
            {
                GameLogger.Instance.Log($"[level_start] stage={_gfc.StageId}");
                LogZoneSnapshot();
            }

            _currentLoop = _gfc.LoopCount;
            _loopStart   = DateTime.Now;
            GameLogger.Instance.Log($"[loop_start] loop={_currentLoop}");
        }

        _turnStart = DateTime.Now;
        GameLogger.Instance.Log($"[turn_start] loop={_gfc.LoopCount} turn={_gfc.TurnCount}");
    }

    private void HandleTurnEndEntered(System.Collections.Generic.IReadOnlyList<string> eventLog, bool isLoopCondition)
    {
        TimeSpan duration = DateTime.Now - _turnStart;
        GameLogger.Instance.Log($"[turn_end] loop={_gfc.LoopCount} turn={_gfc.TurnCount} duration={duration:mm\\:ss}");
    }

    // 루프 종료 조건 달성 — 3턴 미완료 강제 루프 전환
    private void HandleLoopConditionTriggered()
    {
        GameLogger.Instance.Log($"[forced_loop]강제루프 loop={_currentLoop} turn={_gfc.TurnCount}");
    }

    // 게임 시작 시 각 존에 있는 캐릭터와 역할 스냅샷
    private void LogZoneSnapshot()
    {
        var gs = _gfc.GameState;
        if (gs == null) return;
        for (int zone = 0; zone < GameState.ZoneCount; zone++)
        {
            var chars = gs.GetCharactersInZone(zone);
            foreach (var c in chars)
                GameLogger.Instance.Log($"[zone_init] zone={zone} char={c.CharacterName} role={gs.GetRole(c.CharacterId)} id={c.CharacterId}");
        }
    }

    // 루프 N 종료 후 새 루프 셋업 진입 시 발생 — 이 시점에 LoopCount는 이미 새 번호
    private void HandleLoopReset()
    {
        int endedLoop = _gfc.LoopCount - 1;
        TimeSpan duration = DateTime.Now - _loopStart;
        GameLogger.Instance.Log($"[loop_end] loop={endedLoop} duration={duration:mm\\:ss}");
    }

    // 최종판정 제출 — 마지막 루프 종료 기록 후 레벨 결과 기록
    private void HandleFinalDecision(bool isWin)
    {
        // 마지막 루프는 OnLoopReset이 오지 않으므로 여기서 직접 기록
        TimeSpan loopDuration = DateTime.Now - _loopStart;
        GameLogger.Instance.Log($"[loop_end] loop={_currentLoop} duration={loopDuration:mm\\:ss}");

        TimeSpan levelDuration = DateTime.Now - _levelStart;
        string tag = isWin ? "level_complete" : "level_fail";
        GameLogger.Instance.Log($"[{tag}] stage={_gfc.StageId} duration={levelDuration:mm\\:ss}");
    }
}
