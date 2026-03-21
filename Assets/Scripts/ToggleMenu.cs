using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OptionsToggleMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private RectTransform informationButton;
    [SerializeField] private RectTransform deleteButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private CanvasGroup informationCanvasGroup;
    [SerializeField] private CanvasGroup deleteCanvasGroup;

    [Header("Option Icon")]
    [SerializeField] private RectTransform optionIconTransform;
    [SerializeField] private Image optionIconImage;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Positions")]
    [SerializeField] private Vector2 closedInfoPosition;
    [SerializeField] private Vector2 openInfoPosition;
    [SerializeField] private Vector2 closedDeletePosition;
    [SerializeField] private Vector2 openDeletePosition;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.26f;
    [SerializeField] private float rotationAngle = 180f;

    [Header("Panel References")]
    [SerializeField] private GameObject sandSpeciesPanel;
    [SerializeField] private GameObject waterSpeciesPanel;

    [Header("Placement References")]
    [SerializeField] private SandSpeciesPlacement sandSpeciesPlacement;
    [SerializeField] private SeaSpeciesPlacement seaSpeciesPlacement;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;

    private void Start()
    {
        if (optionButton != null)
            optionButton.onClick.AddListener(ToggleMenu);

        SetMenuInstant(false);
    }

    public void ToggleMenu()
    {
        if (isAnimating) return;

        isOpen = !isOpen;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateMenu(isOpen));
    }

    public void OnInformationPressed()
    {
        if (sandSpeciesPanel != null && sandSpeciesPanel.activeSelf && sandSpeciesPlacement != null)
        {
            sandSpeciesPlacement.OpenSpeciesInfo();
        }
        else if (waterSpeciesPanel != null && waterSpeciesPanel.activeSelf && seaSpeciesPlacement != null)
        {
            seaSpeciesPlacement.OpenSpeciesInfo();
        }

        CloseMenu();
    }

    public void OnDeletePressed()
    {
        if (sandSpeciesPanel != null && sandSpeciesPanel.activeSelf && sandSpeciesPlacement != null)
        {
            sandSpeciesPlacement.DeleteSpawned();
        }
        else if (waterSpeciesPanel != null && waterSpeciesPanel.activeSelf && seaSpeciesPlacement != null)
        {
            seaSpeciesPlacement.DeleteSpawned();
        }

        CloseMenu();
    }

    public void CloseMenu()
    {
        if (!isOpen || isAnimating) return;

        isOpen = false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateMenu(false));
    }

    private IEnumerator AnimateMenu(bool open)
    {
        isAnimating = true;

        Vector2 startInfoPos = informationButton.anchoredPosition;
        Vector2 startDeletePos = deleteButton.anchoredPosition;

        Vector2 targetInfoPos = open ? openInfoPosition : closedInfoPosition;
        Vector2 targetDeletePos = open ? openDeletePosition : closedDeletePosition;

        float startZ = optionIconTransform.localEulerAngles.z;
        float targetZ = open ? startZ - rotationAngle : startZ + rotationAngle;

        float startInfoAlpha = informationCanvasGroup.alpha;
        float startDeleteAlpha = deleteCanvasGroup.alpha;

        float targetAlpha = open ? 1f : 0f;

        if (open)
        {
            informationCanvasGroup.blocksRaycasts = true;
            informationCanvasGroup.interactable = true;
            deleteCanvasGroup.blocksRaycasts = true;
            deleteCanvasGroup.interactable = true;
        }

        optionIconImage.sprite = open ? openSprite : closedSprite;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            informationButton.anchoredPosition = Vector2.Lerp(startInfoPos, targetInfoPos, t);
            deleteButton.anchoredPosition = Vector2.Lerp(startDeletePos, targetDeletePos, t);

            informationCanvasGroup.alpha = Mathf.Lerp(startInfoAlpha, targetAlpha, t);
            deleteCanvasGroup.alpha = Mathf.Lerp(startDeleteAlpha, targetAlpha, t);

            float z = Mathf.LerpAngle(startZ, targetZ, t);
            optionIconTransform.localRotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        informationButton.anchoredPosition = targetInfoPos;
        deleteButton.anchoredPosition = targetDeletePos;
        informationCanvasGroup.alpha = targetAlpha;
        deleteCanvasGroup.alpha = targetAlpha;
        optionIconTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);

        if (!open)
        {
            informationCanvasGroup.blocksRaycasts = false;
            informationCanvasGroup.interactable = false;
            deleteCanvasGroup.blocksRaycasts = false;
            deleteCanvasGroup.interactable = false;
        }

        isAnimating = false;
    }

    private void SetMenuInstant(bool open)
    {
        isOpen = open;

        informationButton.anchoredPosition = open ? openInfoPosition : closedInfoPosition;
        deleteButton.anchoredPosition = open ? openDeletePosition : closedDeletePosition;

        informationCanvasGroup.alpha = open ? 1f : 0f;
        deleteCanvasGroup.alpha = open ? 1f : 0f;

        informationCanvasGroup.blocksRaycasts = open;
        informationCanvasGroup.interactable = open;
        deleteCanvasGroup.blocksRaycasts = open;
        deleteCanvasGroup.interactable = open;

        optionIconImage.sprite = open ? openSprite : closedSprite;
        optionIconTransform.localRotation = Quaternion.identity;
    }
}