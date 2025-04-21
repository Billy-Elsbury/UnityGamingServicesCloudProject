using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

// Ensure Initialiser runs first
[RequireComponent(typeof(UGSInitializer))]
public class UGSAuthentication : MonoBehaviour
{
    public TMP_Text statusText; 
    public Button signInButton; 

    void Start()
    {
        if (signInButton != null)
        {
            signInButton.onClick.AddListener(SignInAnonymously);
            signInButton.interactable = false;
        }
        UpdateStatus("Waiting for UGS Initialization...");
        
        StartCoroutine(WaitForInitialization());
    }

    private System.Collections.IEnumerator WaitForInitialization()
    {
        while (!UGSInitializer.IsInitialized)
        {
            yield return null;
        }
        if (signInButton != null) signInButton.interactable = true;
        UpdateStatus("UGS Initialized. Ready to Sign In.");
    }


    public async void SignInAnonymously()
    {
        if (!UGSInitializer.IsInitialized)
        {
            UpdateStatus("Error: UGS not initialized yet.");
            return;
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus($"Already signed in as: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Already signed in as: {AuthenticationService.Instance.PlayerId}");
            return;
        }

        UpdateStatus("Signing in anonymously...");
        signInButton.interactable = false;

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            //Check after await
            if (AuthenticationService.Instance.IsSignedIn)
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                UpdateStatus($"Sign-in successful! Player ID: {playerId}");
                Debug.Log($"Player signed in anonymously. Player ID: {playerId}");
                 
                signInButton.interactable = true; 
            }
            else
            {
                //case if the await completes but sign in state isnt set
                UpdateStatus("Sign-in attempt completed, but not signed in.");
                Debug.LogWarning("Sign-in attempt completed, but IsSignedIn is false.");
                if (signInButton != null) signInButton.interactable = true;
            }
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            UpdateStatus($"Sign-in failed: {ex.Message} (Code: {ex.ErrorCode})");
            Debug.LogError($"AuthenticationException: {ex}");
            if (signInButton != null) signInButton.interactable = true;
        }
        catch (RequestFailedException ex)
        {
            UpdateStatus($"Sign-in request failed: {ex.Message} (Code: {ex.ErrorCode})");
            Debug.LogError($"RequestFailedException: {ex}");
            if (signInButton != null) signInButton.interactable = true;
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Generic Sign-in Exception: {ex.Message}");
            Debug.LogError($"Generic Sign-in Exception: {ex}");
            if (signInButton != null) signInButton.interactable = true;
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Status: {message}");
    }
}