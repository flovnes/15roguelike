using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "Game";

    [Header("Popup UI Elements")]
    public GameObject settingsPopup;
    public GameObject tutorialPopup;
    public Button confirmButton;
    public Button prevTutorialButton;
    public Button nextTutorialButton;
    public Image tutorialDisplayImage;
    private int tutorialIndex = 0;

    [Header("Tutorial Sprites")]
    public Sprite[] tutorialPages;
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        settingsPopup?.SetActive(false);
        tutorialPopup?.SetActive(false);
    }


    public void OnNewGameButtonPressed()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnSettingsButtonPressed()
    {
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true);
            tutorialPopup?.SetActive(false);
        }
    }

    public void OnTutorialButtonPressed()
    {
        if (tutorialPopup != null && tutorialPages != null && tutorialPages.Length > 0)
        {
            tutorialIndex = 0;
            tutorialPopup.SetActive(true);
            DisplayTutorialPage();
            UpdateButtonStates();

            settingsPopup?.SetActive(false);
        }
    }

    void DisplayTutorialPage()
    {
        if (tutorialDisplayImage != null && tutorialPages != null &&
        tutorialIndex >= 0 && tutorialIndex < tutorialPages.Length)
        {
            tutorialDisplayImage.sprite = tutorialPages[tutorialIndex];
        }
    }

    void UpdateButtonStates()
    {
        if (prevTutorialButton != null)
        {
            prevTutorialButton.interactable = tutorialIndex > 0;
        }
        if (nextTutorialButton != null && tutorialPages != null)
        {
            nextTutorialButton.interactable = tutorialIndex < tutorialPages.Length - 1;
        }
    }

    public void OnConfirmRemoveMusicPressed()
    {
        if (confirmButton != null)
        {
            PersistentMusic.Instance.Mute();
            confirmButton.interactable = false;
        }
    }

    public void OnNextTutorialPagePressed()
    {
        if (tutorialPages == null || tutorialPages.Length == 0) return;

        if (tutorialIndex < tutorialPages.Length - 1)
        {
            tutorialIndex++;
            DisplayTutorialPage();
            UpdateButtonStates();
        }
    }

    public void OnPrevTutorialPagePressed()
    {
        if (tutorialPages == null || tutorialPages.Length == 0) return;

        if (tutorialIndex > 0)
        {
            tutorialIndex--;
            DisplayTutorialPage();
            UpdateButtonStates();
        }
    }

    public void CloseActivePopup()
    {
        settingsPopup?.SetActive(false);
        tutorialPopup?.SetActive(false);
    }
}