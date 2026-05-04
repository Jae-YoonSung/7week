using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DrawingTool { None, Pencil, Eraser }

public class HistoryDrawingBoard : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public DrawingTool CurrentTool = DrawingTool.None;
    public Color PencilColor = Color.red;
    public int PencilRadius = 3;
    public int EraserRadius = 15;

    private RawImage _rawImage;
    private Texture2D _texture;
    private RectTransform _rect;
    private Vector2 _lastPos;
    private bool _hasLastPos;
    private GameObject _blocker;
    private RawImage _eraserCursor;

    private void Awake()
    {
        _rawImage = gameObject.AddComponent<RawImage>();
        _rect = GetComponent<RectTransform>();
        _rawImage.raycastTarget = false;

        // 텍스처 초기화 전 기본 흰색 사각형이 보이지 않도록 투명한 더미 텍스처 할당
        Texture2D emptyTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        emptyTex.SetPixel(0, 0, Color.clear);
        emptyTex.Apply();
        _rawImage.texture = emptyTex;
    }

    public void InitCursor()
    {
        if (_eraserCursor != null) Destroy(_eraserCursor.gameObject);

        GameObject cursorObj = new GameObject("EraserCursor");
        cursorObj.transform.SetParent(this.transform, false);
        cursorObj.transform.SetAsLastSibling();
        var rect = cursorObj.AddComponent<RectTransform>();
        
        int size = EraserRadius * 2;
        rect.sizeDelta = new Vector2(size, size);
        
        _eraserCursor = cursorObj.AddComponent<RawImage>();
        _eraserCursor.raycastTarget = false;
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0, 0, 0, 1f);
        float center = EraserRadius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                if (Mathf.Abs(dist - EraserRadius) < 1.0f) tex.SetPixel(x, y, outline);
                else tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        _eraserCursor.texture = tex;
        cursorObj.SetActive(false);
    }

    private void Start()
    {
        // Start 시점에서 실제 크기에 맞게 텍스처 생성
        if (_texture == null || _texture.width == 1)
        {
            InitTexture();
        }
    }

    public void ActivateTool(DrawingTool tool)
    {
        CurrentTool = tool;
        _rawImage.raycastTarget = (tool != DrawingTool.None);

        if (tool != DrawingTool.None)
        {
            if (_blocker == null)
            {
                _blocker = new GameObject("DrawingBlocker");
                _blocker.transform.SetParent(this.transform, false);
                _blocker.transform.SetAsFirstSibling();
                var rect = _blocker.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(20000, 20000);
                var img = _blocker.AddComponent<Image>();
                img.color = Color.clear;
                img.raycastTarget = true;
            }
            _blocker.SetActive(true);
        }
        else
        {
            if (_blocker != null) _blocker.SetActive(false);
        }
    }

    private void Update()
    {
        if (CurrentTool == DrawingTool.Eraser)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, cam))
            {
                if (!_eraserCursor.gameObject.activeSelf) _eraserCursor.gameObject.SetActive(true);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, Input.mousePosition, cam, out Vector2 localPos))
                {
                    _eraserCursor.rectTransform.anchoredPosition = localPos;
                }
            }
            else
            {
                if (_eraserCursor.gameObject.activeSelf) _eraserCursor.gameObject.SetActive(false);
            }
        }
        else
        {
            if (_eraserCursor != null && _eraserCursor.gameObject.activeSelf) _eraserCursor.gameObject.SetActive(false);
        }
    }

    private void InitTexture()
    {
        int w = Mathf.RoundToInt(_rect.rect.width);
        int h = Mathf.RoundToInt(_rect.rect.height);
        if (w <= 0 || h <= 0) { w = 1000; h = 1000; } // Fallback

        _texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        ClearTexture();
        _rawImage.texture = _texture;
    }

    public void ClearTexture()
    {
        if (_texture == null) return;
        Color[] colors = new Color[_texture.width * _texture.height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
        _texture.SetPixels(colors);
        _texture.Apply();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CurrentTool == DrawingTool.None) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            _hasLastPos = true;
            _lastPos = localPos;
            DrawAt(localPos);
            _texture.Apply();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CurrentTool == DrawingTool.None) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            if (_hasLastPos)
            {
                DrawLine(_lastPos, localPos);
            }
            else
            {
                DrawAt(localPos);
            }
            _lastPos = localPos;
            _hasLastPos = true;
            _texture.Apply();
        }
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float dist = Vector2.Distance(start, end);
        int radius = CurrentTool == DrawingTool.Pencil ? PencilRadius : EraserRadius;
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / (radius * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(start, end, (float)i / steps);
            DrawAt(p);
        }
    }

    private void DrawAt(Vector2 localPos)
    {
        int radius = CurrentTool == DrawingTool.Pencil ? PencilRadius : EraserRadius;
        Color targetColor = CurrentTool == DrawingTool.Pencil ? PencilColor : Color.clear;

        float nx = (localPos.x - _rect.rect.x) / _rect.rect.width;
        float ny = (localPos.y - _rect.rect.y) / _rect.rect.height;

        int cx = Mathf.RoundToInt(nx * _texture.width);
        int cy = Mathf.RoundToInt(ny * _texture.height);

        int sqrRadius = radius * radius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= sqrRadius)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < _texture.width && py >= 0 && py < _texture.height)
                    {
                        _texture.SetPixel(px, py, targetColor);
                    }
                }
            }
        }
    }
}

