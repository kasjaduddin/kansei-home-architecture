using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace VRHomeArch.DataCollection
{
    // Drives the full data collection session state machine.
    //
    // State flow:
    //   IDLE
    //     -> [server has active respondent] -> TRAINING
    //   TRAINING  (training area visible, default skybox, locomotion on)
    //     -> [all training steps done + exit trigger] -> WAITING_FOR_BASELINE
    //   WAITING_FOR_BASELINE  (neutral skybox, no 3D objects, locomotion off, remove-headset UI shown)
    //     -> [headset removed] : gray room activated, teleport to origin
    //     -> [headset put on]  -> BASELINE (2 min fixed timer)
    //   BASELINE  (gray room, neutral skybox, controllers hidden)
    //     -> [timer ends] -> NEUTRAL
    //   NEUTRAL  (neutral skybox, no 3D objects, controllers hidden, 30 sec timer)
    //     -> [timer ends] -> HOUSE_EXPLORATION
    //   HOUSE_EXPLORATION  (house active, default skybox, locomotion on, controllers visible, 2 min timer)
    //     -> [timer ends] -> POST combination-done to server
    //                     -> GET next combination from server
    //                     -> if all done: SESSION_COMPLETE
    //                     -> else: WAITING_FOR_BREAK
    //   WAITING_FOR_BREAK  (neutral skybox, no 3D objects, locomotion off, remove-headset UI shown)
    //     -> [headset removed] : teleport to origin
    //     -> [headset put on]  -> NEUTRAL
    //   SESSION_COMPLETE
    //     -> teleport to origin, return to IDLE to await next respondent
    public class SessionManager : MonoBehaviour
    {
        // -- Phase durations
        private const float BaselineDurationSeconds = 120f;
        private const float ExplorationDurationSeconds = 120f;
        private const float NeutralDurationSeconds = 30f;
        private const float IdlePollIntervalSeconds = 5f;

        // -- Inspector: Dependencies
        [Header("Dependencies")]
        [SerializeField] private ApiClient _apiClient;
        [SerializeField] private PresenceSensor _presenceSensor;
        [SerializeField] private TransitionFader _transitionFader;

        // -- Inspector: Layout registry (assign prefabs from Assets/Prefab/HomeDesignPrefab/Type36/)
        [Header("Layout Registry")]
        [SerializeField] private List<LayoutEntry> _layoutRegistry;

        // -- Inspector: All 12 MaterialCombination ScriptableObjects (C01-C12)
        [Header("Material Combinations")]
        [SerializeField] private List<MaterialCombination> _materialCombinations;

        // -- Inspector: Skybox materials for neutralization
        [Header("Skybox")]
        [SerializeField] private Material _neutralSkyboxMaterial;
        [SerializeField] private Material _defaultSkyboxMaterial;

        // -- Inspector: Root transform of the XR Rig — assign the XR Origin (XR Rig) child,
        //    NOT the XR Interaction Setup root. XROrigin component lives on XR Origin (XR Rig).
        [Header("XR Rig")]
        [SerializeField] private Transform _xrRigRoot;

        // -- Inspector: Scene objects toggled during phase transitions
        [Header("Scene Objects")]
        [SerializeField] private GameObject _trainingArea;
        [SerializeField] private GameObject _removeHeadsetPrompt;
        // Gray room geometry is only active during Baseline — kept inactive the rest of the session
        // to avoid visual overlap with the house or training area.
        [SerializeField] private GameObject _grayRoom;
        // The Move GameObject under XR Origin (XR Rig)/Locomotion System/Move.
        // Disabled during WaitingForBaseline so the respondent cannot walk while the UI is shown.
        // Re-enabled when HouseExploration begins.
        [SerializeField] private GameObject _moveProvider;
        // Controller visuals — hidden during Baseline and Neutral so the respondent
        // is not distracted by controller models in stimulus-free phases.
        // Assign: XR Origin (XR Rig)/Camera Offset/Left Controller/XR Controller Left (Clone)
        [SerializeField] private GameObject _leftController;
        // Assign: XR Origin (XR Rig)/Camera Offset/Right Controller/XR Controller Right (Clone)
        [SerializeField] private GameObject _rightController;

        // -- Runtime state
        private SessionPhase _phase = SessionPhase.Idle;
        private RespondentApiResponse _activeRespondent;
        private GameObject _houseInstance;
        private Coroutine _activeTimer;
        private Coroutine _idlePollCoroutine;
        private Coroutine _fadeCoroutine;

        // Exposed read-only for debug UI or future researcher display
        public SessionPhase CurrentPhase => _phase;
        public string ActiveRespondentId => _activeRespondent?.respondentId;
        public string CurrentCombinationId => _activeRespondent?.combinationId;

        [Serializable]
        private class LayoutEntry
        {
            public string layoutName;   // Must match layoutPrefabName in respondent JSON (e.g. "HomeDesignType36_2")
            public GameObject prefab;
        }

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_apiClient == null)
                Debug.LogError("[SessionManager] ApiClient reference is not assigned.");

            if (_presenceSensor == null)
                Debug.LogError("[SessionManager] PresenceSensor reference is not assigned.");

            _presenceSensor.OnHeadsetPutOn += HandleHeadsetPutOn;
            _presenceSensor.OnHeadsetRemoved += HandleHeadsetRemoved;
        }

        private void OnDestroy()
        {
            _presenceSensor.OnHeadsetPutOn -= HandleHeadsetPutOn;
            _presenceSensor.OnHeadsetRemoved -= HandleHeadsetRemoved;
        }

        private void Start()
        {
            TransitionTo(SessionPhase.Idle);
        }

        // -----------------------------------------------------------------------
        // External trigger entry points (called by scene objects, not presence sensor)
        // -----------------------------------------------------------------------

        // Called by TrainingExitTrigger when the respondent physically walks out of the training area.
        // Removing the headset is the signal to transition to WaitingForBaseline —
        // the trigger zone confirms they are done with training and ready for the break.
        public void NotifyTrainingExitTriggered()
        {
            if (_phase != SessionPhase.Training)
            {
                Debug.LogWarning($"[SessionManager] NotifyTrainingExitTriggered called in phase {_phase} — ignored.");
                return;
            }

            FadeAndTransition(SessionPhase.WaitingForBaseline);
        }

        // -----------------------------------------------------------------------
        // Presence sensor event handlers
        // -----------------------------------------------------------------------

        private void HandleHeadsetPutOn()
        {
            switch (_phase)
            {
                case SessionPhase.WaitingForBaseline:
                    TransitionTo(SessionPhase.Baseline);
                    break;

                case SessionPhase.WaitingForBreak:
                    TransitionTo(SessionPhase.Neutral);
                    break;
            }
        }

        private void HandleHeadsetRemoved()
        {
            switch (_phase)
            {
                case SessionPhase.WaitingForBaseline:
                    // Headset is off — safe window to activate gray room invisibly.
                    // Training area was already deactivated in OnEnterWaitingForBaseline.
                    // Teleport happens here so the respondent is already inside the gray room
                    // when they put the headset back on.
                    if (_grayRoom != null)
                        _grayRoom.SetActive(true);

                    TeleportToOrigin();

                    Debug.Log("[SessionManager] Headset removed in WAITING_FOR_BASELINE — " +
                              "gray room activated, awaiting headset put-on to start baseline timer");
                    break;

                case SessionPhase.WaitingForBreak:
                    // House was already deactivated in OnEnterWaitingForBreak. Teleport here while
                    // the headset is off so the respondent wakes up at the origin position.
                    TeleportToOrigin();

                    Debug.Log("[SessionManager] Headset removed in WAITING_FOR_BREAK — " +
                              "awaiting headset put-on to start neutral timer");
                    break;

                case SessionPhase.HouseExploration:
                    // The 2-min timer should end and POST before the respondent removes the headset.
                    // This is a safeguard for unexpected early removal.
                    Debug.LogWarning("[SessionManager] Headset removed during exploration before timer ended. " +
                                     "This may indicate the respondent removed the headset early.");
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // State machine transitions
        // -----------------------------------------------------------------------

        // startTimer controls whether phase timers are started immediately after scene setup.
        // Pass false when entering via a fade — FadeTransitionCoroutine calls StartTimerForPhase
        // after fade-out completes so the timer begins only when the new environment is visible.
        private void TransitionTo(SessionPhase newPhase, bool startTimer = true)
        {
            StopActiveTimer();
            _phase = newPhase;

            switch (newPhase)
            {
                case SessionPhase.Idle: OnEnterIdle(); break;
                case SessionPhase.Training: OnEnterTraining(); break;
                case SessionPhase.WaitingForBaseline: OnEnterWaitingForBaseline(); break;
                case SessionPhase.Baseline: OnEnterBaseline(); break;
                case SessionPhase.HouseExploration: OnEnterHouseExploration(); break;
                case SessionPhase.WaitingForBreak: OnEnterWaitingForBreak(); break;
                case SessionPhase.Neutral: OnEnterNeutral(); break;
                case SessionPhase.SessionComplete: OnEnterSessionComplete(); break;
            }

            if (startTimer)
                StartTimerForPhase(newPhase);
        }

        // -----------------------------------------------------------------------
        // Phase entry handlers
        // -----------------------------------------------------------------------

        private void OnEnterIdle()
        {
            Debug.Log("[SessionManager] Phase: IDLE — polling server for active respondent");
            SetSkybox(_defaultSkyboxMaterial);
            _idlePollCoroutine = StartCoroutine(PollServerForRespondent());
        }

        private void OnEnterTraining()
        {
            Debug.Log($"[SessionManager] Phase: TRAINING — respondent {_activeRespondent.respondentId}, " +
                      $"layout {_activeRespondent.layoutPrefabName}");

            // Restore training area in case a previous session deactivated it
            if (_trainingArea != null)
                _trainingArea.SetActive(true);

            // Restore locomotion in case it was disabled at end of previous session
            if (_moveProvider != null)
                _moveProvider.SetActive(true);

            SetSkybox(_defaultSkyboxMaterial);
            TeleportToOrigin();
        }

        private void OnEnterWaitingForBaseline()
        {
            Debug.Log("[SessionManager] Phase: WAITING_FOR_BASELINE — training complete, " +
                      "showing neutral environment and prompting headset removal");

            // Switch to neutral environment immediately so the respondent sees a clean
            // white skybox with no 3D objects while the remove-headset prompt is shown.
            SetSkybox(_neutralSkyboxMaterial);

            // Training area is no longer needed — deactivate it now while the respondent
            // is still wearing the headset. The neutral skybox replaces it visually.
            if (_trainingArea != null)
                _trainingArea.SetActive(false);

            // Prevent the respondent from walking around while the removal UI is shown
            if (_moveProvider != null)
                _moveProvider.SetActive(false);

            // Prompt respondent to remove the headset so they can fill the pre-study form
            if (_removeHeadsetPrompt != null)
                _removeHeadsetPrompt.SetActive(true);
        }

        private void OnEnterBaseline()
        {
            Debug.Log($"[SessionManager] Phase: BASELINE — {BaselineDurationSeconds}s timer will start after setup");

            // Respondent has put the headset back on — dismiss the removal prompt.
            // GrayRoom was already activated and teleport already happened in HandleHeadsetRemoved
            // while the headset was off, so the scene transition is invisible to the respondent.
            if (_removeHeadsetPrompt != null)
                _removeHeadsetPrompt.SetActive(false);

            // Hide controller visuals — no interaction needed during baseline measurement
            SetControllersActive(false);

            SetSkybox(_neutralSkyboxMaterial);
        }

        private void OnEnterHouseExploration()
        {
            if (_activeRespondent == null || string.IsNullOrEmpty(_activeRespondent.combinationId))
            {
                Debug.LogError("[SessionManager] Cannot enter HouseExploration — no combination data available");
                return;
            }

            // Gray room is only needed during Baseline — dismiss it before showing the house.
            if (_grayRoom != null)
                _grayRoom.SetActive(false);

            // House instance is instantiated inactive during IDLE so it does not appear
            // before the session starts. Activate it now that the respondent is ready.
            if (_houseInstance != null)
                _houseInstance.SetActive(true);

            // Respondent needs to walk freely inside the house
            if (_moveProvider != null)
                _moveProvider.SetActive(true);

            // Restore controller visuals now that the respondent is inside the house
            SetControllersActive(true);

            ApplyCombinationToHouse(_activeRespondent.combinationId);
            SetSkybox(_defaultSkyboxMaterial);
            TeleportToOrigin();

            Debug.Log($"[SessionManager] Phase: HOUSE_EXPLORATION — combination {_activeRespondent.combinationId}, " +
                      $"index {_activeRespondent.nextCombinationIndex} — timer will start after fade-out");
        }

        private void OnEnterWaitingForBreak()
        {
            Debug.Log("[SessionManager] Phase: WAITING_FOR_BREAK — exploration complete, " +
                      "showing neutral environment and prompting headset removal");

            // Switch to neutral environment immediately — the exploration timer has ended
            // so there is no longer a reason to keep the house or default skybox visible.
            SetSkybox(_neutralSkyboxMaterial);

            // Deactivate the house now while the respondent is still wearing the headset.
            // The neutral skybox replaces it visually — the respondent sees a clean white
            // environment rather than the house disappearing without context.
            if (_houseInstance != null)
                _houseInstance.SetActive(false);

            // Disable locomotion — respondent should not walk while prompt is shown
            if (_moveProvider != null)
                _moveProvider.SetActive(false);

            // Prompt respondent to remove the headset to fill the per-combination questionnaire
            if (_removeHeadsetPrompt != null)
                _removeHeadsetPrompt.SetActive(true);

            // No timer — presence sensor drives the next transition
        }

        private void OnEnterNeutral()
        {
            Debug.Log($"[SessionManager] Phase: NEUTRAL — {NeutralDurationSeconds}s timer will start after fade-out");

            // Respondent has put the headset back on — dismiss the removal prompt
            if (_removeHeadsetPrompt != null)
                _removeHeadsetPrompt.SetActive(false);

            // Gray room must be deactivated here — when transitioning from Baseline,
            // it is still active and needs to be cleared before showing the neutral skybox.
            if (_grayRoom != null)
                _grayRoom.SetActive(false);

            // Hide controller visuals — no interaction needed during neutral phase
            SetControllersActive(false);

            SetSkybox(_neutralSkyboxMaterial);
            TeleportToOrigin();
        }

        private void OnEnterSessionComplete()
        {
            Debug.Log($"[SessionManager] Phase: SESSION_COMPLETE — {_activeRespondent?.respondentId ?? "unknown"} " +
                      "has viewed all combinations. Returning to idle.");
            SetSkybox(_defaultSkyboxMaterial);
            TeleportToOrigin();

            // Clear respondent so IDLE polls for the next one
            _activeRespondent = null;

            // Short delay before returning to idle so the state is stable
            StartCoroutine(DelayedTransition(2f, SessionPhase.Idle));
        }

        // -----------------------------------------------------------------------
        // Exploration timer callback — the most complex transition
        // -----------------------------------------------------------------------

        private void OnExplorationTimerEnded()
        {
            int completedIndex = _activeRespondent.nextCombinationIndex;
            string respondentId = _activeRespondent.respondentId;

            Debug.Log($"[SessionManager] Exploration ended — posting completion for index {completedIndex}");

            // POST immediately so progress is recorded before headset removal.
            // Non-fatal: session continues even if the POST fails — researcher can
            // verify server logs after the session and correct manually if needed.
            _apiClient.PostCombinationDone(
                respondentId,
                completedIndex,
                onSuccess: () =>
                {
                    Debug.Log($"[SessionManager] Combination index {completedIndex} recorded on server");
                },
                onError: err =>
                {
                    Debug.LogWarning($"[SessionManager] Failed to record combination on server: {err}. " +
                                     "Progress may need manual correction.");
                }
            );

            // Fetch the updated respondent state to get the next combinationId.
            // Also determines whether the session is complete.
            _apiClient.GetActiveRespondent(
                onSuccess: updatedResponse =>
                {
                    _activeRespondent = updatedResponse;

                    if (updatedResponse.isComplete)
                    {
                        TransitionTo(SessionPhase.SessionComplete);
                    }
                    else
                    {
                        Debug.Log($"[SessionManager] Next combination will be: {updatedResponse.combinationId}");
                        FadeAndTransition(SessionPhase.WaitingForBreak);
                    }
                },
                onError: err =>
                {
                    // Cannot determine if there are more combinations — fail safe to WaitingForBreak
                    // and let the next GET on the next session attempt resolve it.
                    Debug.LogWarning($"[SessionManager] Could not refresh respondent state after exploration: {err}. " +
                                     "Defaulting to WAITING_FOR_BREAK. If this was the last combination, " +
                                     "the next session start will detect isComplete.");
                    FadeAndTransition(SessionPhase.WaitingForBreak);
                }
            );
        }

        // -----------------------------------------------------------------------
        // Server polling (IDLE phase)
        // -----------------------------------------------------------------------

        private IEnumerator PollServerForRespondent()
        {
            while (_phase == SessionPhase.Idle)
            {
                bool responseReceived = false;

                _apiClient.GetActiveRespondent(
                    onSuccess: response =>
                    {
                        if (response.isComplete)
                        {
                            Debug.Log($"[SessionManager] {response.respondentId} already completed all combinations — " +
                                      "ask researcher to POST a new active respondent");
                            responseReceived = true;
                            return;
                        }

                        _activeRespondent = response;
                        LoadLayoutPrefab(response.layoutPrefabName);
                        responseReceived = true;

                        // Stop polling and begin session
                        if (_idlePollCoroutine != null)
                            StopCoroutine(_idlePollCoroutine);

                        TransitionTo(SessionPhase.Training);
                    },
                    onError: err =>
                    {
                        Debug.Log($"[SessionManager] Server poll: {err} — retrying in {IdlePollIntervalSeconds}s");
                        responseReceived = true;
                    }
                );

                yield return new WaitUntil(() => responseReceived);

                if (_phase == SessionPhase.Idle)
                    yield return new WaitForSeconds(IdlePollIntervalSeconds);
            }
        }

        // -----------------------------------------------------------------------
        // Material application
        // -----------------------------------------------------------------------

        private void ApplyCombinationToHouse(string combinationId)
        {
            if (_houseInstance == null)
            {
                Debug.LogError("[SessionManager] Cannot apply combination — house prefab not instantiated");
                return;
            }

            MaterialCombination combination = _materialCombinations.Find(c => c.CombinationId == combinationId);
            if (combination == null)
            {
                Debug.LogError($"[SessionManager] MaterialCombination '{combinationId}' not found in list. " +
                               "Ensure all C01-C12 assets are assigned in the Inspector.");
                return;
            }

            MaterialApplicator applicator = _houseInstance.GetComponent<MaterialApplicator>();
            if (applicator == null)
            {
                Debug.LogError($"[SessionManager] MaterialApplicator component missing on {_houseInstance.name}");
                return;
            }

            applicator.ApplyCombination(combination);
            Debug.Log($"[SessionManager] Applied {combinationId} to {_houseInstance.name}");
        }

        // -----------------------------------------------------------------------
        // Layout management
        // -----------------------------------------------------------------------

        private void LoadLayoutPrefab(string layoutName)
        {
            if (_houseInstance != null)
            {
                Destroy(_houseInstance);
                _houseInstance = null;
            }

            LayoutEntry entry = _layoutRegistry.Find(e => e.layoutName == layoutName);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError($"[SessionManager] Layout '{layoutName}' not found in registry. " +
                               "Drag the prefab into the Layout Registry list in the Inspector.");
                return;
            }

            _houseInstance = Instantiate(entry.prefab);
            _houseInstance.SetActive(false); // Activated later in OnEnterHouseExploration
            Debug.Log($"[SessionManager] Instantiated layout: {layoutName}");
        }

        // -----------------------------------------------------------------------
        // Utilities
        // -----------------------------------------------------------------------

        private void TeleportToOrigin()
        {
            if (_xrRigRoot == null)
            {
                Debug.LogWarning("[SessionManager] XR Rig Root is not assigned — cannot teleport");
                return;
            }

            // All phase transitions land at world origin (0, 0, 0).
            // SpawnPoints are not used — the scene is designed so that all relevant areas
            // (training, gray room, house) are centered at or near the world origin.
            _xrRigRoot.position = Vector3.zero;
            _xrRigRoot.rotation = Quaternion.identity;
        }

        // Runs a fade-in → TransitionTo → fade-out sequence.
        // Falls back to a direct transition if TransitionFader is not assigned.
        // Used for the 4 timed phase transitions where abrupt scene changes would be jarring.
        private void FadeAndTransition(SessionPhase nextPhase)
        {
            if (_transitionFader == null)
            {
                Debug.LogWarning("[SessionManager] TransitionFader is not assigned — transitioning without fade.");
                TransitionTo(nextPhase);
                return;
            }

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeTransitionCoroutine(nextPhase));
        }

        private IEnumerator FadeTransitionCoroutine(SessionPhase nextPhase)
        {
            // Fade to white — scene state swap happens while the overlay is at peak opacity
            yield return StartCoroutine(_transitionFader.FadeIn());

            // Scene objects, skybox, locomotion toggled here while the view is obscured.
            // startTimer: false — timer must not begin until the new environment is visible.
            TransitionTo(nextPhase, startTimer: false);

            // Reveal the new environment gradually
            yield return StartCoroutine(_transitionFader.FadeOut());

            // Timer starts here — the respondent can now see the new environment fully.
            // This ensures phase duration accuracy is not eroded by fade duration.
            StartTimerForPhase(nextPhase);

            _fadeCoroutine = null;
        }

        // Starts the timer for phases that have a fixed duration.
        // Called either by TransitionTo (for instant transitions) or by FadeTransitionCoroutine
        // (after fade-out completes) to ensure timer accuracy is not reduced by fade duration.
        private void StartTimerForPhase(SessionPhase phase)
        {
            switch (phase)
            {
                case SessionPhase.Baseline:
                    Debug.Log($"[SessionManager] BASELINE timer started — {BaselineDurationSeconds}s");
                    _activeTimer = StartCoroutine(RunTimer(BaselineDurationSeconds, () =>
                    {
                        FadeAndTransition(SessionPhase.Neutral);
                    }));
                    break;

                case SessionPhase.Neutral:
                    Debug.Log($"[SessionManager] NEUTRAL timer started — {NeutralDurationSeconds}s");
                    _activeTimer = StartCoroutine(RunTimer(NeutralDurationSeconds, () =>
                    {
                        FadeAndTransition(SessionPhase.HouseExploration);
                    }));
                    break;

                case SessionPhase.HouseExploration:
                    Debug.Log($"[SessionManager] HOUSE_EXPLORATION timer started — {ExplorationDurationSeconds}s");
                    _activeTimer = StartCoroutine(RunTimer(ExplorationDurationSeconds, OnExplorationTimerEnded));
                    break;

                    // Other phases have no fixed-duration timer — presence sensor or server
                    // response drives the next transition.
            }
        }

        private void SetSkybox(Material skybox)
        {
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }

        private void StopActiveTimer()
        {
            if (_activeTimer != null)
            {
                StopCoroutine(_activeTimer);
                _activeTimer = null;
            }
        }

        private IEnumerator RunTimer(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private IEnumerator DelayedTransition(float delay, SessionPhase target)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(target);
        }
        private void SetControllersActive(bool active)
        {
            if (_leftController != null)
                _leftController.SetActive(active);

            if (_rightController != null)
                _rightController.SetActive(active);
        }
    }

    public enum SessionPhase
    {
        Idle,
        Training,
        WaitingForBaseline,
        Baseline,
        HouseExploration,
        WaitingForBreak,
        Neutral,
        SessionComplete
    }
}