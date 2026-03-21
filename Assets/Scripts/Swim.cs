using UnityEngine;

public class Swim : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [SerializeField] private float swimSpeed = 0.2f;
    [SerializeField] private float turnSpeed = 25f;
    [SerializeField] private float directionChangeInterval = 3f;

    [Header("Vertical Bobbing")]
    [SerializeField] private float bobHeight = 0.03f;
    [SerializeField] private float bobSpeed = 1f;

    [Header("Boundary")]
    [SerializeField] private float swimRadius = 1.5f;
    [SerializeField] private float maxVerticalOffset = 0.08f;

    private Vector3 startPosition;
    private float timer;
    private float turnDirection;
    private float bobOffset;

    void Start()
    {
        startPosition = transform.position;
        PickNewDirection();
    }

    void Update()
    {
        HandleHorizontalSwimming();
        HandleVerticalBobbing();
    }

    void HandleHorizontalSwimming()
    {
        timer += Time.deltaTime;

        if (timer >= directionChangeInterval)
        {
            PickNewDirection();
            timer = 0f;
        }

        Vector3 forwardMove = transform.forward * swimSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + new Vector3(forwardMove.x, 0f, forwardMove.z);

        Vector3 flatStart = new Vector3(startPosition.x, 0f, startPosition.z);
        Vector3 flatNext = new Vector3(nextPosition.x, 0f, nextPosition.z);

        float distanceFromStart = Vector3.Distance(flatStart, flatNext);

        if (distanceFromStart > swimRadius)
        {
            TurnBackTowardCenter();
        }
        else
        {
            transform.position = new Vector3(nextPosition.x, transform.position.y, nextPosition.z);
            transform.Rotate(Vector3.up, turnDirection * turnSpeed * Time.deltaTime);
        }
    }

    void HandleVerticalBobbing()
    {
        bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        float targetY = startPosition.y + bobOffset;
        float minY = startPosition.y - maxVerticalOffset;
        float maxY = startPosition.y + maxVerticalOffset;

        targetY = Mathf.Clamp(targetY, minY, maxY);

        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;
    }

    void PickNewDirection()
    {
        turnDirection = Random.Range(-1f, 1f);
    }

    void TurnBackTowardCenter()
    {
        Vector3 directionToCenter = startPosition - transform.position;
        directionToCenter.y = 0f;

        if (directionToCenter != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCenter.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        Vector3 forwardMove = transform.forward * swimSpeed * Time.deltaTime;
        transform.position += new Vector3(forwardMove.x, 0f, forwardMove.z);
    }
}