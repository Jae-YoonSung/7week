using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 메모장 토글 스위치 버튼들을 총괄 관리하는 컴포넌트입니다.
///
/// Inspector 설정:
///   _toggles 배열에 씬의 SequentialImageToggle 컴포넌트를 순서대로 연결하세요.
/// </summary>
public class NotepadToggleManager : MonoBehaviour
{
    [SerializeField] private SequentialImageToggle[] _toggles;

    [Header("공개 역할 스프라이트")]
    [Tooltip("공개된 역할과 캐릭터 ID가 모두 일치하는 버튼에 표시할 스프라이트")]
    [SerializeField] private Sprite _revealedSprite;
    [Tooltip("공개된 역할은 같지만 캐릭터 ID가 다른 버튼에 표시할 스프라이트")]
    [SerializeField] private Sprite _wrongCharacterSprite;

    /// <summary>토글 중 하나라도 변경되면 발생합니다. TutorialManager에서 구독합니다.</summary>
    public event Action OnAnyToggleChanged;

    // ── Unity ────────────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        RestoreStates();

        for (int i = 0; i < _toggles.Length; i++)
        {
            if (_toggles[i] == null) continue;
            _toggles[i].OnIndexChanged += _ => HandleToggleChanged();
        }

        // GameFlowController.Start()가 먼저 실행됐을 수 있으므로 한 프레임 대기 후 직접 호출
        yield return null;
        ApplyRevealedRoles();
    }

    private void OnDestroy()
    {
        foreach (var toggle in _toggles)
        {
            if (toggle == null) continue;
            toggle.OnIndexChanged -= _ => HandleToggleChanged();
        }

    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void RestoreStates()
    {
        for (int i = 0; i < _toggles.Length; i++)
        {
            if (_toggles[i] == null) continue;
            _toggles[i].SetIndex(0);
        }
    }

    private void ApplyRevealedRoles()
    {
        if (_revealedSprite == null) return;

        var gfc = GameFlowController.Instance;
        if (gfc == null) return;

        var revealed = gfc.RevealedRoles;
        if (revealed == null || revealed.Length == 0) return;

        foreach (var toggle in _toggles)
        {
            if (toggle == null) continue;

            foreach (var role in revealed)
            {
                bool sameRole = toggle.Role == role;
                // 이 역할을 실제로 가진 캐릭터 ID
                int correctCharId = -1;
                var status = gfc.GameState?.GetCharacterByRole(role);
                if (status != null) correctCharId = status.CharacterId;

                bool sameChar = toggle.CharacterId == correctCharId;

                if (sameRole && sameChar)
                    toggle.ShowResultSprite(_revealedSprite);           // 역할·캐릭터 모두 일치
                else if ((sameRole && !sameChar) || (!sameRole && sameChar))
                {
                    if (_wrongCharacterSprite != null)
                        toggle.ShowResultSprite(_wrongCharacterSprite); // 역할 또는 캐릭터 중 하나만 일치
                }
            }
        }
    }

    private void HandleToggleChanged()
    {
        OnAnyToggleChanged?.Invoke();
    }
}
