/// <summary>
/// 씬 간 게임 설정을 전달하는 정적 컨테이너입니다.
///
/// 데이터 흐름:
///   타이틀씬 (BookshelfBook) → SetSeed + SetLobbyReturnInfo
///   → 로비씬 (StageLobbyController) → GameSceneName 읽어서 씬 로드
///   → 스테이지씬 (GameFlowController) → Seed/StageId 소비 → 클리어 시 LobbySceneName으로 복귀
/// </summary>
public static class NewGameConfig
{
    public static bool   IsSet      { get; private set; }
    public static bool   UseRandom  { get; private set; }
    public static int    Seed       { get; private set; }
    public static bool   IsTutorial { get; private set; }
    public static string StageId    { get; private set; }
    /// <summary>에필로그 모드로 시작할 때 true. GameFlowController.Awake()에서 읽고 StartGame() 전에 소비됩니다.</summary>
    public static bool   IsEpilogue { get; private set; }

    /// <summary>이 스테이지의 전용 로비 씬 이름. 스테이지 클리어 후 복귀할 씬입니다.</summary>
    public static string LobbySceneName { get; private set; }
    /// <summary>로비에서 로드할 게임플레이 씬 이름 (예: Stage_1).</summary>
    public static string GameSceneName  { get; private set; }

    public static void SetRandom(string stageId = null, bool isEpilogue = false) { IsSet = true; UseRandom = true; StageId = stageId; IsEpilogue = isEpilogue; }
    public static void SetSeed(int seed, string stageId = null, bool isEpilogue = false) { IsSet = true; UseRandom = false; Seed = seed; StageId = stageId; IsEpilogue = isEpilogue; }
    public static void SetTutorial(int fixedSeed)
    {
        IsSet      = true;
        UseRandom  = false;
        Seed       = fixedSeed;
        IsTutorial = true;
    }

    /// <summary>
    /// 타이틀씬에서 호출합니다.
    /// 로비씬 이름과 게임씬 이름을 저장해두어 씬 간 전달에 사용합니다.
    /// </summary>
    public static void SetLobbyReturnInfo(string lobbySceneName, string gameSceneName)
    {
        LobbySceneName = lobbySceneName;
        GameSceneName  = gameSceneName;
    }

    public static void Clear()
    {
        IsSet          = false;
        UseRandom      = false;
        Seed           = 0;
        IsTutorial     = false;
        StageId        = null;
        IsEpilogue     = false;
        LobbySceneName = null;
        GameSceneName  = null;
    }
}

