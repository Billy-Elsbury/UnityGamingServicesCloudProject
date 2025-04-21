using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO; // Required for File and Path operations
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models; // Needed for FileItem
#if UNITY_EDITOR // AssetDatabase is editor-only
using UnityEditor;
#endif

// Ensure Auth script runs first if needed, or rely on Initializer
// [RequireComponent(typeof(UGSAuthentication))] 
public class UGS_PlayerFiles_Image : MonoBehaviour
{
    [Header("Source Asset")]
    [Tooltip("Assign the Texture2D asset from your Project folder to upload.")]
    public Texture2D sourceImageAsset; // Assign in Inspector

    [Header("Download/Display")]
    [Tooltip("Assign a UI RawImage component to display the downloaded image.")]
    public RawImage displayImage; // Assign a UI RawImage component here
    [Tooltip("Filename to use when downloading the file (within PersistentDataPath).")]
    public string downloadFilename = "downloaded_image.png"; // Filename for saving download

    [Header("UI Elements")]
    public Button uploadButton;
    public Button downloadButton;
    public Button listFilesButton;
    public Button deleteButton;
    public TMP_Text statusText; // Can reuse status text

    private string _cloudSaveKey = null; // Will be derived from sourceImageAsset name

    void Start()
    {
        if (uploadButton) uploadButton.onClick.AddListener(UploadImageFile);
        if (downloadButton) downloadButton.onClick.AddListener(DownloadImageFile);
        if (listFilesButton) listFilesButton.onClick.AddListener(ListPlayerFiles);
        if (deleteButton) deleteButton.onClick.AddListener(DeleteImageFile);

        // Derive the key from the assigned texture name
        if (sourceImageAsset != null)
        {
            _cloudSaveKey = sourceImageAsset.name + ".png"; // Assuming PNG, adjust if needed
            UpdateStatus($"Ready. Will manage file '{_cloudSaveKey}' in Cloud Save Player Files.");
        }
        else
        {
            UpdateStatus("Ready. Assign 'Source Image Asset' in Inspector to enable upload/download/delete.");
            // Disable buttons if no source asset is assigned
            if (uploadButton) uploadButton.interactable = false;
            if (downloadButton) downloadButton.interactable = false;
            if (deleteButton) deleteButton.interactable = false;
        }

        if (displayImage) displayImage.texture = null; // Clear display initially
    }

    // --- Upload Image File ---
    public async void UploadImageFile()
    {
        if (!EnsurePrerequisites(true)) return; // Check sign-in and source asset

        UpdateStatus($"Reading and encoding image '{sourceImageAsset.name}'...");
        byte[] imageBytes;
        try
        {
            // Ensure texture is readable (Requires Read/Write Enabled in import settings)
            if (!sourceImageAsset.isReadable)
            {
                UpdateStatus($"Error: Texture '{sourceImageAsset.name}' is not readable. Enable Read/Write in Import Settings.");
                Debug.LogError($"Texture '{sourceImageAsset.name}' is not readable.");
                return;
            }
            imageBytes = sourceImageAsset.EncodeToPNG(); // Or EncodeToJPG()
            if (imageBytes == null || imageBytes.Length == 0)
            {
                UpdateStatus("Error: Failed to encode image to bytes.");
                return;
            }
        }
        catch (Exception e)
        {
            UpdateStatus($"Error encoding image: {e.Message}");
            Debug.LogError($"Error encoding image: {e}");
            return;
        }

        UpdateStatus($"Uploading '{_cloudSaveKey}' ({imageBytes.Length} bytes) to Player Files...");
        try
        {
            // Use the Files API to save the byte array
            await CloudSaveService.Instance.Files.Player.SaveAsync(_cloudSaveKey, imageBytes);
            UpdateStatus($"File '{_cloudSaveKey}' uploaded successfully!");
            Debug.Log($"File '{_cloudSaveKey}' uploaded successfully!");
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Upload Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e) { UpdateStatus($"Upload Error: {e.Message} (Reason: {e.Reason})"); Debug.LogError(e); }
        catch (System.Exception e) { UpdateStatus($"Generic Upload Error: {e.Message}"); Debug.LogError(e); }
    }

