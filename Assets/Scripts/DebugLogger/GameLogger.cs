using System;
using System.IO;
using UnityEngine;

// 파일 로그 싱글톤. GameEventDispatcher의 모든 이벤트를 구독해 텍스트 파일에 기록한다.
// AnalyticsInitializer, AnalyticsReporter와 동일한 GameObject에 배치한다.
public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    private string   logFilePath;
    public  string   LogFilePath => logFilePath;

    private DateTime _sessionStart;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string exeDir = Path.GetDirectoryName(Application.dataPath);
        string logDir = Path.Combine(exeDir, "log");
        Directory.CreateDirectory(logDir);

        _sessionStart = DateTime.Now;
        string fileName = $"GameLog_{_sessionStart:yyyy-MM-dd_HH-mm-ss}.txt";
        logFilePath = Path.Combine(logDir, fileName);

        Application.logMessageReceived += HandleUnityLog;
    }

    // OnEnable에서 구독 — Awake 직후 실행되므로 Start에서 Raise하는 SessionStartEvent를 놓치지 않는다.
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

    // 구독이 완료된 Start에서 세션 시작 이벤트를 Raise한다.
    private void Start()
    {
        GameEventDispatcher.Raise(new SessionStartEvent(_sessionStart, _sessionStart));
    }

    private void OnApplicationQuit()
    {
        TimeSpan duration = DateTime.Now - _sessionStart;
        GameEventDispatcher.Raise(new SessionEndEvent(duration));
    }

    // ── 파일 쓰기 ────────────────────────────────────────────────────────────

    public void Log(string message)
    {
        string formatted = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Debug.Log(formatted);
        File.AppendAllText(logFilePath, formatted + Environment.NewLine);
    }

    // Unity 내부 로그를 파일에 추가한다. Log()가 이미 쓴 항목("[" 시작)은 중복 방지를 위해 건너뛴다.
    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (logString.StartsWith("["))
            return;

        string formatted = $"[{DateTime.Now:HH:mm:ss}] [{type}] {logString}";
        if (type == LogType.Error || type == LogType.Exception)
            formatted += $"\n{stackTrace}";

        File.AppendAllText(logFilePath, formatted + Environment.NewLine);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    // ── 이벤트 핸들러 (파일 포맷 변환) ──────────────────────────────────────

    private void HandleSessionStart(SessionStartEvent e)
        => Log($"[session_start] date={e.Date:yyyy-MM-dd} time={e.Time:HH:mm:ss}");

    private void HandleSessionEnd(SessionEndEvent e)
        => Log($"[session_end] duration={e.Duration:hh\\:mm\\:ss}");

    private void HandleLevelStart(LevelStartEvent e)
        => Log($"[level_start] stage={e.StageId}");

    private void HandleLevelEnd(LevelEndEvent e)
        => Log($"[level_end] stage={e.StageId} is_win={e.IsWin} duration={e.Duration:mm\\:ss}");

    private void HandleLevelAbandon(LevelAbandonEvent e)
        => Log($"[level_abandon] stage={e.StageId} loop={e.Loop} turn={e.Turn}");

    private void HandleStageSelect(StageSelectEvent e)
        => Log($"[stage_select] stage={e.StageId} order={e.Order}");

    private void HandleLoopStart(LoopStartEvent e)
        => Log($"[loop_start] loop={e.Loop}");

    private void HandleLoopEnd(LoopEndEvent e)
        => Log($"[loop_end] loop={e.Loop} duration={e.Duration:mm\\:ss}");

    private void HandleForcedLoop(ForcedLoopEvent e)
        => Log($"[forced_loop] loop={e.Loop} turn={e.Turn} reason={e.Reason}");

    private void HandleTurnStart(TurnStartEvent e)
        => Log($"[turn_start] loop={e.Loop} turn={e.Turn}");

    private void HandleTurnEnd(TurnEndEvent e)
        => Log($"[turn_end] loop={e.Loop} turn={e.Turn} duration={e.Duration:mm\\:ss}");

    private void HandlePieceMove(PieceMoveEvent e)
        => Log($"[piece_move] loop={e.Loop} turn={e.Turn} char={e.CharId} from={e.FromZone} to={e.ToZone} idx={e.MoveIndex}");

    private void HandleSpecialZone(SpecialZoneEvent e)
        => Log($"[special_zone] loop={e.Loop} turn={e.Turn} char={e.CharId} zone={e.Zone} type={e.ZoneType}");

    private void HandleAnswerSubmit(AnswerSubmitEvent e)
        => Log($"[answer_submit] stage={e.StageId} loop={e.Loop} selected={e.SelectedId} correct={e.CorrectId} is_correct={e.IsCorrect}");

    private void HandleRoleConfirmed(RoleConfirmedEvent e)
        => Log($"[role_confirmed] loop={e.Loop} turn={e.Turn} char={e.CharId} role={e.SuspectedRole}");

    private void HandleRulebookOpen(RulebookOpenEvent e)
        => Log($"[rulebook_open] loop={e.Loop} turn={e.Turn} type={e.RulebookType}");

    private void HandleMemoClose(MemoCloseEvent e)
        => Log($"[memo_close] loop={e.Loop} turn={e.Turn} entries={e.EntryCount} has_killer={e.HasKillerNote}");

    private void HandleResultScreenEnter(ResultScreenEnterEvent e)
        => Log($"[result_screen_enter] stage={e.StageId} is_win={e.IsWin}");

    private void HandleResultScreenExit(ResultScreenExitEvent e)
        => Log($"[result_screen_exit] stage={e.StageId} duration={e.Duration:mm\\:ss}");

    private void HandleZoneInit(ZoneInitEvent e)
        => Log($"[zone_init] zone={e.Zone} char={e.CharName} role={e.Role} id={e.CharId}");
}
