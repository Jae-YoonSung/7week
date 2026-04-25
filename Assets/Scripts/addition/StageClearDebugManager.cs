using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 클리어 기록을 실시간으로 토글하는 디버그 전용 매니저입니다.
///
/// 사용법:
///   1. TitleScene의 빈 오브젝트에 이 컴포넌트를 부착합니다.
///   2. Inspector에서 StageIds 배열에 관리할 StageId를 입력합니다.
///   3. 게임 실행 중 화면 왼쪽 상단에 디버그 패널이 나타납니다.
///
/// 기능:
///   - 개별 스테이지를 클리어/취소 토글
///   - 전체 클리어 / 전체 초기화 버튼
///   - 씬 내 BookshelfBook, LobbyUnlockManager 즉시 갱신
///   - F1 키로 패널 토글
///
/// IMPORTANT: 배포 빌드에서는 이 오브젝트를 비활성화하거나 삭제하세요.
/// </summary>
public class StageClearDebugManager : MonoBehaviour
{
    [Serializable]
    public class StageEntry
    {
        [Tooltip("관리할 스테이지 ID")]
        public string stageId;
        [Tooltip("이 스테이지의 표시 이름 (빈칸이면 stageId 그대로 표시)")]
        public string displayName;
    }

    [Header("관리할 스테이지 목록")]
    [SerializeField] private StageEntry[] _stages;

    [Header("패널 설정")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _showOnStart = true;

    // ── 런타임 상태 ──────────────────────────────────────────────────────────

    private bool _panelVisible;
    private Vector2 _scrollPos;

    private const float PanelWidth  = 280f;
    private const float PanelHeight = 400f;
    private Rect _windowRect = new Rect(10f, 10f, PanelWidth, PanelHeight);

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _panelVisible = _showOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            _panelVisible = !_panelVisible;
    }

    private void OnGUI()
    {
        if (!_panelVisible) return;
        _windowRect = GUI.Window(0, _windowRect, DrawWindow, "[Debug] 스테이지 클리어 관리");
    }

    // ── GUI 드로우 ────────────────────────────────────────────────────────────

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        // 전체 버튼 행
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 클리어", GUILayout.Height(28)))
            SetAllCleared(true);
        if (GUILayout.Button("전체 초기화", GUILayout.Height(28)))
            SetAllCleared(false);
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        // 씬 재로드 버튼
        if (GUILayout.Button("현재 씬 재시작", GUILayout.Height(28)))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        GUILayout.Space(6f);
        GUILayout.Label("── 개별 스테이지 ──");

        // 개별 스테이지 스크롤 목록
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        if (_stages != null)
        {
            foreach (var entry in _stages)
            {
                if (string.IsNullOrEmpty(entry.stageId)) continue;

                bool isCleared = StageClearRepository.Instance.HasCleared(entry.stageId);

                GUILayout.BeginHorizontal();

                // 클리어 상태 표시 색상
                Color prev = GUI.contentColor;
                GUI.contentColor = isCleared ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);

                string label = string.IsNullOrEmpty(entry.displayName) ? entry.stageId : entry.displayName;
                GUILayout.Label(isCleared ? $"✔ {label}" : $"✘ {label}", GUILayout.ExpandWidth(true));

                GUI.contentColor = prev;

                // 토글 버튼
                if (isCleared)
                {
                    if (GUILayout.Button("취소", GUILayout.Width(50f)))
                        ToggleClear(entry.stageId, false);
                }
                else
                {
                    if (GUILayout.Button("클리어", GUILayout.Width(50f)))
                        ToggleClear(entry.stageId, true);
                }

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        GUILayout.Label($"[{_toggleKey}] 패널 토글", GUI.skin.label);

        GUILayout.EndVertical();

        // 윈도우 드래그 허용
        GUI.DragWindow(new Rect(0f, 0f, PanelWidth, 20f));
    }

    // ── 로직 ────────────────────────────────────────────────────────────────

    /// <summary>특정 스테이지의 클리어 상태를 설정하고 씬을 즉시 갱신합니다.</summary>
    private void ToggleClear(string stageId, bool cleared)
    {
        if (cleared)
        {
            StageClearRepository.Instance.RecordClear(stageId);
            Debug.Log($"[StageClearDebug] 클리어 기록: {stageId}");
        }
        else
        {
            StageClearRepository.Instance.RemoveClear(stageId);
            Debug.Log($"[StageClearDebug] 클리어 취소: {stageId}");
        }

        RefreshScene();
    }

    /// <summary>전체 스테이지를 클리어하거나 초기화합니다.</summary>
    private void SetAllCleared(bool cleared)
    {
        if (cleared)
        {
            if (_stages == null) return;
            foreach (var entry in _stages)
                if (!string.IsNullOrEmpty(entry.stageId))
                    StageClearRepository.Instance.RecordClear(entry.stageId);
            Debug.Log("[StageClearDebug] 전체 스테이지 클리어 처리됨.");
        }
        else
        {
            StageClearRepository.Instance.ClearAllRecords();
            Debug.Log("[StageClearDebug] 전체 초기화됨.");
        }

        RefreshScene();
    }

    /// <summary>씬 내 모든 관련 컴포넌트의 상태를 즉시 갱신합니다.</summary>
    private void RefreshScene()
    {
        // BookshelfBook (타이틀씬 책 색상 + 비활성화)
        var books = FindObjectsByType<BookshelfBook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in books)
        {
            b.RefreshLockState();
            b.RefreshClearedState();
        }

        // LobbyUnlockManager (로비씬 버튼 해금)
        var unlockManagers = FindObjectsByType<LobbyUnlockManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in unlockManagers)
            m.Refresh();
    }
}
