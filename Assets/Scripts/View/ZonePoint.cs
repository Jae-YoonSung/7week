using UnityEngine;

/// <summary>
/// 씬에서 하나의 구역(Zone) 위치를 표시하는 마커 컴포넌트입니다.
/// 빈 GameObject에 부착하고 ZoneLayout에 등록하세요.
///
/// Inspector 설정:
///   ZoneId   → 0 ~ GameState.ZoneCount-1
///   Slots    → 캐릭터가 서는 자리 Transform 배열 (비워두면 자동 오프셋 사용)
///              나중에 position 애니메이션, 연출 등 확장 시 여기에 추가하세요.
/// </summary>
public class ZonePoint : MonoBehaviour
{
    [SerializeField] private int         _zoneId;
    [SerializeField] private Transform[] _slots;

    [Header("구역 규칙")]
    [Tooltip("true로 설정하면 이 구역에 있는 캐릭터는 턴 종료 시 특수 능력을 사용할 수 없습니다.")]
    [SerializeField] private bool _disableAbilities;

    [Tooltip("턴 종료 후 발동할 구역 효과. null이면 효과 없음. (예: WaveZoneEffect)")]
    [SerializeField] private ZoneEffectConfig _zoneEffect;

    [Header("입장 봉쇄")]
    [Tooltip("true로 설정하면 BlockEntryFromTurn 이상의 턴부터 이 구역으로 캐릭터가 진입할 수 없습니다.")]
    [SerializeField] private bool _blockEntry;

    [Tooltip("몇 번째 턴(1-based)부터 진입을 차단할지 설정합니다.")]
    [SerializeField] private int _blockEntryFromTurn = 1;

    [Tooltip("몇 번째 턴(1-based)까지 차단할지 설정합니다. 0이면 끝까지 차단합니다.")]
    [SerializeField] private int _blockEntryUntilTurn = 0;

    [Tooltip("입장 봉쇄 상태일 때 활성화할 오브젝트 (잠금 표시, 이펙트 등).")]
    [SerializeField] private GameObject _blockEntryIndicator;

    [Header("드롭 인디케이터")]
    [SerializeField] private GameObject _dropIndicator;

    [Header("구역 유령 색상")]
    [Tooltip("ZonePhantom이 이 구역에 있을 때 활성화할 오브젝트 (색상 오버레이 등)")]
    [SerializeField] private GameObject _phantomColorIndicator;

    public int              ZoneId           => _zoneId;
    public Vector3          Position         => transform.position;
    public bool             DisableAbilities => _disableAbilities;
    public ZoneEffectConfig ZoneEffect       => _zoneEffect;
    public bool             BlockEntry       => _blockEntry;
    public int              BlockEntryFromTurn => _blockEntryFromTurn;

    /// <summary>현재 턴(1-based)에서 이 구역으로의 진입이 차단되어 있으면 true를 반환합니다.</summary>
    public bool IsEntryBlockedAtTurn(int turn)
        => _blockEntry
           && turn >= _blockEntryFromTurn
           && (_blockEntryUntilTurn <= 0 || turn <= _blockEntryUntilTurn);

    public void SetDropIndicator(bool active)
    {
        if (_dropIndicator != null)
            _dropIndicator.SetActive(active);
    }

    public void SetBlockEntryIndicator(bool active)
    {
        if (_blockEntryIndicator != null)
            _blockEntryIndicator.SetActive(active);
    }

    public void SetPhantomPresent(bool active)
    {
        if (_phantomColorIndicator != null)
            _phantomColorIndicator.SetActive(active);
    }

    /// <summary>슬롯 배열 길이 (0이면 자동 오프셋 모드).</summary>
    public int SlotCount => _slots != null ? _slots.Length : 0;

    /// <summary>
    /// slotIndex 번째 슬롯의 월드 위치를 반환합니다.
    /// 슬롯이 없거나 인덱스를 벗어나면 캐릭터 수(totalInZone) 기반 자동 오프셋을 사용합니다.
    /// </summary>
    public Vector3 GetSlotPosition(int slotIndex, int totalInZone)
    {
        if (_slots != null && slotIndex < _slots.Length && _slots[slotIndex] != null)
            return _slots[slotIndex].position;

        return Position + AutoOffset(slotIndex, totalInZone);
    }

    /// <summary>
    /// slotIndex 번째 슬롯의 월드 회전을 반환합니다.
    /// 슬롯이 없으면 Zone 자신의 회전을 반환합니다.
    /// </summary>
    public Quaternion GetSlotRotation(int slotIndex)
    {
        if (_slots != null && slotIndex < _slots.Length && _slots[slotIndex] != null)
            return _slots[slotIndex].rotation;

        return transform.rotation;
    }

    // 중앙 기준 수평 등간격 배치
    private static Vector3 AutoOffset(int index, int total)
    {
        const float spacing = 0.8f;
        float offsetX = (index - (total - 1) * 0.5f) * spacing;
        return new Vector3(offsetX, 0f, 0f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_zoneId < 0 || _zoneId >= GameState.ZoneCount)
            Debug.LogWarning($"[ZonePoint] '{name}' ZoneId는 0~{GameState.ZoneCount - 1} 범위여야 합니다. 현재값: {_zoneId}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _disableAbilities ? Color.red : (_blockEntry ? new Color(1f, 0.5f, 0f) : Color.cyan);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        string label = _disableAbilities
            ? $"Zone {_zoneId} [능력 봉인]"
            : (_blockEntry
                ? (_blockEntryUntilTurn > 0
                    ? $"Zone {_zoneId} [T{_blockEntryFromTurn}~T{_blockEntryUntilTurn} 입장봉쇄]"
                    : $"Zone {_zoneId} [T{_blockEntryFromTurn}~ 입장봉쇄]")
                : $"Zone {_zoneId}");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, label);

        // 슬롯 위치 표시
        if (_slots != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                Gizmos.DrawWireSphere(_slots[i].position, 0.2f);
                UnityEditor.Handles.Label(_slots[i].position + Vector3.up * 0.3f, $"S{i}");
            }
        }
    }
#endif
}
