using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StageClearSequenceController : MonoBehaviour
{
    [Serializable]
    private struct Timings
    {
        public float initialDelay;
        public float hideDeskObjectsDelay;
        public float afterBookCloseDelay;
        public float afterStandUpDelay;
        public float waitForShelfCameraTransition;
        public float afterInsertDelay;
        public float finalFadeDuration;
        public float lobbyLoadDelay;
    }

    [Serializable]
    private struct BookCloseEntry
    {
        [Tooltip("Empty means the spawned book root.")]
        public string childPath;
        public float rotationZ;
        public RotateMode rotateMode;
        public float duration;
        public float delay;
        public Ease ease;
    }

    [Serializable]
    private struct BookSpawnSettings
    {
        public Transform sourceTransform;
        public GameObject closingBookPrefab;
        public Transform parent;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Vector3 spawnScale;
        public BookCloseEntry[] closeEntries;
    }

    [Serializable]
    private struct BookInsertSettings
    {
        public Transform stagingTarget;
        public float stagingMoveDuration;
        public Ease stagingMoveEase;
        public bool matchStagingRotation;
        public bool matchStagingScale;
        public Transform shelfTarget;
        public float moveDuration;
        public Ease moveEase;
        public bool matchRotation;
        public bool matchScale;
    }

    [Serializable]
    private struct FigureRevealEntry
    {
        public GameObject existingObject;
        public GameObject prefab;
        public Transform spawnPoint;
        public float delay;
        public float appearDuration;
        public float startScaleMultiplier;
        public Ease appearEase;
    }

    [Header("Start")]
    [SerializeField] private bool _autoPlayOnWin = true;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Header("Hide On Sequence Start")]
    [Tooltip("Result panel, result texts, continue hint, and any object that must disappear immediately.")]
    [SerializeField] private GameObject[] _objectsToHideOnSequenceStart;
    [SerializeField] private bool _hideRuntimeCharacterObjects = true;

    [Header("Hide During Covered Fade")]
    [Tooltip("Opened book on the desk and objects on top of it.")]
    [SerializeField] private GameObject[] _deskObjectsToHide;

    [Header("Book")]
    [SerializeField] private BookSpawnSettings _bookSpawn;
    [SerializeField] private BookInsertSettings _bookInsert;

    [Header("Cinemachine 3")]
    [SerializeField] private CinemachineCamera _standUpCamera;
    [SerializeField] private int _standUpCameraPriority = 90;
    [SerializeField] private CinemachineCamera _lookAtShelfCamera;
    [SerializeField] private int _lookAtShelfCameraPriority = 100;
    [SerializeField] private CinemachineCamera[] _otherCameras;
    [SerializeField] private int _otherCameraPriority = 0;

    [Header("Stand Up Event")]
    [Tooltip("Use this for stand-up animation, chair movement, SFX, etc.")]
    [SerializeField] private UnityEvent _onStandUp;

    [Header("Shelf Look Event")]
    [Tooltip("Use this for player turn, timeline play, lights, SFX, etc.")]
    [SerializeField] private UnityEvent _onLookAtShelf;

    [Header("Figures")]
    [SerializeField] private bool _hideExistingFiguresOnAwake = true;
    [SerializeField] private FigureRevealEntry[] _figureRevealEntries;

    [Header("Scene")]
    [SerializeField] private string _lobbySceneName = "LobbyScene";
    [SerializeField] private bool _recordStageClear = true;
    [SerializeField] private bool _triggerLobbyEndingDialogue;
    [SerializeField] private string _stageIdOverride;

    [Header("Timings")]
    [SerializeField] private Timings _timings = new Timings
    {
        initialDelay = 0.25f,
        hideDeskObjectsDelay = 0.15f,
        afterBookCloseDelay = 0.2f,
        afterStandUpDelay = 0.6f,
        waitForShelfCameraTransition = 0.35f,
        afterInsertDelay = 0.2f,
        finalFadeDuration = 0.75f,
        lobbyLoadDelay = 0.05f
    };

    private bool _isPlaying;
    private GameObject _spawnedClosingBook;
    private int _cachedStandUpCameraPriority;
    private int _cachedLookAtShelfPriority;
    private int[] _cachedOtherCameraPriorities;

    public bool CanHandleWinSequence => enabled && gameObject.activeInHierarchy;

    private void Awake()
    {
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }

        if (_hideExistingFiguresOnAwake)
            HideExistingFigureObjects();
    }

    private void Start()
    {
        RegisterGameFlowCallback();
    }

    private void OnEnable()
    {
        RegisterGameFlowCallback();
    }

    private void OnDisable()
    {
        UnregisterGameFlowCallback();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        RestoreCameraPriorities();
    }

    private void RegisterGameFlowCallback()
    {
        if (!_autoPlayOnWin) return;

        var gfc = GameFlowController.Instance;
        if (gfc == null) return;

        gfc.OnGameEndDialogueComplete -= HandleGameEndDialogueComplete;
        gfc.OnGameEndDialogueComplete += HandleGameEndDialogueComplete;
    }

    private void UnregisterGameFlowCallback()
    {
        var gfc = GameFlowController.Instance;
        if (gfc != null)
            gfc.OnGameEndDialogueComplete -= HandleGameEndDialogueComplete;
    }

    private void HandleGameEndDialogueComplete(bool isWin)
    {
        if (!isWin || _isPlaying) return;
        PlaySequence();
    }

    [ContextMenu("Play Stage Clear Sequence")]
    public void PlaySequence()
    {
        if (_isPlaying) return;
        StartCoroutine(PlaySequenceCoroutine());
    }

    private IEnumerator PlaySequenceCoroutine()
    {
        _isPlaying = true;
        SetObjectsActive(_objectsToHideOnSequenceStart, false);
        HideRuntimeCharacterObjects();

        if (_timings.initialDelay > 0f)
            yield return new WaitForSeconds(_timings.initialDelay);

        if (_timings.hideDeskObjectsDelay > 0f)
            yield return new WaitForSeconds(_timings.hideDeskObjectsDelay);

        SetObjectsActive(_deskObjectsToHide, false);
        SpawnClosingBook();
        yield return CloseSpawnedBook();

        if (_timings.afterBookCloseDelay > 0f)
            yield return new WaitForSeconds(_timings.afterBookCloseDelay);

        ApplyStandUpCameraPriorities();
        _onStandUp?.Invoke();

        if (_timings.afterStandUpDelay > 0f)
            yield return new WaitForSeconds(_timings.afterStandUpDelay);

        ApplyLookAtShelfCameraPriorities();
        _onLookAtShelf?.Invoke();

        if (_timings.waitForShelfCameraTransition > 0f)
            yield return new WaitForSeconds(_timings.waitForShelfCameraTransition);

        yield return InsertBookToShelf();

        if (_timings.afterInsertDelay > 0f)
            yield return new WaitForSeconds(_timings.afterInsertDelay);

        yield return RevealFigures();
        yield return Fade(1f, _timings.finalFadeDuration);

        if (_timings.lobbyLoadDelay > 0f)
            yield return new WaitForSeconds(_timings.lobbyLoadDelay);

        RecordStageClearIfNeeded();
        LobbyDialogueManager.PendingEndingDialogue = _triggerLobbyEndingDialogue;
        SceneManager.LoadScene(_lobbySceneName);
    }

    private void SpawnClosingBook()
    {
        if (_bookSpawn.sourceTransform == null || _bookSpawn.closingBookPrefab == null)
            return;

        var parent = _bookSpawn.parent != null ? _bookSpawn.parent : _bookSpawn.sourceTransform.parent;
        Vector3 worldPosition = _bookSpawn.sourceTransform.position + _bookSpawn.positionOffset;
        Quaternion worldRotation = _bookSpawn.sourceTransform.rotation * Quaternion.Euler(_bookSpawn.rotationOffset);

        _spawnedClosingBook = Instantiate(_bookSpawn.closingBookPrefab, worldPosition, worldRotation, parent);

        if (_bookSpawn.spawnScale != Vector3.zero)
            _spawnedClosingBook.transform.localScale = _bookSpawn.spawnScale;
    }

    private IEnumerator CloseSpawnedBook()
    {
        if (_spawnedClosingBook == null || _bookSpawn.closeEntries == null || _bookSpawn.closeEntries.Length == 0)
            yield break;

        var sequence = DOTween.Sequence().SetLink(_spawnedClosingBook);

        foreach (var entry in _bookSpawn.closeEntries)
        {
            Transform target = ResolveCloseTarget(entry);
            if (target == null) continue;

            Vector3 targetEuler = new Vector3(
                target.localEulerAngles.x,
                target.localEulerAngles.y,
                entry.rotationZ);

            sequence.Insert(
                Mathf.Max(0f, entry.delay),
                target.DOLocalRotate(targetEuler, Mathf.Max(0.01f, entry.duration), entry.rotateMode)
                    .SetEase(entry.ease));
        }

        yield return sequence.WaitForCompletion();
    }

    private Transform ResolveCloseTarget(BookCloseEntry entry)
    {
        if (_spawnedClosingBook == null)
            return null;

        if (string.IsNullOrEmpty(entry.childPath))
            return _spawnedClosingBook.transform;

        Transform target = _spawnedClosingBook.transform.Find(entry.childPath);
        if (target == null)
            Debug.LogWarning($"[StageClearSequence] Missing child path: {entry.childPath}");
        return target;
    }

    private void ApplyStandUpCameraPriorities()
    {
        CacheAndLowerOtherCameraPriorities();

        if (_standUpCamera != null)
        {
            _cachedStandUpCameraPriority = _standUpCamera.Priority.Value;
            _standUpCamera.Priority = _standUpCameraPriority;
        }

        if (_lookAtShelfCamera != null)
        {
            _cachedLookAtShelfPriority = _lookAtShelfCamera.Priority.Value;
            _lookAtShelfCamera.Priority = _otherCameraPriority;
        }
    }

    private void ApplyLookAtShelfCameraPriorities()
    {
        CacheAndLowerOtherCameraPriorities();

        if (_standUpCamera != null)
            _standUpCamera.Priority = _otherCameraPriority;

        if (_lookAtShelfCamera != null)
        {
            _lookAtShelfCamera.Priority = _lookAtShelfCameraPriority;
        }
    }

    private void CacheAndLowerOtherCameraPriorities()
    {
        if (_otherCameras == null)
            return;

        if (_cachedOtherCameraPriorities != null && _cachedOtherCameraPriorities.Length == _otherCameras.Length)
        {
            for (int i = 0; i < _otherCameras.Length; i++)
            {
                var camera = _otherCameras[i];
                if (camera != null)
                    camera.Priority = _otherCameraPriority;
            }
            return;
        }

        _cachedOtherCameraPriorities = new int[_otherCameras.Length];
        for (int i = 0; i < _otherCameras.Length; i++)
        {
            var camera = _otherCameras[i];
            if (camera == null) continue;

            _cachedOtherCameraPriorities[i] = camera.Priority.Value;
            camera.Priority = _otherCameraPriority;
        }
    }

    private void RestoreCameraPriorities()
    {
        if (_standUpCamera != null)
            _standUpCamera.Priority = _cachedStandUpCameraPriority;

        if (_lookAtShelfCamera != null)
            _lookAtShelfCamera.Priority = _cachedLookAtShelfPriority;

        if (_otherCameras == null || _cachedOtherCameraPriorities == null)
            return;

        int count = Mathf.Min(_otherCameras.Length, _cachedOtherCameraPriorities.Length);
        for (int i = 0; i < count; i++)
        {
            if (_otherCameras[i] != null)
                _otherCameras[i].Priority = _cachedOtherCameraPriorities[i];
        }
    }

    private IEnumerator InsertBookToShelf()
    {
        if (_spawnedClosingBook == null)
            yield break;

        if (_bookInsert.stagingTarget != null)
            yield return MoveBookToTarget(
                _bookInsert.stagingTarget,
                _bookInsert.stagingMoveDuration,
                _bookInsert.stagingMoveEase,
                _bookInsert.matchStagingRotation,
                _bookInsert.matchStagingScale);

        if (_bookInsert.shelfTarget != null)
            yield return MoveBookToTarget(
                _bookInsert.shelfTarget,
                _bookInsert.moveDuration,
                _bookInsert.moveEase,
                _bookInsert.matchRotation,
                _bookInsert.matchScale);
    }

    private IEnumerator MoveBookToTarget(
        Transform target,
        float duration,
        Ease moveEase,
        bool matchRotation,
        bool matchScale)
    {
        if (_spawnedClosingBook == null || target == null)
            yield break;

        var sequence = DOTween.Sequence().SetLink(_spawnedClosingBook);
        float tweenDuration = Mathf.Max(0.01f, duration);

        sequence.Join(_spawnedClosingBook.transform
            .DOMove(target.position, tweenDuration)
            .SetEase(moveEase));

        if (matchRotation)
        {
            sequence.Join(_spawnedClosingBook.transform
                .DORotateQuaternion(target.rotation, tweenDuration)
                .SetEase(moveEase));
        }

        if (matchScale)
        {
            sequence.Join(_spawnedClosingBook.transform
                .DOScale(target.lossyScale, tweenDuration)
                .SetEase(moveEase));
        }

        yield return sequence.WaitForCompletion();
    }

    private IEnumerator RevealFigures()
    {
        if (_figureRevealEntries == null) yield break;

        foreach (var entry in _figureRevealEntries)
        {
            if (entry.delay > 0f)
                yield return new WaitForSeconds(entry.delay);

            GameObject figure = ResolveFigureObject(entry);
            if (figure == null) continue;

            Transform target = figure.transform;
            Vector3 targetScale = target.localScale;
            float multiplier = entry.startScaleMultiplier <= 0f ? 0.01f : entry.startScaleMultiplier;

            target.localScale = targetScale * multiplier;
            figure.SetActive(true);

            yield return target
                .DOScale(targetScale, Mathf.Max(0.01f, entry.appearDuration))
                .SetEase(entry.appearEase)
                .SetLink(figure)
                .WaitForCompletion();
        }
    }

    private static GameObject ResolveFigureObject(FigureRevealEntry entry)
    {
        if (entry.existingObject != null)
            return entry.existingObject;

        if (entry.prefab == null || entry.spawnPoint == null)
            return null;

        var instance = Instantiate(entry.prefab, entry.spawnPoint.position, entry.spawnPoint.rotation, entry.spawnPoint.parent);
        instance.SetActive(false);
        return instance;
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (_fadeCanvasGroup == null)
            yield break;

        bool blocksRaycasts = targetAlpha > 0.999f;
        _fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
        _fadeCanvasGroup.interactable = blocksRaycasts;

        yield return _fadeCanvasGroup
            .DOFade(targetAlpha, Mathf.Max(0.01f, duration))
            .SetEase(targetAlpha > _fadeCanvasGroup.alpha ? Ease.InQuad : Ease.OutQuad)
            .SetLink(gameObject)
            .WaitForCompletion();

        _fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
        _fadeCanvasGroup.interactable = blocksRaycasts;
    }

    private void RecordStageClearIfNeeded()
    {
        if (!_recordStageClear) return;

        string stageId = !string.IsNullOrEmpty(_stageIdOverride)
            ? _stageIdOverride
            : NewGameConfig.StageId;

        if (!string.IsNullOrEmpty(stageId))
            StageClearRepository.Instance.RecordClear(stageId);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var go in objects)
        {
            if (go != null)
                go.SetActive(active);
        }
    }

    private void HideExistingFigureObjects()
    {
        if (_figureRevealEntries == null) return;

        foreach (var entry in _figureRevealEntries)
        {
            if (entry.existingObject != null)
                entry.existingObject.SetActive(false);
        }
    }

    private void HideRuntimeCharacterObjects()
    {
        if (!_hideRuntimeCharacterObjects)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        var roots = activeScene.GetRootGameObjects();
        var targets = new HashSet<GameObject>();

        foreach (var root in roots)
            CollectCharacterObjects(root.transform, targets);

        foreach (var target in targets)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    private static void CollectCharacterObjects(Transform current, HashSet<GameObject> targets)
    {
        if (current == null)
            return;

        if (current.name.StartsWith("Character", StringComparison.Ordinal))
        {
            targets.Add(current.gameObject);
            return;
        }

        for (int i = 0; i < current.childCount; i++)
            CollectCharacterObjects(current.GetChild(i), targets);
    }
}
