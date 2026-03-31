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
            StartCoroutine(GetCoroutine($"{_serverBaseUrl}/active-respondent", onSuccess, onError));
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

        private IEnumerator GetCoroutine(string url, Action<RespondentApiResponse> onSuccess, Action<string> onError)
        {
            Debug.Log($"[ApiClient] Sending GET to: {url}");

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = _timeoutSeconds;

            yield return request.SendWebRequest();

            Debug.Log($"[ApiClient] Result: {request.result} | ResponseCode: {request.responseCode} | Error: {request.error}");

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