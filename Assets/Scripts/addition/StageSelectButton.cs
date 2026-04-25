using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비씬 안의 개별 스테이지 진입 버튼에 부착합니다.
/// 본편 / What if / 무작위 세 종류를 모두 지원합니다.
///
/// 잠금 조건:
///   RequiredClearStageId가 비어있으면 항상 활성화됩니다.
///   채워져 있으면 해당 스테이지 ID가 클리어된 뒤에만 클릭 가능합니다.
///
/// Inspector 설정 예시 (Chapter 1 로비씬):
///   [본편 버튼]
///     Stage Id         : "Ch1_Main"
///     Seed             : 12345
///     Is Random        : false
///     Game Scene Name  : "Stage_1"
///     Required Clear   : ""          ← 항상 해금
///
///   [What if 버튼]
///     Stage Id         : "Ch1_WhatIf"
///     Seed             : 99999
///     Is Random        : false
///     Game Scene Name  : "Stage_1"
///     Required Clear   : "Ch1_Main"  ← 본편 클리어 후 해금
///
///   [무작위 버튼]
///     Stage Id         : "Ch1_Random"
///     Is Random        : true
///     Game Scene Name  : "Stage_1"
///     Required Clear   : "Ch1_Main"  ← 본편 클리어 후 해금
/// </summary>
[RequireComponent(typeof(Button))]
public class StageSelectButton : MonoBehaviour
{
    [Header("스테이지 정보")]
    [Tooltip("이 버튼이 시작하는 스테이지 ID (클리어 기록에 사용)")]
    [SerializeField] private string _stageId;

    [Tooltip("true면 랜덤 시드, false면 아래 Seed 값을 사용")]
    [SerializeField] private bool _isRandom = false;

    [Tooltip("고정 시드 (IsRandom = false일 때 사용)")]
    [SerializeField] private int _seed;

    [Tooltip("이 버튼을 눌렀을 때 로드할 게임플레이 씬 이름")]
    [SerializeField] private string _gameSceneName = "Stage_1";

    [Tooltip("에필로그 진입 여부")]
    [SerializeField] private bool _isEpilogue = false;

    [Header("해금 조건")]
    [Tooltip("이 버튼을 활성화하기 위해 클리어되어 있어야 할 스테이지 ID.\n비워두면 항상 해금.")]
    [SerializeField] private string _requiredClearStageId;

    [Header("UI 피드백")]
    [Tooltip("잠금 상태일 때 표시할 오버레이 오브젝트 (선택)")]
    [SerializeField] private GameObject _lockOverlay;

    [Tooltip("클리어 완료 시 텍스트에 취소선을 적용할 TMP_Text (선택)")]
    [SerializeField] private TMP_Text _label;

    // ── 프로퍼티 ─────────────────────────────────────────────────────────────

    public bool IsUnlocked => string.IsNullOrEmpty(_requiredClearStageId)
                           || StageClearRepository.Instance.HasCleared(_requiredClearStageId);

    public bool IsCleared => StageClearRepository.Instance.HasCleared(_stageId);

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
        Refresh();
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>잠금/클리어 상태를 UI에 반영합니다. 씬 시작 시 자동 호출됩니다.</summary>
    public void Refresh()
    {
        bool unlocked = IsUnlocked;

        // 버튼 인터랙션
        GetComponent<Button>().interactable = unlocked;

        // 잠금 오버레이
        if (_lockOverlay != null)
            _lockOverlay.SetActive(!unlocked);

        // 클리어 취소선
        if (_label != null)
        {
            bool cleared = IsCleared;
            string raw = StripStrikethrough(_label.text);
            _label.text = cleared ? $"<s>{raw}</s>" : raw;
        }
    }

    // ── 버튼 콜백 ────────────────────────────────────────────────────────────

    private void OnClicked()
    {
        if (!IsUnlocked) return;

        TurnHistoryRepository.Instance.ClearAll();

        if (_isRandom)
            NewGameConfig.SetRandom(_stageId, _isEpilogue);
        else
            NewGameConfig.SetSeed(_seed, _stageId, _isEpilogue);

        SceneManager.LoadScene(_gameSceneName);
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    private static string StripStrikethrough(string text)
        => text.Replace("<s>", "").Replace("</s>", "");
}
