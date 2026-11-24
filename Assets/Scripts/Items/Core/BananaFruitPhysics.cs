using UnityEngine;

// Gives both hand-placed and tree-spawned banana fruit a correctly sized collider.
public class BananaFruitPhysics : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            MeshFilter mesh = GetComponent<MeshFilter>();
            if (mesh != null && mesh.sharedMesh != null)
            {
                collider.center = mesh.sharedMesh.bounds.center;
                collider.size = mesh.sharedMesh.bounds.size;
            }
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.mass = 0.15f;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
