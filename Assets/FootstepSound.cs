using UnityEngine;

public class FootstepSound : MonoBehaviour
{
    [Header("Footstep Settings")]
    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.7f;
    public float stepVolume = 0.6f;

    [Header("Grass Sounds")]
    public AudioClip[] grassSounds;

    [Header("Wood Sounds")]
    public AudioClip[] woodSounds;

    [Header("Stone Sounds")]
    public AudioClip[] stoneSounds;

    [Header("Default Sounds")]
    public AudioClip[] defaultSounds;

    [Header("Surface Detection")]
    public float raycastDistance = 0.3f;
    public LayerMask groundLayer;

    private AudioSource audioSource;
    private CharacterController cc;
    private float stepTimer = 0f;
    private bool isSprinting = false;
    private bool isCrouching = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = stepVolume;

        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (h != 0 || v != 0) && cc.isGrounded;

        isSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0f;
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        // Count down step timer
        float interval = isSprinting ? sprintStepInterval
            : isCrouching ? crouchStepInterval
            : walkStepInterval;

        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            PlayFootstep();
        }
    }

    void PlayFootstep()
    {
        AudioClip[] clips = GetSurfaceClips();
        if (clips == null || clips.Length == 0) return;

        // Pick random clip
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        // Slightly vary pitch for natural sound
        audioSource.pitch = Random.Range(0.9f, 1.1f);

        // Lower volume when crouching
        audioSource.volume = isCrouching ? stepVolume * 0.5f : stepVolume;

        audioSource.PlayOneShot(clip);
    }

    AudioClip[] GetSurfaceClips()
    {
        // Raycast down to detect surface
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out hit,
            raycastDistance + 0.2f, groundLayer))
        {
            string tag = hit.collider.tag.ToLower();
            string name = hit.collider.gameObject.name.ToLower();
            string matName = "";

            // Check material name if renderer exists
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
                matName = rend.material.name.ToLower();

            // Detect surface by tag
            if (hit.collider.CompareTag("Grass")) return grassSounds;
            if (hit.collider.CompareTag("Wood")) return woodSounds;
            if (hit.collider.CompareTag("Stone")) return stoneSounds;

            // Detect by object name
            if (name.Contains("grass") || name.Contains("terrain"))
                return grassSounds;
            if (name.Contains("wood") || name.Contains("floor") ||
                name.Contains("plank") || name.Contains("board"))
                return woodSounds;
            if (name.Contains("stone") || name.Contains("rock") ||
                name.Contains("brick") || name.Contains("concrete"))
                return stoneSounds;

            // Detect by material name
            if (matName.Contains("grass")) return grassSounds;
            if (matName.Contains("wood") || matName.Contains("floor"))
                return woodSounds;
            if (matName.Contains("stone") || matName.Contains("rock"))
                return stoneSounds;
        }

        // Check if on terrain specifically
        if (Physics.Raycast(origin, Vector3.down, out hit,
            raycastDistance + 0.2f))
        {
            if (hit.collider is TerrainCollider)
                return grassSounds;

            string name = hit.collider.gameObject.name.ToLower();
            if (name.Contains("wood") || name.Contains("floor") ||
                name.Contains("plank"))
                return woodSounds;
        }

        return defaultSounds;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f,
            Vector3.down * (raycastDistance + 0.2f));
    }
}