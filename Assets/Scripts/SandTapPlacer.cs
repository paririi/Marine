using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class SandTapPlacer : MonoBehaviour
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
            raycastManager = GetComponent<ARRaycastManager>();

        if (raycastManager == null)
            raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();

        UpdateHintText();
    }

    // Called by Sand buttons
    public void SelectPrefab(GameObject prefab)
    {
        selectedPrefab = prefab;
        UpdateHintText();
    }

    void Update()
    {
        if (selectedPrefab == null || raycastManager == null) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (!raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;

        if (allowOnlyOne && spawned != null)
        {
            spawned.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
        else
        {
            spawned = Instantiate(selectedPrefab, pose.position, pose.rotation);
        }

        // Optional: clear selection after placement
        selectedPrefab = null;
        UpdateHintText();
    }

    private void UpdateHintText()
    {
        if (placementHint == null) return;

        if (selectedPrefab == null)
            placementHint.text = "";
        else
            placementHint.text = $"Tap the ground to place {PrettyName(selectedPrefab.name)}";
    }

    private string PrettyName(string raw)
    {
        return raw.Replace("Placeholder", "")
                  .Replace("(Clone)", "")
                  .Trim();
    }
}