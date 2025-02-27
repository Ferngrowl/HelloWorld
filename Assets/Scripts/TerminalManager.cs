using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TerminalManager : MonoBehaviour
{
    // Public UI references assigned via the Inspector
    public GameObject directoryLinePrefab;    // Prefab for displaying the user’s command with the directory path
    public GameObject responseLinePrefab;       // Prefab for displaying system responses
    public TMP_InputField terminalInput;        // Input field for user commands
    public TMP_Text directoryLineMain;          // (Optional) Main directory display text if used elsewhere
    public GameObject userInputLine;            // Container for the input field to ensure it’s always at the bottom
    public ScrollRect scrollRect;               // Scroll container for the terminal messages
    public GameObject msgList;                  // Parent container for all terminal messages

    // Private variables for command history and navigation
    private List<string> commandHistory = new List<string>();
    private int currentHistoryIndex = -1;
    private bool isNavigatingHistory = false;

    // Component references to interpreter and file management systems
    private Interpreter interpreter;
    private FileManager fileManager;

    private void Start()
    {
        // Get component references
        interpreter = GetComponent<Interpreter>();
        fileManager = GetComponent<FileManager>();

        if (interpreter == null || fileManager == null)
        {
            Debug.LogError("Missing Interpreter or FileManager component in TerminalManager!");
            enabled = false;
            return;
        }

        // Subscribe to input field events
        terminalInput.onValueChanged.AddListener(HandleInputChange);
        terminalInput.onEndEdit.AddListener(SubmitCommand);
        terminalInput.lineType = TMP_InputField.LineType.SingleLine; // Ensure single-line input

        // Focus the input field on startup
        terminalInput.ActivateInputField();
        terminalInput.Select();

        // Begin startup sequence (e.g., ASCII art, connection notification, helper messages)
        StartCoroutine(DisplayStartupCoroutine());
    }

    private void Update()
    {
        // Process arrow keys for command history navigation
        HandleArrowKeys();

        // Keep the input field in focus
        if (!terminalInput.isFocused)
        {
            terminalInput.ActivateInputField();
            terminalInput.Select();
        }
    }

    /// <summary>
    /// Called when the user finishes editing the input field (typically by pressing Enter).
    /// </summary>
    private void SubmitCommand(string userInput)
    {
        // Avoid processing empty commands (can occur on field de-focus)
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        // Immediately clear the input field
        terminalInput.text = "";

        // Add the user's command to the UI (directory line)
        AddDirectoryLine(userInput);

        // Start asynchronous command processing
        StartCoroutine(ProcessCommandAsync(userInput));

        // Reset command history navigation
        currentHistoryIndex = -1;

        // Re-focus the input field after submission
        terminalInput.ActivateInputField();
        terminalInput.Select();
    }

    /// <summary>
    /// Handles arrow key navigation for browsing previous commands.
    /// </summary>
    private void HandleArrowKeys()
    {
        if (!terminalInput.isFocused)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count == 0) return;

            isNavigatingHistory = true;
            currentHistoryIndex = currentHistoryIndex == -1 
                ? commandHistory.Count - 1
                : Mathf.Clamp(currentHistoryIndex - 1, 0, commandHistory.Count - 1);

            terminalInput.text = commandHistory[currentHistoryIndex];
            terminalInput.caretPosition = terminalInput.text.Length;
            isNavigatingHistory = false;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentHistoryIndex == -1)
                return;

            isNavigatingHistory = true;
            currentHistoryIndex++;

            if (currentHistoryIndex >= commandHistory.Count)
            {
                terminalInput.text = "";
                currentHistoryIndex = -1;
            }
            else
            {
                terminalInput.text = commandHistory[currentHistoryIndex];
            }

            terminalInput.caretPosition = terminalInput.text.Length;
            isNavigatingHistory = false;
        }
    }

    /// <summary>
    /// Processes the user's command asynchronously.
    /// </summary>
    private IEnumerator ProcessCommandAsync(string userInput)
    {
        // Add the command to history (limit history to last 10 commands)
        commandHistory.Add(userInput);
        while (commandHistory.Count > 10)
            commandHistory.RemoveAt(0);
        currentHistoryIndex = -1;

        // Call the interpreter to process the command and get a response
        List<string> response = new List<string>();
        yield return StartCoroutine(interpreter.InterpretAsync(userInput, result => response = result));

        // Display each response line if any were returned
        if (response.Count > 0)
        {
            int lineCount = AddInterpreterLines(response);
            ScrollToBottom(lineCount);
        }

        // Ensure the user input line stays at the bottom and trim excess output
        userInputLine.transform.SetAsLastSibling();
        AdjustRectTransform(directoryLineMain);
        CurateTerminalOutput(200);

        // Re-focus the input field after processing
        terminalInput.ActivateInputField();
        terminalInput.Select();
    }

    /// <summary>
    /// Resets history navigation if the user manually changes the input.
    /// </summary>
    private void HandleInputChange(string newValue)
    {
        if (!isNavigatingHistory && currentHistoryIndex != commandHistory.Count - 1)
        {
            currentHistoryIndex = -1;
        }
    }

    /// <summary>
    /// Displays the startup sequence including ASCII art and helper messages.
    /// </summary>
    private IEnumerator DisplayStartupCoroutine()
    {
        // Load and display ASCII title art
        List<string> startupMessages = new List<string>();
        yield return StartCoroutine(interpreter.LoadTitle("ascii.txt", "red", 1, result => startupMessages = result));
        AddInterpreterLines(startupMessages);

        // Display static connection notifications
        List<string> terminalNotification = new List<string>
        {
            "Connected to 192.168.1.1",
            "@?&*!~# Has Connected..."
        };
        AddInterpreterLines(terminalNotification);

        // Display scene-specific helper messages
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        List<string> helperMessages = GetHelperMessages(currentSceneIndex);
        AddHelperLines(helperMessages);

        // Ensure the user input line remains at the bottom of the message list
        userInputLine.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Returns helper messages based on the current scene index.
    /// </summary>
    private List<string> GetHelperMessages(int sceneIndex)
    {
        if (sceneIndex == 1)
        {
            return new List<string>
            {
                "<???>Hello! Sorry for breaking in but I'm here to assist!",
                "<???>Try typing 'help' and get a feel for things.",
                "<???>There's a system upgrade for you somewhere on this system.",
                "<???>You should look for locked folders and some files might",
                "<???>be able to help crack open folders. Also, the owner",
                "<???>was crazy—obsessed with data types of all things. Keep your eyes peeled!",
                "<???>Good luck!"
            };
        }
        else if (sceneIndex == 2)
        {
            return new List<string>
            {
                "<???>See?!",
                "<???>Told you I was here to help!",
                "<Askie>My name is Askie, think of me like your assistant.",
                "<Askie>Now you can take notes to keep track of stuff!",
                "<Askie>Tip: This system is part of a small network—there’s more than meets the eye."
            };
        }
        return new List<string>();
    }

    /// <summary>
    /// Adjusts the size of a TMP_Text component based on screen width.
    /// </summary>
    private void AdjustRectTransform(TMP_Text textComponent)
    {
        if (textComponent == null)
            return;

        textComponent.ForceMeshUpdate();
        float maxWidth = Screen.width * 0.8f;
        Vector2 preferredValues = textComponent.GetPreferredValues(float.PositiveInfinity, float.PositiveInfinity);
        float adjustedWidth = Mathf.Min(preferredValues.x, maxWidth);
        textComponent.rectTransform.sizeDelta = new Vector2(adjustedWidth, textComponent.rectTransform.sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
    }

    /// <summary>
    /// Instantiates response prefabs for each line from the interpreter.
    /// </summary>
    private int AddInterpreterLines(List<string> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("No interpreter lines to add!");
            return 0;
        }

        foreach (string line in lines)
        {
            if (responseLinePrefab == null)
            {
                Debug.LogError("ResponseLine prefab is not assigned!");
                continue;
            }
            GameObject responseObj = Instantiate(responseLinePrefab, msgList.transform);
            responseObj.transform.SetAsLastSibling();
            TMP_Text textComponent = responseObj.GetComponentInChildren<TMP_Text>();
            textComponent.text = line;
        }
        return lines.Count;
    }

    /// <summary>
    /// Instantiates helper message lines using a specific color.
    /// </summary>
    private int AddHelperLines(List<string> lines)
    {
        foreach (string line in lines)
        {
            GameObject responseObj = Instantiate(responseLinePrefab, msgList.transform);
            responseObj.transform.SetAsLastSibling();
            TMP_Text textComponent = responseObj.GetComponentInChildren<TMP_Text>();
            // Use FileManager's color formatting utility
            textComponent.text = fileManager.ColorString(line, "purple");
        }
        return lines.Count;
    }

    /// <summary>
    /// Scrolls the message list to the bottom.
    /// </summary>
    private void ScrollToBottom(int lineCount)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(msgList.GetComponent<RectTransform>());
        scrollRect.verticalNormalizedPosition = 0;
    }

    /// <summary>
    /// Trims excess messages if the count exceeds the specified limit.
    /// </summary>
    private void CurateTerminalOutput(int maxLines)
    {
        int childCount = msgList.transform.childCount;
        if (childCount > maxLines)
        {
            int overflow = childCount - maxLines;
            RectTransform msgListRect = msgList.GetComponent<RectTransform>();
            for (int i = 0; i < overflow; i++)
            {
                // Adjust the container size assuming each message has a fixed height (e.g., 35 units)
                msgListRect.sizeDelta -= new Vector2(0, 35.0f);
                Destroy(msgList.transform.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Adds a directory line displaying the current path and the user's input command.
    /// </summary>
    private void AddDirectoryLine(string userInput)
    {
        GameObject directoryObj = Instantiate(directoryLinePrefab, msgList.transform);
        directoryObj.transform.SetSiblingIndex(msgList.transform.childCount - 1);
        TMP_Text[] texts = directoryObj.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            texts[0].text = "G:/" + fileManager.GetCurrentFolderPath() + ">";
            texts[1].text = userInput;
            AdjustRectTransform(texts[0]);
            AdjustRectTransform(texts[1]);
        }
        else
        {
            Debug.LogWarning("Directory line prefab is missing required TMP_Text components.");
        }
    }

    /// <summary>
    /// External method to display text in the terminal.
    /// </summary>
    public void DisplayText(List<string> lines)
    {
        AddInterpreterLines(lines);
        userInputLine.transform.SetAsLastSibling();
        ScrollToBottom(lines.Count);
    }
}
