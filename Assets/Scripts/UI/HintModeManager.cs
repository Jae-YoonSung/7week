using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 힌트 선택 모드를 관리합니다.
///
/// 사용법:
///   1. 힌트 활성화 버튼(_hintButton)을 연결합니다.
///   2. O/X 스프라이트(_oSprite, _xSprite)를 지정합니다.
///   3. 힌트 버튼 텍스트 색(_normalColor, _activeModeColor)을 지정합니다.
///   4. 힌트 버튼 클릭 → 힌트 선택 모드 진입 (재클릭 시 취소)
///   5. SequentialImageToggle 버튼 클릭 → 클릭 지점에서 퍼지는 연출로 O/X 표시
/// </summary>
public class HintModeManager : SingletonMonobehaviour<HintModeManager>
{
    [Header("힌트 활성화 버튼")]
    [SerializeField] private Button _hintButton;

    [Header("힌트 모드 중 표시할 텍스트 오브젝트")]
    [SerializeField] private GameObject _hintModeText;

    [Header("힌트 모드 텍스트 색상")]
    [SerializeField] private Color _normalColor     = Color.white;
    [SerializeField] private Color _activeModeColor = Color.yellow;
    [SerializeField] private Color _usedColor       = Color.gray;

    [Header("판정 결과 스프라이트")]
    [SerializeField] private Sprite _oSprite;
    [SerializeField] private Sprite _xSprite;

    [Header("퍼짐 연출")]
    [SerializeField] private float _staggerDelay   = 0.15f;
    [SerializeField] private float _tierThreshold  = 60f;   // 같은 링으로 묶을 거리 허용치 (픽셀)

    public bool IsHintMode { get; private set; }

    private bool _hintUsed;
    private TextMeshProUGUI _buttonText;

    private void Start()
    {
        if (_hintButton != null)
        {
            _buttonText = _hintButton.GetComponentInChildren<TextMeshProUGUI>();
            _hintButton.onClick.AddListener(ToggleHintMode);
        }

        if (_hintModeText != null)
            _hintModeText.SetActive(false);

        if (_buttonText != null)
            _buttonText.color = _normalColor;
    }

    private void OnDestroy()
    {
        if (_hintButton != null)
            _hintButton.onClick.RemoveListener(ToggleHintMode);
    }

    private void ToggleHintMode()
    {
        if (_hintUsed) return;

        IsHintMode = !IsHintMode;

        if (IsHintMode)
        {
            if (_blinkCoroutine != null) { StopCoroutine(_blinkCoroutine); _blinkCoroutine = null; }

            // 튜토리얼 중이면 힌트 버튼 클릭 즐시 화살표 끄기
            TutorialManager.Instance?.HandleHintUIClicked();
        }
        else
        {
            _blinkCoroutine = StartCoroutine(BlinkIdleColor());
        }
        UpdateButtonColor();
    }

    /// <summary>
    /// 버튼의 캐릭터 ID·역할을 실제 게임 역할과 비교해 O/X를 표시하고 힌트 모드를 종료합니다.
    /// O가 뜨면 같은 캐릭터 ID 또는 같은 역할을 가진 나머지 버튼들을 거리 순으로 퍼져나가며 X 표시합니다.
    /// </summary>
    public void EvaluateAndShow(SequentialImageToggle source)
    {
        IsHintMode = false;
        _hintUsed = true;
        if (_hintButton != null) _hintButton.interactable = false;
        UpdateButtonColor();

        var actualRole = GameFlowController.Instance.GetActualRole(source.CharacterId);
        bool isCorrect = actualRole == source.Role;

        StartCoroutine(SpreadResults(source, isCorrect));

        // 튜토리얼에 힌트 사용 완료 알림
        TutorialManager.Instance?.NotifyHintUsed();
    }

    private IEnumerator SpreadResults(SequentialImageToggle source, bool isCorrect)
    {
        source.ShowResultSprite(isCorrect ? _oSprite : _xSprite);

        if (!isCorrect) yield break;

        var related = new List<SequentialImageToggle>();
        foreach (var toggle in FindObjectsOfType<SequentialImageToggle>())
        {
            if (toggle == source) continue;
            if (toggle.CharacterId == source.CharacterId || toggle.Role == source.Role)
                related.Add(toggle);
        }

        // 캔버스 모드에 상관없이 올바른 스크린 좌표로 거리 계산
        Canvas canvas = source.GetComponentInParent<Canvas>();
        Camera uiCam  = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? canvas.worldCamera : null;

        Vector2 origin = RectTransformUtility.WorldToScreenPoint(uiCam, source.transform.position);

        related.Sort((a, b) =>
        {
            float da = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(uiCam, a.transform.position), origin);
            float db = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(uiCam, b.transform.position), origin);
            return da.CompareTo(db);
        });

        // 거리가 비슷한 버튼들을 같은 링(tier)으로 묶어 동시에 표시
        int i = 0;
        while (i < related.Count)
        {
            yield return new WaitForSeconds(_staggerDelay);

            float ringDist = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(uiCam, related[i].transform.position), origin);
            while (i < related.Count &&
                   Vector2.Distance(RectTransformUtility.WorldToScreenPoint(uiCam, related[i].transform.position), origin) <= ringDist + _tierThreshold)
            {
                related[i].ShowResultSprite(_xSprite);
                i++;
            }
        }
    }

    private void UpdateButtonColor()
    {
        if (_buttonText == null) return;
        _buttonText.color = _hintUsed ? _usedColor : (IsHintMode ? _activeModeColor : _normalColor);

        if (_hintModeText != null)
            _hintModeText.SetActive(IsHintMode);
    }
}
