using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class SandSpeciesPlacement : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("UI")]
    [SerializeField] private TMP_Text placementHint;

    [Header("Selected Sand Prefab")]
    [SerializeField] private GameObject selectedPrefab;

    [Header("Behaviour")]
    [SerializeField] private bool allowOnlyOne = true;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    GameObject spawned;

    void Awake()
    {
        if (raycastManager == null)
            raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();

        UpdateHintText();
    }

    public void SelectPrefab(GameObject prefab)
    {
        selectedPrefab = prefab;
        UpdateHintText();
    }

    void Update()
    {
        if (raycastManager == null) return;

        // Placing the selected prefab on the plane when the user taps
        if (selectedPrefab != null && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began &&
                raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;

                // Get the plane we hit
                ARPlane hitPlane = null;
                if (hits[0].trackable is ARPlane plane)
                {
                    hitPlane = plane;
                }

                if (allowOnlyOne && spawned != null)
                {
                    spawned.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    spawned = Instantiate(selectedPrefab, pose.position, pose.rotation);
                }

                // Pass plane to crab movement script
                SandWander crab = spawned.GetComponent<SandWander>();
                if (crab != null && hitPlane != null)
                {
                    crab.SetPlane(hitPlane);
                }

                selectedPrefab = null;
                UpdateHintText();
            }
        }

        // ====== INTERACTION ======
        if (spawned == null) return;

        HandleDrag();
        HandlePinchZoom();
        HandleRotation();
    }

    //Drag to move motion
    void HandleDrag()
    {
        if (Input.touchCount != 1) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;
                spawned.transform.position = pose.position;
            }
        }
    }

    //Pinch to zoom motion
    void HandlePinchZoom()
    {
        if (Input.touchCount != 2) return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        float prevDist = (t1.position - t1.deltaPosition - (t2.position - t2.deltaPosition)).magnitude;
        float currDist = (t1.position - t2.position).magnitude;

        float scaleFactor = (currDist - prevDist) * 0.001f;

        spawned.transform.localScale += Vector3.one * scaleFactor;

        // Clamping the scale to prevent it from being too small or too large
        float min = 0.1f; 
        float max = 2f;
        spawned.transform.localScale = Vector3.Max(Vector3.one * min, 
            Vector3.Min(Vector3.one * max, spawned.transform.localScale)); 
    }

    //Rotation motion
    void HandleRotation()
    {
        if (Input.touchCount != 2) return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        Vector2 prevDir = (t1.position - t1.deltaPosition) - (t2.position - t2.deltaPosition);
        Vector2 currDir = t1.position - t2.position;

        float angle = Vector2.SignedAngle(prevDir, currDir);

        spawned.transform.Rotate(0, -angle, 0);
    }

    public void DeleteSpawned()
    {
        if (spawned != null)
        {
            Destroy(spawned);
            spawned = null;
        }

        selectedPrefab = null;
        UpdateHintText();
    }

    private void UpdateHintText()
    {
        if (placementHint == null) return;

        if (selectedPrefab == null)
            placementHint.text = "";
        else
            placementHint.text = $"Tap the ground to place {selectedPrefab.name}";
    }

}