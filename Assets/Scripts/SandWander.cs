using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;

public class SandWander : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.03f;
    [SerializeField] private float turnSpeed = 60f;
    [SerializeField] private float directionChangeInterval = 2f;

    private ARPlane targetPlane;
    private float timer;
    private float turnDirection;

    public void SetPlane(ARPlane plane)
    {
        targetPlane = plane;
    }

    void Start()
    {
        PickNewDirection();
    }

    void Update()
    {
        if (targetPlane == null) return;

        timer += Time.deltaTime;
        if (timer >= directionChangeInterval)
        {
            PickNewDirection();
            timer = 0f;
        }

        Vector3 nextPosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;

        if (IsSpeciesInsidePlane(nextPosition))
        {
            transform.position = nextPosition;
            transform.Rotate(Vector3.up, turnDirection * turnSpeed * Time.deltaTime);
        }
        else
        {
            TurnBackInside();
        }
    }

    void PickNewDirection()
    {
        turnDirection = Random.Range(-1f, 1f);
    }

    void TurnBackInside()
    {
        Vector3 planeCenter = targetPlane.center;
        planeCenter = targetPlane.transform.TransformPoint(planeCenter);

        Vector3 directionToCenter = (planeCenter - transform.position).normalized;
        directionToCenter.y = 0f;

        if (directionToCenter != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    bool IsSpeciesInsidePlane(Vector3 worldPoint)
    {
        if (targetPlane == null) return false;

        Vector3 localPoint3 = targetPlane.transform.InverseTransformPoint(worldPoint);
        Vector2 localPoint = new Vector2(localPoint3.x, localPoint3.z);

        NativeArray<Vector2> boundary = targetPlane.boundary;

        if (!boundary.IsCreated || boundary.Length < 3)
            return false;

        return IsSpeciesInPolygon(localPoint, boundary);
    }

    bool IsSpeciesInPolygon(Vector2 point, NativeArray<Vector2> polygon) 
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool intersect =
                ((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / ((pj.y - pi.y) + 0.0001f) + pi.x);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }
}