[Serializable]
public struct CharacterTokenPrefabs
{
    [Tooltip("이 캐릭터의 토큰 프리팹 (MovedIcon·StaticIcon·DeadMark 포함)")]
    public HistoryCharacterToken Prefab;
}

/// <summary>
/// 특정 루프·턴의 히스토리를 표시하는 패널입니다.
/// HistoryPageController가 카드 덱 방식으로 관리하며,
/// 헤더 탭을 클릭하면 OnHeaderClicked 이벤트로 컨트롤러에 알립니다.
///
/// ─── 동작 원칙 ───────────────────────────────────────────────────────────
///   Activate()  : 최초 1회만 TurnRecord를 수신하고 헤더를 갱신합니다.
///   Expand()    : RefreshContent()로 내용을 최신화한 뒤 지정 Y 위치로 슬라이드합니다.
///   Collapse()  : 내용 갱신 없이 지정 Y 위치(헤더만 보이는 위치)로 슬라이드합니다.
///
/// ─── 권장 프리팹 구조 (Canvas / VerticalLayoutGroup 불필요) ──────────────
///   HistoryPagePanel          (이 컴포넌트 + RectTransform, 패널 전체 높이)
///   ├── Content               (콘텐츠 영역 — 패널 상단부)
///   │   ├── TokenContainer    ← _tokenContainer
///   │   ├── [Before] Anchor_0 ← _beforeAnchors[0]  ZoneId=0
///   │   ├── [Before] Anchor_1 ← _beforeAnchors[1]  ZoneId=1
///   │   ├── [Before] Anchor_2 ← _beforeAnchors[2]  ZoneId=2
///   │   ├── [Before] Anchor_3 ← _beforeAnchors[3]  ZoneId=3
///   │   ├── [After]  Anchor_0 ← _afterAnchors[0]   ZoneId=0
///   │   ├── [After]  Anchor_1 ← _afterAnchors[1]   ZoneId=1
///   │   ├── [After]  Anchor_2 ← _afterAnchors[2]   ZoneId=2
///   │   └── [After]  Anchor_3 ← _afterAnchors[3]   ZoneId=3
///   └── Header                (헤더 탭 — 패널 하단부, 접혔을 때도 항상 노출)
///       ├── HeaderLabel       ← _headerLabel        "L1  T2"
///       ├── LoopConditionIcon ← _loopConditionIcon  특수 루프 표시
///       └── SelectButton      ← _selectButton
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class HistoryPagePanel : MonoBehaviour, IPointerClickHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("헤더 참조")]
    [SerializeField] private TMP_Text  _headerLabel;
    [SerializeField] private GameObject _loopConditionIcon;
    [SerializeField] private Button    _selectButton;

    [Header("구역 앵커 — Before (이동 전, 4개)")]
    [SerializeField] private HistoryZoneAnchor[] _beforeAnchors = new HistoryZoneAnchor[4];

    [Header("구역 앵커 — After (이동 후, 4개)")]
    [SerializeField] private HistoryZoneAnchor[] _afterAnchors = new HistoryZoneAnchor[4];

    [Header("토큰 설정")]
    [SerializeField] private RectTransform _tokenContainer;
    [Tooltip("캐릭터 0~6 순서로 설정 (CharacterId 기준).\n각 항목에 이동 프리팹(Moved)과 정지 프리팹(Static)을 연결하세요.")]
    [SerializeField] private CharacterTokenPrefabs[] _characterTokenPrefabs = new CharacterTokenPrefabs[7];
    [Tooltip("구역 배경으로 사용할 토큰의 CharacterId. 항상 구역 중앙에 스폰되고 맨 뒤에 깔립니다. -1이면 비활성.")]
    [SerializeField] private int _backgroundTokenId = -1;
    [Tooltip("같은 구역 내 토큰 간 수평 간격 (px)")]
    [SerializeField] private float _tokenSpacing = 30f;
    [Tooltip("이 X 너비(px)를 초과하면 토큰을 두 줄로 표시합니다. 0이면 한 줄 고정.")]
    [SerializeField] private float _maxRowWidth  = 0f;

    [Header("DOTween 설정")]
    [SerializeField] private float _animDuration = 0.35f;
    [SerializeField] private Ease  _expandEase   = Ease.OutCubic;
    [SerializeField] private Ease  _collapseEase = Ease.InCubic;

    [Header("버튼 호버 설정")]
    [Tooltip("호버 시 버튼이 위로 올라가는 거리 (px)")]
    [SerializeField] private float _hoverOffsetY  = 8f;
    [SerializeField] private float _hoverDuration = 0.15f;
    [SerializeField] private Ease  _hoverEase     = Ease.OutCubic;

    [Header("드로잉 툴 설정")]
    [SerializeField] private Button _pencilButton;
    [SerializeField] private Button _eraserButton;
    [SerializeField] private Color _pencilColor = Color.red;
    [SerializeField] private int _pencilRadius = 3;
    [SerializeField] private int _eraserRadius = 15;

    // ── 공개 프로퍼티 ─────────────────────────────────────────────────────────

    public int  LoopIndex   { get; private set; }
    public int  TurnIndex   { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsExpanded  { get; private set; }

    /// <summary>헤더 탭 버튼 클릭 시 발생합니다.</summary>
    public event Action<HistoryPagePanel> OnHeaderClicked;

    /// <summary>드래그 또는 외부 요청으로 패널이 스스로 내려갈 때 발생합니다.</summary>
    public event Action<HistoryPagePanel> OnCollapseRequested;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private TurnRecord        _record;
    private RectTransform     _rect;
    private Tweener           _currentTween;

    /// <summary>씬에 배치된 원래 anchoredPosition.y</summary>
    private float _originY;
    /// <summary>Expand() 호출 시 저장되는 펼친 상태 Y — 드래그 snap-back 기준점</summary>
    private float _expandedY;
    /// <summary>컨트롤러에서 전달받은 펼침 오프셋 — 버튼 드래그 상한 계산에 사용</summary>
    private float _expandOffset;

    /// <summary>버튼 RectTransform 및 호버 원점 Y</summary>
    private RectTransform _selectButtonRect;
    private float         _buttonOriginY;

    private Tweener       _hoverTween;

    private HistoryDrawingBoard _drawingBoard;

    private readonly List<HistoryCharacterToken> _activeTokens = new List<HistoryCharacterToken>();

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect    = GetComponent<RectTransform>();
        _originY = _rect.anchoredPosition.y;
        _selectButton.onClick.AddListener(HandleSelectClicked);

        _selectButtonRect = _selectButton.GetComponent<RectTransform>();
        _buttonOriginY    = _selectButtonRect.anchoredPosition.y;
        RegisterHoverEvents();

        if (_pencilButton != null) _pencilButton.onClick.AddListener(() => SetDrawingTool(DrawingTool.Pencil));
        if (_eraserButton != null) _eraserButton.onClick.AddListener(() => SetDrawingTool(DrawingTool.Eraser));

        InitDrawingBoard();
    }

    private void InitDrawingBoard()
    {
        var boardObj = new GameObject("DrawingBoard");
        boardObj.transform.SetParent(this.transform, false);
        boardObj.transform.SetAsLastSibling();
        
        var boardRect = boardObj.AddComponent<RectTransform>();
        boardRect.anchorMin = Vector2.zero;
        boardRect.anchorMax = Vector2.one;
        boardRect.sizeDelta = Vector2.zero;
        boardRect.anchoredPosition = Vector2.zero;

        _drawingBoard = boardObj.AddComponent<HistoryDrawingBoard>();
        _drawingBoard.PencilColor = _pencilColor;
        _drawingBoard.PencilRadius = _pencilRadius;
        _drawingBoard.EraserRadius = _eraserRadius;
        _drawingBoard.InitCursor();

        // 드로잉 보드가 버튼들의 터치를 가로채지 않도록, 버튼들을 렌더링 최상단으로 끌어올립니다.
        BringToFront(_selectButton?.transform);
        BringToFront(_pencilButton?.transform);
        BringToFront(_eraserButton?.transform);

        // 버튼 색상을 비활성 상태(검정색)로 초기화합니다.
        UpdateToolButtons();
    }

    private void BringToFront(Transform target)
    {
        if (target == null) return;
        Transform p = target;
        while (p != null && p.parent != this.transform)
        {
            p = p.parent;
        }
        if (p != null && p.parent == this.transform)
        {
            p.SetAsLastSibling();
        }
    }

    private void Update()
    {
        if (IsExpanded && Input.GetMouseButtonDown(0))
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (!RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, cam))
            {
                Collapse();
                OnCollapseRequested?.Invoke(this);
            }
        }
    }

    // ── 초기화 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// HistoryPageController가 Awake에서 한 번 호출합니다.
    /// </summary>
    public void Init(int loopIndex, int turnIndex, float expandOffset)
    {
        LoopIndex     = loopIndex;
        TurnIndex     = turnIndex;
        _expandOffset = expandOffset;
        IsActivated   = false;

        SetHeaderLabel(null);
        SetLoopConditionIcon(false);
    }

    // ── 외부 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 해당 TurnRecord를 수신하여 패널을 활성화합니다.
    /// 최초 1회만 유효하며 이후 재호출은 무시됩니다.
    /// </summary>
    public void Activate(TurnRecord record)
    {
        if (IsActivated) return;
        IsActivated = true;
        _record     = record;
        SetHeaderLabel(record);
    }

    /// <summary>
    /// 콘텐츠를 최신화한 뒤 오리진 위치에서 expandOffset만큼 위로 슬라이드합니다.
    /// </summary>
    public void Expand(float expandOffset, bool instant = false)
    {
        IsExpanded = true;
        _expandedY = _originY + expandOffset;
        // 호버 상태로 올라가 있을 수 있으므로 버튼을 원점으로 복귀
        _hoverTween?.Kill();
        _selectButtonRect.anchoredPosition = new Vector2(
            _selectButtonRect.anchoredPosition.x, _buttonOriginY);
        RefreshContent();
        AnimateTo(_expandedY, instant, isExpanding: true);
    }

    /// <summary>
    /// 오리진 위치로 돌아옵니다. 콘텐츠는 갱신하지 않습니다.
    /// </summary>
    public void Collapse(bool instant = false)
    {
        IsExpanded = false;
        AnimateTo(_originY, instant, isExpanding: false);

        SetDrawingTool(DrawingTool.None);
    }

    /// <summary>
    /// 최초 활성화 시 오리진 아래에서 슬라이드업하여 자연스럽게 등장합니다.
    /// SetActive(true) 직후 호출하세요.
    /// </summary>
    /// <param name="belowOffset">오리진에서 시작 위치까지의 거리 (양수 = 아래)</param>
    public void SpawnIn(float belowOffset)
    {
        SnapTo(_originY - belowOffset);
        AnimateTo(_originY, instant: false, isExpanding: true);
    }

    /// <summary>
    /// 애니메이션 없이 즉시 지정 위치로 이동합니다.
    /// </summary>
    public void SnapTo(float targetY)
    {
        _currentTween?.Kill();
        var pos = _rect.anchoredPosition;
        _rect.anchoredPosition = new Vector2(pos.x, targetY);
    }

    // ── 콘텐츠 갱신 ──────────────────────────────────────────────────────────

    private void RefreshContent()
    {
        ClearTokens();
        if (_record == null) return;

        SetLoopConditionIcon(_record.IsLoopConditionTurn);

        if (_record.IsLoopConditionTurn)
            RefreshMovementOnly(_record);
        else
            RefreshFull(_record);
    }

    private void RefreshFull(TurnRecord record)
    {
        var aliveAtStart = BuildAliveSet(record.BeforeAction);
        var diedThisTurn = BuildDeathSet(record.Deaths);

        PlaceTokens(record.BeforeAction, aliveAtStart, diedThisTurn,
                    record, _beforeAnchors, isAfterAction: false);
        PlaceTokens(record.AfterAction,  aliveAtStart, diedThisTurn,
                    record, _afterAnchors,  isAfterAction: true);
    }

    private void RefreshMovementOnly(TurnRecord record)
    {
        var aliveAtStart = BuildAliveSet(record.BeforeAction);
        PlaceMovementTokens(record, record.BeforeAction, _beforeAnchors, aliveAtStart);
        PlaceMovementTokens(record, record.AfterAction,  _afterAnchors,  aliveAtStart);
    }

    private void PlaceTokens(
        List<CharacterPositionSnapshot> snapshots,
        HashSet<int>                    aliveAtStart,
        HashSet<int>                    diedThisTurn,
        TurnRecord                      record,
        HistoryZoneAnchor[]             anchors,
        bool                            isAfterAction)
    {
        var byZone = GroupByZone(snapshots);
        foreach (var kv in byZone)
        {
            Vector2 center    = GetAnchorLocalPos(anchors, kv.Key);
            int     charTotal = CharTokenCount(kv.Value);
            int     charSlot  = 0;
            for (int i = 0; i < kv.Value.Count; i++)
            {
                int  id             = kv.Value[i].CharacterId;
                bool alreadyDead    = !aliveAtStart.Contains(id);
                bool moved          = !alreadyDead && HasMoved(record, id);
                bool diedThisTurnId = isAfterAction && !alreadyDead && diedThisTurn.Contains(id);
                bool isBg           = _backgroundTokenId >= 0 && id == _backgroundTokenId;

                SpawnToken(id, moved, diedThisTurnId, alreadyDead, center, isBg ? 0 : charSlot++, charTotal);
            }
        }
    }

    private void PlaceMovementTokens(
        TurnRecord                      record,
        List<CharacterPositionSnapshot> snapshots,
        HistoryZoneAnchor[]             anchors,
        HashSet<int>                    aliveAtStart)
    {
        var byZone = GroupByZone(snapshots);
        foreach (var kv in byZone)
        {
            Vector2 center    = GetAnchorLocalPos(anchors, kv.Key);
            int     charTotal = CharTokenCount(kv.Value);
            int     charSlot  = 0;
            for (int i = 0; i < kv.Value.Count; i++)
            {
                int  id          = kv.Value[i].CharacterId;
                bool alreadyDead = !aliveAtStart.Contains(id);
                bool moved       = !alreadyDead && HasMoved(record, id);
                bool isBg        = _backgroundTokenId >= 0 && id == _backgroundTokenId;

                SpawnToken(id, moved, diedThisTurn: false, alreadyDead, center, isBg ? 0 : charSlot++, charTotal);
            }
        }
    }

    private int CharTokenCount(List<CharacterPositionSnapshot> list)
    {
        if (_backgroundTokenId < 0) return list.Count;
        int count = 0;
        foreach (var s in list)
            if (s.CharacterId != _backgroundTokenId) count++;
        return count;
    }

    private void SpawnToken(int characterId, bool moved, bool diedThisTurn, bool alreadyDead,
                            Vector2 centerPos, int slotIndex, int totalInZone)
    {
        if (characterId < 0 || _characterTokenPrefabs.Length == 0) return;
        int prefabIndex = Mathf.Min(characterId, _characterTokenPrefabs.Length - 1);
        var entry = _characterTokenPrefabs[prefabIndex];
        if (entry.Prefab == null) return;

        var token = Instantiate(entry.Prefab, _tokenContainer);
        token.Setup(moved, diedThisTurn, alreadyDead);
        bool isBackground = _backgroundTokenId >= 0 && characterId == _backgroundTokenId;
        token.Rect.anchoredPosition = isBackground ? centerPos : centerPos + SlotOffset(slotIndex, totalInZone);
        if (isBackground)
            token.transform.SetAsFirstSibling();
        _activeTokens.Add(token);
    }

    private void ClearTokens()
    {
        foreach (var token in _activeTokens)
            if (token != null) Destroy(token.gameObject);
        _activeTokens.Clear();
    }

    // ── 애니메이션 ────────────────────────────────────────────────────────────

    private void AnimateTo(float targetY, bool instant, bool isExpanding)
    {
        _currentTween?.Kill();
        if (instant)
        {
            var pos = _rect.anchoredPosition;
            _rect.anchoredPosition = new Vector2(pos.x, targetY);
            return;
        }
        _currentTween = _rect.DOAnchorPosY(targetY, _animDuration)
                             .SetEase(isExpanding ? _expandEase : _collapseEase);
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────────────────────

    private void SetHeaderLabel(TurnRecord record)
    {
        if (_headerLabel == null) return;
        _headerLabel.text = record != null
            ? $"L{record.LoopIndex + 1}  T{record.TurnIndex + 1}"
            : "---";
    }

    private void SetLoopConditionIcon(bool active)
    {
        if (_loopConditionIcon != null)
            _loopConditionIcon.SetActive(active);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 도구가 활성화 되어 있으면 클릭으로 닫히지 않음 (드로잉 우선)
        if (_drawingBoard != null && _drawingBoard.CurrentTool != DrawingTool.None) return;
        HandleSelectClicked();
    }

    private void HandleSelectClicked()
    {
        if (IsExpanded)
        {
            Collapse();
            OnCollapseRequested?.Invoke(this);
        }
        else
        {
            OnHeaderClicked?.Invoke(this);
        }
    }

    public void SetDrawingTool(DrawingTool tool)
    {
        if (_drawingBoard == null) return;

        if (_drawingBoard.CurrentTool == tool)
        {
            _drawingBoard.ActivateTool(DrawingTool.None);
        }
        else
        {
            _drawingBoard.ActivateTool(tool);
        }

        UpdateToolButtons();
    }

    private void UpdateToolButtons()
    {
        if (_pencilButton != null)
        {
            var colors = _pencilButton.colors;
            Color targetColor = _drawingBoard.CurrentTool == DrawingTool.Pencil ? Color.red : Color.black;
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.selectedColor = targetColor;
            _pencilButton.colors = colors;
        }
        if (_eraserButton != null)
        {
            var colors = _eraserButton.colors;
            Color targetColor = _drawingBoard.CurrentTool == DrawingTool.Eraser ? Color.red : Color.black;
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.selectedColor = targetColor;
            _eraserButton.colors = colors;
        }
    }

    // ── 버튼 호버 ─────────────────────────────────────────────────────────────

    private void RegisterHoverEvents()
    {
        var trigger = _selectButton.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => OnButtonHoverEnter());
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => OnButtonHoverExit());
        trigger.triggers.Add(exitEntry);
    }

    private void OnButtonHoverEnter()
    {
        if (IsExpanded) return;
        _hoverTween?.Kill();
        _hoverTween = _selectButtonRect
            .DOAnchorPosY(_buttonOriginY + _hoverOffsetY, _hoverDuration)
            .SetEase(_hoverEase);
    }

    private void OnButtonHoverExit()
    {
        if (IsExpanded) return;
        _hoverTween?.Kill();
        _hoverTween = _selectButtonRect
            .DOAnchorPosY(_buttonOriginY, _hoverDuration)
            .SetEase(_hoverEase);
    }

    // ── 위치 헬퍼 ─────────────────────────────────────────────────────────────

    private Vector2 GetAnchorLocalPos(HistoryZoneAnchor[] anchors, int zoneId)
    {
        foreach (var anchor in anchors)
        {
            if (anchor == null || anchor.ZoneId != zoneId) continue;
            Vector3 local = _tokenContainer.InverseTransformPoint(anchor.transform.position);
            return new Vector2(local.x, local.y);
        }
        Debug.LogWarning($"[HistoryPagePanel] L{LoopIndex}T{TurnIndex}: ZoneId {zoneId} 앵커 없음");
        return Vector2.zero;
    }

    private Vector2 SlotOffset(int slotIndex, int total)
    {
        // _maxRowWidth가 0보다 크고 한 줄에 다 들어가지 않으면 두 줄로 표시
        if (_maxRowWidth > 0f && _tokenSpacing > 0f)
        {
            int perRow = Mathf.Max(1, Mathf.FloorToInt(_maxRowWidth / _tokenSpacing));
            if (total > perRow)
            {
                int   row        = slotIndex / perRow;
                int   col        = slotIndex % perRow;
                int   countInRow = (row == 0) ? perRow : total - perRow;
                float x          = (col - (countInRow - 1) * 0.5f) * _tokenSpacing;
                float y          = (row == 0 ? 1f : -1f) * _tokenSpacing * 0.5f;
                return new Vector2(x, y);
            }
        }

        return new Vector2((slotIndex - (total - 1) * 0.5f) * _tokenSpacing, 0f);
    }

    // ── 데이터 헬퍼 (정적) ───────────────────────────────────────────────────

    private static HashSet<int> BuildAliveSet(List<CharacterPositionSnapshot> snapshots)
    {
        var set = new HashSet<int>();
        foreach (var s in snapshots)
            if (s.IsAlive) set.Add(s.CharacterId);
        return set;
    }

    private static HashSet<int> BuildDeathSet(List<TurnDeathRecord> deaths)
    {
        var set = new HashSet<int>();
        foreach (var d in deaths) set.Add(d.CharacterId);
        return set;
    }

    private static Dictionary<int, List<CharacterPositionSnapshot>> GroupByZone(
        List<CharacterPositionSnapshot> snapshots)
    {
        var result = new Dictionary<int, List<CharacterPositionSnapshot>>();
        foreach (var s in snapshots)
        {
            if (!result.TryGetValue(s.ZoneId, out var list))
                result[s.ZoneId] = list = new List<CharacterPositionSnapshot>();
            list.Add(s);
        }
        return result;
    }

    private static bool HasMoved(TurnRecord record, int characterId)
    {
        int before = -1, after = -1;
        foreach (var s in record.BeforeAction)
            if (s.CharacterId == characterId) { before = s.ZoneId; break; }
        foreach (var s in record.AfterAction)
            if (s.CharacterId == characterId) { after  = s.ZoneId; break; }
        return before != -1 && after != -1 && before != after;
    }

    // ── 정리 ──────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        _currentTween?.Kill();
        _hoverTween?.Kill();
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(HandleSelectClicked);
        if (_pencilButton != null)
            _pencilButton.onClick.RemoveAllListeners();
        if (_eraserButton != null)
            _eraserButton.onClick.RemoveAllListeners();
    }
}
