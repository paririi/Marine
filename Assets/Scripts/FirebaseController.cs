using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class FirebaseController : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;

    // ---------------- LOGIN UI ----------------
    [Header("Login UI")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;

    // ---------------- REGISTER UI ----------------
    [Header("Register UI")]
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField confirmPassword;

    [Header("Register Feedback Texts")]
    public TMP_Text passwordMismatchText;   // under confirm password
    public TMP_Text registerErrorText;       // under sign up button

    // ---------------- PANELS ----------------
    [Header("Main Panels")]
    public GameObject splashPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    // ---------------- MODALS ----------------
    [Header("Login Modals")]
    public GameObject loginSuccessModal;
    public GameObject loginErrorModal;
    public TMP_Text loginErrorMessage;

    [Header("Register Modals")]
    public GameObject registerSuccessModal;
    public GameObject registerErrorModal;
    public TMP_Text registerErrorModalMessage;

    void Start()
    {
        // Set Database URL to remove warning (even if you don't use DB)
        FirebaseApp.DefaultInstance.Options.DatabaseUrl =
            new Uri("https://marine-5b4b0-default-rtdb.asia-southeast1.firebasedatabase.app/");

        // Initialize Firebase Auth
        auth = FirebaseAuth.DefaultInstance;

        // Show initial panel
        ShowSplash();

        // Clear inline texts
        passwordMismatchText.text = "";
        registerErrorText.text = "";
    }

    // ---------------- PANEL NAVIGATION ----------------
    public void ShowSplash()
    {
        splashPanel.SetActive(true);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        CloseAllModals();
    }

    public void ShowLogin()
    {
        splashPanel.SetActive(false);
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        CloseAllModals();
    }

    public void ShowRegister()
    {
        splashPanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        CloseAllModals();

        passwordMismatchText.text = "";
        registerErrorText.text = "";
    }

    // ---------------- LOGIN ----------------
    public void Login()
    {
        string email = loginEmail.text.Trim();
        string password = loginPassword.text.Trim();

        // Check for empty fields
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Please fill in all fields.");
            return;
        }

        // Firebase sign-in
        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowLoginError(task.Exception.GetBaseException().Message);
                    return;
                }

                // Show success modal
                loginSuccessModal.SetActive(true);
            });
    }

    // Called when user confirms login success
    public void ConfirmLoginSuccess()
    {
        if (!loginSuccessModal.activeSelf) return; // Safety check
        SceneManager.LoadScene("HomeScreen");
    }

    void ShowLoginError(string message)
    {
        loginErrorMessage.text = message;
        loginErrorModal.SetActive(true);
    }

    // ---------------- REGISTER ----------------
    public void Register()
    {
        passwordMismatchText.text = "";
        registerErrorText.text = "";

        string email = registerEmail.text.Trim();
        string password = registerPassword.text.Trim();
        string confirm = confirmPassword.text.Trim();

        // Check for empty fields
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            registerErrorText.text = "Please fill in all fields.";
            return;
        }

        // Password mismatch check
        if (password != confirm)
        {
            passwordMismatchText.text = "Passwords do not match.";
            return;
        }

        // Firebase create user
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    registerErrorModalMessage.text = task.Exception.GetBaseException().Message;
                    registerErrorModal.SetActive(true);
                    return;
                }

                // Show register success modal
                registerSuccessModal.SetActive(true);
            });
    }

    // ---------------- MODAL CONTROL ----------------
    public void CloseAllModals()
    {
        loginSuccessModal.SetActive(false);
        loginErrorModal.SetActive(false);
        registerSuccessModal.SetActive(false);
        registerErrorModal.SetActive(false);
    }
}
