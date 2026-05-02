using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인게임 메모장 UI를 관리합니다.
/// 클릭 시 펼쳐지고 닫히며, 입력 중 Enter는 줄바꿈으로 동작합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InGameNotepad : MonoBehaviour, IPointerClickHandler
{
    [Header("위치 설정")]
    [Tooltip("메모장이 펼쳐졌을 때의 anchored position")]
    [SerializeField] private Vector2 _shownAnchoredPos = Vector2.zero;

    [Header("DOTween 설정")]
    [SerializeField] private float _animDuration = 0.4f;
    [SerializeField] private Ease _showEase = Ease.OutCubic;
    [SerializeField] private Ease _hideEase = Ease.InCubic;

    [Header("메모장 UI")]
    [Tooltip("사용자가 메모를 입력할 TextMeshPro InputField")]
    [SerializeField] private TMP_InputField _inputField;

    private RectTransform _rect;
    private Vector2 _hiddenAnchoredPos;
    private bool _isShown;
    private Tweener _moveTween;
    private Coroutine _focusCoroutine;
    private Color _selectionColor;

    private int _lastCaretPosition;
    private bool _clickedOutside;
    private bool _suppressEndEdit;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _hiddenAnchoredPos = _rect.anchoredPosition;
    }

    private void Start()
    {
        if (_inputField == null) return;

        _inputField.text = string.Empty;
        _inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        _selectionColor = _inputField.selectionColor;
        _inputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    private void Update()
    {
        _clickedOutside = false;

        if (_inputField != null && _inputField.isFocused)
            _lastCaretPosition = _inputField.caretPosition;

        if (!_isShown || !Input.GetMouseButtonDown(0))
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        if (RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, cam))
            return;

        _clickedOutside = true;
        Hide();
    }

    /// <summary>
    /// Enter 입력은 줄바꿈으로 유지하고, 닫힘이나 바깥 클릭으로 발생한 EndEdit는 무시합니다.
    /// </summary>
    private void OnInputEndEdit(string text)
    {
        if (_inputField == null) return;

        if (_suppressEndEdit)
        {
            _suppressEndEdit = false;
            return;
        }

        if (!_isShown) return;
        if (_inputField.wasCanceled) return;
        if (_clickedOutside) return;

        string safeText = text ?? string.Empty;
        int insertIndex = Mathf.Clamp(_lastCaretPosition, 0, safeText.Length);
        _inputField.text = safeText.Insert(insertIndex, "\n");
        _inputField.ActivateInputField();

        int next = insertIndex + 1;
        _inputField.caretPosition = next;
        _inputField.selectionAnchorPosition = next;
        _inputField.selectionFocusPosition = next;
    }

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
        StopFocusRoutine();

        if (show && _inputField != null)
        {
            _focusCoroutine = StartCoroutine(FocusWithoutFlash());
        }
        else if (!show && _inputField != null)
        {
            _suppressEndEdit = true;
            _inputField.selectionColor = _selectionColor;
            _inputField.DeactivateInputField();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        if (instant)
            _rect.anchoredPosition = targetPos;
        else
            _moveTween = _rect.DOAnchorPos(targetPos, _animDuration).SetEase(ease);
    }

    private IEnumerator FocusWithoutFlash()
    {
        _inputField.selectionColor = new Color(0f, 0f, 0f, 0f);
        _inputField.ActivateInputField();
        yield return null;

        if (!_isShown || _inputField == null)
        {
            if (_inputField != null)
                _inputField.selectionColor = _selectionColor;
            _focusCoroutine = null;
            yield break;
        }

        int textLength = _inputField.text.Length;
        _inputField.caretPosition = textLength;
        _inputField.selectionAnchorPosition = textLength;
        _inputField.selectionFocusPosition = textLength;
        _inputField.selectionColor = _selectionColor;
        _focusCoroutine = null;
    }

    private void StopFocusRoutine()
    {
        if (_focusCoroutine == null) return;

        StopCoroutine(_focusCoroutine);
        if (_inputField != null)
            _inputField.selectionColor = _selectionColor;
        _focusCoroutine = null;
    }

    private void OnDestroy()
    {
        if (_inputField != null)
            _inputField.onEndEdit.RemoveListener(OnInputEndEdit);

        StopFocusRoutine();
        _moveTween?.Kill();
    }
}
