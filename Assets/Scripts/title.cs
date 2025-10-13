using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public GameObject titlePanel;
    public GameObject menuPanel;

    void Start()
    {
        // Show title first, hide menu
        if (titlePanel != null) titlePanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        // Hide title, show menu
        if (titlePanel != null) titlePanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);

        // Optionally start music here
        if (AudioManager.I != null)
            AudioManager.I.PlayMusic(Resources.Load<AudioClip>("Audio/YourMenuMusic"));
    }
}
