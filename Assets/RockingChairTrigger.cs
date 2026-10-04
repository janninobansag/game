// PURPOSE: Rocks a chair when the player enters this trigger volume.
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class RockingChairTrigger : MonoBehaviour
{
    [Header("Chair")]
    [Tooltip("Drag the rocking chair's root Transform here. Its pivot should be at the chair's rocking point.")]
    public Transform rockingChair;

    [Tooltip("The local axis the chair rotates around. Most rocking chairs use X.")]
    public Vector3 localRockAxis = Vector3.right;

    [Min(0f)]
    [Tooltip("The largest angle, in degrees, from the chair's resting rotation.")]
    public float rockAngle = 12f;

    [Min(0.01f)]
    [Tooltip("How many forward-and-back rocks happen each second.")]
    public float rocksPerSecond = 1.2f;

    [Min(0.01f)]
    [Tooltip("How long the rocking lasts before the chair returns to rest.")]
    public float rockingDuration = 4f;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    private Quaternion restingLocalRotation;
    private bool hasTriggered;
    private Coroutine rockingRoutine;

    private void Awake()
    {
        if (rockingChair == null)
            rockingChair = transform.parent;

        if (rockingChair != null)
            restingLocalRotation = rockingChair.localRotation;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

#if UNITY_EDITOR
        // Static-batched renderers cannot visibly move at runtime. The trigger is
        // a child of the chair, so remove Static from the entire chair hierarchy.
        if (rockingChair != null)
        {
            foreach (Transform chairPart in rockingChair.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(chairPart.gameObject, 0);
        }
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || (triggerOnce && hasTriggered))
            return;

        hasTriggered = true;
        StartRocking();
    }

    [ContextMenu("Test Rocking")]
    public void StartRocking()
    {
        if (rockingChair == null)
        {
            Debug.LogWarning($"{name}: Assign the Rocking Chair field before using this trigger.", this);
            return;
        }

        if (rockingRoutine != null)
            StopCoroutine(rockingRoutine);

        restingLocalRotation = rockingChair.localRotation;
        rockingRoutine = StartCoroutine(RockChair());
    }

    private IEnumerator RockChair()
    {
        float elapsed = 0f;
        Vector3 axis = localRockAxis.sqrMagnitude > 0.0001f
            ? localRockAxis.normalized
            : Vector3.right;

        while (elapsed < rockingDuration)
        {
            elapsed += Time.deltaTime;
            float fadeOut = 1f - Mathf.Clamp01(elapsed / rockingDuration);
            float angle = Mathf.Sin(elapsed * rocksPerSecond * Mathf.PI * 2f) * rockAngle * fadeOut;
            rockingChair.localRotation = restingLocalRotation * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }

        rockingChair.localRotation = restingLocalRotation;
        rockingRoutine = null;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag) ||
               (other.transform.root != other.transform && other.transform.root.CompareTag(playerTag));
    }
}
