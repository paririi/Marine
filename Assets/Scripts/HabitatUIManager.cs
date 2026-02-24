using UnityEngine;
using TMPro;

public enum Habitat
{
    Unknown,
    Sand,
    Water
}

public class HabitatUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject waterSpeciesPanel;
    [SerializeField] private GameObject sandSpeciesPanel;
    [SerializeField] private GameObject manualSelectionPanel;

    [Header("Top Status Text")]
    [SerializeField] private TMP_Text detectedText;

    public Habitat CurrentHabitat { get; private set; } = Habitat.Unknown;

    void Start()
    {
        SetHabitat(Habitat.Unknown);
    }

    // Later, Lightship scanner will call this automatically
    public void SetHabitat(Habitat habitat)
    {
        CurrentHabitat = habitat;

        if (detectedText != null)
            detectedText.text = $"Detected: {habitat}";

        if (waterSpeciesPanel != null)
            waterSpeciesPanel.SetActive(habitat == Habitat.Water);

        if (sandSpeciesPanel != null)
            sandSpeciesPanel.SetActive(habitat == Habitat.Sand);

        if (manualSelectionPanel != null)
            manualSelectionPanel.SetActive(habitat == Habitat.Unknown);
    }

    // Buttons in Manual Selection call these
    public void ChooseSandManually() => SetHabitat(Habitat.Sand);
    public void ChooseWaterManually() => SetHabitat(Habitat.Water);
}
