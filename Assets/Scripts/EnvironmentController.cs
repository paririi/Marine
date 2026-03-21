using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public enum EnvironmentType
{
    Unknown,
    Sand,
    Water
}

public class EnvironmentController : MonoBehaviour
{
    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionModal;
    [SerializeField] private GameObject sandPlacementInstructionModal;

    [Header("Main Scan UI")]
    [SerializeField] private GameObject scanPanel;
    [SerializeField] private TMP_Text scanTitleText;
    [SerializeField] private GameObject hintBox;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button rescanButton;
    [SerializeField] private RectTransform loadingIcon;

    [Header("Result Modals")]
    [SerializeField] private GameObject successModal;
    [SerializeField] private TMP_Text successModalText;
    [SerializeField] private GameObject errorModal;
    [SerializeField] private TMP_Text errorModalText;

    [Header("Species Panels")]
    [SerializeField] private GameObject waterSpeciesPanel;
    [SerializeField] private GameObject sandSpeciesPanel;
    [SerializeField] private GameObject manualSelectionPanel;

    [Header("Options Menu")]
    [SerializeField] private GameObject optionsMenu;

    [Header("Environment Detection Model")]
    [SerializeField] private EnvironmentMLClassifier mlClassifier;

    [Header("AR Plane")]
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Loading Animation")]
    [SerializeField] private float loadingSpinSpeed = 180f;
    [SerializeField] private float dotsAnimationSpeed = 0.5f;

    [Header("UI Elements Timing")]
    [SerializeField] private float scanDuration = 3.5f;
    [SerializeField] private float firstErrorModalDuration = 3f;
    [SerializeField] private float secondErrorModalDuration = 3f;
    [SerializeField] private float successModalDuration = 2f;

    [Header("Scan Settings")]
    [SerializeField] private int maxScanAttempts = 2;
    [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.75f;

    public EnvironmentType CurrentEnvironment { get; private set; } = EnvironmentType.Unknown;

    private int currentScanAttempts = 0;
    private bool isScanning = false;
    private Coroutine dotsCoroutine;
    private Coroutine scanRoutineCoroutine;

    private const string ScanningBaseText = "Scanning Environment";

    private void Start()
    {
        if (rescanButton != null)
            rescanButton.onClick.AddListener(OnRescanPressed);

        if (mlClassifier == null)
            mlClassifier = Object.FindFirstObjectByType<EnvironmentMLClassifier>();

        CurrentEnvironment = EnvironmentType.Unknown;

        scanPanel?.SetActive(false);
        instructionModal?.SetActive(true);

        waterSpeciesPanel?.SetActive(false);
        sandSpeciesPanel?.SetActive(false);
        manualSelectionPanel?.SetActive(false);
        successModal?.SetActive(false);
        errorModal?.SetActive(false);
        optionsMenu?.SetActive(false);
        hintBox?.SetActive(false);

        SetPlaneState(false);
    }

    private void Update()
    {
        if (isScanning && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -loadingSpinSpeed * Time.deltaTime);
    }

    private void SetPlaneState(bool enabledState)
    {
        if (planeManager == null) return;

        planeManager.enabled = enabledState;

        foreach (var plane in planeManager.trackables)
        {
            if (plane != null)
                plane.gameObject.SetActive(enabledState);
        }
    }

    private void UpdateOptionsMenuVisibility()
    {
        if (optionsMenu == null) return;

        bool shouldShow =
            (waterSpeciesPanel != null && waterSpeciesPanel.activeSelf) ||
            (sandSpeciesPanel != null && sandSpeciesPanel.activeSelf);

        optionsMenu.SetActive(shouldShow);
    }

    public void CloseInstructionAndStartScan()
    {
        instructionModal?.SetActive(false);
        StartScan();
    }

    public void StartScan()
    {
        if (isScanning) return;

        currentScanAttempts++;
        isScanning = true;
        CurrentEnvironment = EnvironmentType.Unknown;

        scanPanel?.SetActive(true);
        successModal?.SetActive(false);
        errorModal?.SetActive(false);
        waterSpeciesPanel?.SetActive(false);
        sandSpeciesPanel?.SetActive(false);
        manualSelectionPanel?.SetActive(false);
        optionsMenu?.SetActive(false);
        hintBox?.SetActive(false);

        SetPlaneState(false);

        if (scanTitleText != null)
            scanTitleText.text = ScanningBaseText;

        if (hintText != null)
            hintText.text = string.Empty;

        rescanButton?.gameObject.SetActive(false);

        if (loadingIcon != null)
        {
            loadingIcon.gameObject.SetActive(true);
            loadingIcon.localRotation = Quaternion.identity;
        }

        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);

        dotsCoroutine = StartCoroutine(AnimateLoadingDots());

        if (scanRoutineCoroutine != null)
            StopCoroutine(scanRoutineCoroutine);

        scanRoutineCoroutine = StartCoroutine(ScanRoutine());
    }

    private void OnRescanPressed()
    {
        StartScan();
    }

