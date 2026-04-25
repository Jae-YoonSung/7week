using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 결정 UI입니다. 씬에 미리 배치된 슬롯·카드를 사용합니다.
///
/// 흐름:
///   Submit 클릭 → PreDialogueObject 활성화 → SubmitFinalDecision(정답 여부)
///   → 게임 종료 다이얼로그(DialogueManager)
///   → OnGameEndDialogueComplete → PreDialogueObject 비활성화 → Win/Lose 텍스트 활성화
///   → _continueDelay 후 Continue 텍스트 활성화
///   → 아무 곳 클릭 → FinishGameEndDialogue() → WinState/LoseState → 로비
/// </summary>
public class FinalDecisionUI : MonoBehaviour
{
    [Header("빈칸 슬롯 (배열 인덱스 = CharacterId)")]
    [SerializeField] private RoleSlot[] _roleSlots;

    [Header("직업 카드 (RoleType은 Inspector에서 enum으로 설정)")]
    [SerializeField] private RoleCard[] _roleCards;

    [Header("결과 텍스트 — 다이얼로그 종료 후 활성화")]
    [SerializeField] private TMP_Text _winText;
    [SerializeField] private TMP_Text _loseText;
    [SerializeField] private TMP_Text _continueText;

    [Header("제출 후 다이얼로그 전 활성화할 오브젝트")]
    [SerializeField] private GameObject _preDialogueObject;

    [Header("버튼")]
    [SerializeField] private Button _submitButton;

    [Header("연출 설정")]
    [SerializeField] private float _fadeInDuration = 0.6f;
    [SerializeField] private float _continueDelay  = 2f;

    private CanvasGroup _canvasGroup;
    private bool        _awaitingContinueClick;
    private StageClearSequenceController _stageClearSequence;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _stageClearSequence = FindObjectOfType<StageClearSequenceController>();

        var gfc = GameFlowController.Instance;
        if (gfc != null)
        {
            gfc.OnFinalDecisionEntered    += Show;
            gfc.OnFinalDecisionExited     += Hide;
            gfc.OnGameEndDialogueComplete += HandleGameEndDialogueComplete;
        }

        if (_submitButton != null)
            _submitButton.onClick.AddListener(OnSubmitClicked);

        if (_preDialogueObject != null) _preDialogueObject.SetActive(false);
        HideText(_winText);
        HideText(_loseText);
        HideText(_continueText);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        var gfc = GameFlowController.Instance;
        if (gfc == null) return;
        gfc.OnFinalDecisionEntered    -= Show;
        gfc.OnFinalDecisionExited     -= Hide;
        gfc.OnGameEndDialogueComplete -= HandleGameEndDialogueComplete;
    }

    private void Update()
    {
        if (!_awaitingContinueClick) return;
        if (Input.GetMouseButtonDown(0))
        {
            _awaitingContinueClick = false;
            GameFlowController.Instance?.FinishGameEndDialogue();
        }
    }

    // ── 표시 / 숨기기 ────────────────────────────────────────────────────────

    private void Show()
    {
        gameObject.SetActive(true);
        _awaitingContinueClick = false;
        if (_preDialogueObject != null) _preDialogueObject.SetActive(false);
        HideText(_winText);
        HideText(_loseText);
        HideText(_continueText);

        for (int i = 0; i < _roleSlots.Length; i++)
            if (_roleSlots[i] != null) _roleSlots[i].Init();

        if (_submitButton != null)
            _submitButton.interactable = true;

        _canvasGroup.alpha = 0f;
        _canvasGroup.DOFade(1f, _fadeInDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
    }

    private void Hide()
    {
        _awaitingContinueClick = false;
        DOTween.Kill(gameObject);
        foreach (var slot in _roleSlots) slot?.ForceRelease();
        foreach (var card in _roleCards) card?.ReturnHomeInstant();
        if (_preDialogueObject != null) _preDialogueObject.SetActive(false);
        HideText(_winText);
        HideText(_loseText);
        HideText(_continueText);
        _canvasGroup.alpha = 1f;
        gameObject.SetActive(false);
    }

    // ── 제출 & 판정 ──────────────────────────────────────────────────────────

    private void OnSubmitClicked()
    {
        var gfc = GameFlowController.Instance;
        if (gfc == null) return;

        foreach (var slot in _roleSlots)
        {
            if (slot == null) continue;
            if (gfc.GetActualRole(slot.CharacterId) == RoleType.ZonePhantom) continue;
            if (slot.AssignedCard == null) return;
        }

        var wrongSlots = new List<RoleSlot>();
        foreach (var slot in _roleSlots)
        {
            if (slot == null || slot.AssignedCard == null) continue;
            var actual   = gfc.GetActualRole(slot.CharacterId);
            if (actual == RoleType.ZonePhantom) continue;
            var assigned = slot.AssignedCard.RoleType;
            bool correct = assigned == actual;
            Debug.Log($"[FinalDecision] CharId={slot.CharacterId}  배정={assigned}  정답={actual}  → {(correct ? "O" : "X")}");
            if (!correct) wrongSlots.Add(slot);
        }

        // 슬롯 전체를 순회해 정답/오답 모두 answer_submit 이벤트로 기록한다.
        // 기존 wrongSlots 기반 직접 로그 대신 Dispatcher를 통해
        // 파일 로그·Analytics 양쪽에 동시에 전달된다.
        foreach (var slot in _roleSlots)
        {
            if (slot == null || slot.AssignedCard == null) continue;
            var  actual  = gfc.GetActualRole(slot.CharacterId);
            bool correct = slot.AssignedCard.RoleType == actual;
            GameEventDispatcher.Raise(new AnswerSubmitEvent(
                gfc.StageId,
                gfc.LoopCount,
                slot.AssignedCard.RoleType.ToString(),
                actual.ToString(),
                correct
            ));
        }

        _submitButton.interactable = false;

        // 정답 여부에 상관없이 바로 제출 로직 진행 (정답 표시 생략)
        bool isCorrect = wrongSlots.Count == 0;
        if (_preDialogueObject != null) _preDialogueObject.SetActive(true);
        gfc.SubmitFinalDecision(isCorrect);
    }

    // ── 게임 종료 결과 텍스트 ────────────────────────────────────────────────

    private void HandleGameEndDialogueComplete(bool isWin)
    {
        if (isWin && _stageClearSequence != null && _stageClearSequence.CanHandleWinSequence)
        {
            if (_preDialogueObject != null) _preDialogueObject.SetActive(false);
            HideText(_winText);
            HideText(_loseText);
            HideText(_continueText);
            _awaitingContinueClick = false;
            return;
        }

        var resultText = isWin ? _winText : _loseText;
        if (resultText != null) resultText.gameObject.SetActive(true);

        DOVirtual.DelayedCall(_continueDelay, () =>
        {
            if (_continueText != null) _continueText.gameObject.SetActive(true);
            _awaitingContinueClick = true;
        }).SetLink(gameObject);
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    private static void HideText(TMP_Text text)
    {
        if (text == null) return;
        text.gameObject.SetActive(false);
    }
}
