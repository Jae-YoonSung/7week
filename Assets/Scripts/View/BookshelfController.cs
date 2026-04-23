using System;
using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 책장(Bookshelf) 씬의 진입점입니다.
/// 책장 위의 BookshelfBookSlot 들을 관리하며, 책 클릭 → 책 이동 → 카메라 전환 → 책 열기 시퀀스를 조율합니다.
///
/// Cinemachine 세팅:
///   _bookshelfCamera  : 초기 책장 바라보기 Virtual Camera (Priority 높게)
///   _bookCloseupCamera: 책 배치 완료 후 클로즈업 Virtual Camera (초기 Priority 낮게)
///
/// Inspector 필수 연결:
///   BookshelfCamera      → 책장 바라보기 CinemachineCamera
///   BookCloseupCamera    → 책 클로즈업 CinemachineCamera
///   BookDestination      → 책이 도착할 목표 위치/회전 Transform (빈 GameObject)
///   BookAnimator         → 기존 TitleBookAnimator (책 열기 연출)
///   LobbyDialogueManager → (선택) 다이얼로그 매니저
/// </summary>
[DisallowMultipleComponent]
public class BookshelfController : MonoBehaviour
{
    [Header("Cinemachine 카메라")]
    [Tooltip("초기에 책장을 바라보는 Virtual Camera (Priority를 높게 설정)")]
    [SerializeField] private CinemachineCamera _bookshelfCamera;
    [Tooltip("책이 배치된 뒤 클로즈업하는 Virtual Camera (초기 Priority 낮게)")]
    [SerializeField] private CinemachineCamera _bookCloseupCamera;

    [Header("책 이동 설정")]
    [Tooltip("책이 최종적으로 놓일 위치/회전을 나타내는 빈 Transform")]
    [SerializeField] private Transform _bookDestination;
    [Tooltip("경유 웨이포인트 목록 (순서대로 거쳐감). 비워두면 직선 이동.")]
    [SerializeField] private Transform[] _waypoints;
    [Tooltip("각 웨이포인트 구간의 이동 시간 (초)")]
    [SerializeField] private float _moveSegmentDuration = 0.6f;
    [SerializeField] private Ease  _moveEase            = Ease.InOutSine;

    [Header("카메라 전환 타이밍")]
    [Tooltip("책이 목적지에 도착한 뒤 클로즈업 카메라로 전환하기까지 대기 시간")]
    [SerializeField] private float _cameraBlendDelay = 0.2f;
    [Tooltip("클로즈업 카메라로 전환 후 책 열기까지 대기 시간 (Cinemachine blend 시간과 맞추세요)")]
    [SerializeField] private float _openBookDelay    = 1.0f;

    [Header("연결 컴포넌트")]
    [Tooltip("책 열기 애니메이션 담당 (기존 TitleBookAnimator)")]
    [SerializeField] private TitleBookAnimator _bookAnimator;
    [Tooltip("(선택) 로비 다이얼로그 매니저 — 다이얼로그 완료 전까지 클릭 차단에 사용")]
    [SerializeField] private LobbyDialogueManager _dialogueManager;
    [Tooltip("(선택) 챕터 UI의 시작 버튼들과 선택된 책을 연결하는 브릿지")]
    [SerializeField] private BookshelfChapterBridge _chapterBridge;

    // ── 런타임 상태 ──────────────────────────────────────────────────────────

