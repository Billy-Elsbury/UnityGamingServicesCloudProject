using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; } = false;

    async void Awake()
    {
        // Prevent multiple initialisations
        if (IsInitialized) return;

        try
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync(); // Initialise Core Services

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                IsInitialized = true;
                Debug.Log("Unity Services Initialized Successfully.");
                // You could potentially trigger authentication here if desired
                // await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                Debug.LogError($"Failed to initialize Unity Services. State: {UnityServices.State}");
            }
        }
        catch (ServicesInitializationException e)
        {
            Debug.LogError($"Unity Services Initialization Exception: {e}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Generic Exception during Unity Services Initialization: {e}");
        }
    }
}