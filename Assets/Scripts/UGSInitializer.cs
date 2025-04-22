using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; } = false;

    async void Awake()
    {
        if (IsInitialized) return;

        try
        {
            Debug.Log("Initialising Unity Services...");
            await UnityServices.InitializeAsync();

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                IsInitialized = true;
                Debug.Log("Unity Services Initialised Successfully.");
            }
            else
            {
                Debug.LogError($"Failed to initialise Unity Services. State: {UnityServices.State}");
            }
        }
        catch (ServicesInitializationException e)
        {
            Debug.LogError($"Unity Services Initialisation Exception: {e}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Generic Exception during Unity Services Initialisation: {e}");
        }
    }
}