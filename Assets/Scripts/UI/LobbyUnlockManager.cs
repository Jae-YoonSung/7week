using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 로비에서 스테이지 클리어 기록을 확인해 버튼 오브젝트의 활성화 여부를 관리합니다.
///
/// Inspector 설정:
///   Entries 배열에 (ButtonObject, RequiredClearStageId, PlayUnlockAnim) 쌍을 등록하세요.
///   PlayUnlockAnim을 체크한 버튼은 부모 오브젝트가 활성화될 때 처음 한 번만 강조 연출이 재생됩니다.
/// </summary>
public class LobbyUnlockManager : MonoBehaviour
{
    [Serializable]
    public struct UnlockEntry
    {
        [Tooltip("활성화/비활성화할 버튼 오브젝트")]
        public GameObject buttonObject;
        [Tooltip("클리어되어야 열리는 스테이지 ID. 비워두면 항상 활성화.")]
        public string requiredClearStageId;
        [Tooltip("처음 해금될 때 강조 연출(페이드인 + 팝)을 재생합니다.")]
        public bool playUnlockAnim;
    }

    [SerializeField] private UnlockEntry[] _entries;

    [Header("해금 강조 연출")]
    [SerializeField] private float _fadeInDuration = 0.4f;
    [SerializeField] private float _popScale       = 1.2f;
    [SerializeField] private float _popDuration    = 0.3f;

    private const string SeenKeyPrefix = "UnlockSeen_";

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        foreach (var entry in _entries)
        {
            if (entry.buttonObject == null) continue;

            bool unlocked = string.IsNullOrEmpty(entry.requiredClearStageId)
                         || StageClearRepository.Instance.HasCleared(entry.requiredClearStageId);

            entry.buttonObject.SetActive(unlocked);

            if (!unlocked) continue;
            if (!entry.playUnlockAnim) continue;
            if (string.IsNullOrEmpty(entry.requiredClearStageId)) continue;
            if (HasSeenUnlock(entry.requiredClearStageId)) continue;

            // 부모가 activeInHierarchy가 될 때까지 기다렸다가 연출 재생
            var parent     = entry.buttonObject.transform.parent;
            var triggerObj = parent != null ? parent.gameObject : entry.buttonObject;
            StartCoroutine(WaitForActiveAndAnimate(triggerObj, entry.buttonObject, entry.requiredClearStageId));
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private IEnumerator WaitForActiveAndAnimate(GameObject trigger, GameObject target, string stageId)
    {
        yield return new WaitUntil(() => trigger.activeInHierarchy);
        if (HasSeenUnlock(stageId)) yield break;
        MarkSeenUnlock(stageId);
        yield return StartCoroutine(PlayUnlockAnim(target));
    }

    private IEnumerator PlayUnlockAnim(GameObject obj)
    {
        if (!obj.TryGetComponent(out CanvasGroup cg))
            cg = obj.AddComponent<CanvasGroup>();
        var tf = obj.transform;
        Vector3 baseScale = tf.localScale;

        // 페이드 인
        cg.alpha = 0f;
        for (float t = 0f; t < _fadeInDuration; t += Time.deltaTime)
        {
            cg.alpha = t / _fadeInDuration;
            yield return null;
        }
        cg.alpha = 1f;

        // 팝: 커졌다 원래 크기로
        for (float t = 0f; t < _popDuration; t += Time.deltaTime)
        {
            float ratio = t / _popDuration;
            float mul   = ratio < 0.5f
                ? Mathf.Lerp(1f, _popScale, ratio * 2f)
                : Mathf.Lerp(_popScale, 1f, (ratio - 0.5f) * 2f);
            tf.localScale = baseScale * mul;
            yield return null;
        }
        tf.localScale = baseScale;
    }

    [ContextMenu("해금 연출 기록 초기화 (테스트용)")]
    private void ResetSeenFlags()
    {
        foreach (var entry in _entries)
            if (!string.IsNullOrEmpty(entry.requiredClearStageId))
                PlayerPrefs.DeleteKey(SeenKeyPrefix + entry.requiredClearStageId);
        PlayerPrefs.Save();
        Debug.Log("[LobbyUnlockManager] 해금 연출 기록 초기화 완료");
    }

    private static bool HasSeenUnlock(string stageId) =>
        PlayerPrefs.GetInt(SeenKeyPrefix + stageId, 0) == 1;

    private static void MarkSeenUnlock(string stageId)
    {
        PlayerPrefs.SetInt(SeenKeyPrefix + stageId, 1);
        PlayerPrefs.Save();
    }
}
