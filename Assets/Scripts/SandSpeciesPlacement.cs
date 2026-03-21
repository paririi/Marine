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

    [Header("Information Modal")]
    [SerializeField] private GameObject informationModal;
    [SerializeField] private TMP_Text speciesNameText;
    [SerializeField] private TMP_Text speciesDataText;

    [Header("Sand Species")]
    [SerializeField] private SpeciesData crabSpecies;
    [SerializeField] private SpeciesData starfishSpecies;
    [SerializeField] private SpeciesData seashellSpecies;

    private SpeciesData selectedSpecies;
    private GameObject selectedPrefab;

    [Header("Behaviour")]
    [SerializeField] private bool allowOnlyOne = true;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    GameObject spawned;

    void Awake()
    {
        if (raycastManager == null)
            raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();

        if (informationModal != null)
            informationModal.SetActive(false);

        UpdateHintText();
    }

    public void SelectCrab()
    {
        selectedSpecies = crabSpecies;
        selectedPrefab = crabSpecies.prefab;
        UpdateHintText();
    }

    public void SelectStarfish()
    {
        selectedSpecies = starfishSpecies;
        selectedPrefab = starfishSpecies.prefab;
        UpdateHintText();
    }

    public void SelectSeashell()
    {
        selectedSpecies = seashellSpecies;
        selectedPrefab = seashellSpecies.prefab;
        UpdateHintText();
    }

    void Update()
    {
        if (raycastManager == null) return;

        if (selectedPrefab != null && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began &&
                raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;

                if (allowOnlyOne && spawned != null)
                {
                    spawned.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    spawned = Instantiate(selectedPrefab, pose.position, pose.rotation);
                }

                selectedPrefab = null;
                UpdateHintText();
            }
        }

        if (spawned == null) return;

        HandleDrag();
        HandlePinchZoom();
        HandleRotation();
    }

    void HandleDrag()
    {
        if (Input.touchCount != 1) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                spawned.transform.position = hits[0].pose.position;
            }
        }
    }

    void HandlePinchZoom()
    {
        if (Input.touchCount != 2) return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        float prevDist = (t1.position - t1.deltaPosition - (t2.position - t2.deltaPosition)).magnitude;
        float currDist = (t1.position - t2.position).magnitude;

        float scaleFactor = (currDist - prevDist) * 0.001f;

        spawned.transform.localScale += Vector3.one * scaleFactor;

        float min = 0.1f;
        float max = 2f;

        spawned.transform.localScale = Vector3.Max(
            Vector3.one * min,
            Vector3.Min(Vector3.one * max, spawned.transform.localScale));
    }

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

    public void OpenSpeciesInfo()
    {
        if (selectedSpecies == null)
        {
            speciesNameText.text = "No Species Selected";
            speciesDataText.text =
                "Please select a species first to view its educational information.";

            informationModal.SetActive(true);
            return;
        }

        speciesNameText.text = selectedSpecies.speciesName;
        speciesDataText.text = selectedSpecies.educationalInfo;

        informationModal.SetActive(true);
    }

    public void CloseSpeciesInfo()
    {
        informationModal.SetActive(false);
    }

    public void DeleteSpawned()
    {
        if (spawned != null)
        {
            Destroy(spawned);
            spawned = null;
        }

        selectedPrefab = null;
        selectedSpecies = null;

        UpdateHintText();
    }

    private void UpdateHintText()
    {
        if (placementHint == null) return;

        if (selectedPrefab == null || selectedSpecies == null)
            placementHint.text = "";
        else
            placementHint.text =
                $"Tap the ground to place {selectedSpecies.speciesName}";
    }
}