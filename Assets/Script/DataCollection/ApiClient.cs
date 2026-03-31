using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VRHomeArch.DataCollection
{
    // Handles all HTTP communication with the researcher's laptop server.
    // Attach to the same GameObject as SessionManager.
    // All callbacks are invoked on the main thread (Unity coroutine guarantee).
    public class ApiClient : MonoBehaviour
    {
        [SerializeField] private string _serverBaseUrl = "http://192.168.1.100:8080";
        [SerializeField] private int _timeoutSeconds = 5;

        public string ServerBaseUrl => _serverBaseUrl;

        // Fetches the current active respondent and their next combination from the server.
        // onSuccess is called with the deserialized response.
        // onError is called with a human-readable error string if the request fails.
        public void GetActiveRespondent(
            Action<RespondentApiResponse> onSuccess,
            Action<string> onError)
        {
            StartCoroutine(GetRespondentCoroutine($"{_serverBaseUrl}/active-respondent", onSuccess, onError));
        }

        // Polls for the current pending session signal from the researcher.
        // The server clears the signal after this call — each signal is consumed exactly once.
        // onSuccess is called with the signal string ("break", "start_baseline", "start_neutral")
        // or null if no signal is pending.
        public void GetSessionSignal(
            Action<string> onSuccess,
            Action<string> onError)
        {
            StartCoroutine(GetSignalCoroutine($"{_serverBaseUrl}/session-signal", onSuccess, onError));
        }

        // Records that the respondent has finished viewing a combination.
        // Called immediately when the 2-minute exploration timer ends.
        public void PostCombinationDone(
            string respondentId,
            int completedIndex,
            Action onSuccess,
            Action<string> onError)
        {
            var body = new CombinationDoneRequest
            {
                respondentId = respondentId,
                completedIndex = completedIndex
            };
            string json = JsonUtility.ToJson(body);
            StartCoroutine(PostCoroutine($"{_serverBaseUrl}/combination-done", json, onSuccess, onError));
        }

        // -----------------------------------------------------------------------
        // Coroutines
        // -----------------------------------------------------------------------

        private IEnumerator GetRespondentCoroutine(
            string url,
            Action<RespondentApiResponse> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ApiClient] GET {url}");

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET {url} failed: {request.error}");
                yield break;
            }

            RespondentApiResponse response = JsonUtility.FromJson<RespondentApiResponse>(request.downloadHandler.text);
            if (response == null)
            {
                onError?.Invoke($"GET {url} returned unparseable JSON: {request.downloadHandler.text}");
                yield break;
            }

            onSuccess?.Invoke(response);
        }

        private IEnumerator GetSignalCoroutine(
            string url,
            Action<string> onSuccess,
            Action<string> onError)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET {url} failed: {request.error}");
                yield break;
            }

            SessionSignalResponse response = JsonUtility.FromJson<SessionSignalResponse>(request.downloadHandler.text);
            if (response == null)
            {
                onError?.Invoke($"GET {url} returned unparseable JSON: {request.downloadHandler.text}");
                yield break;
            }

            // signal field is null when nothing is pending — pass null through to the caller
            onSuccess?.Invoke(response.signal);
        }

        private IEnumerator PostCoroutine(
            string url,
            string jsonBody,
            Action onSuccess,
            Action<string> onError)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"POST {url} failed: {request.error}");
                yield break;
            }

            onSuccess?.Invoke();
        }
    }
}