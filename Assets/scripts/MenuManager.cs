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
    public InputField unitCountInput; // Add this to inspector
    public Dropdown pathfindingDropdown; // Add this to inspector
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
        
        // Setup unit count input field
        if (unitCountInput != null)
        {
            unitCountInput.text = GameState.UnitCount.ToString();
            unitCountInput.onEndEdit.AddListener(OnUnitCountChanged);
        }
        
        // Setup pathfinding dropdown
        if (pathfindingDropdown != null)
        {
            pathfindingDropdown.ClearOptions();
            pathfindingDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "A* Pathfinding", 
                "Potential Fields Only", 
                "A* + Potential Fields",
                "RRT Pathfinding" 
            });
            // Set to current saved value
            pathfindingDropdown.value = (int)GameState.SelectedPathfinding;
            pathfindingDropdown.RefreshShownValue(); // Ensure display updates
            pathfindingDropdown.onValueChanged.AddListener(OnPathfindingChanged);
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
    
    public void OnUnitCountChanged(string value)
    {
        if (int.TryParse(value, out int count))
        {
            // Clamp to reasonable values
            count = Mathf.Clamp(count, 0, 100);
            GameState.UnitCount = count;
            unitCountInput.text = count.ToString(); // Update display with clamped value
            Debug.Log("Unit count set to: " + count);
        }
        else
        {
            // Invalid input, reset to current value
            unitCountInput.text = GameState.UnitCount.ToString();
            Debug.LogWarning("Invalid unit count input");
        }
    }
    
    public void OnPathfindingChanged(int index)
    {
        GameState.SelectedPathfinding = (GameState.PathfindingMethod)index;
        Debug.Log("Pathfinding method set to: " + GameState.SelectedPathfinding);
    }
}