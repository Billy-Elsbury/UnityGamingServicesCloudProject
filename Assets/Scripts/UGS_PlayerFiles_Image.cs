using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UGS_PlayerFiles_Image : MonoBehaviour
{
    [Header("Source Asset (for Upload)")]
    [Tooltip("Assign the Texture2D asset from your Project folder ONLY for the initial upload.")]
    public Texture2D sourceImageAsset; // Assign for UPLOAD

    [Header("Cloud File Management")]
    [Tooltip("The exact filename (key) to use in Cloud Save Player Files. MUST match the intended file.")]
    public string cloudSaveKey = "my_uploaded_image.png"; // *** SET THIS MANUALLY TO THE FILENAME YOU WANT TO MANAGE ***

    [Tooltip("Relative path within Assets folder to save the downloaded file (e.g., Textures/Downloaded). MUST match original location for RawImage re-linking.")]
    public string savePathInAssets = "Textures/Downloaded"; // *** SET YOUR DESIRED SAVE FOLDER ***

    [Header("UI Elements")]
    public Button uploadButton;
    public Button downloadButton;
    public Button listFilesButton; // Note: Will only list metadata for the specific cloudSaveKey
    public Button deleteButton;
    public TMP_Text statusText;
    // Removed RawImage reference - relying on AssetDatabase refresh

    void Start()
    {
        if (uploadButton) uploadButton.onClick.AddListener(UploadImageFile);
        if (downloadButton) downloadButton.onClick.AddListener(DownloadAndSaveToAssets);
        if (listFilesButton) listFilesButton.onClick.AddListener(ListSpecificFileMetadata); // Renamed listener
        if (deleteButton) deleteButton.onClick.AddListener(DeleteImageFile);

        // Validate essential configuration
        if (string.IsNullOrEmpty(cloudSaveKey))
        {
            UpdateStatus("Error: 'Cloud Save Key' must be set in the Inspector.");
            SetButtonsInteractable(false); // Disable all buttons
        }
        else if (sourceImageAsset == null)
        {
            UpdateStatus("Warning: 'Source Image Asset' not assigned. Upload will be disabled.");
            if (uploadButton) uploadButton.interactable = false;
        }
        else
        {
            // Optionally derive key from source asset ONLY if key isn't manually set
            // if (string.IsNullOrEmpty(cloudSaveKey)) {
            //    _cloudSaveKey = Path.ChangeExtension(sourceImageAsset.name, ".png");
            // }
            UpdateStatus($"Ready. Managing file '{cloudSaveKey}' in Cloud Save Player Files.");
        }
    }

    // --- Upload Image File ---
    public async void UploadImageFile()
    {
        // Check sign-in, source asset, and key
        if (!EnsurePrerequisites(requireSourceAsset: true, requireKey: true)) return;

        UpdateStatus($"Reading and encoding image '{sourceImageAsset.name}'...");
        byte[] imageBytes;
        try
        {
            if (!sourceImageAsset.isReadable)
            {
                UpdateStatus($"Error: Texture '{sourceImageAsset.name}' is not readable. Enable Read/Write in Import Settings.");
                return;
            }
            imageBytes = sourceImageAsset.EncodeToPNG(); // Encode to PNG
            if (imageBytes == null || imageBytes.Length == 0)
            {
                UpdateStatus("Error: Failed to encode image to bytes.");
                return;
            }
        }
        catch (Exception e) { UpdateStatus($"Error encoding image: {e.Message}"); Debug.LogError(e); return; }

        UpdateStatus($"Uploading '{cloudSaveKey}' ({imageBytes.Length} bytes) to Player Files...");
        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync(cloudSaveKey, imageBytes);
            UpdateStatus($"File '{cloudSaveKey}' uploaded successfully!");
        }
        catch (Exception e) { HandleException("Upload", e); }
    }

    // --- Download Image File (Saves to Assets path) ---
    public async void DownloadAndSaveToAssets()
    {
        // Only need sign-in and key for download
        if (!EnsurePrerequisites(requireSourceAsset: false, requireKey: true)) return;
        if (string.IsNullOrEmpty(savePathInAssets))
        {
            UpdateStatus("Error: 'Save Path In Assets' cannot be empty.");
            return;
        }

        string filename = Path.GetFileName(cloudSaveKey);
        string fullLocalDirPath = Path.Combine(Application.dataPath, savePathInAssets);
        string fullLocalPath = Path.Combine(fullLocalDirPath, filename);

        UpdateStatus($"Attempting to download '{cloudSaveKey}' to '{fullLocalPath}'...");
        try
        {
            byte[] fileBytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(cloudSaveKey);

            if (fileBytes != null && fileBytes.Length > 0)
            {
                UpdateStatus("Download complete. Saving file locally into Assets...");
                try
                {
                    if (!Directory.Exists(fullLocalDirPath))
                    {
                        Directory.CreateDirectory(fullLocalDirPath);
                    }
                    File.WriteAllBytes(fullLocalPath, fileBytes);
                    UpdateStatus($"File saved to: {fullLocalPath}. Requesting AssetDB refresh...");
                    Debug.Log($"File saved to: {fullLocalPath}");

#if UNITY_EDITOR
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // Force update might help re-linking
                    Debug.Log("AssetDatabase refresh requested.");
#endif
                }
                catch (Exception e) { UpdateStatus($"Error saving downloaded file: {e.Message}"); Debug.LogError(e); }
            }
            else { UpdateStatus($"Downloaded data for '{cloudSaveKey}' was null or empty."); }
        }
        catch (Exception e) { HandleException("Download", e); }
    }

    // --- List Metadata for Specific File ---
    public async void ListSpecificFileMetadata() // Renamed
    {
        // Only need sign-in and key
        if (!EnsurePrerequisites(requireSourceAsset: false, requireKey: true)) return;

        UpdateStatus($"Getting metadata for specific file '{cloudSaveKey}'...");
        try
        {
            FileItem metadata = await CloudSaveService.Instance.Files.Player.GetMetadataAsync(cloudSaveKey);
            if (metadata != null)
            {
                string fileInfoText = $"Metadata for '{metadata.Key}':\n" +
                                      $"- Size: {metadata.Size} bytes\n" +
                                      $"- Modified: {metadata.Modified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}"; // Simplified output
                UpdateStatus(fileInfoText);
            }
            else { UpdateStatus($"Metadata retrieved for '{cloudSaveKey}', but it was null."); }
        }
        catch (Exception e) { HandleException("List/GetMetadata", e); }
    }


    // --- Delete Image File ---
    public async void DeleteImageFile()
    {
        // Only need sign-in and key
        if (!EnsurePrerequisites(requireSourceAsset: false, requireKey: true)) return;

        UpdateStatus($"Deleting '{cloudSaveKey}' from Player Files...");
        try
        {
            await CloudSaveService.Instance.Files.Player.DeleteAsync(cloudSaveKey);
            UpdateStatus($"File '{cloudSaveKey}' delete request sent successfully!");
            // Note: Deletion might take a moment on the backend. Listing immediately might still show it.
        }
        catch (Exception e) { HandleException("Delete", e); }
    }

    // --- Helpers ---
    private bool EnsureSignedIn()
    {
        if (!UGSInitializer.IsInitialized)
        {
            UpdateStatus("Error: UGS not initialized.");
            return false;
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in first.");
            return false;
        }
        return true;
    }

    // Updated prerequisite check
    private bool EnsurePrerequisites(bool requireSourceAsset, bool requireKey)
    {
        if (!EnsureSignedIn()) return false;

        if (requireKey && string.IsNullOrEmpty(cloudSaveKey))
        {
            UpdateStatus("Error: 'Cloud Save Key' must be set in the Inspector.");
            return false;
        }
        if (requireSourceAsset && sourceImageAsset == null)
        {
            UpdateStatus("Error: 'Source Image Asset' must be assigned for this operation.");
            return false;
        }
        return true;
    }

    // Centralized exception handling
    private void HandleException(string operation, Exception e)
    {
        string baseMessage = $"Error during {operation}";
        if (e is CloudSaveException csEx)
        {
            if (csEx.Reason == CloudSaveExceptionReason.NotFound)
            {
                UpdateStatus($"{baseMessage}: File '{cloudSaveKey}' not found.");
            }
            else
            {
                UpdateStatus($"{baseMessage}: {csEx.Message} (Reason: {csEx.Reason})");
            }
        }
        else
        {
            UpdateStatus($"{baseMessage}: {e.Message}");
        }
        Debug.LogError($"{baseMessage}: {e}");
    }


    private void UpdateStatus(string message)
    {
        if (statusText != null) { statusText.text = "\n" + message; }
        Debug.Log($"Status File: {message}");
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        if (uploadButton) uploadButton.interactable = isInteractable;
        if (downloadButton) downloadButton.interactable = isInteractable;
        if (listFilesButton) listFilesButton.interactable = isInteractable;
        if (deleteButton) deleteButton.interactable = isInteractable;
    }
}