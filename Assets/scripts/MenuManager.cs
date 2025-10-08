// Assets/Scripts/MenuManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI / Prefabs")]
    public Transform boardParent;           // panel that contains the buttons (GridLayoutGroup)
    public GameObject jeopardyButtonPrefab; // prefab with Button + JeopardyButton script
    public Text titleText;
    public AudioClip jeopardyMusic;
    public AudioClip deathSfx;
    public AudioClip winSfx;
    public int lives = 3;

    private AudioSource _audio;

    // define the order/labels of the board
    public List<GameState.Preset> presetsOrder = new List<GameState.Preset> {
        GameState.Preset.RandomCircles20,
        GameState.Preset.RandomCircles30,
        GameState.Preset.RandomCircles100,
        GameState.Preset.RandomRects20,
        GameState.Preset.RandomRects30,
        GameState.Preset.RandomRects100,
        GameState.Preset.AStarShowcase,
        GameState.Preset.Office,
        GameState.Preset.Difficult
    };

    // optional: indices on the board to mark as death buttons (players don't know)
    public List<int> deathButtonIndices = new List<int> { 2, 7 }; // example: 3rd and 8th buttons are deadly

    private List<JeopardyButton> _buttons = new List<JeopardyButton>();

    void Start()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.loop = true;
        if (jeopardyMusic != null) { _audio.clip = jeopardyMusic; _audio.Play(); }

        PopulateBoard();
        RefreshButtons();
    }

    void PopulateBoard()
    {
        // clear existing (editor-friendly)
        foreach (Transform t in boardParent) Destroy(t.gameObject);
        _buttons.Clear();

        for (int i = 0; i < presetsOrder.Count; i++)
        {
            var preset = presetsOrder[i];
            GameObject go = Instantiate(jeopardyButtonPrefab, boardParent);
            var jb = go.GetComponent<JeopardyButton>();
            bool isDeath = deathButtonIndices.Contains(i);
            jb.Initialize(this, preset, preset.ToString(), isDeath);
            _buttons.Add(jb);
        }
    }

    public void OnPresetChosen(GameState.Preset preset, JeopardyButton sourceButton)
    {
        if (sourceButton.isDeath)
        {
            // death behavior: lose life, play SFX, disable the button
            lives--;
            if (deathSfx != null) _audio.PlayOneShot(deathSfx);
            sourceButton.SetDead();
            if (lives <= 0)
            {
                // fully "game over" - show a quick modal; here we just reset completions and lives
                GameState.ResetAll();
                lives = 3;
                RefreshButtons();
            }
            return;
        }

        GameState.SelectedPreset = preset;
        SceneManager.LoadScene("PlayScene");
    }

    public void NotifyLevelComplete(GameState.Preset preset)
    {
        GameState.MarkCompleted(preset);
        if (winSfx != null) _audio.PlayOneShot(winSfx);
        RefreshButtons();
    }

    void RefreshButtons()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var b = _buttons[i];
            if (GameState.IsCompleted(b.preset)) b.SetCompleted();
            else b.SetNormal();
        }
    }

    // call from a UI Reset button if you want to unhide everything
    public void ResetAll()
    {
        GameState.ResetAll();
        lives = 3;
        RefreshButtons();
    }
}
