using UnityEngine;

public class SpeciesSpawner : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Spawn Settings")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private bool onlyOneAtATime = true;

    private GameObject spawned;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void SpawnPrefab(GameObject prefab)
    {
        if (prefab == null || targetCamera == null) return;

        Vector3 pos = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
        Quaternion rot = Quaternion.LookRotation(targetCamera.transform.forward, Vector3.up);

        if (onlyOneAtATime && spawned != null)
        {
            spawned.transform.SetPositionAndRotation(pos, rot);
        }
        else
        {
            spawned = Instantiate(prefab, pos, rot);
        }
    }

    public void Despawn()
    {
        if (spawned != null) Destroy(spawned);
    }
}