    private bool          _sequencePlaying = false;
    private BookshelfBook _selectedBook;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 책장 카메라 우선, 클로즈업 카메라 비활성
        SetCameraPriority(_bookshelfCamera,  10);
        SetCameraPriority(_bookCloseupCamera, 0);
    }

    // ── 외부 API (BookshelfBook에서 호출) ───────────────────────────────────

    /// <summary>
    /// BookshelfBook이 클릭됐을 때 호출합니다.
    /// 이미 시퀀스가 진행 중이거나 다이얼로그가 끝나지 않았으면 무시합니다.
    /// </summary>
    public void OnBookClicked(BookshelfBook book)
    {
        if (_sequencePlaying) return;
        if (!IsInteractionAllowed()) return;

        _selectedBook    = book;
        _sequencePlaying = true;

        // 챕터 UI 브릿지에 선택된 책 정보 전달
        _chapterBridge?.SetSelectedBook(book);

        StartCoroutine(PlayBookSequence(book));
    }

    // ── Private — 시퀀스 ─────────────────────────────────────────────────────

    private IEnumerator PlayBookSequence(BookshelfBook book)
    {
        // 1. 책을 책장에서 분리(부모 해제)해 독립적으로 이동 가능하게 합니다.
        Transform bookTransform = book.transform;
        bookTransform.SetParent(null, worldPositionStays: true);

        // 2. 경유 웨이포인트를 거쳐 목적지로 이동합니다.
        yield return MoveBookAlongPath(bookTransform);

        // 3. 잠깐 대기 후 클로즈업 카메라 전환
        yield return new WaitForSeconds(_cameraBlendDelay);
        SwitchToCloseupCamera(book);

        // 4. 카메라 블렌드 완료 대기
        yield return new WaitForSeconds(_openBookDelay);

        // 5. 키 입력 없이 자동으로 책 열기 (ForceOpen)
        _bookAnimator?.ForceOpen();

        // 6. 이후 흐름은 TitleBookAnimator → LobbyUIManager.Show() → 기존 챕터 UI가 담당
    }

    /// <summary>웨이포인트 목록을 순서대로 거쳐 목적지까지 이동합니다.</summary>
    private IEnumerator MoveBookAlongPath(Transform bookTransform)
    {
        // 웨이포인트가 있으면 순서대로 이동
        if (_waypoints != null && _waypoints.Length > 0)
        {
            foreach (var wp in _waypoints)
            {
                if (wp == null) continue;
                bool done = false;
                bookTransform.DOMove(wp.position, _moveSegmentDuration)
                             .SetEase(_moveEase)
                             .OnComplete(() => done = true);
                bookTransform.DORotateQuaternion(wp.rotation, _moveSegmentDuration)
                             .SetEase(_moveEase);
                yield return new WaitUntil(() => done);
            }
        }

        // 최종 목적지
        if (_bookDestination != null)
        {
            bool done = false;
            bookTransform.DOMove(_bookDestination.position, _moveSegmentDuration)
                         .SetEase(_moveEase)
                         .OnComplete(() => done = true);
            bookTransform.DORotateQuaternion(_bookDestination.rotation, _moveSegmentDuration)
                         .SetEase(_moveEase);
            yield return new WaitUntil(() => done);
        }
    }

    /// <summary>클로즈업 카메라의 Look At / Follow 대상을 책으로 지정하고 Priority를 높입니다.</summary>
    private void SwitchToCloseupCamera(BookshelfBook book)
    {
        if (_bookCloseupCamera != null)
        {
            _bookCloseupCamera.Follow  = book.transform;
            _bookCloseupCamera.LookAt  = book.transform;
        }

        SetCameraPriority(_bookshelfCamera,   0);
        SetCameraPriority(_bookCloseupCamera, 10);
    }

    private static void SetCameraPriority(CinemachineCamera cam, int priority)
    {
        if (cam == null) return;
        cam.Priority = priority;
    }

    /// <summary>다이얼로그가 아직 진행 중이면 상호작용을 차단합니다.</summary>
    private bool IsInteractionAllowed()
    {
        // LobbyDialogueManager가 없으면 항상 허용
        // 있으면 phase 3(완료)일 때만 허용 — 내부 상태에 직접 접근하기 어려우므로
        // 외부 공개 프로퍼티 IsComplete를 사용합니다. (LobbyDialogueManager 수정 필요)
        if (_dialogueManager == null) return true;
        return _dialogueManager.IsComplete;
    }
}
