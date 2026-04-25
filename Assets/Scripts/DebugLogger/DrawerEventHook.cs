using UnityEngine;

// DrawerPanel의 OnShown / OnHidden 이벤트를 구독해 패널 종류에 따라 로그를 기록한다.
// 같은 GameObject의 DrawerPanel을 GetComponent로 참조한다.
// Inspector에서 PanelType을 설정해 역할 룰북 / 사건 서술 순서 룰북 / 메모 패널을 구분한다.
//
// 씬 배치:
//   역할 룰북 패널 오브젝트          → PanelType = RulebookRole
//   사건 서술 순서 룰북 패널 오브젝트 → PanelType = RulebookSequence
//   메모 패널 오브젝트               → PanelType = Memo
[DisallowMultipleComponent]
[RequireComponent(typeof(DrawerPanel))]
public class DrawerEventHook : MonoBehaviour
{
    // 패널 종류를 구분하는 열거형.
    public enum DrawerPanelType
    {
        RulebookRole,     // 역할 룰북
        RulebookSequence, // 사건 서술 순서 룰북
        Memo              // 메모 패널
    }

    [SerializeField] private DrawerPanelType _panelType;

    private DrawerPanel _drawer;

    private void Awake()
    {
        _drawer = GetComponent<DrawerPanel>();
    }

    private void OnEnable()
    {
        if (_drawer == null) return;
        _drawer.OnShown  += HandleShown;
        _drawer.OnHidden += HandleHidden;
    }

    private void OnDisable()
    {
        if (_drawer == null) return;
        _drawer.OnShown  -= HandleShown;
        _drawer.OnHidden -= HandleHidden;
    }

    // 패널이 열렸을 때 호출된다.
    // 룰북 패널은 열림 시점에 기록하고, 메모 패널은 닫힐 때만 기록하므로 여기선 무시한다.
    private void HandleShown()
    {
        switch (_panelType)
        {
            case DrawerPanelType.RulebookRole:
                GameEventLogger.Instance?.LogRulebookOpen("role");
                break;
            case DrawerPanelType.RulebookSequence:
                GameEventLogger.Instance?.LogRulebookOpen("sequence");
                break;
            case DrawerPanelType.Memo:
                // 열기는 기록하지 않음 — 닫을 때만 기록한다
                break;
        }
    }

    // 패널이 닫혔을 때 호출된다.
    // 메모 패널은 닫힘 시점에 기록하고, 룰북 패널은 열릴 때만 기록하므로 여기선 무시한다.
    // entryCount / hasKillerNote는 메모 데이터 모델 확정 후 연결한다.
    private void HandleHidden()
    {
        switch (_panelType)
        {
            case DrawerPanelType.Memo:
                GameEventLogger.Instance?.LogMemoClose(0, false); // stub: 메모 데이터 모델 연결 전
                break;
            case DrawerPanelType.RulebookRole:
            case DrawerPanelType.RulebookSequence:
                // 닫기는 기록하지 않음 — 열릴 때만 기록한다
                break;
        }
    }
}
