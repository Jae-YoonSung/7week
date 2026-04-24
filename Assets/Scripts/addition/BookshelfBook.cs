using UnityEngine;
using DG.Tweening;

/// <summary>
/// 책장 위 개별 책 3D 오브젝트에 부착합니다.
/// 마우스 클릭 시 TitleSceneController에 자신을 전달합니다.
///
/// 새 구조:
///   각 책 = 하나의 스테이지를 대표합니다.
///   클릭하면 스테이지 정보를 NewGameConfig에 저장한 뒤,
///   해당 스테이지 전용 LobbyScene으로 전환합니다.
///
/// 요구 조건:
///   - 이 오브젝트에 Collider (BoxCollider 등) 필수
///   - Physics Raycasting이 활성화된 Camera 필요 (일반 3D 카메라)
///
/// Inspector 필수 연결:
///   Controller → 씬의 TitleSceneController
/// </summary>
[DisallowMultipleComponent]
public class BookshelfBook : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("씬의 TitleSceneController")]
    [SerializeField] private TitleSceneController _controller;

    [Header("스테이지 정보")]
    [Tooltip("이 책이 담당하는 스테이지 시드")]
    [SerializeField] private int _seed;
    [Tooltip("스테이지 ID (StageClearRepository / StageRoleConfig와 일치)")]
    [SerializeField] private string _stageId;
    [Tooltip("게임플레이 씬 이름 (예: Stage_1)")]
    [SerializeField] private string _gameSceneName = "Stage_1";
    [Tooltip("에필로그 진입 여부")]
    [SerializeField] private bool _isEpilogue;

    [Header("로비 씬 설정")]
    [Tooltip("이 책을 클릭했을 때 이동할 전용 LobbyScene 이름")]
    [SerializeField] private string _lobbySceneName = "LobbyScene";

    [Header("잠금 설정")]
    [Tooltip("이 책을 클릭하려면 클리어되어 있어야 하는 스테이지 ID. 비워두면 항상 해금.")]
    [SerializeField] private string _requiredClearStageId;
    [Tooltip("잠금 상태일 때 활성화할 오버레이 오브젝트 (선택)")]
    [SerializeField] private GameObject _lockOverlay;
    [Tooltip("잠금 상태일 때 책 오브젝트 자체를 비활성화할지 여부")]
    [SerializeField] private bool _hideWhenLocked = false;

    [Header("호버 효과 (선택)")]
    [Tooltip("마우스 오버 시 책을 살짝 튀어나오게 할 거리 (0이면 비활성)")]
    [SerializeField] private float _hoverOffset = 0.05f;
    [SerializeField] private float _hoverDuration = 0.15f;

    // ── 프로퍼티 ─────────────────────────────────────────────────────────────

    public bool IsUnlocked => string.IsNullOrEmpty(_requiredClearStageId)
                           || StageClearRepository.Instance.HasCleared(_requiredClearStageId);

    // ── 런타임 상태 ──────────────────────────────────────────────────────────

    private Vector3 _originalLocalPos;
    private bool    _isHovering = false;
    private bool    _isClicked = false;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _originalLocalPos = transform.localPosition;
        RefreshLockState();
    }

    private void OnMouseDown()
    {
        if (!IsUnlocked || _isClicked) return;
        _isClicked = true;
        DOTween.Kill(transform);

        // 스테이지 정보를 NewGameConfig에 저장 (로비씬에서 읽어감)
        TurnHistoryRepository.Instance.ClearAll();
        NewGameConfig.SetSeed(_seed, _stageId, _isEpilogue);
        NewGameConfig.SetLobbyReturnInfo(_lobbySceneName, _gameSceneName);

        _controller?.OnBookClicked(transform, _lobbySceneName);
    }

    private void OnMouseEnter()
    {
        if (!IsUnlocked || _hoverOffset <= 0f || _isHovering || _isClicked) return;
        _isHovering = true;
        DOTween.Kill(transform);
        transform.DOLocalMove(_originalLocalPos + transform.forward * _hoverOffset, _hoverDuration).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        if (!_isHovering || _isClicked) return;
        _isHovering = false;
        DOTween.Kill(transform);
        transform.DOLocalMove(_originalLocalPos, _hoverDuration).SetEase(Ease.OutQuad);
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    public void RefreshLockState()
    {
        if (_lockOverlay != null)
            _lockOverlay.SetActive(!IsUnlocked);

        if (_hideWhenLocked)
            gameObject.SetActive(IsUnlocked);
    }
}
