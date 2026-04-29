using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// 인게임 메모장 UI를 관리합니다.
/// DrawerPanel처럼 클릭 시 화면 밖에서 부드럽게 슬라이드되어 나타나고 들어갑니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InGameNotepad : MonoBehaviour, IPointerClickHandler
{
    [Header("위치 설정 (Drawer 연출)")]
    [Tooltip("완전히 꺼낸 상태의 anchoredPosition (숨김 위치는 시작 시 현재 위치로 자동 캡처)")]
    [SerializeField] private Vector2 _shownAnchoredPos = Vector2.zero;

    [Header("DOTween 설정")]
    [SerializeField] private float _animDuration = 0.4f;
    [SerializeField] private Ease  _showEase     = Ease.OutCubic;
    [SerializeField] private Ease  _hideEase     = Ease.InCubic;

    [Header("메모장 UI")]
    [Tooltip("사용자가 글씨를 입력할 InputField (TextMeshPro)")]
    [SerializeField] private TMP_InputField _inputField;

    private RectTransform _rect;
    private Vector2       _hiddenAnchoredPos;
    private bool          _isShown = false;
    private Tweener       _moveTween;

    private int  _lastCaretPosition  = 0;
    private bool _clickedOutside     = false; // 이번 프레임에 바깥 클릭으로 닫혔는지

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _hiddenAnchoredPos = _rect.anchoredPosition;
    }

    private void Start()
    {
        if (_inputField != null)
        {
            _inputField.text     = "";
            _inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            _inputField.onEndEdit.AddListener(OnInputEndEdit);
        }
    }

    private void Update()
    {
        _clickedOutside = false;

        if (_inputField != null && _inputField.isFocused)
            _lastCaretPosition = _inputField.caretPosition;

        if (_isShown && Input.GetMouseButtonDown(0))
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (!RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, cam))
            {
                _clickedOutside = true;
                Hide();
            }
        }
    }

    /// <summary>
    /// onEndEdit 발생 원인:
    ///   - Escape        → wasCanceled = true  → 무시
    ///   - 바깥 클릭     → _clickedOutside = true → 무시
    ///   - Enter(Submit) → 위 두 경우가 아님 → 줄바꿈 삽입 후 재활성화
    /// </summary>
    private void OnInputEndEdit(string text)
    {
        if (_inputField.wasCanceled) return;
        if (_clickedOutside) return;

        _inputField.text = text.Insert(_lastCaretPosition, "\n");
        _inputField.ActivateInputField();
        int next = _lastCaretPosition + 1;
        _inputField.caretPosition           = next;
        _inputField.selectionAnchorPosition = next;
        _inputField.selectionFocusPosition  = next;
    }

    // ── 패널 클릭 시 열기/닫기 ───────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isShown) Hide();
        else Show();
    }

    public void Show(bool instant = false) => TransitionTo(true, instant);
    public void Hide(bool instant = false) => TransitionTo(false, instant);

    private void TransitionTo(bool show, bool instant)
    {
        _isShown = show;
        Vector2 targetPos = show ? _shownAnchoredPos : _hiddenAnchoredPos;
        Ease ease = show ? _showEase : _hideEase;

        _moveTween?.Kill();

        if (show && _inputField != null)
            StartCoroutine(FocusWithoutFlash());

        if (instant)
            _rect.anchoredPosition = targetPos;
        else
            _moveTween = _rect.DOAnchorPos(targetPos, _animDuration).SetEase(ease);
    }

    private System.Collections.IEnumerator FocusWithoutFlash()
    {
        Color originalSelectionColor = _inputField.selectionColor;
        _inputField.selectionColor = new Color(0, 0, 0, 0);
        _inputField.ActivateInputField();
        yield return null;

        int textLength = _inputField.text.Length;
        _inputField.caretPosition           = textLength;
        _inputField.selectionAnchorPosition = textLength;
        _inputField.selectionFocusPosition  = textLength;
        _inputField.selectionColor          = originalSelectionColor;
    }

    private void OnDestroy()
    {
        if (_inputField != null)
            _inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        _moveTween?.Kill();
    }
}
