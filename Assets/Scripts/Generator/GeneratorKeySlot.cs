using System.Collections;
using UnityEngine;

/// Attach this to the Generator Key Slot collider in Chapter 2.
[RequireComponent(typeof(Collider))]
public class GeneratorKeySlot : MonoBehaviour
{
    private enum GasRestoreAction
    {
        KeepExistingGas,
        RemoveGas,
        RespawnGas
    }

    // Generator parts that react after the correct key is inserted.
    [Header("References")]
    public GeneratorFuelInteraction fuelInteraction;
    public Transform keyInsertPoint;
    public Light generatorLight1;
    [Tooltip("Optional second generator light. Both lights turn on and off together.")]
    public Light generatorLight2;
    [Tooltip("Add every other duplicated generator light here. All assigned lights turn on and off together.")]
    public Light[] additionalGeneratorLights;

    [Header("Lights Out Background Music")]
    [Tooltip("Your normal Chapter 2 background Audio Source. If empty, the script finds the object named 'BG music'.")]
    public AudioSource backgroundMusicSource;
    [Tooltip("The scene object name used to find the normal background Audio Source automatically.")]
    public string backgroundMusicObjectName = "BG music";
    [Tooltip("Drag the intense background music clip here. It plays after the generator lights stay off.")]
    public AudioClip lightsOutBackgroundMusic;
    [Range(0f, 1f)] public float lightsOutBackgroundMusicVolume = 0.37f;
    [Tooltip("Prevents music from switching rapidly while the generator lights flicker.")]
    [Min(0f)] public float lightsOutMusicDelay = 0.35f;

