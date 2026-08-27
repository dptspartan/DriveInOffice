using UnityEngine;

public enum SplitScreenLayout
{
    Horizontal = 0,
    Vertical = 1
}

[DisallowMultipleComponent]
public class GameDriveSetup : MonoBehaviour
{
    [Header("Split Screen")]
    public bool splitScreenEnabled;

    public SplitScreenLayout splitLayout = SplitScreenLayout.Horizontal;

    [Header("Cars")]
    public GameObject playerOneCarPrefab;
    public GameObject playerTwoCarPrefab;
    public KenneyCarController existingPlayerOneCar;
    public Transform playerOneSpawn;
    public Transform playerTwoSpawn;
    public Vector3 playerTwoSpawnOffset = new Vector3(6f, 0f, 0f);

    [Header("Controls")]
    public CarControlScheme playerOneControl = CarControlScheme.KeyboardWASD;
    public CarControlScheme playerTwoControl = CarControlScheme.KeyboardArrows;

    [Header("Environment")]
    public bool disableLightPoleCollidersOnStart = true;

    private KenneyCarController playerOneCar;
    private KenneyCarController playerTwoCar;
    private GameObject playerTwoInstance;

    private void Start()
    {
        if (disableLightPoleCollidersOnStart)
            DisableLightPoleColliders();

        ApplySetup();
    }

    public void SetSplitScreenEnabled(bool enabled)
    {
        splitScreenEnabled = enabled;
        ApplySetup();
    }

    public void ApplySetup()
    {
        EnsurePlayerOne();
        if (splitScreenEnabled)
            EnsurePlayerTwo();
        else
            HidePlayerTwo();

        ConfigureCameras();
    }

    public void SetPlayerOneControl(CarControlScheme scheme)
    {
        playerOneControl = scheme;
        if (playerOneCar != null)
            WireInput(playerOneCar, playerOneControl);
    }

    public void SetPlayerTwoControl(CarControlScheme scheme)
    {
        playerTwoControl = scheme;
        if (playerTwoCar != null)
            WireInput(playerTwoCar, playerTwoControl);
    }

    private void EnsurePlayerOne()
    {
        if (existingPlayerOneCar != null)
        {
            playerOneCar = existingPlayerOneCar;
        }
        else if (playerOneCar == null)
        {
            playerOneCar = FindAnyObjectByType<KenneyCarController>();
        }

        if (playerOneCar == null && playerOneCarPrefab != null)
        {
            Vector3 spawn = playerOneSpawn != null ? playerOneSpawn.position : Vector3.zero;
            Quaternion rotation = playerOneSpawn != null ? playerOneSpawn.rotation : Quaternion.identity;
            GameObject instance = Instantiate(playerOneCarPrefab, spawn, rotation);
            playerOneCar = instance.GetComponentInChildren<KenneyCarController>(true);
        }

        if (playerOneCar != null)
            WireInput(playerOneCar, playerOneControl);
    }

    private void EnsurePlayerTwo()
    {
        if (playerTwoCar != null && playerTwoInstance != null)
        {
            playerTwoInstance.SetActive(true);
            WireInput(playerTwoCar, playerTwoControl);
            return;
        }

        GameObject prefab = playerTwoCarPrefab != null ? playerTwoCarPrefab : playerOneCarPrefab;
        if (prefab == null)
            return;

        Vector3 spawn = playerTwoSpawn != null
            ? playerTwoSpawn.position
            : GetDefaultPlayerTwoSpawn();

        Quaternion rotation = playerTwoSpawn != null
            ? playerTwoSpawn.rotation
            : playerOneCar != null ? playerOneCar.transform.rotation : Quaternion.identity;

        playerTwoInstance = Instantiate(prefab, spawn, rotation);
        playerTwoCar = playerTwoInstance.GetComponentInChildren<KenneyCarController>(true);
        if (playerTwoCar == null)
            return;

        WireInput(playerTwoCar, playerTwoControl);

        CarPhysicsTier tier = playerTwoCar.GetComponent<CarPhysicsTier>();
        if (tier != null)
            tier.SetTier(CarTier.Sport);
    }

    private Vector3 GetDefaultPlayerTwoSpawn()
    {
        if (playerOneCar != null)
            return playerOneCar.transform.position + playerTwoSpawnOffset;

        return playerTwoSpawnOffset;
    }

    private void HidePlayerTwo()
    {
        if (playerTwoInstance != null)
            playerTwoInstance.SetActive(false);
    }

    private void ConfigureCameras()
    {
        if (playerOneCar == null)
            return;

        CarFollowCamera cameraOne = playerOneCar.GetComponentInChildren<CarFollowCamera>(true);
        if (cameraOne == null)
            return;

        cameraOne.Bind(playerOneCar.transform, playerOneCar);
        cameraOne.SetPrimaryAudioListener(true);
        cameraOne.SetMainCameraTag(true);

        if (!splitScreenEnabled || playerTwoCar == null || !playerTwoCar.gameObject.activeInHierarchy)
        {
            cameraOne.SetViewportRect(new Rect(0f, 0f, 1f, 1f));
            return;
        }

        CarFollowCamera cameraTwo = playerTwoCar.GetComponentInChildren<CarFollowCamera>(true);
        if (cameraTwo == null)
            return;

        cameraTwo.Bind(playerTwoCar.transform, playerTwoCar);
        cameraTwo.SetPrimaryAudioListener(false);
        cameraTwo.SetMainCameraTag(false);

        if (splitLayout == SplitScreenLayout.Horizontal)
        {
            cameraOne.SetViewportRect(new Rect(0f, 0f, 0.5f, 1f));
            cameraTwo.SetViewportRect(new Rect(0.5f, 0f, 0.5f, 1f));
        }
        else
        {
            cameraOne.SetViewportRect(new Rect(0f, 0.5f, 1f, 0.5f));
            cameraTwo.SetViewportRect(new Rect(0f, 0f, 1f, 0.5f));
        }
    }

    private static void WireInput(KenneyCarController car, CarControlScheme scheme)
    {
        if (car == null)
            return;

        CarPlayerInput input = car.GetComponent<CarPlayerInput>();
        if (input == null)
            input = car.gameObject.AddComponent<CarPlayerInput>();

        input.controlScheme = scheme;
        car.playerInput = input;
        car.useExternalInput = true;
    }

    public static void DisableLightPoleColliders()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;

            if (IsLightPoleObject(collider.gameObject))
                collider.enabled = false;
        }
    }

    private static bool IsLightPoleObject(GameObject obj)
    {
        for (Transform node = obj.transform; node != null; node = node.parent)
        {
            string name = node.name.ToLowerInvariant();
            if (name.Contains("light-curved")
                || name.Contains("light-square")
                || name.Contains("electricity-pole")
                || name.Contains("electricity-side")
                || name.StartsWith("prop_light")
                || name.StartsWith("prop_electricity"))
            {
                return true;
            }
        }

        return false;
    }
}
