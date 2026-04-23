using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 책장 위의 개별 책(3D 오브젝트)에 부착합니다.
/// 마우스 클릭 시 BookshelfController에 통보합니다.
///
/// 이 스크립트가 동작하려면 해당 GameObject에 Collider가 있어야 합니다.
///
/// Inspector 필수 연결:
///   Controller     → 씬의 BookshelfController
///   Seed           → 이 책이 실행할 스테이지 시드
///   StageId        → 이 책이 실행할 스테이지 ID (예: "Stage_1")
///   GameSceneName  → 로드할 게임 씬 이름
/// </summary>
[DisallowMultipleComponent]
public class BookshelfBook : MonoBehaviour
{
    [Header("게임 설정")]
    [Tooltip("이 책의 스테이지 시드")]
    [SerializeField] private int    _seed;
    [Tooltip("이 책의 스테이지 ID (StageClearRepository / StageRoleConfig와 일치해야 합니다)")]
    [SerializeField] private string _stageId;
    [Tooltip("로드할 게임 씬 이름")]
    [SerializeField] private string _gameSceneName = "Stage_1";
    [Tooltip("에필로그 진입 여부")]
    [SerializeField] private bool   _isEpilogue;

    [Header("연결")]
    [SerializeField] private BookshelfController _controller;

    [Header("잠금 설정")]
    [Tooltip("이 책을 열려면 클리어되어 있어야 하는 스테이지 ID. 비워두면 항상 해금.")]
    [SerializeField] private string _requiredClearStageId;
    [Tooltip("잠금 상태일 때 비활성화할 시각적 하이라이트 오브젝트 (선택)")]
    [SerializeField] private GameObject _lockOverlay;

    // ── 프로퍼티 ─────────────────────────────────────────────────────────────

    public int    Seed          => _seed;
    public string StageId       => _stageId;
    public string GameSceneName => _gameSceneName;
    public bool   IsEpilogue    => _isEpilogue;

    public bool IsUnlocked => string.IsNullOrEmpty(_requiredClearStageId)
                           || StageClearRepository.Instance.HasCleared(_requiredClearStageId);

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        RefreshLockState();
    }

    private void OnMouseDown()
    {
        if (!IsUnlocked) return;
        _controller?.OnBookClicked(this);
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>잠금 오버레이 활성화 여부를 갱신합니다.</summary>
    public void RefreshLockState()
    {
        if (_lockOverlay != null)
            _lockOverlay.SetActive(!IsUnlocked);
    }

    /// <summary>
    /// BookshelfController가 시퀀스 완료 후 이 책에 등록된 씬을 로드할 때 호출합니다.
    /// </summary>
    public void StartGame()
    {
        TurnHistoryRepository.Instance.ClearAll();
        NewGameConfig.SetSeed(_seed, _stageId, _isEpilogue);
        UnityEngine.SceneManagement.SceneManager.LoadScene(_gameSceneName);
    }
}
