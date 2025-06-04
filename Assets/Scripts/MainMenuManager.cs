using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "Game";

    [Header("Popup UI Elements")]
    public GameObject settingsPopup;
    public GameObject tutorialPopup;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (settingsPopup != null) settingsPopup.SetActive(false);
        if (tutorialPopup != null) tutorialPopup.SetActive(false);
    }


    public void OnNewGameButtonPressed()
    {
        Debug.Log("New Game button pressed. Loading game scene...");
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnSettingsButtonPressed()
    {
        Debug.Log("Settings button pressed.");
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true);
            if (tutorialPopup != null) tutorialPopup.SetActive(false);
        }
    }

    public void OnTutorialButtonPressed()
    {
        Debug.Log("Tutorial button pressed.");
        if (tutorialPopup != null)
        {
            tutorialPopup.SetActive(true);
            if (settingsPopup != null) settingsPopup.SetActive(false);
        }
    }

    public void CloseSettingsPopup()
    {
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(false);
        }
    }

    public void CloseTutorialPopup()
    {
        if (tutorialPopup != null)
        {
            tutorialPopup.SetActive(false);
        }
    }

    public void CloseActivePopup()
    {
        if (settingsPopup != null && settingsPopup.activeSelf)
        {
            settingsPopup.SetActive(false);
        }
        else if (tutorialPopup != null && tutorialPopup.activeSelf)
        {
            tutorialPopup.SetActive(false);
        }
    }
}