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
    private Vector2 _hiddenAnchoredPos;
    private bool _isShown = false;
    private Tweener _moveTween;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _hiddenAnchoredPos = _rect.anchoredPosition;
    }

    private void Start()
    {
        if (_inputField != null)
        {
            // 1. 매 스테이지마다 메모 초기화
            _inputField.text = "";

            // 2. 엔터 키를 눌렀을 때 줄바꿈이 되도록 강제 설정
            _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        }
    }

    private void Update()
    {
        // 3. 메모장이 열려있을 때, 마우스 좌클릭이 발생하면 바깥 영역인지 검사합니다.
        if (_isShown && Input.GetMouseButtonDown(0))
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            // 클릭한 마우스 좌표가 메모장 패널(RectTransform) 영역 밖이라면 메모장을 닫음
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, cam))
            {
                Hide();
            }
        }
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
        {
            // 이동 애니메이션이 시작되기도 전에 미리 포커스를 맞추되, 번쩍임을 막기 위해 색상을 투명하게 처리
            StartCoroutine(FocusWithoutFlash());
        }

        if (instant)
        {
            _rect.anchoredPosition = targetPos;
        }
        else
        {
            _moveTween = _rect.DOAnchorPos(targetPos, _animDuration).SetEase(ease);
        }
    }

    private System.Collections.IEnumerator FocusWithoutFlash()
    {
        // 1. 현재 선택 색상을 저장하고 잠시 투명하게 만듦 (번쩍임 방지)
        Color originalSelectionColor = _inputField.selectionColor;
        _inputField.selectionColor = new Color(0, 0, 0, 0);

        // 2. 입력창 활성화 (이때 내부적으로 전체 선택이 일어나지만 투명해서 안 보임)
        _inputField.ActivateInputField();
        
        // 3. 한 프레임 대기 (유니티의 강제 선택 로직이 끝날 때까지)
        yield return null;

        // 4. 커서를 맨 끝으로 이동시키고 선택 영역(Highlight)을 강제로 해제
        int textLength = _inputField.text.Length;
        _inputField.caretPosition = textLength;
        _inputField.selectionAnchorPosition = textLength;
        _inputField.selectionFocusPosition = textLength;

        // 5. 원래 색상으로 복구
        _inputField.selectionColor = originalSelectionColor;
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}
