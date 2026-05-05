using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 직후 재생되는 컷씬을 관리합니다.
///
/// ── 흐름 ──
///   [StageClearSequenceController] 가 컷씬을 갖고 있으면 PlayCutscene()을 먼저 호출합니다.
///   1. 화면 페이드 아웃(어두워짐)
///   2. 컷씬 패널 활성화 (이미지 + 대사 표시)
///   3. 화면 페이드 인(밝아짐)
///   4. 대사들을 타이핑 효과로 순서대로 표시, 클릭으로 다음 대사로 이동
///   5. 마지막 대사 클릭 후 화면 페이드 아웃
///   6. 컷씬 패널 비활성화
///   7. onComplete 콜백 호출 → StageClearSequenceController가 기존 클리어 연출 시작
///
/// ── Inspector 구성 (Canvas 안에 아래 구조를 만드세요) ──
///   CutsceneRoot (GameObject)          ← _cutsceneRoot  (기본 비활성화)
///   ├── Background (Image)             ← _backgroundImage
///   └── DialogueArea (Transform)
///       └── DialogueText (TMP_Text)    ← _dialogueText
///   FadePanel (Image, 검정, full-stretch) ← _fadePanel (CanvasGroup 포함)
/// </summary>
[DisallowMultipleComponent]
public class StageClearCutsceneController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("컷씬 데이터")]
    [Tooltip("StageClearCutsceneSO 에셋을 연결하세요. null이면 컷씬을 건너뜁니다.")]
    [SerializeField] private StageClearCutsceneSO _cutscene;

    [Header("UI 참조")]
    [Tooltip("컷씬 전체를 감싸는 루트 GameObject. 기본적으로 비활성화 상태여야 합니다.")]
    [SerializeField] private GameObject _cutsceneRoot;

    [Tooltip("배경 이미지를 표시하는 UI Image 컴포넌트.")]
    [SerializeField] private Image _backgroundImage;

    [Tooltip("대사를 표시하는 TMP_Text 컴포넌트.")]
    [SerializeField] private TMP_Text _dialogueText;

    [Header("페이드 패널")]
    [Tooltip("화면 전체를 덮는 CanvasGroup (검정 Image 위에 얹음). 알파 1 = 완전 어둠.")]
    [SerializeField] private CanvasGroup _fadePanel;

    [Header("타이핑 설정")]
    [SerializeField] [Range(0.01f, 0.2f)] private float _charDelay  = 0.04f;
    [SerializeField] [Range(0.05f, 1f)]   private float _inputDelay = 0.3f;
    [SerializeField]                       private string _cursorChar = "▮";
    [SerializeField] [Range(0.1f, 1f)]    private float _cursorBlink = 0.5f;

    [Header("페이드 시간")]
    [SerializeField] [Range(0.1f, 2f)] private float _fadeOutDuration = 0.6f;  // 컷씬 진입 시 어두워지는 시간
    [SerializeField] [Range(0.1f, 2f)] private float _fadeInDuration  = 0.6f;  // 컷씬 이미지 나타나는 시간
    [SerializeField] [Range(0.1f, 2f)] private float _exitFadeDuration = 0.8f; // 컷씬 종료 시 어두워지는 시간

    // ── 런타임 상태 ───────────────────────────────────────────────────────

    private string[] _lines;
    private int      _lineIndex;

    private string _currentFullLine;
    private string _currentDisplay;
    private bool   _isTyping;
    private bool   _inputEnabled;
    private bool   _cursorOn = true;

    private Coroutine _typewriterCo;
    private Coroutine _cursorCo;
    private Coroutine _playCo;

    private Action _onComplete;
    private Action _onFadeOutComplete;
    private bool   _isPlaying;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// 컷씬 데이터가 설정되어 있고 오브젝트가 활성화된 경우 true를 반환합니다.
    /// StageClearSequenceController가 이 값으로 컷씬 재생 여부를 결정합니다.
    /// </summary>
    public bool HasCutscene => _cutscene != null
                               && _cutscene.lines != null
                               && _cutscene.lines.Length > 0
                               && enabled
                               && gameObject.activeInHierarchy
                               && !(GameFlowController.Instance != null && GameFlowController.Instance.IsEpilogue);

    /// <summary>
    /// 컷씬을 재생합니다. 컷씬이 끝나면 <paramref name="onComplete"/>를 호출합니다.
    /// HasCutscene이 false이면 즉시 콜백을 호출합니다.
    /// </summary>
    public void PlayCutscene(Action onComplete, Action onFadeOutComplete = null)
    {
        if (!HasCutscene)
        {
            onFadeOutComplete?.Invoke();
            onComplete?.Invoke();
            return;
        }

        if (_isPlaying)
        {
            Debug.LogWarning("[StageClearCutscene] 이미 재생 중입니다.");
            return;
        }

        _onComplete = onComplete;
        _onFadeOutComplete = onFadeOutComplete;
        _playCo     = StartCoroutine(PlayCoroutine());
    }

    // ── Unity ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 시작 시 컷씬 패널은 반드시 꺼져 있어야 합니다.
        if (_cutsceneRoot != null) _cutsceneRoot.SetActive(false);

        // 페이드 패널은 투명하게 시작합니다.
        if (_fadePanel != null)
        {
            _fadePanel.alpha          = 0f;
            _fadePanel.blocksRaycasts = false;
            _fadePanel.interactable   = false;
        }
    }

    private void Update()
    {
        // 컷씬 재생 중이 아닐 때는 입력을 무시합니다.
        if (!_isPlaying) return;
        if (!_inputEnabled) return;
        if (Input.GetMouseButtonDown(0)) OnClick();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    // ── 핵심 코루틴 ───────────────────────────────────────────────────────

    private IEnumerator PlayCoroutine()
    {
        _isPlaying  = true;
        _inputEnabled = false;

        // 대사 배열 준비
        _lines     = _cutscene.lines;
        _lineIndex = 0;

        // ── Step 1: 화면 페이드 아웃 (어두워짐) ──
        yield return FadeCanvas(_fadePanel, 0f, 1f, _fadeOutDuration);

        // 진입 페이드 완료 시점에 씬 준비 콜백 호출
        var fadeCb = _onFadeOutComplete;
        _onFadeOutComplete = null;
        fadeCb?.Invoke();

        // ── Step 2: 컷씬 패널 활성화 & 배경 이미지 설정 ──
        if (_backgroundImage != null && _cutscene.backgroundImage != null)
            _backgroundImage.sprite = _cutscene.backgroundImage;

        if (_cutsceneRoot != null) _cutsceneRoot.SetActive(true);
        if (_dialogueText != null) _dialogueText.text = "";

        // ── Step 3: 화면 페이드 인 (밝아짐) ──
        yield return FadeCanvas(_fadePanel, 1f, 0f, _fadeInDuration);

        _fadePanel.blocksRaycasts = false;

        // ── Step 4: 대사 순서 재생 (클릭으로 진행, ShowLine이 핵심) ──
        // ShowLine은 대사를 표시하고, _inputEnabled를 통해 클릭 대기로 넘깁니다.
        // OnClick → 마지막 대사 이후 _lineIndex가 _lines.Length에 도달하면
        // 이 코루틴에 제어를 돌려주기 위해 WaitUntil을 사용합니다.
        ShowLine();
        yield return new WaitUntil(() => !_isPlaying || _lineIndex >= _lines.Length);

        // ── Step 5: 컷씬 종료 페이드 아웃 ──
        _fadePanel.blocksRaycasts = true;
        yield return FadeCanvas(_fadePanel, 0f, 1f, _exitFadeDuration);

        // ── Step 6: 컷씬 패널 비활성화 ──
        StopDialogueCoroutines();
        if (_cutsceneRoot != null) _cutsceneRoot.SetActive(false);
        if (_dialogueText != null) _dialogueText.text = "";

        _isPlaying  = false;
        _lineIndex  = _lines.Length; // 명시적으로 종료 상태 표기

        // ── Step 7: 콜백 → StageClearSequenceController가 기존 연출 재개 ──
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

    // ── 대사 재생 ─────────────────────────────────────────────────────────

    private void ShowLine()
    {
        if (_lineIndex >= _lines.Length)
        {
            // 모든 대사 완료 → PlayCoroutine의 WaitUntil 조건을 충족시킵니다.
            _isPlaying = false;
            return;
        }

        // 배경 변경 조건 확인
        if (_backgroundImage != null && _cutscene.backgroundChanges != null)
        {
            foreach (var change in _cutscene.backgroundChanges)
            {
                if (change.lineIndex == _lineIndex && change.backgroundImage != null)
                {
                    _backgroundImage.sprite = change.backgroundImage;
                    break;
                }
            }
        }

        if (_dialogueText != null) _dialogueText.text = "";

        _currentFullLine = _lines[_lineIndex];
        _currentDisplay  = "";
        _isTyping        = true;
        _inputEnabled    = false;

        StopDialogueCoroutines();
        _typewriterCo = StartCoroutine(TypewriterCoroutine());
        _cursorCo     = StartCoroutine(CursorBlinkCoroutine());
    }

    private void OnClick()
    {
        if (!_inputEnabled) return;

        if (_isTyping)
        {
            // 타이핑 중 → 전체 텍스트 즉시 표시
            StopCoroutineSafe(ref _typewriterCo);
            _isTyping       = false;
            _currentDisplay = _currentFullLine;
            RefreshDisplay();
        }
        else
        {
            // 타이핑 완료 → 다음 대사로
            _lineIndex++;
            ShowLine();
        }
    }

    // ── 코루틴 ────────────────────────────────────────────────────────────

    private IEnumerator TypewriterCoroutine()
    {
        // 입력 딜레이 — 클릭이 즉시 다음 대사로 넘어가는 것을 방지합니다.
        yield return new WaitForSeconds(_inputDelay);
        _inputEnabled = true;

        _currentDisplay = "";
        for (int i = 0; i < _currentFullLine.Length; i++)
        {
            // Rich Text 태그 통째로 삽입 (지연 없음)
            if (_currentFullLine[i] == '<')
            {
                int closeIdx = _currentFullLine.IndexOf('>', i);
                if (closeIdx != -1)
                {
                    _currentDisplay += _currentFullLine.Substring(i, closeIdx - i + 1);
                    i = closeIdx;
                    continue;
                }
            }

            _currentDisplay += _currentFullLine[i];
            RefreshDisplay();
            yield return new WaitForSeconds(_charDelay);
        }

        _isTyping     = false;
        _typewriterCo = null;
    }

    private IEnumerator CursorBlinkCoroutine()
    {
        while (true)
        {
            _cursorOn = !_cursorOn;
            RefreshDisplay();
            yield return new WaitForSeconds(_cursorBlink);
        }
    }

    private void RefreshDisplay()
    {
        if (_dialogueText == null) return;
        _dialogueText.text = _currentDisplay + (_cursorOn ? _cursorChar : " ");
    }

    // ── 페이드 유틸 ───────────────────────────────────────────────────────

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.blocksRaycasts = true;
        cg.alpha          = from;

        yield return cg.DOFade(to, duration)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .WaitForCompletion();
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    private void StopDialogueCoroutines()
    {
        StopCoroutineSafe(ref _typewriterCo);
        StopCoroutineSafe(ref _cursorCo);
    }

    private void StopCoroutineSafe(ref Coroutine co)
    {
        if (co == null) return;
        StopCoroutine(co);
        co = null;
    }
}
