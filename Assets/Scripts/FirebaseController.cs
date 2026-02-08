using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using TMPro;

public class FirebaseController : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;

    [Header("Login UI")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;

    [Header("Register UI")]
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField confirmPassword;

    [Header("Feedback")]
    public TMP_Text feedbackText;

    [Header("Panels")]
    public GameObject welcomePanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        ShowWelcome();
    }

    // ---------------- PANEL NAVIGATION ----------------

    public void ShowWelcome()
    {
        welcomePanel.SetActive(true);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
    }

    public void ShowLogin()
    {
        welcomePanel.SetActive(false);
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
    }

    public void ShowRegister()
    {
        welcomePanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    // ---------------- LOGIN ----------------

    public void Login()
    {
        string email = loginEmail.text;
        string password = loginPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    feedbackText.text = "Login failed. Please check your details.";
                    return;
                }

                feedbackText.text = "Login successful!";
                SceneManager.LoadScene("Home");
            });
    }

    // ---------------- REGISTER ----------------

    public void Register()
    {
        if (registerPassword.text != confirmPassword.text)
        {
            feedbackText.text = "Passwords do not match.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(
            registerEmail.text,
            registerPassword.text
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                feedbackText.text = "Registration failed.";
                return;
            }

            feedbackText.text = "Account created! Please login.";
            ShowLogin();
        });
    }
}
