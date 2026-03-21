using UnityEngine;
using TMPro;

public class SeaSpeciesPlacement : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("UI")]
    [SerializeField] private TMP_Text placementHint;

    [Header("Information Modal")]
    [SerializeField] private GameObject informationModal;
    [SerializeField] private TMP_Text speciesNameText;
    [SerializeField] private TMP_Text speciesDataText;

    [Header("Sea Species")]
    [SerializeField] private SpeciesData dolphinSpecies;
    [SerializeField] private SpeciesData whaleSpecies;
    [SerializeField] private SpeciesData turtleSpecies;

    [Header("Spawn Settings")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private bool allowOnlyOne = true;

    [Header("Interaction Settings")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float pinchScaleSpeed = 0.001f;

    private SpeciesData selectedSpecies;
    private GameObject selectedPrefab;
    private GameObject spawnedObject;

    private float dragDepth;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (informationModal != null)
            informationModal.SetActive(false);

        UpdateHintText();
    }

    public void SelectDolphin()
    {
        selectedSpecies = dolphinSpecies;
        selectedPrefab = dolphinSpecies.prefab;
        UpdateHintText();
    }

    public void SelectWhale()
    {
        selectedSpecies = whaleSpecies;
        selectedPrefab = whaleSpecies.prefab;
        UpdateHintText();
    }

    public void SelectTurtle()
    {
        selectedSpecies = turtleSpecies;
        selectedPrefab = turtleSpecies.prefab;
        UpdateHintText();
    }

    void Update()
    {
        // Tap-to-place behaviour restored
        if (selectedPrefab != null && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                SpawnSelected();
            }
        }

        if (spawnedObject == null || targetCamera == null)
            return;

        HandleDrag();
        HandlePinchZoom();
        HandleRotation();
    }

    void SpawnSelected()
    {
        if (selectedPrefab == null)
            return;

        Vector3 spawnPosition =
            targetCamera.transform.position +
            targetCamera.transform.forward * distanceFromCamera;

        Quaternion spawnRotation =
            Quaternion.LookRotation(-targetCamera.transform.forward);

        if (allowOnlyOne && spawnedObject != null)
        {
            spawnedObject.transform.SetPositionAndRotation(
                spawnPosition,
                spawnRotation
            );
        }
        else
        {
            spawnedObject = Instantiate(
                selectedPrefab,
                spawnPosition,
                spawnRotation
            );
        }

        dragDepth = Vector3.Distance(
            targetCamera.transform.position,
            spawnedObject.transform.position
        );

        // Clear prefab but keep species for info modal
        selectedPrefab = null;

        UpdateHintText();
    }

    void HandleDrag()
    {
        if (Input.touchCount != 1 || spawnedObject == null)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            Vector3 screenPoint =
                new Vector3(
                    touch.position.x,
                    touch.position.y,
                    dragDepth
                );

            Vector3 worldPoint =
                targetCamera.ScreenToWorldPoint(screenPoint);

            spawnedObject.transform.position = worldPoint;
        }
    }

    void HandlePinchZoom()
    {
        if (Input.touchCount != 2 || spawnedObject == null)
            return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        float prevDist =
            ((t1.position - t1.deltaPosition) -
             (t2.position - t2.deltaPosition)).magnitude;

        float currDist =
            (t1.position - t2.position).magnitude;

        float scaleFactor =
            (currDist - prevDist) * pinchScaleSpeed;

        spawnedObject.transform.localScale +=
            Vector3.one * scaleFactor;

        spawnedObject.transform.localScale =
            Vector3.Max(
                Vector3.one * minScale,
                Vector3.Min(
                    Vector3.one * maxScale,
                    spawnedObject.transform.localScale
                )
            );
    }

    void HandleRotation()
    {
        if (Input.touchCount != 2 || spawnedObject == null)
            return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        Vector2 prevDir =
            (t1.position - t1.deltaPosition) -
            (t2.position - t2.deltaPosition);

        Vector2 currDir =
            t1.position - t2.position;

        float angle =
            Vector2.SignedAngle(prevDir, currDir);

        spawnedObject.transform.Rotate(
            0f,
            -angle,
            0f,
            Space.World
        );
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

        speciesNameText.text =
            selectedSpecies.speciesName;

        speciesDataText.text =
            selectedSpecies.educationalInfo;

        informationModal.SetActive(true);
    }

    public void CloseSpeciesInfo()
    {
        if (informationModal != null)
            informationModal.SetActive(false);
    }

    public void DeleteSpawned()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
        }

        selectedSpecies = null;
        selectedPrefab = null;

        UpdateHintText();
    }

    void UpdateHintText()
    {
        if (placementHint == null)
            return;

        if (selectedPrefab == null || selectedSpecies == null)
            placementHint.text = "";
        else
            placementHint.text =
                $"Tap anywhere to place {selectedSpecies.speciesName}";
    }
}