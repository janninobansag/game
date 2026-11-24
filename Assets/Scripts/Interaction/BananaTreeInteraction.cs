using UnityEngine;

// Hold E while looking at a nearby tree to shake fruit loose.
public class BananaTreeInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactionDistance = 3f;
    [Min(0.1f)] public float holdDuration = 1.5f;
    public LayerMask interactionLayers = ~0;
    public Camera playerCamera;

    [Header("Shake")]
    [Tooltip("Assign the visual group, leaving the trunk collider outside it.")]
    public Transform visualRoot;
    [Range(0f, 10f)] public float shakeAngle = 2f;
    [Min(0f)] public float shakeFrequency = 7f;

    [Header("Fruit")]
    public GameObject fruitPrefab;
    public Transform fruitDropPoint;
    [Min(1)] public int fruitCount = 3;
    [Min(0f)] public float dropHeight = 2.5f;
    [Min(0f)] public float dropRadius = 0.9f;
    [Min(0f)] public float outwardImpulse = 0.4f;
    // Retain older serialized fields; the shared cycle now controls regrowth.
    [HideInInspector] public bool allowRepeatHarvest = true;
    [HideInInspector] public float harvestCooldown = 30f;

    private Quaternion restingRotation;
    private float holdProgress;
    private bool showPrompt;
    private bool mustReleaseKey;
    private bool isShaking;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (visualRoot != null)
            restingRotation = visualRoot.localRotation;
    }

    private void Update()
    {
        showPrompt = false;
        if (!Input.GetKey(interactKey))
            mustReleaseKey = false;

        if (Time.timeScale <= 0f || PlayerController.IsReadingDocument || AIEncounterGate.IsActive ||
            (PauseMenu.Instance != null && PauseMenu.Instance.isPaused) || fruitPrefab == null)
        {
            StopShaking();
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
        {
            StopShaking();
            return;
        }

        // Trees outside interaction range should not each cast a ray every frame.
        float searchDistance = interactionDistance + 2f;
        if ((playerCamera.transform.position - transform.position).sqrMagnitude > searchDistance * searchDistance)
        {
            StopShaking();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers,
                QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<BananaTreeInteraction>() != this)
        {
            StopShaking();
            return;
        }

        showPrompt = true;
        if (!BananaHarvestCycle.CanHarvest)
        {
            if (Input.GetKey(interactKey))
                mustReleaseKey = true;
            StopShaking();
            return;
        }

        if (!Input.GetKey(interactKey) || mustReleaseKey)
        {
            StopShaking();
            return;
        }

        isShaking = true;
        holdProgress += Time.deltaTime;
        if (holdProgress >= holdDuration)
        {
            // Reserve the shared harvest before spawning so two trees cannot
            // both finish a hold and create a batch in the same frame.
            if (BananaHarvestCycle.TryBeginHarvest())
            {
                try
                {
                    DropFruit();
                }
                finally
                {
                    BananaHarvestCycle.FinishHarvest();
                }
            }
            mustReleaseKey = true;
            showPrompt = false;
            StopShaking();
        }
    }

    private void LateUpdate()
    {
        if (!isShaking || visualRoot == null)
            return;

        float phase = holdProgress * shakeFrequency * Mathf.PI * 2f;
        visualRoot.localRotation = restingRotation * Quaternion.Euler(
            Mathf.Sin(phase) * shakeAngle, 0f, Mathf.Sin(phase * 0.8f) * shakeAngle);
    }

    private void StopShaking()
    {
        if (isShaking && visualRoot != null)
            visualRoot.localRotation = restingRotation;
        isShaking = false;
        holdProgress = 0f;
    }

    private void DropFruit()
    {
        Vector3 origin = fruitDropPoint != null
            ? fruitDropPoint.position
            : transform.position + Vector3.up * dropHeight;
        float offset = Random.Range(0f, 360f);

        for (int i = 0; i < fruitCount; i++)
        {
            float angle = (offset + i * 360f / fruitCount) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            GameObject fruit = Instantiate(fruitPrefab, origin + outward * dropRadius,
                Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * fruitPrefab.transform.rotation);
            BananaUse edible = fruit.GetComponent<BananaUse>();
            if (edible == null)
                edible = fruit.AddComponent<BananaUse>();
            BananaHarvestCycle.RegisterFruit(edible);

            // The supplied fruit model has no physics components. Create a box
            // from its local mesh bounds so its existing model scale is respected.
            if (fruit.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter mesh = fruit.GetComponent<MeshFilter>();
                BoxCollider collider = fruit.AddComponent<BoxCollider>();
                if (mesh != null && mesh.sharedMesh != null)
                {
                    collider.center = mesh.sharedMesh.bounds.center;
                    collider.size = mesh.sharedMesh.bounds.size;
                }
            }

            Rigidbody body = fruit.GetComponent<Rigidbody>();
            if (body == null)
                body = fruit.AddComponent<Rigidbody>();
            body.mass = 0.15f;
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(outward * outwardImpulse, ForceMode.Impulse);
        }
    }

    private void OnGUI()
    {
        if (!showPrompt)
            return;
        string message = $"Hold {interactKey} to shake the banana tree";
        if (BananaHarvestCycle.RemainingFruitCount > 0)
            message = $"Eat the remaining bananas first ({BananaHarvestCycle.RemainingFruitCount})";
        else if (!BananaHarvestCycle.CanHarvest)
            message = $"Bananas regrow in {Mathf.CeilToInt(BananaHarvestCycle.SecondsUntilReady)}s";
        else if (holdProgress > 0f)
            message += $"  {Mathf.RoundToInt(Mathf.Clamp01(holdProgress / holdDuration) * 100f)}%";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22
        };
        Rect rect = new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f + 50f, 600f, 45f);
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, style);
        style.normal.textColor = Color.white;
        GUI.Label(rect, message, style);
    }

    private void OnDisable()
    {
        showPrompt = false;
        StopShaking();
    }
}
