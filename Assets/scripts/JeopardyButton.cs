using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class JeopardyButton : MonoBehaviour
{
    public GameState.Preset preset;
    public bool isDeath = false;
    public string moneyLabel;  // "$100", "$200", "$300"

    public Text labelText;
    public TextMeshProUGUI labelTMP;
    public TextMeshProUGUI moneyTMP; // assign in prefab or auto-detect
    public Button button;

    private string originalLabel;
    private bool isCompletedFlag = false;
    private bool isDeadFlag = false;
    private MenuManager menu;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        // auto-wire text if not manually assigned
        if (moneyTMP == null)
            moneyTMP = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Initialize(MenuManager m, GameState.Preset p, string label, bool death)
    {
        menu = m;
        preset = p;
        isDeath = death;
        originalLabel = label ?? p.ToString();
        isCompletedFlag = false;
        isDeadFlag = false;
        SetLabelDisplayed(false);
    }

    void OnClick()
    {
        if (menu == null) menu = FindObjectOfType<MenuManager>();
        if (menu != null)
            menu.OnPresetChosen(preset, this);
    }

    public void SetLabelDisplayed(bool reveal)
    {
        string display = "???";

        if (isCompletedFlag)
            display = "COMPLETED";
        else if (isDeadFlag)
            display = "DEAD";
        else if (reveal)
            display = originalLabel;

        if (labelTMP != null) labelTMP.text = display;
        if (labelText != null) labelText.text = display;

        if (moneyTMP != null)
            moneyTMP.text = moneyLabel; // always show money

        if (button != null)
            button.interactable = !(isCompletedFlag || isDeadFlag);
    }

    public void SetCompleted()
    {
        isCompletedFlag = true;
        isDeadFlag = false;
        SetLabelDisplayed(true);
    }

    public void SetDead()
    {
        isDeadFlag = true;
        SetLabelDisplayed(true);
    }

    public void SetNormal()
    {
        isCompletedFlag = false;
        isDeadFlag = false;
        SetLabelDisplayed(false);
    }

    // Called by the toggle
    public void UpdateDisplay(bool reveal)
    {
        SetLabelDisplayed(reveal);
    }
}
