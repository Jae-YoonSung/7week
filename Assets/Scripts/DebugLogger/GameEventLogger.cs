using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 흐름 이벤트를 구독해 GameEventDispatcher로 전달하는 컴포넌트.
// GameFlowController / TurnStateMachine의 이벤트를 구독하고,
// 각 핸들러에서 이벤트 구조체를 생성해 Dispatcher에 Raise한다.
// 신규 기능(기물 이동·추리 등)용 퍼블릭 메서드는 게임 시스템 연결 전이므로 stub 상태로 존재한다.
[DefaultExecutionOrder(-15)]
public class GameEventLogger : MonoBehaviour
{
    private GameFlowController _gfc;
    private TurnStateMachine   _turnSM;

    private DateTime _levelStart;
    private DateTime _loopStart;
    private DateTime _turnStart;
    private int      _currentLoop;

    // 레벨이 정상 종료(클리어·실패)됐는지 추적한다. 앱 강제 종료 시 이탈 이벤트 판별에 사용된다.
    private bool _levelEnded;

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

    // 앱이 종료될 때 레벨이 아직 진행 중이면 이탈로 기록한다.
    private void OnApplicationQuit()
    {
        if (!_levelEnded && _gfc != null)
            GameEventDispatcher.Raise(new LevelAbandonEvent(_gfc.StageId, _gfc.LoopCount, _gfc.TurnCount));
    }

    // ── 내부 핸들러 ──────────────────────────────────────────────────────────

    // 각 턴의 플레이어 행동 단계 시작. 루프 첫 턴이면 루프 시작도 함께 기록한다.
    private void HandlePlayerActionStarted()
    {
        if (_gfc.TurnCount == 1)
        {
            if (_gfc.LoopCount == 1)
            {
                _levelStart = DateTime.Now;
                GameEventDispatcher.Raise(new LevelStartEvent(_gfc.StageId));
                RaiseZoneSnapshot();
            }

            _currentLoop = _gfc.LoopCount;
            _loopStart   = DateTime.Now;
            GameEventDispatcher.Raise(new LoopStartEvent(_currentLoop));
        }

        _turnStart = DateTime.Now;
        GameEventDispatcher.Raise(new TurnStartEvent(_gfc.LoopCount, _gfc.TurnCount));
    }

    // 턴 종료 상태 진입 시 소요 시간과 함께 기록한다.
    private void HandleTurnEndEntered(IReadOnlyList<string> eventLog, bool isLoopCondition)
    {
        TimeSpan duration = DateTime.Now - _turnStart;
        GameEventDispatcher.Raise(new TurnEndEvent(_gfc.LoopCount, _gfc.TurnCount, duration));
    }

    // 3턴 미완료로 강제 루프 전환이 발생했을 때 기록한다.
    private void HandleLoopConditionTriggered()
    {
        GameEventDispatcher.Raise(new ForcedLoopEvent(_currentLoop, _gfc.TurnCount, "turn_limit"));
    }

    // 루프 N이 종료된 뒤 새 루프 셋업에 진입할 때 호출된다. LoopCount는 이미 새 번호다.
    private void HandleLoopReset()
    {
        int endedLoop = _gfc.LoopCount - 1;
        TimeSpan duration = DateTime.Now - _loopStart;
        GameEventDispatcher.Raise(new LoopEndEvent(endedLoop, duration));
    }

    // 최종 판정 제출 시 마지막 루프 종료와 레벨 결과를 기록한다.
    // OnLoopReset은 마지막 루프에서 호출되지 않으므로 여기서 직접 처리한다.
    private void HandleFinalDecision(bool isWin)
    {
        _levelEnded = true;

        TimeSpan loopDuration  = DateTime.Now - _loopStart;
        GameEventDispatcher.Raise(new LoopEndEvent(_currentLoop, loopDuration));

        TimeSpan levelDuration = DateTime.Now - _levelStart;
        GameEventDispatcher.Raise(new LevelEndEvent(_gfc.StageId, isWin, levelDuration));
    }

    // 게임 시작 시 각 존에 있는 캐릭터와 역할을 스냅샷으로 기록한다.
    private void RaiseZoneSnapshot()
    {
        var gs = _gfc.GameState;
        if (gs == null) return;
        for (int zone = 0; zone < GameState.ZoneCount; zone++)
        {
            foreach (var c in gs.GetCharactersInZone(zone))
                GameEventDispatcher.Raise(new ZoneInitEvent(zone, c.CharacterName, gs.GetRole(c.CharacterId).ToString(), c.CharacterId.ToString()));
        }
    }

    // ── 외부 연결용 퍼블릭 stub ─────────────────────────────────────────────
    // 아래 메서드들은 각 게임 시스템이 완성된 후 해당 시스템에서 직접 호출한다.

    // 로비에서 스테이지를 선택할 때 호출한다. order는 이번 세션의 선택 순번이다.
    public void LogStageSelect(string stageId, int order)
        => GameEventDispatcher.Raise(new StageSelectEvent(stageId, order));

    // 플레이어가 기물을 이동시킬 때 호출한다.
    public void LogPieceMove(string charId, int fromZone, int toZone, int moveIndex)
        => GameEventDispatcher.Raise(new PieceMoveEvent(_gfc.LoopCount, _gfc.TurnCount, charId, fromZone, toZone, moveIndex));

    // 기물이 특수 구역에 진입할 때 호출한다.
    public void LogSpecialZone(string charId, int zone, string zoneType)
        => GameEventDispatcher.Raise(new SpecialZoneEvent(_gfc.LoopCount, _gfc.TurnCount, charId, zone, zoneType));

    // 플레이어가 마감일 추리 선택지를 제출할 때 호출한다.
    public void LogAnswerSubmit(string selectedId, string correctId, bool isCorrect)
        => GameEventDispatcher.Raise(new AnswerSubmitEvent(_gfc.StageId, _gfc.LoopCount, selectedId, correctId, isCorrect));

    // 플레이어가 특정 역할을 확신했다고 표시할 때 호출한다.
    public void LogRoleConfirmed(string charId, string suspectedRole)
        => GameEventDispatcher.Raise(new RoleConfirmedEvent(_gfc.LoopCount, _gfc.TurnCount, charId, suspectedRole));

    // 룰북을 열 때 호출한다.
    public void LogRulebookOpen()
        => GameEventDispatcher.Raise(new RulebookOpenEvent(_gfc.LoopCount, _gfc.TurnCount));

    // 메모장을 닫을 때 현재 상태와 함께 호출한다.
    public void LogMemoClose(int entryCount, bool hasKillerNote)
        => GameEventDispatcher.Raise(new MemoCloseEvent(_gfc.LoopCount, _gfc.TurnCount, entryCount, hasKillerNote));

    // 결과 화면에 진입할 때 호출한다.
    public void LogResultScreenEnter(bool isWin)
        => GameEventDispatcher.Raise(new ResultScreenEnterEvent(_gfc.StageId, isWin));

    // 결과 화면을 떠날 때 체류 시간과 함께 호출한다.
    public void LogResultScreenExit(TimeSpan duration)
        => GameEventDispatcher.Raise(new ResultScreenExitEvent(_gfc.StageId, duration));
}
