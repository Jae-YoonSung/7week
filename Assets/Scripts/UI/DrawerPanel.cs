using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 패널을 클릭으로 꺼내고 집어넣는 Drawer 컴포넌트.
///
/// ─── 동작 원칙 ───────────────────────────────────────────────────────────
///   Hidden 상태 : Awake에서 캡처한 초기 anchoredPosition.x / localRotation
///   Shown  상태 : _shownAnchoredX 위치 / Rotation(0,0,0)
///
///   클릭 시 IsShown 상태를 반전시켜 애니메이션과 함께 열고 닫습니다.
///
/// ─── 설정 방법 ───────────────────────────────────────────────────────────
///   1. 프리팹/씬에서 패널의 anchoredPosition.x 와 localRotation을 숨김 상태로 배치
///   2. Inspector의 _shownAnchoredX 에 완전히 꺼낸 상태의 X값을 입력
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DrawerPanel : MonoBehaviour, IPointerClickHandler
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("위치 설정")]
    [Tooltip("완전히 꺼낸 상태의 anchoredPosition X.\n" +
             "숨김 위치는 Awake에서 초기 anchoredPosition.x로 자동 캡처됩니다.")]
    [SerializeField] private float _shownAnchoredX = 0f;

    [Header("DOTween 설정")]
    [SerializeField] private float _animDuration = 0.4f;
    [SerializeField] private Ease  _showEase     = Ease.OutCubic;
    [SerializeField] private Ease  _hideEase     = Ease.InCubic;

    // ── 이벤트 ───────────────────────────────────────────────────────────────

    /// <summary>Show 애니메이션이 완전히 끝났을 때 발생합니다.</summary>
    public event Action OnShown;

    /// <summary>Hide 애니메이션이 완전히 끝났을 때 발생합니다.</summary>
    public event Action OnHidden;

    // ── 공개 프로퍼티 ────────────────────────────────────────────────────────

    public bool IsShown { get; private set; }

    // ── 내부 상태 ────────────────────────────────────────────────────────────

    private RectTransform _rect;

    /// <summary>씬/프리팹에서 설정한 숨김 위치 X (Awake 시 자동 캡처)</summary>
    private float      _hiddenAnchoredX;
    /// <summary>씬/프리팹에서 설정한 숨김 Rotation (Awake 시 자동 캡처)</summary>
    private Quaternion _hiddenLocalRotation;

    // 꺼낸 상태 목표 Rotation은 항상 정방향
    private static readonly Quaternion ShownLocalRotation = Quaternion.identity;

    private Tweener _moveTween;
    private Tweener _rotateTween;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect                = GetComponent<RectTransform>();
        _hiddenAnchoredX     = _rect.anchoredPosition.x;
        _hiddenLocalRotation = _rect.localRotation;
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
        _rotateTween?.Kill();
    }

    // ── 외부 API ─────────────────────────────────────────────────────────────

    /// <summary>패널을 꺼냅니다 (Shown 위치로 슬라이드, Rotation → 0,0,0).</summary>
    public void Show(bool instant = false) => TransitionTo(isShowing: true, instant);

    /// <summary>패널을 집어넣습니다 (Hidden 위치로 슬라이드, Rotation → 초기값).</summary>
    public void Hide(bool instant = false) => TransitionTo(isShowing: false, instant);

    // ── 클릭 핸들러 ──────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsShown) Hide();
        else         Show();
    }

    // ── 내부 전환 로직 ───────────────────────────────────────────────────────

    private void TransitionTo(bool isShowing, bool instant)
    {
        IsShown = isShowing;

        float      targetX   = isShowing ? _shownAnchoredX      : _hiddenAnchoredX;
        Quaternion targetRot = isShowing ? ShownLocalRotation    : _hiddenLocalRotation;
        Ease       ease      = isShowing ? _showEase             : _hideEase;

        _moveTween?.Kill();
        _rotateTween?.Kill();

        if (instant)
        {
            _rect.anchoredPosition = new Vector2(targetX, _rect.anchoredPosition.y);
            _rect.localRotation    = targetRot;
            NotifyComplete(isShowing);
            return;
        }

        // 위치와 회전을 동일한 Duration/Ease로 동기화
        _moveTween = _rect
            .DOAnchorPosX(targetX, _animDuration)
            .SetEase(ease)
            .OnComplete(() => NotifyComplete(isShowing));

        _rotateTween = _rect
            .DOLocalRotateQuaternion(targetRot, _animDuration)
            .SetEase(ease);
    }

    private void NotifyComplete(bool isShowing)
    {
        if (isShowing) OnShown?.Invoke();
        else           OnHidden?.Invoke();
    }

    // ── Editor 방어 ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        if (Mathf.Approximately(_shownAnchoredX, rt.anchoredPosition.x))
            Debug.LogWarning(
                $"[DrawerPanel] _shownAnchoredX({_shownAnchoredX})가 " +
                $"초기 anchoredPosition.x({rt.anchoredPosition.x})와 같습니다. " +
                "두 값이 달라야 패널이 움직입니다.");

        if (_animDuration <= 0f)
            Debug.LogWarning("[DrawerPanel] _animDuration은 0보다 커야 합니다.");
    }
#endif
}
