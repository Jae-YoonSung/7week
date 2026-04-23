using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

// GameEventDispatcher의 모든 이벤트를 구독해 Unity Analytics로 전송하는 컴포넌트.
// GameLogger와 동일한 GameObject에 추가한다.
// Inspector에서 컴포넌트를 비활성화하면 구독이 해제되어 전송이 즉시 중단된다.
public class AnalyticsReporter : MonoBehaviour
{
    private void OnEnable()
    {
        GameEventDispatcher.OnSessionStart      += HandleSessionStart;
        GameEventDispatcher.OnSessionEnd        += HandleSessionEnd;
        GameEventDispatcher.OnLevelStart        += HandleLevelStart;
        GameEventDispatcher.OnLevelEnd          += HandleLevelEnd;
        GameEventDispatcher.OnLevelAbandon      += HandleLevelAbandon;
        GameEventDispatcher.OnStageSelect       += HandleStageSelect;
        GameEventDispatcher.OnLoopStart         += HandleLoopStart;
        GameEventDispatcher.OnLoopEnd           += HandleLoopEnd;
        GameEventDispatcher.OnForcedLoop        += HandleForcedLoop;
        GameEventDispatcher.OnTurnStart         += HandleTurnStart;
        GameEventDispatcher.OnTurnEnd           += HandleTurnEnd;
        GameEventDispatcher.OnPieceMove         += HandlePieceMove;
        GameEventDispatcher.OnSpecialZone       += HandleSpecialZone;
        GameEventDispatcher.OnAnswerSubmit      += HandleAnswerSubmit;
        GameEventDispatcher.OnRoleConfirmed     += HandleRoleConfirmed;
        GameEventDispatcher.OnRulebookOpen      += HandleRulebookOpen;
        GameEventDispatcher.OnMemoClose         += HandleMemoClose;
        GameEventDispatcher.OnResultScreenEnter += HandleResultScreenEnter;
        GameEventDispatcher.OnResultScreenExit  += HandleResultScreenExit;
        GameEventDispatcher.OnZoneInit          += HandleZoneInit;
    }

    private void OnDisable()
    {
        GameEventDispatcher.OnSessionStart      -= HandleSessionStart;
        GameEventDispatcher.OnSessionEnd        -= HandleSessionEnd;
        GameEventDispatcher.OnLevelStart        -= HandleLevelStart;
        GameEventDispatcher.OnLevelEnd          -= HandleLevelEnd;
        GameEventDispatcher.OnLevelAbandon      -= HandleLevelAbandon;
        GameEventDispatcher.OnStageSelect       -= HandleStageSelect;
        GameEventDispatcher.OnLoopStart         -= HandleLoopStart;
        GameEventDispatcher.OnLoopEnd           -= HandleLoopEnd;
        GameEventDispatcher.OnForcedLoop        -= HandleForcedLoop;
        GameEventDispatcher.OnTurnStart         -= HandleTurnStart;
        GameEventDispatcher.OnTurnEnd           -= HandleTurnEnd;
        GameEventDispatcher.OnPieceMove         -= HandlePieceMove;
        GameEventDispatcher.OnSpecialZone       -= HandleSpecialZone;
        GameEventDispatcher.OnAnswerSubmit      -= HandleAnswerSubmit;
        GameEventDispatcher.OnRoleConfirmed     -= HandleRoleConfirmed;
        GameEventDispatcher.OnRulebookOpen      -= HandleRulebookOpen;
        GameEventDispatcher.OnMemoClose         -= HandleMemoClose;
        GameEventDispatcher.OnResultScreenEnter -= HandleResultScreenEnter;
        GameEventDispatcher.OnResultScreenExit  -= HandleResultScreenExit;
        GameEventDispatcher.OnZoneInit          -= HandleZoneInit;
    }

    // Analytics가 초기화됐는지, 그리고 서비스가 현재도 살아있는지 이중으로 확인한다.
    // IsReady만으로는 앱 종료 시 서비스가 먼저 해제되는 경우를 막지 못한다.
    private bool CanSend()
    {
        if (!AnalyticsInitializer.IsReady) return false;
        if (UnityServices.State != ServicesInitializationState.Initialized) return false;
        return true;
    }

