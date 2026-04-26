using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 스테이지 전용 로비 씬 컨트롤러입니다.
///
/// 이 컨트롤러는 페이드 인 연출만 담당합니다.
/// 나머지 흐름은 기존 컴포넌트들이 그대로 처리합니다:
///
///   LobbyDialogueManager → 대사 재생 → 클릭 → 책 열기 허용
///   TitleBookAnimator    → 키 입력 → 책 펼침 → LobbyUIManager.Show()
///   LobbyUIManager       → 챕터(페이지) UI 전환 (본편 / What if / ...)
///   LobbyUnlockManager   → 스테이지 클리어에 따른 버튼 해금
///   StageSelectButton    → 개별 버튼이 직접 게임 씬 로드
///
/// 씬 흐름:
///   페이드 인 → LobbyDialogueManager (대사)
///   → TitleBookAnimator (책 펼침) → LobbyUIManager.Show()
///   → 첫 페이지 표시 (본편) → 넘기면 What if
///   → StageSelectButton 클릭 → 게임 씬 로드
/// </summary>
[DisallowMultipleComponent]
public class StageLobbyController : MonoBehaviour
{
    [Header("페이드")]
    [Tooltip("전체 화면 덮는 Image. 씬 시작 시 Alpha=1에서 0으로 페이드 인됩니다.")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeInDuration = 0.6f;

    [Header("뒤로가기")]
    [Tooltip("타이틀씬으로 돌아가는 버튼 (선택)")]
    [SerializeField] private Button _backButton;
    [SerializeField] private string _titleSceneName = "TitleScene";

    [Header("컴포넌트 참조")]
    [SerializeField] private TitleBookAnimator _bookAnimator;
    [SerializeField] private LobbyDialogueManager _dialogueManager;
    [SerializeField] private StageSelectButton[] _stageButtons;
    [SerializeField] private float _afterFadeInDelay = 0.5f;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 다이얼로그 매니저가 있으면 자동으로 찾아 연결 (미연결 시)
        if (_dialogueManager == null)
            _dialogueManager = FindObjectOfType<LobbyDialogueManager>();

        // 책 열기 입력 차단 (다이얼로그 완료 후 열 예정)
        if (_bookAnimator != null)
            _bookAnimator.SetInputReady(false);

        // 페이드 이미지 시작 상태: 완전 불투명 (타이틀에서 페이드 아웃 → 여기서 페이드 인)
        if (_fadeImage != null)
        {
            var c = _fadeImage.color;
            _fadeImage.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    private void Start()
    {
        _backButton?.onClick.AddListener(() => StartCoroutine(BackToTitleSequence()));

        // 버튼 잠금/해금 상태 갱신
        RefreshAllButtons();

        // 연출 시퀀스 시작
        StartCoroutine(PlayLobbySequence());
    }

    private void Update()
    {
        // 디버그: F12 키를 누르면 모든 기록 초기화 후 현재 씬 재시작
        if (Input.GetKeyDown(KeyCode.F12))
        {
            StageClearRepository.Instance.ClearAllRecords();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ── 시퀀스 ────────────────────────────────────────────────────────────────

    private IEnumerator PlayLobbySequence()
    {
        // 1. 페이드 인 (검정 → 투명)
        yield return FadeIn();

        // 2. 다이얼로그 매니저가 있다면 모든 대사와 힌트 단계가 끝날 때까지 대기
        if (_dialogueManager != null)
        {
            yield return new WaitUntil(() => _dialogueManager.IsComplete);
        }

        // 3. 잠깐 대기 (페이드나 대사 연출 후 여유 시간)
        if (_afterFadeInDelay > 0f)
            yield return new WaitForSeconds(_afterFadeInDelay);

        // 4. 자동으로 책 열기
        //    이후 TitleBookAnimator → LobbyUIManager.Show() 가 자동 진행됩니다.
        _bookAnimator?.ForceOpen();
    }


    // ── 버튼 상태 갱신 ────────────────────────────────────────────────────────

    /// <summary>
    /// 모든 StageSelectButton의 잠금/클리어 상태를 갱신합니다.
    /// 외부(예: 씬 복귀 후)에서도 호출 가능합니다.
    /// </summary>
    public void RefreshAllButtons()
    {
        if (_stageButtons == null) return;
        foreach (var btn in _stageButtons)
            btn?.Refresh();
    }

    // ── 페이드 ────────────────────────────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        if (_fadeImage == null) yield break;

        yield return _fadeImage.DOFade(0f, _fadeInDuration)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        // 페이드 완료 후 레이캐스트 차단 해제
        _fadeImage.raycastTarget = false;
    }

    private bool _isFadingOut = false;

    private IEnumerator BackToTitleSequence()
    {
        if (_isFadingOut) yield break;
        _isFadingOut = true;

        // 뒤로가기 클릭 시 페이드 아웃 후 씬 전환
        yield return FadeOut();
        
        NewGameConfig.Clear();
        SceneManager.LoadScene(_titleSceneName);
    }

    private IEnumerator FadeOut()
    {
        if (_fadeImage == null) yield break;

        _fadeImage.raycastTarget = true; // 페이드 시작 시 클릭 즉시 차단
        yield return _fadeImage.DOFade(1f, _fadeInDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
    }
}