    // The selected inventory key must match Required Key Name before it can be inserted.
    [Header("Key")]
    public string requiredKeyName = "Generator key";
    public string keyInsertedSaveId = "GeneratorKeyInserted";
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRange = 2.5f;
    [Min(1f)] public float runningSeconds = 60f;
    public Vector3 turnRotation = new Vector3(0f, 90f, 0f);
    [Tooltip("Local scale after insertion. Generatorbox is scaled to 100, so 0.01 keeps the key normal-sized.")]
    public Vector3 insertedKeyLocalScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Header("Crosshair Prompts")]
    public string generatorKeyRequiredPrompt = "Generator Key required";
    public string insertKeyPrompt = "Press E to insert Generator Key";
    public string fuelRequiredPrompt = "Fuel required";
    public string startGeneratorPrompt = "Press E to turn Generator on";

    [Header("Lights Shutdown Warning")]
    [Tooltip("Shows an urgent notice shortly before the generator lights turn off.")]
    public bool showLightsShutdownWarning = true;
    [Min(0f)] public float warningSecondsBeforeShutdown = 10f;
    [Min(0.1f)] public float warningDisplayDuration = 10f;
    [TextArea(2, 4)] public string shutdownWarningMessage = "WARNING: Generator power is failing! Find fuel quickly before the lights go out.";
    public Color shutdownWarningColor = new Color(1f, 0.25f, 0.1f);

    [Header("Gas Respawn After Shutdown")]
    [Tooltip("Creates one fresh Gas can when this generator finishes and its lights turn off.")]
    public bool respawnGasWhenLightsTurnOff = true;
    [Tooltip("Optional. Drag the Gas object here. If left empty, the script finds the active Pickup Item named Gas at scene start.")]
    public GameObject gasPrefab;
    [Tooltip("Drag your gas spawn point objects here. If empty, objects named 'gas spawn point' are found automatically.")]
    public Transform[] gasSpawnPoints;
    [Min(0f)]
    [Tooltip("Small upward offset so the spawned Gas does not begin inside the floor.")]
    public float gasSpawnHeightOffset = 0.1f;

    private Camera playerCamera;
    private GameObject insertedKey;
    private Quaternion insertedRotation;
    private bool isRunning;
    private bool showPrompt;
    private string currentPromptMessage;
    private bool showShutdownWarning;
    private GameObject gasRespawnTemplate;
    private AudioClip normalBackgroundMusic;
    private float normalBackgroundMusicVolume;
    private bool normalBackgroundMusicLoop;
    private float lightsOffSince = -1f;
    private float generatorRunEndTime = -1f;

    /// <summary>True while at least one generator light assigned to this slot is enabled.</summary>
    public bool AreAnyGeneratorLightsOn
    {
        get
        {
            if (IsLightOn(generatorLight1) || IsLightOn(generatorLight2))
                return true;

            if (additionalGeneratorLights == null)
                return false;

            foreach (Light light in additionalGeneratorLights)
                if (IsLightOn(light)) return true;

            return false;
        }
    }

    // Exposes the timed generator sequence for save and load.
    public bool IsRunning => isRunning;
    public float RemainingRunSeconds => isRunning
        ? Mathf.Max(0f, generatorRunEndTime - Time.time)
        : 0f;

    // Applies the saved light state after the scene's default setup has completed.
    public void RestoreGeneratorLights(bool enabled)
    {
        SetGeneratorLights(enabled);
        lightsOffSince = enabled ? -1f : Time.time;
    }

    // Resumes a saved timed run after the scene and its saved light state are restored.
    public void RestoreRunningState(float remainingSeconds)
    {
        if (isRunning || remainingSeconds <= 0f)
            return;

        StartCoroutine(RunGenerator(remainingSeconds));
    }

    /// <summary>
    /// Restores the original Gas before the first pour, or the replacement Gas
    /// after the generator has consumed fuel and its lights have gone out.
    /// </summary>
    public void ReconcileGasWithLightState(bool savedGasWasHeldOrDropped)
    {
        if (fuelInteraction == null)
            return;

        bool hasEverPouredGas = fuelInteraction.HasEverPouredGas;
        bool anyGeneratorLightIsOn = hasEverPouredGas && AreAnyGeneratorLightsOn;
        if (hasEverPouredGas && !anyGeneratorLightIsOn)
        {
            foreach (GeneratorKeySlot generator in Object.FindObjectsOfType<GeneratorKeySlot>())
            {
                if (generator != null && generator != this && generator.AreAnyGeneratorLightsOn)
                {
                    anyGeneratorLightIsOn = true;
                    break;
                }
            }
        }

        GasRestoreAction action = DetermineGasRestoreAction(
            hasEverPouredGas,
            anyGeneratorLightIsOn,
            isRunning,
            fuelInteraction.HasFuel());

        if (action == GasRestoreAction.KeepExistingGas)
            return;

        if (action == GasRestoreAction.RemoveGas)
        {
            bool removedByFuelInteraction = false;
            foreach (GeneratorFuelInteraction generator in Object.FindObjectsOfType<GeneratorFuelInteraction>())
            {
                generator.RemoveActiveSceneGas();
                removedByFuelInteraction = true;
            }

            if (!removedByFuelInteraction)
                RemoveActiveSceneGas();

            return;
        }

        // A fresh replacement has no dropped position in the save. Replace the
        // scene's original Gas with one at a configured random spawn point.
        if (!savedGasWasHeldOrDropped)
            RemoveActiveSceneGas();

        SpawnGasAtRandomPoint();
    }

    private static GasRestoreAction DetermineGasRestoreAction(bool hasEverPouredGas,
        bool anyGeneratorLightIsOn, bool isRunning, bool hasFuel)
    {
        if (!hasEverPouredGas)
            return GasRestoreAction.KeepExistingGas;

        return anyGeneratorLightIsOn || isRunning || hasFuel
            ? GasRestoreAction.RemoveGas
            : GasRestoreAction.RespawnGas;
    }

    private void Awake()
    {
        // Cache the original scene Gas before GeneratorFuelInteraction.Start may
        // remove it while restoring a fueled save.
        PrepareGasRespawnTemplate();
    }

    private void Start()
    {
        playerCamera = Camera.main;
        FindAndCacheBackgroundMusic();
        // The generator area starts lit. A separate GeneratorLightsOffTrigger
        // handles the scripted blink and blackout when the player reaches it.
        SetGeneratorLights(true);
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(keyInsertedSaveId))
            RestoreInsertedKey();
    }

    private void Update()
    {
        UpdateBackgroundMusicForLightState();
        showPrompt = false;
        currentPromptMessage = null;
        if (!IsLookingAtSlot()) return;

        if (insertedKey == null)
        {
            // Always provide feedback while the crosshair is on the slot, even when empty-handed.
            showPrompt = true;
            GameObject key = GetSelectedKey();
            if (key == null)
            {
                currentPromptMessage = generatorKeyRequiredPrompt;
                return;
            }

            currentPromptMessage = insertKeyPrompt;
            if (Input.GetKeyDown(interactKey)) InsertKey(key, true);
            return;
        }

        if (isRunning) return;
        showPrompt = true;
        if (fuelInteraction == null)
        {
            currentPromptMessage = fuelRequiredPrompt;
            return;
        }
        if (!fuelInteraction.HasFuel())
        {
            currentPromptMessage = fuelRequiredPrompt;
            return;
        }

        currentPromptMessage = startGeneratorPrompt;
        if (Input.GetKeyDown(interactKey)) StartCoroutine(RunGenerator());
    }
    private bool IsLookingAtSlot()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * .5f, Screen.height * .5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
    }

    private GameObject GetSelectedKey()
    {
        if (Inventory.Instance == null) return null;
        int selected = Inventory.Instance.GetSelectedIndex();
        var items = Inventory.Instance.GetItems();
        if (selected < 0 || selected >= items.Count || items[selected] == null) return null;
        GameObject item = items[selected];
        PickupItem pickup = item.GetComponent<PickupItem>();
        Key key = item.GetComponent<Key>();
        string itemName = pickup != null ? pickup.itemName : key != null ? key.itemName : item.name;
        return string.Equals(itemName, requiredKeyName, System.StringComparison.OrdinalIgnoreCase) ? item : null;
    }

    private void InsertKey(GameObject key, bool save)
    {
        if (key == null) return;
        if (Inventory.Instance != null) Inventory.Instance.RemoveWithoutDestroy(key);
        key.transform.SetParent(keyInsertPoint != null ? keyInsertPoint : transform, false);
        key.transform.localPosition = Vector3.zero;
        key.transform.localRotation = Quaternion.identity;
        key.transform.localScale = insertedKeyLocalScale;
        insertedRotation = key.transform.localRotation;
        insertedKey = key;
        foreach (Collider c in key.GetComponentsInChildren<Collider>()) c.enabled = false;
        Rigidbody body = key.GetComponent<Rigidbody>();
        if (body != null) { body.isKinematic = true; body.useGravity = false; }
        PickupItem pickup = key.GetComponent<PickupItem>();
        if (pickup != null) { pickup.isHeld = false; pickup.enabled = false; }
        Key keyComponent = key.GetComponent<Key>();
        if (keyComponent != null) keyComponent.enabled = false;
        if (save && SaveSystem.Instance != null) SaveSystem.Instance.MarkGeneratorCoverRemoved(keyInsertedSaveId);
    }

    private IEnumerator RunGenerator()
    {
        yield return RunGenerator(runningSeconds);
    }

    private IEnumerator RunGenerator(float duration)
    {
        isRunning = true;
        generatorRunEndTime = Time.time + duration;
        showPrompt = false;
        SetGeneratorLights(true);
        if (insertedKey != null) insertedKey.transform.localRotation = insertedRotation * Quaternion.Euler(turnRotation);
        if (showLightsShutdownWarning)
            StartCoroutine(ShowLightsShutdownWarning(duration));
        yield return new WaitForSeconds(duration);
        showShutdownWarning = false;
        SetGeneratorLights(false);
        if (insertedKey != null) insertedKey.transform.localRotation = insertedRotation;
        if (fuelInteraction != null) fuelInteraction.ConsumeFuel();
        SpawnGasAtRandomPoint();
        isRunning = false;
        generatorRunEndTime = -1f;
    }

    private IEnumerator ShowLightsShutdownWarning(float duration)
    {
        float waitBeforeWarning = Mathf.Max(0f, duration - warningSecondsBeforeShutdown);
        if (waitBeforeWarning > 0f)
            yield return new WaitForSeconds(waitBeforeWarning);

        if (!isRunning)
            yield break;

        showShutdownWarning = true;
        float availableWarningTime = Mathf.Min(warningDisplayDuration, warningSecondsBeforeShutdown, duration);
        yield return new WaitForSeconds(availableWarningTime);
        showShutdownWarning = false;
    }

    private void SetGeneratorLights(bool enabled)
    {
        SetLightState(generatorLight1, enabled);
        SetLightState(generatorLight2, enabled);
        if (additionalGeneratorLights != null)
        {
            foreach (Light light in additionalGeneratorLights)
                SetLightState(light, enabled);
        }

        // The blackout trigger includes the extra generator lights that are not
        // assigned directly to this key slot. Keep the entire set in sync so Gas
        // only appears after every generator light has turned off.
        foreach (GeneratorLightsOffTrigger trigger in Object.FindObjectsOfType<GeneratorLightsOffTrigger>())
            trigger.SetControlledLights(enabled);
    }

    private static void SetLightState(Light light, bool enabled)
    {
        if (light != null) light.enabled = enabled;
    }

    private static bool IsLightOn(Light light)
    {
        return light != null && light.enabled && light.gameObject.activeInHierarchy;
    }

    // Uses the existing Chapter 2 music source, without requiring a scene reference to be set manually.
    private void FindAndCacheBackgroundMusic()
    {
        if (backgroundMusicSource == null && !string.IsNullOrWhiteSpace(backgroundMusicObjectName))
        {
            GameObject musicObject = GameObject.Find(backgroundMusicObjectName);
            if (musicObject != null)
                backgroundMusicSource = musicObject.GetComponent<AudioSource>();
        }

        if (backgroundMusicSource == null)
            return;

        normalBackgroundMusic = backgroundMusicSource.clip;
        normalBackgroundMusicVolume = backgroundMusicSource.volume;
        normalBackgroundMusicLoop = backgroundMusicSource.loop;
    }

    // Plays intense music only after the flicker has finished, then restores the original track when power returns.
    private void UpdateBackgroundMusicForLightState()
    {
        if (backgroundMusicSource == null)
            return;

        bool lightsAreOn = AreAnyGeneratorLightsOn;
        if (!lightsAreOn)
        {
            if (lightsOffSince < 0f)
                lightsOffSince = Time.time;

            if (Time.time - lightsOffSince < lightsOutMusicDelay)
                return;

            SwitchBackgroundMusic(lightsOutBackgroundMusic, lightsOutBackgroundMusicVolume, true);
            return;
        }

        lightsOffSince = -1f;
        SwitchBackgroundMusic(normalBackgroundMusic, normalBackgroundMusicVolume, normalBackgroundMusicLoop);
    }

    private void SwitchBackgroundMusic(AudioClip clip, float volume, bool loop)
    {
        if (clip == null || backgroundMusicSource == null)
            return;

        if (backgroundMusicSource.clip == clip && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.volume = volume;
            backgroundMusicSource.loop = loop;
            return;
        }

        backgroundMusicSource.Stop();
        backgroundMusicSource.clip = clip;
        backgroundMusicSource.volume = volume;
        backgroundMusicSource.loop = loop;
        backgroundMusicSource.Play();
    }

    private void PrepareGasRespawnTemplate()
    {
        if (!respawnGasWhenLightsTurnOff || gasRespawnTemplate != null)
            return;

        GameObject source = gasPrefab != null ? gasPrefab : FindSceneGas();
        if (source == null)
        {
            Debug.LogWarning("Generator Key Slot: Gas respawn needs a Gas prefab or an active Pickup Item named Gas in this scene.", this);
            return;
        }

        gasRespawnTemplate = Instantiate(source, transform);
        gasRespawnTemplate.name = source.name + " Respawn Template";
        gasRespawnTemplate.SetActive(false);
    }

    private void SpawnGasAtRandomPoint()
    {
        if (!respawnGasWhenLightsTurnOff)
            return;

        if (gasRespawnTemplate == null)
            PrepareGasRespawnTemplate();
        if (gasRespawnTemplate == null)
            return;

        if (AreAnyGeneratorLightsOn)
            return;

        if (HasActiveGas())
            return;

        FindGasSpawnPointsIfNeeded();
        Transform spawnPoint = GetRandomGasSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("Generator Key Slot: Assign Gas Spawn Points or name them 'gas spawn point'.", this);
            return;
        }

        Vector3 position = spawnPoint.position + Vector3.up * gasSpawnHeightOffset;
        GameObject gas = Instantiate(gasRespawnTemplate, position, spawnPoint.rotation);
        gas.name = "Gas";
        gas.SetActive(true);

        PickupItem pickup = gas.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.enabled = true;
            pickup.isPickedUp = false;
            pickup.isHeld = false;
            // DroppedItemData stores world transforms for items outside the
            // inventory. Preserve this spawn position across save and load.
            pickup.wasDropped = true;
        }

        foreach (Collider itemCollider in gas.GetComponentsInChildren<Collider>(true))
            itemCollider.enabled = true;

        Rigidbody body = gas.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

    }

    private GameObject FindSceneGas()
    {
        foreach (PickupItem pickup in Resources.FindObjectsOfTypeAll<PickupItem>())
        {
            if (pickup == null || pickup.gameObject.scene != gameObject.scene || !pickup.gameObject.activeInHierarchy)
                continue;
            if (string.Equals(pickup.itemName, "Gas", System.StringComparison.OrdinalIgnoreCase))
                return pickup.gameObject;
        }
        return null;
    }

    private bool HasActiveGas()
    {
        return FindSceneGas() != null;
    }

    private void RemoveActiveSceneGas()
    {
        foreach (PickupItem pickup in Resources.FindObjectsOfTypeAll<PickupItem>())
        {
            if (pickup == null || pickup.gameObject.scene != gameObject.scene ||
                !pickup.gameObject.activeInHierarchy ||
                !string.Equals(pickup.itemName, "Gas", System.StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject gas = pickup.gameObject;
            gas.SetActive(false);
            if (Inventory.Instance != null && Inventory.Instance.GetItems().Contains(gas))
                Inventory.Instance.RemoveAndDestroy(gas);
            else
                Destroy(gas);
        }
    }

    private void FindGasSpawnPointsIfNeeded()
    {
        if (gasSpawnPoints != null && gasSpawnPoints.Length > 0)
            return;

        var foundPoints = new System.Collections.Generic.List<Transform>();
        foreach (Transform sceneTransform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (sceneTransform == null || sceneTransform.gameObject.scene != gameObject.scene)
                continue;
            if (sceneTransform.name.StartsWith("gas spawn point", System.StringComparison.OrdinalIgnoreCase))
                foundPoints.Add(sceneTransform);
        }
        gasSpawnPoints = foundPoints.ToArray();
    }

    private Transform GetRandomGasSpawnPoint()
    {
        if (gasSpawnPoints == null || gasSpawnPoints.Length == 0)
            return null;

        var validPoints = new System.Collections.Generic.List<Transform>();
        foreach (Transform point in gasSpawnPoints)
            if (point != null) validPoints.Add(point);

        return validPoints.Count == 0 ? null : validPoints[Random.Range(0, validPoints.Count)];
    }

    private void RestoreInsertedKey()
    {
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj == null || obj.scene != gameObject.scene) continue;
            PickupItem pickup = obj.GetComponent<PickupItem>();
            Key key = obj.GetComponent<Key>();
            string itemName = pickup != null ? pickup.itemName : key != null ? key.itemName : obj.name;
            if (!string.Equals(itemName, requiredKeyName, System.StringComparison.OrdinalIgnoreCase)) continue;
            InsertKey(obj, false);
            return;
        }
    }

    private void OnGUI()
    {
        if (showShutdownWarning)
        {
            GUIStyle warningShadow = new GUIStyle { fontSize = 25, alignment = TextAnchor.UpperCenter, wordWrap = true };
            warningShadow.normal.textColor = Color.black;
            GUIStyle warningText = new GUIStyle(warningShadow);
            warningText.normal.textColor = shutdownWarningColor;
            Rect warningRect = new Rect(Screen.width * 0.5f - 310f, Screen.height * 0.17f, 620f, 100f);
            string localizedWarning = GameplayLocalization.TranslateSubtitle(shutdownWarningMessage);
            GUI.Label(new Rect(warningRect.x + 2f, warningRect.y + 2f, warningRect.width, warningRect.height), localizedWarning, warningShadow);
            GUI.Label(warningRect, localizedWarning, warningText);
        }

        if (!showPrompt || isRunning) return;
        string message = string.IsNullOrEmpty(currentPromptMessage)
            ? (insertedKey == null ? insertKeyPrompt : startGeneratorPrompt)
            : currentPromptMessage;
        message = GameplayLocalization.TranslateSubtitle(message);
        GUIStyle shadow = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        shadow.normal.textColor = Color.black;
        GUIStyle text = new GUIStyle(shadow); text.normal.textColor = Color.white;
        Rect rect = new Rect(Screen.width * .5f - 200f, Screen.height * .5f + 50f, 400f, 35f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }
}
