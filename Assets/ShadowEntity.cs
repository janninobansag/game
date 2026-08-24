using System.Collections;
using UnityEngine;

public class ShadowEntity : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 3f;
    public bool disappearAfterReach = true;
    public float disappearDistance = 1.5f;

    [Header("Waypoints")]
    public Transform[] waypoints;
    public bool randomOrder = false;
    public float waitAtWaypoint = 0.5f;

    [Header("Visibility")]
    public float viewAngle = 60f;
    public float viewRange = 15f;
    public bool hideWhenLookedAt = true;
    public float hideSpeed = 8f;

    [Header("Flicker")]
    public bool flicker = true;
    public float flickerSpeed = 8f;
    public float minOpacity = 0.3f;
    public float maxOpacity = 0.85f;

    [Header("Spawn Settings")]
    public float appearFadeDuration = 0.5f;
    public float disappearFadeDuration = 0.3f;

    private Renderer[] renderers;
    private Material[] shadowMaterials;
    private Camera playerCamera;
    private Transform player;

    private int currentWaypoint = 0;
    private bool isMoving = false;
    private bool isVisible = true;
    private float currentOpacity = 0f;
    private float flickerTimer = 0f;
    private bool isFading = false;

    void Start()
    {
        playerCamera = Camera.main;
        player = playerCamera.transform;

        renderers = GetComponentsInChildren<Renderer>();
        shadowMaterials = new Material[renderers.Length];

        // Create dark shadow materials
        for (int i = 0; i < renderers.Length; i++)
        {
            shadowMaterials[i] = new Material(renderers[i].material);
            shadowMaterials[i].color = new Color(0f, 0f, 0f, 0f);

            // Enable transparency
            shadowMaterials[i].SetFloat("_Mode", 3);
            shadowMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shadowMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shadowMaterials[i].SetInt("_ZWrite", 0);
            shadowMaterials[i].DisableKeyword("_ALPHATEST_ON");
            shadowMaterials[i].EnableKeyword("_ALPHABLEND_ON");
            shadowMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
            shadowMaterials[i].renderQueue = 3000;

            renderers[i].material = shadowMaterials[i];
        }

        StartCoroutine(AppearFade());
        StartCoroutine(MoveShadow());
    }

    void Update()
    {
        if (player == null) return;

        // Check if player is looking at shadow
        bool playerLooking = IsPlayerLooking();

        if (hideWhenLookedAt)
        {
            if (playerLooking && isVisible)
            {
                // Disappear when looked at
                StopCoroutine("FadeOut");
                StartCoroutine(FadeOut(disappearFadeDuration));
                isVisible = false;
            }
            else if (!playerLooking && !isVisible && !isFading)
            {
                // Reappear when not looked at
                StartCoroutine(FadeIn(appearFadeDuration));
                isVisible = true;
            }
        }

        // Flicker effect
        if (flicker && isVisible && !isFading)
        {
            flickerTimer += Time.deltaTime * flickerSpeed;
            float flickerVal = (Mathf.Sin(flickerTimer) + 1f) / 2f;
            float opacity = Mathf.Lerp(minOpacity, maxOpacity, flickerVal);
            SetOpacity(opacity);
        }
    }

    bool IsPlayerLooking()
    {
        Vector3 dirToShadow = (transform.position - player.position).normalized;
        float angle = Vector3.Angle(player.forward, dirToShadow);
        float dist = Vector3.Distance(transform.position, player.position);

        if (angle < viewAngle / 2f && dist < viewRange)
        {
            // Raycast to check no walls blocking
            RaycastHit hit;
            if (Physics.Raycast(player.position, dirToShadow, out hit, viewRange))
            {
                if (hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.gameObject == gameObject)
                    return true;
            }
        }

        return false;
    }

    IEnumerator MoveShadow()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            yield break;
        }

        while (true)
        {
            if (!isVisible)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            Transform target = waypoints[currentWaypoint];
            isMoving = true;

            // Move to waypoint
            while (Vector3.Distance(transform.position, target.position)
                > disappearDistance)
            {
                Vector3 direction = (target.position - transform.position).normalized;

                // Smooth rotation
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot,
                    Time.deltaTime * rotationSpeed);

                transform.position = Vector3.MoveTowards(
                    transform.position, target.position,
                    moveSpeed * Time.deltaTime);

                yield return null;
            }

            isMoving = false;

            // Wait at waypoint
            yield return new WaitForSeconds(waitAtWaypoint);

            // Next waypoint
            if (randomOrder)
                currentWaypoint = Random.Range(0, waypoints.Length);
            else
            {
                currentWaypoint++;
                if (currentWaypoint >= waypoints.Length)
                {
                    if (disappearAfterReach)
                    {
                        // Disappear after completing path
                        yield return StartCoroutine(
                            FadeOut(disappearFadeDuration));
                        gameObject.SetActive(false);
                        yield break;
                    }
                    currentWaypoint = 0;
                }
            }
        }
    }

    IEnumerator AppearFade()
    {
        isFading = true;
        float elapsed = 0f;

        while (elapsed < appearFadeDuration)
        {
            elapsed += Time.deltaTime;
            currentOpacity = Mathf.Lerp(0f, maxOpacity,
                elapsed / appearFadeDuration);
            SetOpacity(currentOpacity);
            yield return null;
        }

        isFading = false;
    }

    IEnumerator FadeIn(float duration)
    {
        isFading = true;
        float elapsed = 0f;
        float startOpacity = currentOpacity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentOpacity = Mathf.Lerp(startOpacity, maxOpacity,
                elapsed / duration);
            SetOpacity(currentOpacity);
            yield return null;
        }

        isFading = false;
    }

    IEnumerator FadeOut(float duration)
    {
        isFading = true;
        float elapsed = 0f;
        float startOpacity = currentOpacity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentOpacity = Mathf.Lerp(startOpacity, 0f,
                elapsed / duration);
            SetOpacity(currentOpacity);
            yield return null;
        }

        currentOpacity = 0f;
        SetOpacity(0f);
        isFading = false;
    }

    void SetOpacity(float opacity)
    {
        foreach (Material mat in shadowMaterials)
        {
            if (mat != null)
                mat.color = new Color(0f, 0f, 0f, opacity);
        }
    }

    void OnDestroy()
    {
        foreach (Material mat in shadowMaterials)
            if (mat != null) Destroy(mat);
    }

    // Draw waypoint gizmos in editor
    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position,
                    waypoints[i + 1].position);
        }
    }
}