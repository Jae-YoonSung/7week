using UnityEngine;
using DG.Tweening;

/// <summary>
/// 책장 위 개별 책 3D 오브젝트에 부착합니다.
/// 마우스 클릭 시 TitleSceneController에 자신을 전달합니다.
///
/// 요구 조건:
///   - 이 오브젝트에 Collider (BoxCollider 등) 필수
///   - Physics Raycasting이 활성화된 Camera 필요 (일반 3D 카메라)
///
/// Inspector 필수 연결:
///   Controller → 씬의 TitleSceneController
///
/// 잠금 설정:
///   RequiredClearStageId → 비워두면 항상 클릭 가능
///                           채우면 해당 스테이지 클리어 후 클릭 가능
/// </summary>
[DisallowMultipleComponent]
public class BookshelfBook : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("씬의 TitleSceneController")]
    [SerializeField] private TitleSceneController _controller;

    [Header("잠금 설정")]
    [Tooltip("이 책을 클릭하려면 클리어되어 있어야 하는 스테이지 ID. 비워두면 항상 해금.")]
    [SerializeField] private string _requiredClearStageId;
    [Tooltip("잠금 상태일 때 활성화할 오버레이 오브젝트 (선택)")]
    [SerializeField] private GameObject _lockOverlay;

    [Header("호버 효과 (선택)")]
    [Tooltip("마우스 오버 시 책을 살짝 튀어나오게 할 거리 (0이면 비활성)")]
    [SerializeField] private float _hoverOffset = 0.05f;
    [SerializeField] private float _hoverDuration = 0.15f;

    // ── 프로퍼티 ─────────────────────────────────────────────────────────────

    public bool IsUnlocked => string.IsNullOrEmpty(_requiredClearStageId)
                           || StageClearRepository.Instance.HasCleared(_requiredClearStageId);

    // ── 런타임 상태 ──────────────────────────────────────────────────────────

    private Vector3 _originalLocalPos;
    private bool    _isHovering = false;
    private bool    _isClicked = false;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _originalLocalPos = transform.localPosition;
        RefreshLockState();
    }

    private void OnMouseDown()
    {
        if (!IsUnlocked || _isClicked) return;
        _isClicked = true;
        DOTween.Kill(transform); // 호버 애니메이션이 타이틀 연출을 방해하지 않도록 취소
        _controller?.OnBookClicked(transform);
    }

    private void OnMouseEnter()
    {
        if (!IsUnlocked || _hoverOffset <= 0f || _isHovering || _isClicked) return;
        _isHovering = true;
        // 책이 책장에서 살짝 튀어나오는 효과 (로컬 Z축 방향)
        DOTween.Kill(transform);
        transform.DOLocalMove(_originalLocalPos + transform.forward * _hoverOffset, _hoverDuration).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        if (!_isHovering || _isClicked) return;
        _isHovering = false;
        DOTween.Kill(transform);
        transform.DOLocalMove(_originalLocalPos, _hoverDuration).SetEase(Ease.OutQuad);
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    public void RefreshLockState()
    {
        if (_lockOverlay != null)
            _lockOverlay.SetActive(!IsUnlocked);
    }
}
