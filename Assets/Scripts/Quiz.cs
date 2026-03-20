using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject questionsPanel;
    [SerializeField] private GameObject explanationPanel;   // dark overlay parent
    [SerializeField] private GameObject rightAnswerModal;
    [SerializeField] private GameObject wrongAnswerModal;
    [SerializeField] private GameObject resultsPanel;

    [Header("Difficulty Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    [Header("Question UI")]
    [SerializeField] private TMP_Text questionCounterText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons;       // size = 3
    [SerializeField] private TMP_Text[] optionButtonTexts; // size = 3
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;

    [Header("Right Answer Modal UI")]
    [SerializeField] private TMP_Text rightModalTitleText;
    [SerializeField] private TMP_Text rightModalExplanationText;

    [Header("Wrong Answer Modal UI")]
    [SerializeField] private TMP_Text wrongModalTitleText;
    [SerializeField] private TMP_Text wrongModalExplanationText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [Header("Result UI")]
    [SerializeField] private TMP_Text scoreTitleText;      // "- Your Score -" or "New High Score!"
    [SerializeField] private TMP_Text finalScoreText;      // current score, e.g. 2/3
    [SerializeField] private TMP_Text bestScoreText;       // best score, e.g. 1/3
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button changeDifficultyButton;

    [Header("Result Images")]
    [SerializeField] private GameObject excellentResultImages;
    [SerializeField] private GameObject goodResultImages;
    [SerializeField] private GameObject badResultImages;

    private List<QuizQuestion> allQuestions = new List<QuizQuestion>();
    private List<QuizQuestion> currentQuestions = new List<QuizQuestion>();

    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private int previousBestScore = 0;
    private int selectedAnswerIndex = -1;
    private string currentDifficulty = "";
    private bool canAnswer = true;

    private void Start()
    {
        LoadQuestions();
        HookButtons();
        ShowDifficultyPanel();
    }

    private void HookButtons()
    {
        easyButton.onClick.AddListener(() => StartQuiz("Easy"));
        mediumButton.onClick.AddListener(() => StartQuiz("Medium"));
        hardButton.onClick.AddListener(() => StartQuiz("Hard"));

        backButton.onClick.AddListener(BackToDifficultyPanel);
        nextButton.onClick.AddListener(GoToNextQuestion);

        tryAgainButton.onClick.AddListener(RestartSameDifficulty);
        changeDifficultyButton.onClick.AddListener(ShowDifficultyPanel);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => SelectAnswer(index));
        }
    }

    private void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("questions");
        QuizQuestionList loadedData = JsonUtility.FromJson<QuizQuestionList>(jsonFile.text);
        allQuestions = loadedData.questions;
    }

    public void StartQuiz(string difficulty)
    {
        currentDifficulty = difficulty;

        scoreTitleText.text = "- Your Score -";

        currentQuestions = allQuestions
            .Where(q => q.difficulty == difficulty)
            .ToList();

        ShuffleQuestions(currentQuestions);

        currentQuestionIndex = 0;
        correctAnswers = 0;
        previousBestScore = LoadBestScore(currentDifficulty);
        selectedAnswerIndex = -1;
        canAnswer = true;

        difficultyPanel.SetActive(false);
        questionsPanel.SetActive(true);
        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);
        resultsPanel.SetActive(false);

        nextButton.gameObject.SetActive(false);
        nextButton.interactable = false;

        ShowQuestion();
    }

    private void ShuffleQuestions(List<QuizQuestion> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            QuizQuestion temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void ShowDifficultyPanel()
    {
        difficultyPanel.SetActive(true);
        questionsPanel.SetActive(false);
        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);
        resultsPanel.SetActive(false);

        HideResultImages();
    }

    private void BackToDifficultyPanel()
    {
        currentDifficulty = "";
        currentQuestionIndex = 0;
        correctAnswers = 0;
        selectedAnswerIndex = -1;
        canAnswer = true;

        // Hide everything quiz-related
        questionsPanel.SetActive(false);
        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);
        resultsPanel.SetActive(false);

        nextButton.gameObject.SetActive(false);
        nextButton.interactable = false;

        // Go back to difficulty selection
        ShowDifficultyPanel();
    }

    private void ShowQuestion()
    {
        selectedAnswerIndex = -1;
        canAnswer = true;

        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);

        nextButton.gameObject.SetActive(false);
        nextButton.interactable = false;

        QuizQuestion currentQuestion = currentQuestions[currentQuestionIndex];

        questionCounterText.text = $"Question {currentQuestionIndex + 1}/{currentQuestions.Count}";
        questionText.text = currentQuestion.question;

        for (int i = 0; i < 3; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].interactable = true;
            optionButtonTexts[i].text = currentQuestion.options[i];
        }
    }

    private void SelectAnswer(int answerIndex)
    {
        if (!canAnswer || selectedAnswerIndex != -1)
            return;

        selectedAnswerIndex = answerIndex;
        canAnswer = false;

        QuizQuestion currentQuestion = currentQuestions[currentQuestionIndex];
        bool isCorrect = answerIndex == currentQuestion.correctAnswerIndex;

        foreach (Button button in optionButtons)
        {
            button.interactable = false;
        }

        explanationPanel.SetActive(true);

        if (isCorrect)
        {
            correctAnswers++;

            rightModalTitleText.text = "Correct!";
            rightModalExplanationText.text = currentQuestion.correctExplanation;

            rightAnswerModal.SetActive(true);
            wrongAnswerModal.SetActive(false);

            audioSource.PlayOneShot(correctSound);

            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
        else
        {
            wrongModalTitleText.text = "Not quite!";
            wrongModalExplanationText.text = currentQuestion.wrongExplanation;

            rightAnswerModal.SetActive(false);
            wrongAnswerModal.SetActive(true);

            audioSource.PlayOneShot(wrongSound);

            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    public void GoToNextQuestion()
    {
        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);

        nextButton.gameObject.SetActive(false);
        nextButton.interactable = false;

        currentQuestionIndex++;

        if (currentQuestionIndex >= currentQuestions.Count)
        {
            ShowResults();
        }
        else
        {
            ShowQuestion();
        }
    }

    private void ShowResults()
    {
        questionsPanel.SetActive(false);
        explanationPanel.SetActive(false);
        rightAnswerModal.SetActive(false);
        wrongAnswerModal.SetActive(false);
        resultsPanel.SetActive(true);

        int oldBestScore = previousBestScore;
        int currentScore = correctAnswers;

        bool isNewHighScore = currentScore > oldBestScore;

        if (isNewHighScore)
        {
            scoreTitleText.text = "New High Score!";
            SaveBestScore(currentDifficulty, currentScore);
            previousBestScore = currentScore;
        }
        else
        {
            scoreTitleText.text = "- Your Score -";
        }

        finalScoreText.text = $"{currentScore}/{currentQuestions.Count}";
        bestScoreText.text = $"{oldBestScore}/{currentQuestions.Count}";

        ShowCorrectResultImage();
    }

    private void ShowCorrectResultImage()
    {
        HideResultImages();

        float percentage = (float)correctAnswers / currentQuestions.Count;

        if (percentage >= 0.8f)
            excellentResultImages.SetActive(true);
        else if (percentage >= 0.5f)
            goodResultImages.SetActive(true);
        else
            badResultImages.SetActive(true);
    }

    private void HideResultImages()
    {
        excellentResultImages.SetActive(false);
        goodResultImages.SetActive(false);
        badResultImages.SetActive(false);
    }

    private void RestartSameDifficulty()
    {
        StartQuiz(currentDifficulty);
    }

    private string GetBestScoreKey(string difficulty)
    {
        return $"BestScore_{difficulty}";
    }

    private int LoadBestScore(string difficulty)
    {
        return PlayerPrefs.GetInt(GetBestScoreKey(difficulty), 0);
    }

    private void SaveBestScore(string difficulty, int score)
    {
        PlayerPrefs.SetInt(GetBestScoreKey(difficulty), score);
        PlayerPrefs.Save();
    }

    public void ResetAllBestScores()
    {
        PlayerPrefs.DeleteKey("BestScore_Easy");
        PlayerPrefs.DeleteKey("BestScore_Medium");
        PlayerPrefs.DeleteKey("BestScore_Hard");
        PlayerPrefs.Save();
    }
}