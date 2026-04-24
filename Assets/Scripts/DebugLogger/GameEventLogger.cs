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

    // 턴 내 기물 이동 순번 카운터 — HandlePlayerActionStarted에서 매 턴 0으로 리셋된다.
    private int _moveIndexThisTurn;

    // 캐릭터별 현재 존 추적 — 이동 전 prevZone 계산에 사용한다.
    // 매 턴 시작 시 SyncAssignedZones()로 GameState 기준 최신화한다.
    private Dictionary<int, int> _assignedZones = new();

    // PlayerActionState 참조 — OnActionConfirmed 구독용. Start에서 획득, OnDestroy에서 해제한다.
    private PlayerActionState _playerAction;

    // 씬의 ZoneLayout 참조 — IsSpecialZone / GetZoneType에서 ZoneEffect 조회에 사용한다.
    private ZoneLayout _zoneLayout;

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

        // PlayerActionState 구독 — piece_move / special_zone 기록용
        _playerAction = _gfc.GetPlayerActionState();
        if (_playerAction != null)
            _playerAction.OnActionConfirmed += HandleActionConfirmed;

        // ZoneLayout 캐시 — IsSpecialZone / GetZoneType에서 ZoneEffect 조회에 사용
        _zoneLayout = FindFirstObjectByType<ZoneLayout>();
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

        if (_playerAction != null)
            _playerAction.OnActionConfirmed -= HandleActionConfirmed;
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
        // 턴 시작마다 이동 순번을 초기화하고 존 상태를 최신화한다.
        // 파도 구역 등 외부 효과로 인한 위치 변경도 여기서 반영된다.
        _moveIndexThisTurn = 0;
        SyncAssignedZones();

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

    // ── piece_move / special_zone 자동 기록 ─────────────────────────────────

    // GameState 기준으로 _assignedZones를 최신화한다.
    // CharacterViews가 초기화된 후 매 턴 시작 시 호출되므로
    // Start 시점에 CharacterViews가 비어 있었더라도 첫 턴에 자동으로 보완된다.
    private void SyncAssignedZones()
    {
        if (_gfc == null) return;
        var gs = _gfc.GameState;
        if (gs == null) return;
        if (_gfc.CharacterViews == null) return;
        foreach (var charId in _gfc.CharacterViews.Keys)
            _assignedZones[charId] = gs.GetZone(charId);
    }

    // PlayerActionState.OnActionConfirmed 핸들러.
    // 기물 이동 예약이 확정되는 시점마다 호출된다.
    // prevZone은 _assignedZones에서 조회하고, 이동 후 _assignedZones를 갱신한다.
    // targetZoneId < 0은 대기 확정(제자리)이므로 기록하지 않는다.
    private void HandleActionConfirmed(int characterId, int targetZoneId)
    {
        if (targetZoneId < 0) return;

        int prevZone = _assignedZones.TryGetValue(characterId, out var z) ? z : targetZoneId;

        GameEventDispatcher.Raise(new PieceMoveEvent(
            _gfc.LoopCount,
            _gfc.TurnCount,
            characterId.ToString(),
            prevZone,
            targetZoneId,
            _moveIndexThisTurn++
        ));

        _assignedZones[characterId] = targetZoneId;

        if (IsSpecialZone(targetZoneId))
        {
            GameEventDispatcher.Raise(new SpecialZoneEvent(
                _gfc.LoopCount,
                _gfc.TurnCount,
                characterId.ToString(),
                targetZoneId,
                GetZoneType(targetZoneId)
            ));
        }
    }

    // 능력 봉인 구역이거나 ZoneEffect가 설정된 구역이면 특수 구역으로 판별한다.
    private bool IsSpecialZone(int zoneId)
    {
        var gs = _gfc.GameState;
        if (gs != null && gs.IsAbilityDisabledInZone(zoneId)) return true;
        if (_zoneLayout != null)
        {
            var point = _zoneLayout.GetZonePoint(zoneId);
            if (point != null && point.ZoneEffect != null) return true;
        }
        return false;
    }

    // 특수 구역 타입 문자열을 반환한다.
    // ZoneEffect가 있으면 그 타입 이름을 우선하고, 없으면 "ability_disabled"를 반환한다.
    private string GetZoneType(int zoneId)
    {
        if (_zoneLayout != null)
        {
            var point = _zoneLayout.GetZonePoint(zoneId);
            if (point != null && point.ZoneEffect != null)
                return point.ZoneEffect.GetType().Name;
        }
        var gs = _gfc.GameState;
        if (gs != null && gs.IsAbilityDisabledInZone(zoneId)) return "ability_disabled";
        return "unknown";
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
