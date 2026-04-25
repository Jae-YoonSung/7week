using System;

// ─────────────────────────────────────────────
// 세션
// ─────────────────────────────────────────────

// 앱 실행 시 세션 시작을 나타낸다.
public readonly struct SessionStartEvent
{
    public readonly DateTime Date;
    public readonly DateTime Time;
    public SessionStartEvent(DateTime date, DateTime time) { Date = date; Time = time; }
}

// 앱 종료 시 세션 종료를 나타낸다.
public readonly struct SessionEndEvent
{
    public readonly TimeSpan Duration;
    public double DurationSeconds => Duration.TotalSeconds;
    public SessionEndEvent(TimeSpan duration) { Duration = duration; }
}

// ─────────────────────────────────────────────
// 레벨
// ─────────────────────────────────────────────

// 스테이지가 시작될 때 발생한다.
public readonly struct LevelStartEvent
{
    public readonly string StageId;
    public LevelStartEvent(string stageId) { StageId = stageId; }
}

// 스테이지가 클리어되거나 실패로 종료될 때 발생한다.
public readonly struct LevelEndEvent
{
    public readonly string   StageId;
    public readonly bool     IsWin;
    public readonly TimeSpan Duration;
    public double DurationSeconds => Duration.TotalSeconds;
    public LevelEndEvent(string stageId, bool isWin, TimeSpan duration)
    {
        StageId = stageId; IsWin = isWin; Duration = duration;
    }
}

// 플레이어가 스테이지 도중 이탈(앱 종료)할 때 발생한다.
public readonly struct LevelAbandonEvent
{
    public readonly string StageId;
    public readonly int    Loop;
    public readonly int    Turn;
    public LevelAbandonEvent(string stageId, int loop, int turn)
    {
        StageId = stageId; Loop = loop; Turn = turn;
    }
}

// 로비에서 스테이지를 선택할 때 발생한다. order는 이번 세션에서 몇 번째 선택인지를 나타낸다.
public readonly struct StageSelectEvent
{
    public readonly string StageId;
    public readonly int    Order;
    public StageSelectEvent(string stageId, int order) { StageId = stageId; Order = order; }
}

// ─────────────────────────────────────────────
// 루프
// ─────────────────────────────────────────────

// 새 루프가 시작될 때 발생한다.
public readonly struct LoopStartEvent
{
    public readonly int Loop;
    public LoopStartEvent(int loop) { Loop = loop; }
}

// 루프가 정상 종료될 때 발생한다.
public readonly struct LoopEndEvent
{
    public readonly int      Loop;
    public readonly TimeSpan Duration;
    public double DurationSeconds => Duration.TotalSeconds;
    public LoopEndEvent(int loop, TimeSpan duration) { Loop = loop; Duration = duration; }
}

// 3턴을 채우지 못해 루프가 강제 전환될 때 발생한다.
public readonly struct ForcedLoopEvent
{
    public readonly int    Loop;
    public readonly int    Turn;
    public readonly string Reason;
    public ForcedLoopEvent(int loop, int turn, string reason)
    {
        Loop = loop; Turn = turn; Reason = reason;
    }
}

// ─────────────────────────────────────────────
// 턴
// ─────────────────────────────────────────────

// 플레이어 행동 단계가 시작될 때 발생한다.
public readonly struct TurnStartEvent
{
    public readonly int Loop;
    public readonly int Turn;
    public TurnStartEvent(int loop, int turn) { Loop = loop; Turn = turn; }
}

// 턴이 종료될 때 발생한다.
public readonly struct TurnEndEvent
{
    public readonly int      Loop;
    public readonly int      Turn;
    public readonly TimeSpan Duration;
    public double DurationSeconds => Duration.TotalSeconds;
    public TurnEndEvent(int loop, int turn, TimeSpan duration)
    {
        Loop = loop; Turn = turn; Duration = duration;
    }
}

// ─────────────────────────────────────────────
// 기물 이동
// ─────────────────────────────────────────────

// 플레이어가 기물(캐릭터)을 이동시킬 때 발생한다.
// moveIndex는 해당 턴 내에서 몇 번째 이동인지를 나타낸다.
public readonly struct PieceMoveEvent
{
    public readonly int    Loop;
    public readonly int    Turn;
    public readonly string CharId;
    public readonly int    FromZone;
    public readonly int    ToZone;
    public readonly int    MoveIndex;
    public PieceMoveEvent(int loop, int turn, string charId, int fromZone, int toZone, int moveIndex)
    {
        Loop = loop; Turn = turn; CharId = charId;
        FromZone = fromZone; ToZone = toZone; MoveIndex = moveIndex;
    }
}

