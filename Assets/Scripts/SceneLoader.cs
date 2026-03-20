using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class SceneLoader : MonoBehaviour
{
    public void LoadHomeScreen()
    {
        //This clears the AR session
        ARSession arSession = FindFirstObjectByType<ARSession>();
        if (arSession != null)
            arSession.Reset();

        SceneManager.LoadScene("HomeScreen");
    }

    public void LoadARScene()
    {
        SceneManager.LoadScene("AR");
    }

    public void LoadQuizScene()
    {
        SceneManager.LoadScene("Quiz");
    }
}