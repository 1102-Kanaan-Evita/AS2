using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI / Prefabs")]
    public Transform boardParent;
    public GameObject jeopardyButtonPrefab;
    public Toggle showLabelsToggle;
    public AudioClip jeopardyMusic;
    public AudioClip deathSfx;
    public AudioClip winSfx;

    private AudioSource _audio;
    private List<JeopardyButton> _buttons = new List<JeopardyButton>();

    private static bool boardShuffled = false;

    private bool showAllLabels = false;

    // Environment list (8 buttons)
    public List<GameState.Preset> presetsOrder = new List<GameState.Preset> {
        GameState.Preset.RandomCircles20,
        GameState.Preset.RandomCircles30,
        GameState.Preset.RandomCircles100,
        GameState.Preset.RandomRects20,
        GameState.Preset.RandomRects30,
        GameState.Preset.RandomRects100,
        GameState.Preset.AStarShowcase,
        GameState.Preset.Office
    };

    void Start()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.loop = true;
        if (jeopardyMusic != null)
        {
            _audio.clip = jeopardyMusic;
            _audio.Play();
        }

        if (!boardShuffled)
        {
            PopulateBoard();      
            ShuffleButtons();     
            boardShuffled = true;
        }
        else
        {
            //If board already exists, just repopulate without reshuffle
            PopulateBoard();
        }

        AssignMoneyValues();

        if (showLabelsToggle != null)
        {
            showLabelsToggle.onValueChanged.RemoveAllListeners();
            showLabelsToggle.onValueChanged.AddListener(OnShowLabelsChanged);
            showAllLabels = showLabelsToggle.isOn;
        }

        RefreshButtons();
    }


    void PopulateBoard()
    {
        Debug.Log("PopulateBoard() called");

        // Clear existing buttons
        foreach (Transform t in boardParent)
            Destroy(t.gameObject);
        _buttons.Clear();

        // Create 8 environment buttons
        for (int i = 0; i < presetsOrder.Count; i++)
        {
            GameObject go = Instantiate(jeopardyButtonPrefab, boardParent);
            var jb = go.GetComponent<JeopardyButton>();
            jb.Initialize(this, presetsOrder[i], presetsOrder[i].ToString(), false);
            _buttons.Add(jb);
        }

        // Create 1 death button
        GameObject deathGo = Instantiate(jeopardyButtonPrefab, boardParent);
        var deathJb = deathGo.GetComponent<JeopardyButton>();
        // Use the first preset as placeholder; label and isDeath flag control behavior
        deathJb.Initialize(this, presetsOrder[0], "DEATH", true);
        deathJb.isDeath = true;
        _buttons.Add(deathJb);

        // Shuffle all 9 buttons once
        //ShuffleButtons();

        // Assign money values by row
        AssignMoneyValues();
    }


    void ShuffleButtons()
    {
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].transform.SetSiblingIndex(Random.Range(0, _buttons.Count));
    }

    void AssignMoneyValues()
    {
        for (int i = 0; i < boardParent.childCount; i++)
        {
            var child = boardParent.GetChild(i);
            var jb = child.GetComponent<JeopardyButton>();

            if (jb == null)
            {
                Debug.LogWarning($"Child {child.name} has no JeopardyButton component!");
                continue;
            }

            int row = i / 3;
            string value = row == 0 ? "$100" : row == 1 ? "$200" : "$300";
            jb.moneyLabel = value;
            jb.UpdateDisplay(showAllLabels);
        }
    }

    public void OnPresetChosen(GameState.Preset preset, JeopardyButton sourceButton)
    {
        if (sourceButton.isDeath)
        {
            if (deathSfx != null) _audio.PlayOneShot(deathSfx);

            sourceButton.SetDead();
            GameState.ResetAll();

            // Reshuffle and reassign money values after death
            ShuffleButtons();
            AssignMoneyValues();

            RefreshButtons();

            return;
        }


        GameState.SelectedPreset = preset;
        SceneManager.LoadScene("PlayScene");
    }

    void RefreshButtons()
    {
        foreach (var jb in _buttons)
        {
            if (jb.isDeath)
            {
                jb.SetNormal(); // keep its visual consistent
                jb.UpdateDisplay(showAllLabels);
                continue;
            }

            if (GameState.IsCompleted(jb.preset))
                jb.SetCompleted();
            else
                jb.SetNormal();

            jb.UpdateDisplay(showAllLabels);
        }
    }

    public void OnShowLabelsChanged(bool show)
    {
        showAllLabels = show;
        foreach (var jb in _buttons)
            jb.UpdateDisplay(showAllLabels);
    }
}