// 기물이 특수 구역(예: 무효화 지역)에 진입할 때 발생한다.
public readonly struct SpecialZoneEvent
{
    public readonly int    Loop;
    public readonly int    Turn;
    public readonly string CharId;
    public readonly int    Zone;
    public readonly string ZoneType;
    public SpecialZoneEvent(int loop, int turn, string charId, int zone, string zoneType)
    {
        Loop = loop; Turn = turn; CharId = charId; Zone = zone; ZoneType = zoneType;
    }
}

// ─────────────────────────────────────────────
// 추리 / 판정
// ─────────────────────────────────────────────

// 플레이어가 마감일 추리 선택지를 제출할 때 발생한다.
public readonly struct AnswerSubmitEvent
{
    public readonly string StageId;
    public readonly int    Loop;
    public readonly string SelectedId;
    public readonly string CorrectId;
    public readonly bool   IsCorrect;
    public AnswerSubmitEvent(string stageId, int loop, string selectedId, string correctId, bool isCorrect)
    {
        StageId = stageId; Loop = loop;
        SelectedId = selectedId; CorrectId = correctId; IsCorrect = isCorrect;
    }
}

// 플레이어가 특정 캐릭터의 역할(살인자·물귀신 등)을 확신했다고 표시할 때 발생한다.
public readonly struct RoleConfirmedEvent
{
    public readonly int    Loop;
    public readonly int    Turn;
    public readonly string CharId;
    public readonly string SuspectedRole;
    public RoleConfirmedEvent(int loop, int turn, string charId, string suspectedRole)
    {
        Loop = loop; Turn = turn; CharId = charId; SuspectedRole = suspectedRole;
    }
}

// ─────────────────────────────────────────────
// UI
// ─────────────────────────────────────────────

// 플레이어가 룰북을 열 때 발생한다. RulebookType: "role" = 역할 룰북, "sequence" = 사건 서술 순서 룰북.
public readonly struct RulebookOpenEvent
{
    public readonly int    Loop;
    public readonly int    Turn;
    public readonly string RulebookType;
    public RulebookOpenEvent(int loop, int turn, string rulebookType) { Loop = loop; Turn = turn; RulebookType = rulebookType; }
}

// 플레이어가 메모장을 닫을 때 발생한다. 닫는 시점의 기록 상태를 스냅샷으로 저장한다.
public readonly struct MemoCloseEvent
{
    public readonly int  Loop;
    public readonly int  Turn;
    public readonly int  EntryCount;
    public readonly bool HasKillerNote;
    public MemoCloseEvent(int loop, int turn, int entryCount, bool hasKillerNote)
    {
        Loop = loop; Turn = turn; EntryCount = entryCount; HasKillerNote = hasKillerNote;
    }
}

// 결과 화면에 진입할 때 발생한다.
public readonly struct ResultScreenEnterEvent
{
    public readonly string StageId;
    public readonly bool   IsWin;
    public ResultScreenEnterEvent(string stageId, bool isWin) { StageId = stageId; IsWin = isWin; }
}

// 결과 화면을 떠날 때 발생한다. 체류 시간으로 스토리 감상 여부를 판별한다.
public readonly struct ResultScreenExitEvent
{
    public readonly string   StageId;
    public readonly TimeSpan Duration;
    public double DurationSeconds => Duration.TotalSeconds;
    public ResultScreenExitEvent(string stageId, TimeSpan duration)
    {
        StageId = stageId; Duration = duration;
    }
}

// ─────────────────────────────────────────────
// 초기화
// ─────────────────────────────────────────────

// 게임 시작 시 각 존의 캐릭터·역할 배치를 기록하기 위해 발생한다.
public readonly struct ZoneInitEvent
{
    public readonly int    Zone;
    public readonly string CharName;
    public readonly string Role;
    public readonly string CharId;
    public ZoneInitEvent(int zone, string charName, string role, string charId)
    {
        Zone = zone; CharName = charName; Role = role; CharId = charId;
    }
}
