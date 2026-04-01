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
    //     -> [POST /session-break received]      : gray room activated, teleport to origin
    //     -> [POST /start-timer baseline received] -> BASELINE (2 min fixed timer)
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
    //     -> [POST /session-break received]     : teleport to origin
    //     -> [POST /start-timer neutral received] -> NEUTRAL
    //   SESSION_COMPLETE
    //     -> teleport to origin, return to IDLE to await next respondent
    //
    // Phase transitions driven by headset presence sensor have been replaced by
    // explicit REST API signals from the researcher. This allows the researcher
    // to confirm the respondent is comfortable before starting a timed phase,
    // rather than relying on the proximity sensor which fires the moment the
    // headset is placed on the head regardless of fit adjustment.
    public class SessionManager : MonoBehaviour
    {
        // -- Phase durations
        private const float BaselineDurationSeconds = 120f;
        private const float ExplorationDurationSeconds = 120f;
        private const float NeutralDurationSeconds = 30f;
        private const float IdlePollIntervalSeconds = 5f;

        // How often to poll GET /session-signal while waiting for a researcher action.
        // 2 seconds is responsive enough for a human-triggered event without hammering the server.
        private const float SignalPollIntervalSeconds = 2f;

        // -- Inspector: Dependencies
        [Header("Dependencies")]
        [SerializeField] private ApiClient _apiClient;
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
        [SerializeField] private TrainingGuide _trainingGuide;
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
        private Coroutine _signalPollCoroutine;

        // Tracks whether the "break" signal has been received in the current waiting phase.
        // This guards against a start-timer signal arriving before the break signal,
        // or being replayed unexpectedly by the researcher.
        private bool _breakSignalReceived;

        // Set when the last combination has been completed. Causes WaitingForBreak to route
        // to SessionComplete instead of Neutral when the start-timer signal is received,
        // while still showing the remove-headset prompt after the final exploration phase.
        private bool _pendingSessionComplete;

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

            // TrainingGuide fires this when the last training step is completed.
            // SessionManager responds by triggering the transition to WaitingForBaseline.
            if (_trainingGuide != null)
                _trainingGuide.OnTrainingCompleted += NotifyTrainingExitTriggered;
            else
                Debug.LogWarning("[SessionManager] TrainingGuide is not assigned — training will never complete.");
        }

        private void OnDestroy()
        {
            if (_trainingGuide != null)
                _trainingGuide.OnTrainingCompleted -= NotifyTrainingExitTriggered;
        }

        private void Start()
        {
            TransitionTo(SessionPhase.Idle);
        }

        // -----------------------------------------------------------------------
        // External trigger entry points
        // -----------------------------------------------------------------------

        // Called by TrainingGuide when all training steps are completed.
        // Fade transition begins immediately — no researcher intervention required here.
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
        // State machine transitions
        // -----------------------------------------------------------------------

        // startTimer controls whether phase timers are started immediately after scene setup.
        // Pass false when entering via a fade — FadeTransitionCoroutine calls StartTimerForPhase
        // after fade-out completes so the timer begins only when the new environment is visible.
        private void TransitionTo(SessionPhase newPhase, bool startTimer = true)
        {
            StopActiveTimer();
            StopSignalPolling();

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

            // Reset training guide so the next respondent starts from step 0.
            _trainingGuide?.ResetTraining();

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

            // Begin the guided training sequence — activates steps one by one.
            // The exit trigger fires via OnTrainingCompleted when all steps are done.
            _trainingGuide?.BeginTraining();
        }

        private void OnEnterWaitingForBaseline()
        {
            Debug.Log("[SessionManager] Phase: WAITING_FOR_BASELINE — showing neutral environment, " +
                      "awaiting researcher POST /session-break then POST /start-timer baseline");

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

            _breakSignalReceived = false;
            StartSignalPolling(expectedTimerType: "baseline");
        }

        private void OnEnterBaseline()
        {
            Debug.Log($"[SessionManager] Phase: BASELINE — {BaselineDurationSeconds}s timer will start after setup");

            // Gray room was activated and teleport happened when the "break" signal was received,
            // so the scene swap is already invisible. Just dismiss the removal prompt.
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
                      "awaiting researcher POST /session-break then POST /start-timer neutral");

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

            _breakSignalReceived = false;
            _pendingSessionComplete = false;  // Will be set by OnExplorationTimerEnded if this is the final break
            StartSignalPolling(expectedTimerType: "neutral");
        }

        private void OnEnterNeutral()
        {
            Debug.Log($"[SessionManager] Phase: NEUTRAL — {NeutralDurationSeconds}s timer will start after fade-out");

            // Gray room was used during Baseline — clear it before the neutral phase.
            // When coming from WaitingForBreak, gray room is already inactive.
            if (_grayRoom != null)
                _grayRoom.SetActive(false);

            // Dismiss the removal prompt — teleport already happened on "break" signal
            if (_removeHeadsetPrompt != null)
                _removeHeadsetPrompt.SetActive(false);

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

            // POST first, GET inside the callback — guarantees server progress is updated
            // before we ask for the next combinationId, preventing the race condition where
            // GET returns the stale (just-completed) combinationId.
            _apiClient.PostCombinationDone(
                respondentId,
                completedIndex,
                onSuccess: () =>
                {
                    Debug.Log($"[SessionManager] Combination index {completedIndex} recorded on server");

                    _apiClient.GetActiveRespondent(
                        onSuccess: updatedResponse =>
                        {
                            _activeRespondent = updatedResponse;

                            if (updatedResponse.isComplete)
                            {
                                // All combinations done — flag for SessionComplete but still show
                                // the remove-headset prompt via WaitingForBreak so the respondent
                                // can hand back the headset before the session is closed.
                                _pendingSessionComplete = true;
                                Debug.Log("[SessionManager] All combinations complete — routing through WAITING_FOR_BREAK before SESSION_COMPLETE");
                            }
                            else
                            {
                                Debug.Log($"[SessionManager] Next combination will be: {updatedResponse.combinationId}");
                            }

                            FadeAndTransition(SessionPhase.WaitingForBreak);
                        },
                        onError: err =>
                        {
                            // Cannot determine if there are more combinations — fail safe to WaitingForBreak.
                            // The next start-timer signal will resolve the correct next phase.
                            Debug.LogWarning($"[SessionManager] Could not refresh respondent state after exploration: {err}. " +
                                             "Defaulting to WAITING_FOR_BREAK.");
                            FadeAndTransition(SessionPhase.WaitingForBreak);
                        }
                    );
                },
                onError: err =>
                {
                    Debug.LogWarning($"[SessionManager] Failed to record combination on server: {err}. " +
                                     "Progress may need manual correction. Attempting GET anyway.");

                    // POST failed — still GET so the session can continue.
                    // The duplicate-index guard on the server means re-POSTing the same index
                    // on the next timer end is safe and will not double-count.
                    _apiClient.GetActiveRespondent(
                        onSuccess: updatedResponse =>
                        {
                            _activeRespondent = updatedResponse;
                            if (updatedResponse.isComplete)
                            {
                                _pendingSessionComplete = true;
                                Debug.Log("[SessionManager] All combinations complete (after POST failure) — routing through WAITING_FOR_BREAK");
                            }
                            FadeAndTransition(SessionPhase.WaitingForBreak);
                        },
                        onError: getErr =>
                        {
                            Debug.LogWarning($"[SessionManager] GET also failed after POST failure: {getErr}. " +
                                             "Defaulting to WAITING_FOR_BREAK.");
                            FadeAndTransition(SessionPhase.WaitingForBreak);
                        }
                    );
                }
            );
        }

        // -----------------------------------------------------------------------
        // Server polling — IDLE phase
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
        // Signal polling — WAITING_FOR_BASELINE and WAITING_FOR_BREAK phases
        // -----------------------------------------------------------------------

        // Begins polling GET /session-signal. The expectedTimerType determines which
        // start-timer signal to act on: "baseline" for WaitingForBaseline,
        // "neutral" for WaitingForBreak.
        private void StartSignalPolling(string expectedTimerType)
        {
            _signalPollCoroutine = StartCoroutine(PollSessionSignal(expectedTimerType));
        }

        private void StopSignalPolling()
        {
            if (_signalPollCoroutine != null)
            {
                StopCoroutine(_signalPollCoroutine);
                _signalPollCoroutine = null;
            }
        }

        private IEnumerator PollSessionSignal(string expectedTimerType)
        {
            Debug.Log($"[SessionManager] Signal polling started — expecting 'break' then 'start_{expectedTimerType}'");

            while (true)
            {
                bool responseReceived = false;

                _apiClient.GetSessionSignal(
                    onSuccess: signal =>
                    {
                        if (!string.IsNullOrEmpty(signal))
                            HandleSessionSignal(signal, expectedTimerType);

                        responseReceived = true;
                    },
                    onError: err =>
                    {
                        Debug.LogWarning($"[SessionManager] Signal poll failed: {err} — retrying in {SignalPollIntervalSeconds}s");
                        responseReceived = true;
                    }
                );

                yield return new WaitUntil(() => responseReceived);
                yield return new WaitForSeconds(SignalPollIntervalSeconds);
            }
        }

        private void HandleSessionSignal(string signal, string expectedTimerType)
        {
            Debug.Log($"[SessionManager] Signal received: '{signal}' (phase: {_phase})");

            switch (signal)
            {
                case "break":
                    HandleBreakSignal();
                    break;

                case "start_baseline":
                    if (expectedTimerType == "baseline")
                        HandleStartTimerSignal(SessionPhase.Baseline);
                    else
                        Debug.LogWarning($"[SessionManager] Received 'start_baseline' but expected 'start_{expectedTimerType}' — ignored.");
                    break;

                case "start_neutral":
                    if (expectedTimerType == "neutral")
                        HandleStartTimerSignal(SessionPhase.Neutral);
                    else
                        Debug.LogWarning($"[SessionManager] Received 'start_neutral' but expected 'start_{expectedTimerType}' — ignored.");
                    break;

                default:
                    Debug.LogWarning($"[SessionManager] Unrecognized signal: '{signal}' — ignored.");
                    break;
            }
        }

        // Researcher confirmed the respondent has removed the headset.
        // Activate gray room and teleport while the headset is off so the
        // scene swap is invisible when the respondent puts it back on.
        private void HandleBreakSignal()
        {
            if (_breakSignalReceived)
            {
                Debug.LogWarning("[SessionManager] Duplicate 'break' signal received — ignored.");
                return;
            }

            _breakSignalReceived = true;

            switch (_phase)
            {
                case SessionPhase.WaitingForBaseline:
                    // Training area was already deactivated in OnEnterWaitingForBaseline.
                    if (_grayRoom != null)
                        _grayRoom.SetActive(true);

                    TeleportToOrigin();
                    Debug.Log("[SessionManager] Break signal in WAITING_FOR_BASELINE — gray room activated, " +
                              "awaiting start-timer baseline signal to begin measurement");
                    break;

                case SessionPhase.WaitingForBreak:
                    // House was already deactivated in OnEnterWaitingForBreak.
                    TeleportToOrigin();
                    Debug.Log("[SessionManager] Break signal in WAITING_FOR_BREAK — teleported to origin, " +
                              "awaiting start-timer neutral signal");
                    break;

                default:
                    Debug.LogWarning($"[SessionManager] 'break' signal received in unexpected phase {_phase} — teleport skipped.");
                    break;
            }
        }

        // Researcher confirmed the respondent has the headset on and is comfortable.
        // Transition to the next timed phase.
        private void HandleStartTimerSignal(SessionPhase targetPhase)
        {
            if (!_breakSignalReceived)
            {
                // Defensive: if break was never signalled, gray room / teleport may not have
                // happened yet. Log a warning but proceed — the researcher knows the state.
                Debug.LogWarning($"[SessionManager] start-timer signal received before break signal. " +
                                 "Gray room activation and teleport may be missing. Proceeding anyway.");
            }

            // If all combinations were completed in the previous exploration phase,
            // skip Neutral and go directly to SessionComplete.
            if (_pendingSessionComplete)
            {
                _pendingSessionComplete = false;
                Debug.Log("[SessionManager] Session complete flag detected — transitioning to SESSION_COMPLETE");
                TransitionTo(SessionPhase.SessionComplete);
                return;
            }

            TransitionTo(targetPhase);
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

        // Runs a fade-in -> TransitionTo -> fade-out sequence.
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

                    // Waiting phases have no fixed-duration timer — REST API signal drives the next transition.
                    // Idle has no timer — server poll drives the next transition.
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