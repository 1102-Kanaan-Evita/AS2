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
        if (hudPresetLabel != null)
            hudPresetLabel.text = activePreset.ToString();

        // Generate environment FIRST (this will handle zone visibility)
        if (envGen != null)
            envGen.GenerateEnvironment(activePreset);

        // --- Bake grid after environment generated ---
        var gridMaker = FindObjectOfType<GridMaker>();
        if (gridMaker != null)
        {
            gridMaker.Bake();
        }

        // Hook up button
        if (completeButton != null)
            completeButton.onClick.AddListener(OnManualComplete);

        // Play music if available
        if (AudioManager.I != null && playMusic != null)
            AudioManager.I.PlayMusic(playMusic);
    }

    void Update()
    {
        // Regenerate environment when pressing "C"
        if (Input.GetKeyDown(KeyCode.C) && envGen != null)
        {
            Debug.Log("Regenerating environment...");

            envGen.ClearEnvironment();
            envGen.GenerateEnvironment(activePreset);

            // --- Re-bake the grid after regenerating obstacles ---
            var gridMaker = FindObjectOfType<GridMaker>();
            if (gridMaker != null)
                gridMaker.Bake();
        }
    }

    // Called when the player finishes an environment
    public void OnManualComplete()
    {
        GameState.MarkCompleted(activePreset);
        SceneManager.LoadScene("MenuScene");
    }

    // Optional: Auto completion
    public void OnAutoCompleteDetected()
    {
        GameState.MarkCompleted(activePreset);
        SceneManager.LoadScene("MenuScene");
    }
}