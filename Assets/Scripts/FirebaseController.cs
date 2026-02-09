using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using TMPro;

public class FirebaseController : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;
    private bool firebaseReady = false;

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
        // Show initial panel
        ShowSplash();

        // Clear mismatch text
        passwordMismatchText.text = "";

        // Initialize Firebase properly
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                firebaseReady = false;
                ShowLoginError("Firebase not ready: " + task.Result);
                return;
            }

            firebaseReady = true;
            auth = FirebaseAuth.DefaultInstance;
        });
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
    }

    // ---------------- LOGIN ----------------
    public void Login()
    {
        if (!firebaseReady)
        {
            ShowLoginError("Firebase is still loading. Try again.");
            return;
        }

        string email = loginEmail.text.Trim();
        string password = loginPassword.text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowLoginError("Please fill in all fields.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowLoginError(task.Exception.GetBaseException().Message);
                    return;
                }

                loginEmail.text = "";
                loginPassword.text = "";
                loginSuccessModal.SetActive(true);

            });
    }

    public void ConfirmLoginSuccess()
    {
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

        if (!firebaseReady)
        {
            registerErrorModalMessage.text = "Firebase is still loading. Try again.";
            registerErrorModal.SetActive(true);
            return;
        }

        string email = registerEmail.text.Trim();
        string password = registerPassword.text;
        string confirm = confirmPassword.text;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirm))
        {
            passwordMismatchText.text = "";  
            registerErrorModalMessage.text = "Please fill in all fields.";
            registerErrorModal.SetActive(true);
            return;
        }

        if (password != confirm)
        {
            passwordMismatchText.text = "Passwords do not match.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    registerErrorModalMessage.text = task.Exception.GetBaseException().Message;
                    registerErrorModal.SetActive(true);
                    return;
                }

                registerEmail.text = "";
                registerPassword.text = "";
                confirmPassword.text = "";
                passwordMismatchText.text = "";
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
