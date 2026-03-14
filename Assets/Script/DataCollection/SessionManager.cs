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
    //   TRAINING
    //     -> [headset removed] -> WAITING_FOR_BASELINE
    //   WAITING_FOR_BASELINE
    //     -> [headset put on] -> BASELINE (3 min fixed timer)
    //   BASELINE
    //     -> [timer ends] -> HOUSE_EXPLORATION
    //   HOUSE_EXPLORATION
    //     -> [2 min timer ends] -> POST combination-done to server
    //                           -> GET next combination from server
    //                           -> if all done: SESSION_COMPLETE
    //                           -> else: STANDBY
    //   STANDBY  (respondent has removed headset, filling questionnaire)
    //     -> [headset put on] -> NEUTRAL (30 sec fixed timer)
    //   NEUTRAL
    //     -> [timer ends] -> HOUSE_EXPLORATION (next combination)
    //   SESSION_COMPLETE
    //     -> teleport to training area, return to IDLE to await next respondent
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

        // -- Inspector: Layout registry (assign prefabs from Assets/Prefab/HomeDesignPrefab/Type36/)
        [Header("Layout Registry")]
        [SerializeField] private List<LayoutEntry> _layoutRegistry;

        // -- Inspector: All 12 MaterialCombination ScriptableObjects (C01-C12)
        [Header("Material Combinations")]
        [SerializeField] private List<MaterialCombination> _materialCombinations;

        // -- Inspector: Spawn points for each phase area
        [Header("Spawn Points")]
        [SerializeField] private Transform _trainingSpawnPoint;
        [SerializeField] private Transform _baselineSpawnPoint;
        [SerializeField] private Transform _neutralSpawnPoint;
        [SerializeField] private Transform _houseSpawnPoint;

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
        [SerializeField] private GameObject _removeHeadsetUI;
        // Black room geometry is only active during Baseline — kept inactive the rest of the session
        // to avoid visual overlap with the house or training area.
        [SerializeField] private GameObject _blackRoom;
        // The Move GameObject under XR Origin (XR Rig)/Locomotion System/Move.
        // Disabled during WaitingForBaseline so the respondent cannot walk while the UI is shown.
        // Re-enabled when HouseExploration begins.
        [SerializeField] private GameObject _moveProvider;

        // -- Runtime state
        private SessionPhase _phase = SessionPhase.Idle;
        private RespondentApiResponse _activeRespondent;
        private GameObject _houseInstance;
        private Coroutine _activeTimer;
        private Coroutine _idlePollCoroutine;

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

            TransitionTo(SessionPhase.WaitingForBaseline);
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

                case SessionPhase.Standby:
                    TransitionTo(SessionPhase.Neutral);
                    break;
            }
        }

        private void HandleHeadsetRemoved()
        {
            switch (_phase)
            {
                case SessionPhase.WaitingForBaseline:
                    // Headset is off — safe window to swap scene state invisibly.
                    // Activate black room and deactivate training area before the respondent
                    // puts the headset back on so the transition is seamless.
                    if (_blackRoom != null)
                        _blackRoom.SetActive(true);

                    if (_trainingArea != null)
                        _trainingArea.SetActive(false);

                    // Teleport to baseline spawn point while headset is off so the
                    // respondent is already inside the black room when they put it back on.
                    TeleportTo(_baselineSpawnPoint);

                    Debug.Log("[SessionManager] Headset removed in WAITING_FOR_BASELINE — " +
                              "scene swapped to black room, awaiting headset put-on to start baseline timer");
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

        private void TransitionTo(SessionPhase newPhase)
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
                case SessionPhase.Standby: OnEnterStandby(); break;
                case SessionPhase.Neutral: OnEnterNeutral(); break;
                case SessionPhase.SessionComplete: OnEnterSessionComplete(); break;
            }
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
            TeleportTo(_trainingSpawnPoint);
        }

        private void OnEnterWaitingForBaseline()
        {
            Debug.Log("[SessionManager] Phase: WAITING_FOR_BASELINE — respondent has exited training area, " +
                      "waiting for headset removal before baseline begins");

            // Prevent the respondent from walking around while the removal UI is shown
            if (_moveProvider != null)
                _moveProvider.SetActive(false);

            // Prompt respondent to remove the headset so they can fill the pre-study form
            if (_removeHeadsetUI != null)
                _removeHeadsetUI.SetActive(true);

            // TrainingArea and BlackRoom are toggled in HandleHeadsetRemoved, not here,
            // because the respondent is still wearing the headset at this point.
        }

        private void OnEnterBaseline()
        {
            Debug.Log($"[SessionManager] Phase: BASELINE — fixed {BaselineDurationSeconds}s timer started");

            // Respondent has put the headset back on — dismiss the removal prompt.
            // BlackRoom was already activated and teleport already happened in HandleHeadsetRemoved
            // while the headset was off, so the scene transition is invisible to the respondent.
            if (_removeHeadsetUI != null)
                _removeHeadsetUI.SetActive(false);

            SetSkybox(_neutralSkyboxMaterial);
            _activeTimer = StartCoroutine(RunTimer(BaselineDurationSeconds, () =>
            {
                TransitionTo(SessionPhase.HouseExploration);
            }));
        }

        private void OnEnterHouseExploration()
        {
            if (_activeRespondent == null || string.IsNullOrEmpty(_activeRespondent.combinationId))
            {
                Debug.LogError("[SessionManager] Cannot enter HouseExploration — no combination data available");
                return;
            }

            // Black room is only needed during Baseline — dismiss it before showing the house.
            if (_blackRoom != null)
                _blackRoom.SetActive(false);

            // House instance is instantiated inactive during IDLE so it does not appear
            // before the session starts. Activate it now that the respondent is ready.
            if (_houseInstance != null)
                _houseInstance.SetActive(true);

            // Respondent needs to walk freely inside the house
            if (_moveProvider != null)
                _moveProvider.SetActive(true);

            ApplyCombinationToHouse(_activeRespondent.combinationId);
            SetSkybox(_defaultSkyboxMaterial);
            TeleportTo(_houseSpawnPoint);

            Debug.Log($"[SessionManager] Phase: HOUSE_EXPLORATION — combination {_activeRespondent.combinationId}, " +
                      $"index {_activeRespondent.nextCombinationIndex} — {ExplorationDurationSeconds}s timer started");

            _activeTimer = StartCoroutine(RunTimer(ExplorationDurationSeconds, OnExplorationTimerEnded));
        }

        private void OnEnterStandby()
        {
            Debug.Log("[SessionManager] Phase: STANDBY — respondent filling questionnaire, awaiting headset put-on");

            // Hide the house so the respondent does not see it when they put the headset back on
            // before the neutral timer has run. It will be re-activated in OnEnterHouseExploration.
            if (_houseInstance != null)
                _houseInstance.SetActive(false);

            // Disable locomotion — respondent is not supposed to walk during standby
            if (_moveProvider != null)
                _moveProvider.SetActive(false);

            // Prompt respondent to remove the headset to fill the per-combination questionnaire
            if (_removeHeadsetUI != null)
                _removeHeadsetUI.SetActive(true);

            // No timer — presence sensor drives the next transition
        }

        private void OnEnterNeutral()
        {
            Debug.Log($"[SessionManager] Phase: NEUTRAL — {NeutralDurationSeconds}s timer started");

            // Respondent has put the headset back on — dismiss the removal prompt
            if (_removeHeadsetUI != null)
                _removeHeadsetUI.SetActive(false);

            SetSkybox(_neutralSkyboxMaterial);
            TeleportTo(_neutralSpawnPoint);
            _activeTimer = StartCoroutine(RunTimer(NeutralDurationSeconds, () =>
            {
                TransitionTo(SessionPhase.HouseExploration);
            }));
        }

        private void OnEnterSessionComplete()
        {
            Debug.Log($"[SessionManager] Phase: SESSION_COMPLETE — {_activeRespondent?.respondentId ?? "unknown"} " +
                      "has viewed all combinations. Returning to idle.");
            SetSkybox(_defaultSkyboxMaterial);
            TeleportTo(_trainingSpawnPoint);

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
                        TransitionTo(SessionPhase.Standby);
                    }
                },
                onError: err =>
                {
                    // Cannot determine if there are more combinations — fail safe to Standby
                    // and let the next GET on the next session attempt resolve it.
                    Debug.LogWarning($"[SessionManager] Could not refresh respondent state after exploration: {err}. " +
                                     "Defaulting to STANDBY. If this was the last combination, " +
                                     "the next session start will detect isComplete.");
                    TransitionTo(SessionPhase.Standby);
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

        private void TeleportTo(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[SessionManager] Teleport target is null — skipping");
                return;
            }

            if (_xrRigRoot == null)
            {
                Debug.LogWarning("[SessionManager] XR Rig Root is not assigned — cannot teleport");
                return;
            }

            // Direct position assignment works correctly for standing VR experiences
            // where the rig origin represents the player's floor-level position.
            // _xrRigRoot must be assigned to XR Origin (XR Rig), not XR Interaction Setup.
            _xrRigRoot.position = target.position;
            _xrRigRoot.rotation = target.rotation;
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
    }

    public enum SessionPhase
    {
        Idle,
        Training,
        WaitingForBaseline,
        Baseline,
        HouseExploration,
        Standby,
        Neutral,
        SessionComplete
    }
}