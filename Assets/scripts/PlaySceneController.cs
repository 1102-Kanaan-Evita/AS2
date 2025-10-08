// Assets/Scripts/PlaySceneController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlaySceneController : MonoBehaviour
{
    public EnvironmentGenerator envGen;     // drag the EnvironmentGenerator in inspector
    public Transform playerPrefab;          // player prefab to spawn (capsule)
    public Transform spawnPoint;            // StartArea or specific spawn
    public Text hudPresetLabel;
    public Button completeButton;
    public AudioClip playMusic;

    private GameState.Preset activePreset;

    void Start()
    {
        activePreset = GameState.SelectedPreset;
        if (hudPresetLabel != null) hudPresetLabel.text = activePreset.ToString();

        // make sure envGen exists
        if (envGen != null)
        {
            envGen.preset = (EnvironmentGenerator.Preset)System.Enum.Parse(typeof(EnvironmentGenerator.Preset), activePreset.ToString());
            envGen.GeneratePreset(); // generate on scene load
        }

        // spawn player
        if (playerPrefab != null && spawnPoint != null)
        {
            Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        if (completeButton != null)
            completeButton.onClick.AddListener(OnManualComplete);

        // music
        if (AudioManager.I != null && playMusic != null) AudioManager.I.PlayMusic(playMusic);
    }

    void Update()
    {
        // regenerate on C
        if (Input.GetKeyDown(KeyCode.C) && envGen != null)
        {
            envGen.GeneratePreset();
        }
    }

    // called when player decides they've "completed" the challenge (or after auto detect)
    public void OnManualComplete()
    {
        GameState.MarkCompleted(activePreset);
        // notify menu manager when they return (menu reads GameState)
        SceneManager.LoadScene("MainMenu");
    }

    // helper for auto-complete if you want (not used by default)
    public void OnAutoCompleteDetected()
    {
        GameState.MarkCompleted(activePreset);
        SceneManager.LoadScene("MainMenu");
    }
}
