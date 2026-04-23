using System;

// 모든 게임 이벤트를 중앙에서 분배하는 정적 디스패처.
// 이벤트 발생원은 Raise()를 호출하고, GameLogger·AnalyticsReporter 등 구독자는 On* 이벤트에 핸들러를 등록한다.
public static class GameEventDispatcher
{
    // ── 세션 ──────────────────────────────────
    public static event Action<SessionStartEvent>       OnSessionStart;
    public static event Action<SessionEndEvent>         OnSessionEnd;

    // ── 레벨 ──────────────────────────────────
    public static event Action<LevelStartEvent>         OnLevelStart;
    public static event Action<LevelEndEvent>           OnLevelEnd;
    public static event Action<LevelAbandonEvent>       OnLevelAbandon;
    public static event Action<StageSelectEvent>        OnStageSelect;

    // ── 루프 ──────────────────────────────────
    public static event Action<LoopStartEvent>          OnLoopStart;
    public static event Action<LoopEndEvent>            OnLoopEnd;
    public static event Action<ForcedLoopEvent>         OnForcedLoop;

    // ── 턴 ────────────────────────────────────
    public static event Action<TurnStartEvent>          OnTurnStart;
    public static event Action<TurnEndEvent>            OnTurnEnd;

    // ── 기물 이동 ─────────────────────────────
    public static event Action<PieceMoveEvent>          OnPieceMove;
    public static event Action<SpecialZoneEvent>        OnSpecialZone;

    // ── 추리 / 판정 ───────────────────────────
    public static event Action<AnswerSubmitEvent>       OnAnswerSubmit;
    public static event Action<RoleConfirmedEvent>      OnRoleConfirmed;

    // ── UI ────────────────────────────────────
    public static event Action<RulebookOpenEvent>       OnRulebookOpen;
    public static event Action<MemoCloseEvent>          OnMemoClose;
    public static event Action<ResultScreenEnterEvent>  OnResultScreenEnter;
    public static event Action<ResultScreenExitEvent>   OnResultScreenExit;

    // ── 초기화 ────────────────────────────────
    public static event Action<ZoneInitEvent>           OnZoneInit;

    // ── Raise ─────────────────────────────────

    public static void Raise(SessionStartEvent e)       => OnSessionStart?.Invoke(e);
    public static void Raise(SessionEndEvent e)         => OnSessionEnd?.Invoke(e);

    public static void Raise(LevelStartEvent e)         => OnLevelStart?.Invoke(e);
    public static void Raise(LevelEndEvent e)           => OnLevelEnd?.Invoke(e);
    public static void Raise(LevelAbandonEvent e)       => OnLevelAbandon?.Invoke(e);
    public static void Raise(StageSelectEvent e)        => OnStageSelect?.Invoke(e);

    public static void Raise(LoopStartEvent e)          => OnLoopStart?.Invoke(e);
    public static void Raise(LoopEndEvent e)            => OnLoopEnd?.Invoke(e);
    public static void Raise(ForcedLoopEvent e)         => OnForcedLoop?.Invoke(e);

    public static void Raise(TurnStartEvent e)          => OnTurnStart?.Invoke(e);
    public static void Raise(TurnEndEvent e)            => OnTurnEnd?.Invoke(e);

    public static void Raise(PieceMoveEvent e)          => OnPieceMove?.Invoke(e);
    public static void Raise(SpecialZoneEvent e)        => OnSpecialZone?.Invoke(e);

    public static void Raise(AnswerSubmitEvent e)       => OnAnswerSubmit?.Invoke(e);
    public static void Raise(RoleConfirmedEvent e)      => OnRoleConfirmed?.Invoke(e);

    public static void Raise(RulebookOpenEvent e)       => OnRulebookOpen?.Invoke(e);
    public static void Raise(MemoCloseEvent e)          => OnMemoClose?.Invoke(e);
    public static void Raise(ResultScreenEnterEvent e)  => OnResultScreenEnter?.Invoke(e);
    public static void Raise(ResultScreenExitEvent e)   => OnResultScreenExit?.Invoke(e);

    public static void Raise(ZoneInitEvent e)           => OnZoneInit?.Invoke(e);
}
