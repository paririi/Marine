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
    [Header("Main Scan UI")]
    [SerializeField] private GameObject scanPanel;
    [SerializeField] private GameObject topBarPanel;
    [SerializeField] private TMP_Text scanTitleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text topBarText;
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

    [Header("Environment Detection Model")]
    [SerializeField] private EnvironmentMLClassifier mlClassifier;

    [Header("AR Plane")] //To turn on only after successful scan
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Loading Animation")]
    [SerializeField] private float loadingSpinSpeed = 180f;
    [SerializeField] private float dotsAnimationSpeed = 0.5f;

    [Header("UI Elements Timing")]
    [SerializeField] private float scanDuration = 4f;
    [SerializeField] private float firstErrorModalDuration = 3f;
    [SerializeField] private float secondErrorModalDuration = 3f;
    [SerializeField] private float successModalDuration = 2f;

    [Header("Scan Settings")]
    [SerializeField] private int maxScanAttempts = 2;
    [SerializeField][Range(0f, 1f)] private float confidenceThreshold = 0.75f;

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

        if (topBarText != null)
            topBarText.text = "Detected: Unknown";

        if (waterSpeciesPanel != null)
            waterSpeciesPanel.SetActive(false);

        if (sandSpeciesPanel != null)
            sandSpeciesPanel.SetActive(false);

        if (manualSelectionPanel != null)
            manualSelectionPanel.SetActive(false);

        if (successModal != null)
            successModal.SetActive(false);

        if (errorModal != null)
            errorModal.SetActive(false);

        StartScan();
    }

    private void Update()
    {
        if (isScanning && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -loadingSpinSpeed * Time.deltaTime);
    }

    public void StartScan() 
    {
        if (isScanning)
            return;

        currentScanAttempts++;
        isScanning = true;

        if (scanPanel != null)
            scanPanel.SetActive(true);

        if (topBarPanel != null)
            topBarPanel.SetActive(false);

        if (successModal != null)
            successModal.SetActive(false);

        if (errorModal != null)
            errorModal.SetActive(false);

        if (scanTitleText != null)
            scanTitleText.text = ScanningBaseText;

        if (infoText != null)
            infoText.text = "Move your phone around slowly";

        if (rescanButton != null)
            rescanButton.gameObject.SetActive(false);

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
            {
                string dots = new string('.', dotCount);
                scanTitleText.text = ScanningBaseText + dots;
            }

            dotCount++;
            if (dotCount > 3)
                dotCount = 0;

            yield return new WaitForSeconds(dotsAnimationSpeed);
        }
    }
   
    private void GetScanPrediction(out EnvironmentType predictedEnvironment, out float confidence) 
    {
        if (mlClassifier != null)
        {
            bool success = mlClassifier.TryPredictFromCamera(out predictedEnvironment, out confidence);
            if (success)
                return;
        }

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
        {
            StartCoroutine(HandleLowConfidenceFailure(predictedEnvironment, confidence));
        }
        else
        {
            StartCoroutine(HandleManualSelectionFallback());
        }
    }

    private IEnumerator HandleSuccess(EnvironmentType predictedEnvironment, float confidence)
    {
        if (loadingIcon != null)
            loadingIcon.gameObject.SetActive(false);

        if (successModal != null)
        {
            successModal.SetActive(true);

            if (successModalText != null)
                successModalText.text = $"Environment Detected!\n{predictedEnvironment} ({confidence:P0})";
        }

        SetEnvironment(predictedEnvironment);

        // Enable AR plane detection for the detected environment (had to put this so that it does not show during scanning)
        if (planeManager != null)
            planeManager.enabled = true;


        // Wait for a moment to let users see the success message before switching to the main UI
        yield return new WaitForSeconds(successModalDuration);

        if (successModal != null)
            successModal.SetActive(false);

        if (scanPanel != null)
            scanPanel.SetActive(false);

        if (topBarPanel != null)
            topBarPanel.SetActive(true);
    }

    // This handles the case where the model gives an "unknown" prediction, meaning it couldn't identify a known environment at all (as opposed to giving a low confidence prediction for a specific environment)
    private IEnumerator HandleUnknownEnvironment()
    {
        if (loadingIcon != null)
            loadingIcon.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text = "Unknown environment detected.\nPlease scan a sand or sea surface.";
        }

        yield return new WaitForSeconds(firstErrorModalDuration);

        // Hide error modal but keep user on scan screen to try again
        if (errorModal != null)
            errorModal.SetActive(false);

        if (scanTitleText != null)
            scanTitleText.text = "Scan Failed";

        if (infoText != null)
            infoText.text = "Please scan a sand or sea surface";

        if (rescanButton != null)
            rescanButton.gameObject.SetActive(true);
    }

    // This handles cases where the model gives a prediction but it's not confident enough (low confidence basically)
    private IEnumerator HandleLowConfidenceFailure(EnvironmentType predictedEnvironment, float confidence)
    {
        if (loadingIcon != null)
            loadingIcon.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text =
                    $"Could not confidently detect environment.\nResult: {predictedEnvironment} ({confidence:P0})";
        }

        yield return new WaitForSeconds(firstErrorModalDuration);

        if (errorModal != null)
            errorModal.SetActive(false);

        if (scanTitleText != null)
            scanTitleText.text = "Scan Failed";

        if (infoText != null)
            infoText.text = "Move camera closer to the surface and try again";

        if (rescanButton != null)
            rescanButton.gameObject.SetActive(true);
    }

    // This handles the case where after max attempts, we still don't have a confident prediction, so we ask the user to choose manually
    private IEnumerator HandleManualSelectionFallback()
    {
        if (loadingIcon != null)
            loadingIcon.gameObject.SetActive(false);

        if (rescanButton != null)
            rescanButton.gameObject.SetActive(false);

        if (errorModal != null)
        {
            errorModal.SetActive(true);

            if (errorModalText != null)
                errorModalText.text = "Unable to confidently detect environment.\nPlease choose the environment manually.";
        }

        yield return new WaitForSeconds(secondErrorModalDuration);

        if (errorModal != null)
            errorModal.SetActive(false);

        if (scanPanel != null)
            scanPanel.SetActive(false);

        if (topBarPanel != null)
            topBarPanel.SetActive(true);

        CurrentEnvironment = EnvironmentType.Unknown;

        if (topBarText != null)
            topBarText.text = "Detected: Unknown";

        if (waterSpeciesPanel != null)
            waterSpeciesPanel.SetActive(false);

        if (sandSpeciesPanel != null)
            sandSpeciesPanel.SetActive(false);

        if (manualSelectionPanel != null)
            manualSelectionPanel.SetActive(true);
    }
    
    public void SetEnvironment(EnvironmentType environment)
    {
        CurrentEnvironment = environment;

        if (topBarText != null)
            topBarText.text = $"Detected: {environment}";

        if (waterSpeciesPanel != null)
            waterSpeciesPanel.SetActive(environment == EnvironmentType.Water);

        if (sandSpeciesPanel != null)
            sandSpeciesPanel.SetActive(environment == EnvironmentType.Sand);

        if (manualSelectionPanel != null)
            manualSelectionPanel.SetActive(false);
    }

    public void ChooseSandManually()
    {
        SetEnvironment(EnvironmentType.Sand);

        if (scanPanel != null)
            scanPanel.SetActive(false);

        if (topBarPanel != null)
            topBarPanel.SetActive(true);

        if (planeManager != null) 
            planeManager.enabled = true;
    }

    public void ChooseWaterManually()
    {
        SetEnvironment(EnvironmentType.Water);

        if (scanPanel != null) 
            scanPanel.SetActive(false);

        if (topBarPanel != null)
            topBarPanel.SetActive(true);
    }
}