    private IEnumerator ScanRoutine()
    {
        yield return new WaitForSeconds(scanDuration);

        EnvironmentType predictedEnvironment;
        float confidence;

        GetScanPrediction(out predictedEnvironment, out confidence);

        isScanning = false;

        if (dotsCoroutine != null)
        {
            StopCoroutine(dotsCoroutine);
            dotsCoroutine = null;
        }

        HandleScanResult(predictedEnvironment, confidence);
    }

    private IEnumerator AnimateLoadingDots()
    {
        int dotCount = 0;

        while (isScanning)
        {
            if (scanTitleText != null)
                scanTitleText.text = ScanningBaseText + new string('.', dotCount);

            dotCount = (dotCount + 1) % 4;

            yield return new WaitForSeconds(dotsAnimationSpeed);
        }
    }

    private void GetScanPrediction(out EnvironmentType predictedEnvironment, out float confidence)
    {
        if (mlClassifier != null &&
            mlClassifier.TryPredictFromCamera(out predictedEnvironment, out confidence))
            return;

        predictedEnvironment = EnvironmentType.Unknown;
        confidence = 0f;
    }

    private void HandleScanResult(EnvironmentType predictedEnvironment, float confidence)
    {
        if (predictedEnvironment == EnvironmentType.Unknown)
        {
            StartCoroutine(HandleUnknownEnvironment());
            return;
        }

        bool validEnvironment =
            predictedEnvironment == EnvironmentType.Sand ||
            predictedEnvironment == EnvironmentType.Water;

        bool confidentEnough = confidence >= confidenceThreshold;

        if (validEnvironment && confidentEnough)
        {
            StartCoroutine(HandleSuccess(predictedEnvironment, confidence));
            return;
        }

        if (currentScanAttempts < maxScanAttempts)
            StartCoroutine(HandleLowConfidenceFailure(predictedEnvironment, confidence));
        else
            StartCoroutine(HandleManualSelectionFallback());
    }

    private IEnumerator HandleSuccess(EnvironmentType predictedEnvironment, float confidence)
    {
        loadingIcon?.gameObject.SetActive(false);
        hintBox?.SetActive(false);

        // Hide scanning title immediately
        if (scanTitleText != null)
            scanTitleText.gameObject.SetActive(false);

        if (successModal != null)
        {
            successModal.SetActive(true);

            if (successModalText != null)
                successModalText.text = $"Environment Detected!\n{predictedEnvironment} ({confidence:P0})";
        }

        SetEnvironment(predictedEnvironment);

        yield return new WaitForSeconds(successModalDuration);

        successModal?.SetActive(false);
        scanPanel?.SetActive(false);
    }

    private IEnumerator HandleUnknownEnvironment()
    {
        loadingIcon?.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text = "Unknown environment detected.\nPlease scan a sand or sea surface.";
        }

        yield return new WaitForSeconds(firstErrorModalDuration);

        errorModal?.SetActive(false);

        if (scanTitleText != null)
            scanTitleText.text = "Scan Failed";

        hintBox?.SetActive(true);

        if (hintText != null)
            hintText.text = "Please scan a sand or sea surface";

        rescanButton?.gameObject.SetActive(true);

        SetPlaneState(false);
    }

    private IEnumerator HandleLowConfidenceFailure(EnvironmentType predictedEnvironment, float confidence)
    {
        loadingIcon?.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text =
                    $"Could not confidently detect environment.\nResult: {predictedEnvironment} ({confidence:P0})";
        }

        yield return new WaitForSeconds(firstErrorModalDuration);

        errorModal?.SetActive(false);

        if (scanTitleText != null)
            scanTitleText.text = "Scan Failed";

        hintBox?.SetActive(true);

        if (hintText != null)
            hintText.text = "Move camera closer to the surface and try again";

        rescanButton?.gameObject.SetActive(true);

        SetPlaneState(false);
    }

    private IEnumerator HandleManualSelectionFallback()
    {
        loadingIcon?.gameObject.SetActive(false);
        hintBox?.SetActive(false);
        rescanButton?.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text = "Unable to confidently detect environment.\nPlease choose the environment manually.";
        }

        yield return new WaitForSeconds(secondErrorModalDuration);

        errorModal?.SetActive(false);
        scanPanel?.SetActive(false);

        waterSpeciesPanel?.SetActive(false);
        sandSpeciesPanel?.SetActive(false);
        manualSelectionPanel?.SetActive(true);

        SetPlaneState(false);
    }

    public void SetEnvironment(EnvironmentType environment)
    {
        CurrentEnvironment = environment;

        waterSpeciesPanel?.SetActive(environment == EnvironmentType.Water);
        sandSpeciesPanel?.SetActive(environment == EnvironmentType.Sand);
        manualSelectionPanel?.SetActive(false);

        UpdateOptionsMenuVisibility();
        SetPlaneState(environment == EnvironmentType.Sand);

        if (environment == EnvironmentType.Sand && sandPlacementInstructionModal != null)
        {
            sandPlacementInstructionModal.SetActive(true);
        }
    }

    public void ChooseSandManually()
    {
        SetEnvironment(EnvironmentType.Sand);
        scanPanel?.SetActive(false);
    }

    public void ChooseWaterManually()
    {
        SetEnvironment(EnvironmentType.Water);
        scanPanel?.SetActive(false);
    }
}