using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public abstract class FlowGameStatus : MonoBehaviour
{
    public abstract void UpdateStatus(string newStatus);
}

[RequireComponent(typeof(UIDocument))]
public class MainMenu : FlowGameStatus
{
    [SerializeField]
    private FlowGame flowGame;

    private Button playButton = null;
    private TextField userNameField = null;
    private Label statusLabel = null;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        playButton = root.Q<Button>("PlayButton");
        userNameField = root.Q<TextField>("UserNameField");
        statusLabel = root.Q<Label>("StatusLabel");
        playButton.clicked += () => flowGame.LoginRequest(userNameField.text);
    }

    public override void UpdateStatus(string newStatus)
    {
        Debug.Log($"[GAME STATUS] {newStatus}");
        statusLabel.text = newStatus;
    }
}
