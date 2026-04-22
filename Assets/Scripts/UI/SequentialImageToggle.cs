using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼을 누를 때마다 단일 Image의 Sprite를 순차적으로 교체하는 컴포넌트.
///
/// Canvas 구조 예시:
///   SequentialImageToggle (이 컴포넌트)
///   ├── Button   ← ToggleButton 연결
///   └── Image    ← TargetImage 연결
///
/// - _sprites 배열에 표시할 Sprite를 순서대로 연결합니다.
/// - 좌클릭 시 다음 Sprite로, 우클릭 시 이전 Sprite로 교체됩니다.
/// </summary>
public class SequentialImageToggle : MonoBehaviour
{
    [Header("순환할 Sprite 목록 (순서대로 연결)")]
    [SerializeField] private Sprite[] _sprites = new Sprite[4];

    [Header("Sprite를 표시할 Image")]
    [SerializeField] private Image _targetImage;

    [Header("토글 버튼")]
    [SerializeField] private Button _toggleButton;

    private int _currentIndex;

    /// <summary>인덱스가 변경될 때 발생합니다. 인수는 새 인덱스 값입니다.</summary>
    public event Action<int> OnIndexChanged;

    /// <summary>현재 스프라이트 인덱스입니다.</summary>
    public int CurrentIndex => _currentIndex;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_toggleButton != null)
        {
            _toggleButton.onClick.AddListener(OnLeftClick);

            var trigger = _toggleButton.gameObject.GetComponent<EventTrigger>()
                       ?? _toggleButton.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(OnPointerClick);
            trigger.triggers.Add(entry);
        }

        ApplySprite(_currentIndex);
    }

    private void OnDestroy()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.RemoveListener(OnLeftClick);
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary>인덱스를 외부에서 직접 설정합니다. 이벤트는 발생하지 않습니다.</summary>
    public void SetIndex(int index)
    {
        if (_sprites == null || _sprites.Length == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, _sprites.Length - 1);
        ApplySprite(_currentIndex);
    }

    /// <summary>인덱스를 0으로 되돌리고 첫 번째 Sprite를 표시합니다. 이벤트는 발생하지 않습니다.</summary>
    public void ResetImage()
    {
        _currentIndex = 0;
        ApplySprite(_currentIndex);
    }

    // ── 버튼 콜백 ─────────────────────────────────────────────────────────────

    private void OnLeftClick()
    {
        Advance(1);
    }

    private void OnPointerClick(BaseEventData data)
    {
        if (data is PointerEventData pointerData && pointerData.button == PointerEventData.InputButton.Right)
            Advance(-1);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void Advance(int direction)
    {
        _currentIndex = (_currentIndex + direction + _sprites.Length) % _sprites.Length;
        ApplySprite(_currentIndex);
        OnIndexChanged?.Invoke(_currentIndex);
    }

    private void ApplySprite(int index)
    {
        if (_targetImage == null) return;

        var sprite = _sprites[index];
        if (sprite == null)
        {
            _targetImage.color = new Color(_targetImage.color.r, _targetImage.color.g, _targetImage.color.b, 0f);
            return;
        }

        _targetImage.sprite = sprite;
        _targetImage.color = new Color(_targetImage.color.r, _targetImage.color.g, _targetImage.color.b, 1f);
    }
}