    private void HandleSessionStart(SessionStartEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("session_start")
        {
            { "date", e.Date.ToString("yyyy-MM-dd") },
            { "time", e.Time.ToString("HH:mm:ss") }
        });
    }

    private void HandleSessionEnd(SessionEndEvent e)
    {
        if (!CanSend()) return;
        try
        {
            AnalyticsService.Instance.RecordEvent(new CustomEvent("session_end")
            {
                { "duration", e.DurationSeconds }
            });
            AnalyticsService.Instance.Flush();
        }
        catch (ServicesInitializationException)
        {
            Debug.Log("[AnalyticsReporter] 앱 종료 중 서비스가 해제되어 session_end 로그를 생략합니다.");
        }
    }

    private void HandleLevelStart(LevelStartEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("level_start")
        {
            { "stage_id", e.StageId }
        });
    }

    private void HandleLevelEnd(LevelEndEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("level_end")
        {
            { "stage_id", e.StageId },
            { "is_win",   e.IsWin },
            { "duration", e.DurationSeconds }
        });
    }

    private void HandleLevelAbandon(LevelAbandonEvent e)
    {
        if (!CanSend()) return;
        try
        {
            AnalyticsService.Instance.RecordEvent(new CustomEvent("level_abandon")
            {
                { "stage_id", e.StageId },
                { "loop",     e.Loop },
                { "turn",     e.Turn }
            });
            AnalyticsService.Instance.Flush();
        }
        catch (ServicesInitializationException)
        {
            Debug.Log("[AnalyticsReporter] 앱 종료 중 서비스가 해제되어 level_abandon 로그를 생략합니다.");
        }
    }

    private void HandleStageSelect(StageSelectEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("stage_select")
        {
            { "stage_id", e.StageId },
            { "order",    e.Order }
        });
    }

    private void HandleLoopStart(LoopStartEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("loop_start")
        {
            { "loop", e.Loop }
        });
    }

    private void HandleLoopEnd(LoopEndEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("loop_end")
        {
            { "loop",     e.Loop },
            { "duration", e.DurationSeconds }
        });
    }

    private void HandleForcedLoop(ForcedLoopEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("forced_loop")
        {
            { "loop",   e.Loop },
            { "turn",   e.Turn },
            { "reason", e.Reason }
        });
    }

    private void HandleTurnStart(TurnStartEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("turn_start")
        {
            { "loop", e.Loop },
            { "turn", e.Turn }
        });
    }

    private void HandleTurnEnd(TurnEndEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("turn_end")
        {
            { "loop",     e.Loop },
            { "turn",     e.Turn },
            { "duration", e.DurationSeconds }
        });
    }

    private void HandlePieceMove(PieceMoveEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("piece_move")
        {
            { "loop",       e.Loop },
            { "turn",       e.Turn },
            { "char_id",    e.CharId },
            { "from_zone",  e.FromZone },
            { "to_zone",    e.ToZone },
            { "move_index", e.MoveIndex }
        });
    }

    private void HandleSpecialZone(SpecialZoneEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("special_zone")
        {
            { "loop",      e.Loop },
            { "turn",      e.Turn },
            { "char_id",   e.CharId },
            { "zone",      e.Zone },
            { "zone_type", e.ZoneType }
        });
    }

    private void HandleAnswerSubmit(AnswerSubmitEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("answer_submit")
        {
            { "stage_id",    e.StageId },
            { "loop",        e.Loop },
            { "selected_id", e.SelectedId },
            { "correct_id",  e.CorrectId },
            { "is_correct",  e.IsCorrect }
        });
    }

    private void HandleRoleConfirmed(RoleConfirmedEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("role_confirmed")
        {
            { "loop",           e.Loop },
            { "turn",           e.Turn },
            { "char_id",        e.CharId },
            { "suspected_role", e.SuspectedRole }
        });
    }

    private void HandleRulebookOpen(RulebookOpenEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("rulebook_open")
        {
            { "loop", e.Loop },
            { "turn", e.Turn }
        });
    }

    private void HandleMemoClose(MemoCloseEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("memo_close")
        {
            { "loop",            e.Loop },
            { "turn",            e.Turn },
            { "entry_count",     e.EntryCount },
            { "has_killer_note", e.HasKillerNote }
        });
    }

    private void HandleResultScreenEnter(ResultScreenEnterEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("result_screen_enter")
        {
            { "stage_id", e.StageId },
            { "is_win",   e.IsWin }
        });
    }

    private void HandleResultScreenExit(ResultScreenExitEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("result_screen_exit")
        {
            { "stage_id", e.StageId },
            { "duration", e.DurationSeconds }
        });
    }

    private void HandleZoneInit(ZoneInitEvent e)
    {
        if (!CanSend()) return;
        AnalyticsService.Instance.RecordEvent(new CustomEvent("zone_init")
        {
            { "zone",      e.Zone },
            { "char_name", e.CharName },
            { "role",      e.Role },
            { "char_id",   e.CharId }
        });
    }
}
