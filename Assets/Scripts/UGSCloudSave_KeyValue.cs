using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models; // Needed for Item
using System.Threading.Tasks;

public class UGSCloudSave_KeyValue : MonoBehaviour
{
    public TMP_InputField playerNameInput;
    public TMP_InputField aliasInput;     
    public Button saveButton;              
    public Button loadButton;             
    public TMP_Text statusText;            

    void Start()
    {
        if (saveButton != null) saveButton.onClick.AddListener(SavePlayerData);
        if (loadButton != null) loadButton.onClick.AddListener(LoadPlayerData);
        // Optionally load existing data on start after sign-in
        // StartCoroutine(LoadDataAfterSignIn()); 
    }

    // Optional: Load data automatically after sign-in
    private IEnumerator<YieldInstruction> LoadDataAfterSignIn()
    {
        // Wait until signed in
        while (!AuthenticationService.Instance.IsSignedIn)
        {
            yield return null;
        }
        LoadPlayerData(); // Call load once signed in
    }

    public async void SavePlayerData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in to save data.");
            return;
        }

        string playerNameValue = playerNameInput.text;
        string aliasValue = aliasInput.text;

        var data = new Dictionary<string, object>
        {
            { "playerName", playerNameValue },
            { "alias", aliasValue }
        };

        UpdateStatus("Saving player data to Cloud Save...");
        try
        {
            // Save player-specific data
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            UpdateStatus("Player data saved successfully!");
            Debug.Log("Player data saved successfully!");
        }
        catch (CloudSaveValidationException e)
        {
            UpdateStatus($"Cloud Save Validation Error: {e.Message}");
            Debug.LogError($"CloudSaveValidationException: {e}");
        }
        catch (CloudSaveException e)
        {
            UpdateStatus($"Cloud Save Error: {e.Message} (Reason: {e.Reason})");
            Debug.LogError($"CloudSaveException: {e}");
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Generic Save Exception: {e.Message}");
            Debug.LogError($"Generic Save Exception: {e}");
        }
    }

    public async void LoadPlayerData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in to load data.");
            return;
        }

        UpdateStatus("Loading player data from Cloud Save...");
        try
        {
            // Specify the keys you want to load
            var keysToLoad = new HashSet<string> { "playerName", "alias" };

            // Load player-specific data for the specified keys
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keysToLoad);

            string loadedName = "DefaultName"; // Default if not found
            string loadedAlias = "DefaultAlias"; // Default if not found

            if (results.TryGetValue("playerName", out var nameItem))
            {
                loadedName = nameItem.Value.GetAs<string>();
            }
            if (results.TryGetValue("alias", out var aliasItem))
            {
                loadedAlias = aliasItem.Value.GetAs<string>();
            }

            playerNameInput.text = loadedName;
            aliasInput.text = loadedAlias;

            UpdateStatus("Player data loaded successfully!");
            Debug.Log($"Player data loaded: Name='{loadedName}', Alias='{loadedAlias}'");

        }
        catch (CloudSaveValidationException e)
        {
            UpdateStatus($"Cloud Save Validation Error: {e.Message}");
            Debug.LogError($"CloudSaveValidationException: {e}");
        }
        catch (CloudSaveException e)
        {
            UpdateStatus($"Cloud Save Error: {e.Message} (Reason: {e.Reason})");
            Debug.LogError($"CloudSaveException: {e}");
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Generic Load Exception: {e.Message}");
            Debug.LogError($"Generic Load Exception: {e}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Status KV: {message}");
    }
}