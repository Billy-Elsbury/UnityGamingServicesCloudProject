using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using System.Threading.Tasks;

public class UGSCloudSave_Structured : MonoBehaviour
{
    public TMP_InputField bookIdInput;     // Assign in Inspector
    public TMP_InputField bookTitleInput;  // Assign in Inspector
    public TMP_InputField bookIsbnInput;   // Assign in Inspector
    public Button saveBookButton;          // Assign in Inspector
    public Button loadBookButton;          // Assign in Inspector
    public Button deleteBookButton;        // Assign in Inspector
    public TMP_Text statusText;            // Assign in Inspector (can reuse)

    private const string BOOK_KEY_PREFIX = "book_"; // Prefix for keys

    void Start()
    {
        if (saveBookButton != null) saveBookButton.onClick.AddListener(SaveBookData);
        if (loadBookButton != null) loadBookButton.onClick.AddListener(LoadBookData);
        if (deleteBookButton != null) deleteBookButton.onClick.AddListener(DeleteBookData);
    }

    public async void SaveBookData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in to save book data.");
            return;
        }
        if (!int.TryParse(bookIdInput.text, out int bookId))
        {
            UpdateStatus("Error: Invalid Book ID format.");
            return;
        }

        // Create Book instance from UI
        BookData bookInstance = new BookData
        {
            Id = bookId,
            Title = bookTitleInput.text,
            ISBN = bookIsbnInput.text,
            BookAuthors = new List<string> { "Billy", "Darragh" } 
        };

        // Serialize to JSON
        string jsonPayload = JsonUtility.ToJson(bookInstance);
        string bookKey = BOOK_KEY_PREFIX + bookId;

        var data = new Dictionary<string, object> { { bookKey, jsonPayload } };

        UpdateStatus($"Saving book {bookKey} to Cloud Save...");
        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            UpdateStatus($"Book {bookKey} saved successfully!");
            Debug.Log($"Book {bookKey} saved successfully!");
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Save Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e) { UpdateStatus($"Save Error: {e.Message}"); Debug.LogError(e); }
        catch (System.Exception e) { UpdateStatus($"Generic Save Error: {e.Message}"); Debug.LogError(e); }
    }

    public async void LoadBookData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in to load book data.");
            return;
        }
        if (!int.TryParse(bookIdInput.text, out int bookIdToLoad))
        {
            UpdateStatus("Error: Invalid Book ID format to load.");
            return;
        }

        string bookKey = BOOK_KEY_PREFIX + bookIdToLoad;
        UpdateStatus($"Loading book {bookKey} from Cloud Save...");

        try
        {
            var keysToLoad = new HashSet<string> { bookKey };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keysToLoad);

            if (results.TryGetValue(bookKey, out var bookItem))
            {
                string jsonPayload = bookItem.Value.GetAs<string>();
                // Deserialize JSON back into BookData object
                BookData loadedBook = JsonUtility.FromJson<BookData>(jsonPayload);

                // Update UI fields
                bookIdInput.text = loadedBook.Id.ToString(); // Keep ID consistent
                bookTitleInput.text = loadedBook.Title;
                bookIsbnInput.text = loadedBook.ISBN;
                // Note: Authors list isn't displayed in this simple UI

                UpdateStatus($"Book {bookKey} loaded successfully!");
                Debug.Log($"Book loaded: {jsonPayload}");
            }
            else
            {
                UpdateStatus($"Book {bookKey} not found in Cloud Save.");
                Debug.LogWarning($"Book {bookKey} not found.");
                // Clear fields if not found
                bookTitleInput.text = "";
                bookIsbnInput.text = "";
            }
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Load Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e) { UpdateStatus($"Load Error: {e.Message}"); Debug.LogError(e); }
        catch (System.Exception e) { UpdateStatus($"Generic Load Error: {e.Message}"); Debug.LogError(e); }
    }

    public async void DeleteBookData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateStatus("Error: Must be signed in to delete book data.");
            return;
        }
        if (!int.TryParse(bookIdInput.text, out int bookIdToDelete))
        {
            UpdateStatus("Error: Invalid Book ID format to delete.");
            return;
        }

        string bookKey = BOOK_KEY_PREFIX + bookIdToDelete;
        UpdateStatus($"Deleting book {bookKey} from Cloud Save...");

        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(bookKey);
            UpdateStatus($"Book {bookKey} deleted successfully!");
            Debug.Log($"Book {bookKey} deleted successfully!");
            // Clear fields after delete
            // bookIdInput.text = ""; // Optional: clear ID field too
            bookTitleInput.text = "";
            bookIsbnInput.text = "";
        }
        catch (CloudSaveValidationException e) { UpdateStatus($"Delete Error: {e.Message}"); Debug.LogError(e); }
        catch (CloudSaveException e) { UpdateStatus($"Delete Error: {e.Message}"); Debug.LogError(e); }
        catch (System.Exception e) { UpdateStatus($"Generic Delete Error: {e.Message}"); Debug.LogError(e); }
    }


    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Status Struct: {message}");
    }
}