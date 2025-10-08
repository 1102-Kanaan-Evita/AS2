// Assets/Scripts/JeopardyButton.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class JeopardyButton : MonoBehaviour
{
    public GameState.Preset preset;
    public bool isDeath = false;

    public Text labelText;
    public Button button;
    private MenuManager menu;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Initialize(MenuManager menuManager, GameState.Preset preset, string label, bool death)
    {
        menu = menuManager;
        this.preset = preset;
        isDeath = death;
        if (labelText != null) labelText.text = death ? "???" : label;
    }

    public void OnClick()
    {
        if (menu != null) menu.OnPresetChosen(preset, this);
    }

    public void SetCompleted()
    {
        if (labelText != null) labelText.text = "COMPLETED";
        button.interactable = false;
    }

    public void SetDead()
    {
        if (labelText != null) labelText.text = "DEAD";
        button.interactable = false;
    }

    public void SetNormal()
    {
        if (labelText != null) labelText.text = isDeath ? "???" : preset.ToString();
        button.interactable = true;
    }
}
