using System;
using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬의 전체 연출을 관리합니다.
///
/// 흐름:
///   씬 시작 → BookshelfCamera 활성화 (책장 바라보기)
///   → [책 클릭] → (동시) 책 경로 이동 + 카메라 순차 전환
///   → 모두 완료 → 화면 페이드 아웃 → LobbyScene 로드
///
/// Inspector 필수 연결:
///   CameraSequence  : 전환할 Virtual Camera 배열 (순서대로)
///                     CameraSequence[0] = 초기 책장 카메라
///                     CameraSequence[N] = 마지막 책상 카메라
///   CameraHoldTimes : 각 카메라가 유지될 시간 배열 (CameraSequence와 같은 길이)
///   BookDestination : 책이 최종적으로 놓일 위치/회전 Transform
///   Waypoints       : 경유 지점 Transform 배열 (없으면 직선 이동)
///   FadeImage       : 전체 화면을 덮는 검은 Image (CanvasGroup or Image)
///   LobbySceneName  : 로드할 씬 이름 (기본값 "LobbyScene")
/// </summary>
[DisallowMultipleComponent]
public class TitleSceneController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Cinemachine 카메라 시퀀스")]
    [Tooltip("순서대로 전환할 Virtual Camera 배열. [0]이 초기(책장), 마지막이 책상.")]
    [SerializeField] private CinemachineCamera[] _cameraSequence;

    [Tooltip("각 카메라를 보여줄 시간(초). _cameraSequence와 같은 길이로 맞추세요.")]
    [SerializeField] private float[] _cameraHoldTimes;

    // ─────────────────────────────────────────────────────────────────────────
    [Header("책 이동 설정")]
    [Tooltip("책이 최종적으로 놓일 위치/회전 (책상 위 빈 Transform)")]
    [SerializeField] private Transform _bookDestination;

    [Tooltip("경유 웨이포인트 목록 (순서대로). 비워두면 목적지로 직선 이동.")]
    [SerializeField] private Transform[] _waypoints;

    [Tooltip("웨이포인트 구간당 이동 시간 (초)")]
    [SerializeField] private float _moveSegmentDuration = 0.55f;

    [SerializeField] private Ease _moveEase = Ease.InOutSine;

    [Header("첫 번째 웨이포인트 연출")]
    [Tooltip("첫 번째 웨이포인트 도착 시 수행할 추가 회전량 (예: Z축 360도)")]
    [SerializeField] private Vector3 _firstWPRotationAmount = new Vector3(0, 0, 360);

    [Tooltip("첫 번째 웨이포인트 추가 회전 소요 시간")]
    [SerializeField] private float _firstWPRotationDuration = 0.5f;

    [Tooltip("첫 번째 웨이포인트 추가 회전 Ease")]
    [SerializeField] private Ease _firstWPRotationEase = Ease.OutBack;

    [Tooltip("첫 번째 웨이포인트 도착 시 적용할 목표 스케일 값 (예: 1.5 1.5 1.5를 넣으면 그 크기가 됨). (0,0,0)이면 변경하지 않습니다.")]
    [SerializeField] private Vector3 _firstWPScale = Vector3.zero;

    [Tooltip("첫 번째 웨이포인트 추가 스케일 소요 시간")]
    [SerializeField] private float _firstWPScaleDuration = 0.5f;

    [Tooltip("첫 번째 웨이포인트 추가 스케일 Ease")]
    [SerializeField] private Ease _firstWPScaleEase = Ease.OutBack;

    // ─────────────────────────────────────────────────────────────────────────
    [Header("페이드 설정")]
    [Tooltip("전체 화면을 덮는 검은 Image 컴포넌트 (Canvas > 최상단 레이어)")]
    [SerializeField] private Image _fadeImage;

    [Tooltip("페이드 아웃에 걸리는 시간 (초)")]
    [SerializeField] private float _fadeDuration = 0.8f;

    [Tooltip("페이드 완료 후 씬 전환까지 추가 대기 시간 (초)")]
    [SerializeField] private float _fadeHoldDuration = 0.2f;

    // ─────────────────────────────────────────────────────────────────────────
    [Header("씬 전환 (기본값)")]
    [Tooltip("BookshelfBook에서 씬 이름을 지정하지 않았을 때 사용할 기본 로비 씬 이름")]
    [SerializeField] private string _defaultLobbySceneName = "LobbyScene";

    // 런타임 상태
    private bool _sequencePlaying = false;
    private string _currentTargetSceneName;
    private bool _isInputReady = false; // 대사 완료 전까지 책 클릭 방지
    
    /// <summary>타이틀 씬의 페이드 및 대사가 완료되어 상호작용 가능한 상태인지 반환합니다.</summary>
    public bool IsInputReady => _isInputReady;

    private LobbyDialogueManager _dialogueManager;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        InitializeCameras();
        InitializeFade();
        _dialogueManager = FindFirstObjectByType<LobbyDialogueManager>();
    }

    // ── 외부 API (BookshelfBook에서 호출) ───────────────────────────────────

    /// <summary>
    /// 책이 클릭됐을 때 BookshelfBook이 호출합니다.
    /// 이미 시퀀스가 진행 중이거나 대사가 안 끝났으면 무시합니다.
    /// </summary>
    public void OnBookClicked(Transform bookTransform, string targetSceneName)
    {
        if (!_isInputReady || _sequencePlaying) return;
        _sequencePlaying = true;

        _currentTargetSceneName = string.IsNullOrEmpty(targetSceneName) ? _defaultLobbySceneName : targetSceneName;

        // 책을 씬 루트로 분리해 독립적으로 이동
        bookTransform.SetParent(null, worldPositionStays: true);

        StartCoroutine(PlayTitleSequence(bookTransform));
    }

    private void Start()
    {
        StartCoroutine(TitleStartSequence());
    }

    private IEnumerator TitleStartSequence()
    {
        yield return FadeIn();

        if (_dialogueManager != null)
        {
            yield return new WaitUntil(() => _dialogueManager.IsComplete);
        }

        _isInputReady = true;
    }

    private void Update()
    {
        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        // 1. F12: 모든 클리어 기록 초기화 및 씬 재시작
        if (Input.GetKeyDown(KeyCode.F12))
        {
            StageClearRepository.Instance.ClearAllRecords();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // 2. 숫자 키 1~9: 책장 왼쪽에서부터 순서대로 강제 클리어
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ForceClearBook(i);
            }
        }
    }

    private void ForceClearBook(int index)
    {
        var books = FindObjectsByType<BookshelfBook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (books == null || books.Length == 0) return;

        // 책들을 왼쪽(X좌표 작은 순)에서 오른쪽으로 정렬하여 1, 2, 3... 키에 대응
        System.Array.Sort(books, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        if (index >= 0 && index < books.Length)
        {
            var targetBook = books[index];
            string sid = targetBook.StageId;

            if (!string.IsNullOrEmpty(sid))
            {
                StageClearRepository.Instance.RecordClear(sid);
                Debug.Log($"[Debug] 스테이지 강제 클리어 처리됨: {sid}");

                // 씬 내 모든 책들의 시각적 상태(색상, 잠금) 즉시 갱신
                foreach (var b in books)
                {
                    b.RefreshClearedState();
                    b.RefreshLockState();
                }
            }
        }
    }

    // ── Private — 초기화 ─────────────────────────────────────────────────────

    private void InitializeCameras()
    {
        if (_cameraSequence == null || _cameraSequence.Length == 0) return;

        // 첫 번째 카메라만 최고 Priority, 나머지는 0으로 초기화
        for (int i = 0; i < _cameraSequence.Length; i++)
        {
            if (_cameraSequence[i] == null) continue;
            _cameraSequence[i].Priority = (i == 0) ? 10 : 0;
        }
    }

    private void InitializeFade()
    {
        if (_fadeImage == null) return;
        // 시작 시 페이드 이미지 완전 불투명 (Start에서 FadeIn 실행)
        var c = _fadeImage.color;
        _fadeImage.color = new Color(c.r, c.g, c.b, 1f);
        _fadeImage.raycastTarget = true;
    }

    private IEnumerator FadeIn()
    {
        if (_fadeImage == null) yield break;

        yield return _fadeImage.DOFade(0f, _fadeDuration)
                  .SetEase(Ease.OutQuad)
                  .WaitForCompletion();
        
        _fadeImage.raycastTarget = false;
    }

    // ── Private — 메인 시퀀스 ────────────────────────────────────────────────

    private IEnumerator PlayTitleSequence(Transform bookTransform)
    {
        // 책 이동 & 카메라 전환을 동시에 실행
        bool bookDone   = false;
        bool cameraDone = false;

        StartCoroutine(MoveBookAlongPath(bookTransform, () => bookDone = true));
        StartCoroutine(PlayCameraSequence(() => cameraDone = true));

        // 둘 다 완료될 때까지 대기
        yield return new WaitUntil(() => bookDone && cameraDone);

        // 페이드 아웃 후 씬 전환
        yield return FadeOut();
        yield return new WaitForSeconds(_fadeHoldDuration);

        SceneManager.LoadScene(_currentTargetSceneName);
    }

    // ── Private — 책 이동 ────────────────────────────────────────────────────

    private IEnumerator MoveBookAlongPath(Transform bookTransform, Action onComplete)
    {
        Quaternion rotationOffset = Quaternion.identity;
        Vector3 scaleOffset = Vector3.one;

        // 웨이포인트 순서대로 이동
        if (_waypoints != null && _waypoints.Length > 0)
        {
            for (int i = 0; i < _waypoints.Length; i++)
            {
                var wp = _waypoints[i];
                if (wp == null) continue;
                
                // 이전에 첫 번째 웨이포인트에서 회전 및 스케일 변화가 발생했다면 해당 오프셋을 유지하며 이동
                yield return MoveToTarget(bookTransform, wp.position, wp.rotation * rotationOffset, Vector3.Scale(wp.localScale, scaleOffset));

                // 첫 번째 웨이포인트 도착 시 추가 회전 및 스케일 연출
                if (i == 0)
                {
                    Quaternion rotBefore = bookTransform.rotation;
                    Vector3 scaleBefore = bookTransform.localScale;
                    Sequence wpSeq = DOTween.Sequence();

                    wpSeq.Join(bookTransform.DORotate(bookTransform.eulerAngles + _firstWPRotationAmount, _firstWPRotationDuration, RotateMode.FastBeyond360)
                        .SetEase(_firstWPRotationEase));

                    if (_firstWPScale != Vector3.zero)
                    {
                        wpSeq.Join(bookTransform.DOScale(_firstWPScale, _firstWPScaleDuration).SetEase(_firstWPScaleEase));
                    }

                    yield return wpSeq.WaitForCompletion();
                    
                    // 회전 및 스케일 연출 후의 상태와 원래 웨이포인트 값 사이의 차이를 오프셋으로 저장
                    rotationOffset = bookTransform.rotation * Quaternion.Inverse(rotBefore);
                    scaleOffset = new Vector3(
                        scaleBefore.x == 0f ? 1f : bookTransform.localScale.x / scaleBefore.x,
                        scaleBefore.y == 0f ? 1f : bookTransform.localScale.y / scaleBefore.y,
                        scaleBefore.z == 0f ? 1f : bookTransform.localScale.z / scaleBefore.z
                    );
                }
            }
        }

        // 최종 목적지 (책상 위) - 오프셋을 유지한 채 이동
        if (_bookDestination != null)
            yield return MoveToTarget(bookTransform, _bookDestination.position, _bookDestination.rotation * rotationOffset, bookTransform.localScale);

        onComplete?.Invoke();
    }

    private IEnumerator MoveToTarget(Transform target, Vector3 destPos, Quaternion destRot, Vector3 destScale)
    {
        target.DORotateQuaternion(destRot, _moveSegmentDuration)
              .SetEase(_moveEase);

        target.DOScale(destScale, _moveSegmentDuration)
              .SetEase(_moveEase);

        yield return target.DOMove(destPos, _moveSegmentDuration)
              .SetEase(_moveEase)
              .WaitForCompletion();
    }

    // ── Private — 카메라 시퀀스 ──────────────────────────────────────────────

    private IEnumerator PlayCameraSequence(Action onComplete)
    {
        if (_cameraSequence == null || _cameraSequence.Length == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < _cameraSequence.Length; i++)
        {
            // 현재 카메라 활성화 (이전 카메라는 비활성)
            ActivateCamera(i);

            // 마지막 카메라는 페이드가 끝날 때까지 유지하므로 대기 없음
            if (i < _cameraSequence.Length - 1)
            {
                float holdTime = GetHoldTime(i);
                yield return new WaitForSeconds(holdTime);
            }
        }

        onComplete?.Invoke();
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < _cameraSequence.Length; i++)
        {
            if (_cameraSequence[i] == null) continue;
            _cameraSequence[i].Priority = (i == index) ? 10 : 0;
        }
    }

    private float GetHoldTime(int index)
    {
        if (_cameraHoldTimes == null || index >= _cameraHoldTimes.Length)
            return _moveSegmentDuration; // 폴백: 책 이동 구간 시간과 동일
        return _cameraHoldTimes[index];
    }

    // ── Private — 페이드 ────────────────────────────────────────────────────

    private IEnumerator FadeOut()
    {
        if (_fadeImage == null)
        {
            yield break;
        }

        yield return _fadeImage.DOFade(1f, _fadeDuration)
                  .SetEase(Ease.InQuad)
                  .WaitForCompletion();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_cameraSequence != null && _cameraHoldTimes != null
            && _cameraSequence.Length != _cameraHoldTimes.Length)
        {
            Debug.LogWarning(
                "[TitleSceneController] CameraSequence와 CameraHoldTimes의 길이가 다릅니다. " +
                "같은 길이로 맞춰주세요.");
        }
    }
#endif
}