    // --- Download Image File ---
    public async void DownloadImageFile()
    {
        if (!EnsurePrerequisites(true)) return; // Check sign-in and source asset (to know the key)

        string localDownloadPath = Path.Combine(Application.persistentDataPath, downloadFilename);

        UpdateStatus($"Downloading '{_cloudSaveKey}' from Player Files to {localDownloadPath}...");
        try
        {
            // Load the raw bytes from Player Files
            byte[] fileBytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(_cloudSaveKey);

            if (fileBytes != null && fileBytes.Length > 0)
            {
                UpdateStatus("Download complete. Saving file locally...");
                try
                {
                    // Ensure directory exists
                    string directoryPath = Path.GetDirectoryName(localDownloadPath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Write bytes to the local file
                    File.WriteAllBytes(localDownloadPath, fileBytes);
                    UpdateStatus($"File saved to: {localDownloadPath}. Loading texture...");
                    Debug.Log($"File saved to: {localDownloadPath}");

                    // --- Load into Texture for Display ---
                    Texture2D loadedTexture = new Texture2D(2, 2); // Temp size
                    if (loadedTexture.LoadImage(fileBytes)) // LoadImage works with PNG/JPG bytes
                    {
                        if (displayImage != null)
                        {
                            displayImage.texture = loadedTexture;
                            displayImage.color = Color.white;
                            UpdateStatus($"Image '{_cloudSaveKey}' downloaded and displayed.");
                        }
                        else
                        {
                            UpdateStatus($"Image '{_cloudSaveKey}' downloaded to {localDownloadPath}, but no display target assigned.");
                        }

                        // --- Refresh Asset Database (Editor Only) ---
                        // If the download path was inside Assets/, this makes it show up
#if UNITY_EDITOR
                        if (localDownloadPath.StartsWith(Application.dataPath))
                        {
                            Debug.Log("Requesting AssetDatabase refresh...");
                            AssetDatabase.Refresh();
                        }
#endif
                        // --- End Refresh ---
                    }
                    else
                    {
                        UpdateStatus($"Downloaded data for '{_cloudSaveKey}', but failed to load into Texture2D.");
                        Debug.LogError($"Texture2D.LoadImage failed for key '{_cloudSaveKey}'.");
                    }
                    // --- End Texture Load ---
                }
                catch (Exception e)
                {
                    UpdateStatus($"Error saving downloaded file: {e.Message}");
                    Debug.LogError($"Error saving downloaded file: {e}");
                }
            }
            else
            {
                UpdateStatus($"Downloaded data for '{_cloudSaveKey}' was null or empty.");
                Debug.LogWarning($"Downloaded data for '{_cloudSaveKey}' was null or empty.");
            }
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Download Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e)
        {
            if (e.Reason == CloudSaveExceptionReason.NotFound)
            {
                UpdateStatus($"File '{_cloudSaveKey}' not found in Player Files.");
            }
            else
            {
                UpdateStatus($"Download Error: {e.Message} (Reason: {e.Reason})");
            }
            Debug.LogError(e);
        }
        catch (System.Exception e) { UpdateStatus($"Generic Download Error: {e.Message}"); Debug.LogError(e); }
    }

    // --- List Player Files ---
    public async void ListPlayerFiles()
    {
        if (!EnsureSignedIn()) return;

        UpdateStatus("Listing files from Player Files...");
        try
        {
            List<FileItem> fileList = await CloudSaveService.Instance.Files.Player.GetMetadataAsync();

            string fileListText = $"Player Files ({fileList.Count}):\n";
            if (fileList.Count > 0)
            {
                foreach (var item in fileList)
                {
                    fileListText += $"- {item.Key} ({item.Size} bytes, Modified: {item.Modified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"})\n";
                }
            }
            else
            {
                fileListText += "(No files found)";
            }
            UpdateStatus(fileListText);
            Debug.Log(fileListText.Replace("\n", System.Environment.NewLine)); // Log with proper newlines
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"List Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e) { UpdateStatus($"List Error: {e.Message} (Reason: {e.Reason})"); Debug.LogError(e); }
        catch (System.Exception e) { UpdateStatus($"Generic List Error: {e.Message}"); Debug.LogError(e); }
    }


    // --- Delete Image File ---
    public async void DeleteImageFile()
    {
        if (!EnsurePrerequisites(true)) return; // Check sign-in and source asset (to know the key)

        UpdateStatus($"Deleting '{_cloudSaveKey}' from Player Files...");
        try
        {
            await CloudSaveService.Instance.Files.Player.DeleteAsync(_cloudSaveKey);
            UpdateStatus($"File '{_cloudSaveKey}' deleted successfully!");
            Debug.Log($"File '{_cloudSaveKey}' deleted successfully!");
            if (displayImage != null) displayImage.texture = null; // Clear display
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Delete Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e)
        {
            if (e.Reason == CloudSaveExceptionReason.NotFound)
            {
                UpdateStatus($"File '{_cloudSaveKey}' not found, cannot delete.");
            }
            else
            {
                UpdateStatus($"Delete Error: {e.Message} (Reason: {e.Reason})");
            }
            Debug.LogError(e);
        }
        catch (System.Exception e) { UpdateStatus($"Generic Delete Error: {e.Message}"); Debug.LogError(e); }
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
    private bool EnsurePrerequisites(bool requireSourceAsset)
    {
        if (!EnsureSignedIn()) return false;

        if (requireSourceAsset && sourceImageAsset == null)
        {
            UpdateStatus("Error: 'Source Image Asset' must be assigned in the Inspector.");
            return false;
        }
        if (requireSourceAsset && string.IsNullOrEmpty(_cloudSaveKey))
        {
            UpdateStatus("Error: Could not determine Cloud Save Key from Source Image Asset name.");
            return false;
        }
        return true;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            // Append for log-like behavior
            statusText.text += "\n" + message;
            // Optional: Auto-scroll logic for TMP_Text if needed
        }
        Debug.Log($"Status Img: {message}");
    }
}