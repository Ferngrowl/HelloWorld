using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq; 

public class TerminalManager : MonoBehaviour
{
    public GameObject directoryLine;
    public GameObject responseLine;

    public TMP_InputField terminalInput;
    public TMP_Text directoryLineMain;
    public GameObject userInputLine;
    public ScrollRect sr;
    private List<string> commandHistory = new List<string>();
    private int currentHistoryIndex = -1;
    private bool isNavigatingHistory = false;

    public GameObject msgList;

    Interpreter interpreter;
    private FileManager fileManager;

    private void Start()
    {
        interpreter = GetComponent<Interpreter>();
        fileManager = GetComponent<FileManager>();

        if (interpreter == null || fileManager == null)
        {
            Debug.LogError("Missing component references in TerminalManager!");
            enabled = false;
            return;
        }

        // Listen to text changes (if needed for history navigation)
        terminalInput.onValueChanged.AddListener(HandleInputChange);
        
        // **** NEW: Subscribe to onEndEdit so that when the user submits their input (by pressing Enter), we process it.
        terminalInput.onEndEdit.AddListener(SubmitCommand);
        // Ensure we are in single-line mode so that Enter submits the command.
        terminalInput.lineType = TMP_InputField.LineType.SingleLine;

        terminalInput.ActivateInputField();
        terminalInput.Select();
        
        StartCoroutine(DisplayStartupCoroutine());
    }

    private void Update()
    {
        // We no longer rely on checking Input.GetKeyDown(KeyCode.Return) in Update.
        // However, we still handle arrow keys for history navigation.
        HandleArrowKeys();
        
        // Keep the input field focused.
        if (!terminalInput.isFocused)
        {
            terminalInput.ActivateInputField();
            terminalInput.Select();
        }
    }

    // This method will be called when the user finishes editing the InputField (typically by pressing Enter).
    private void SubmitCommand(string userInput)
    {
        // When the user clicks away, onEndEdit can fire, so we only process if there is text.
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        // Clear the input field right away.
        terminalInput.text = "";
        
        // Add the directory line (shows the command the user entered)
        AddDirectoryLine(userInput);
        
        // Process the command.
        StartCoroutine(ProcessCommandAsync(userInput));

        // Reset history navigation if needed.
        currentHistoryIndex = -1;
        
        // Re-focus the input field.
        terminalInput.ActivateInputField();
        terminalInput.Select();
    }

    private void HandleArrowKeys()
    {
        if (!terminalInput.isFocused) return;

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
            if (currentHistoryIndex == -1) return;
            
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

    private IEnumerator ProcessCommandAsync(string userInput)
    {
        // Add to history.
        commandHistory.Add(userInput);
        while (commandHistory.Count > 10) commandHistory.RemoveAt(0);
        currentHistoryIndex = -1;

        // Get command response.
        List<string> response = new List<string>();
        yield return StartCoroutine(interpreter.InterpretAsync(userInput, result => {
            response = result;
        }));

        if (response.Count > 0)
        {
            int lines = AddInterpreterLines(response);
            ScrollToBottom(lines);
        }

        // Ensure the input line stays at the bottom.
        userInputLine.transform.SetAsLastSibling();
        AdjustRectTransform(directoryLineMain);
        CurateTerminalOutput(200);

        // Re-focus input.
        terminalInput.ActivateInputField();
        terminalInput.Select();
    }

    private void HandleInputChange(string newValue)
    {
        if (!isNavigatingHistory)
        {
            if (currentHistoryIndex != commandHistory.Count - 1)
            {
                currentHistoryIndex = -1;
            }
        }
    }

    // The rest of the TerminalManager methods remain unchanged...

    private IEnumerator DisplayStartupCoroutine()
    {
        List<string> startupMessages = new List<string>();
        yield return StartCoroutine(interpreter.LoadTitle("ascii.txt", "red", 1, result => {
            startupMessages = result;
        }));
        
        AddInterpreterLines(startupMessages);

        List<string> terminalNotification = new List<string> {
            "Connected to 192.168.1.1",
            "@?&*!~# Has Connected..."
        };
        AddInterpreterLines(terminalNotification);

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        List<string> helper = GetHelperMessages(currentSceneIndex);
        AddHelperLines(helper);

        userInputLine.transform.SetAsLastSibling();
    }

    private List<string> GetHelperMessages(int sceneIndex)
    {
        if (sceneIndex == 1)
        {
            return new List<string>{ 
                "<???>Hello! Sorry for breaking in but I'm here to assist!", 
                "<???>Try typing 'help' and get a feel for things.",
                "<???>Theres a system upgrade for you somewhere on this system.",
                "<???>You should look for locked folders and some files might",
                "<???>be able to help crack open folders. Also, dude who owns",
                "<???>this system was crazy, obsessed with data types of",
                "<???>all things, keep your eyes peeled, good luck!"
            };
        }
        else if (sceneIndex == 2)
        {
            return new List<string>{    
                "<???>See?!", 
                "<???>Told you I was here to help!", 
                "<Askie>My name is Askie, think of me like your assistant.", 
                "<Askie>Anyway now you can take notes to keep track of stuff!", 
                "<Askie>Just a tip, this system seems to be part of a small",
                "<Askie>network, maybe there's more to find than meets the eye."
            };
        }
        return new List<string>();
    }
   
    void AdjustRectTransform(TMP_Text textComponent)
    {    
        textComponent.ForceMeshUpdate();
        float maxWidth = Screen.width * 0.8f;
        Vector2 preferredValues = textComponent.GetPreferredValues(float.PositiveInfinity, float.PositiveInfinity);
        float adjustedWidth = Mathf.Min(preferredValues.x, maxWidth);
        textComponent.rectTransform.sizeDelta = new Vector2(adjustedWidth, textComponent.rectTransform.sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
    }

    int AddInterpreterLines(List<string> interpretation)
    {
        if (interpretation == null || interpretation.Count == 0)
        {
            Debug.LogWarning("No lines to add!");
            return 0;
        }
        foreach (string line in interpretation)
        {
            Debug.Log("Adding interpreter line: " + line); // Check output in console
            if (responseLine == null)
            {
                Debug.LogError("ResponseLine prefab is not assigned!");
                continue;
            }
            GameObject res = Instantiate(responseLine, msgList.transform);
            res.transform.SetAsLastSibling();
            TMP_Text txt = res.GetComponentInChildren<TMP_Text>();
            txt.text = line;
            Debug.Log("TMP_Text now has: " + txt.text);
        }
        return interpretation.Count;
    }

    int AddHelperLines(List<string> interpretation)
    {
        foreach (string line in interpretation)
        {
            GameObject res = Instantiate(responseLine, msgList.transform);
            res.transform.SetAsLastSibling();
            res.GetComponentInChildren<TMP_Text>().text = fileManager.ColorString(line, "purple");
        }
        return interpretation.Count;
    }

    void ScrollToBottom(int lines)
    {
        Canvas.ForceUpdateCanvases();
        // Force rebuild the layout to update content size
        LayoutRebuilder.ForceRebuildLayoutImmediate(msgList.GetComponent<RectTransform>());
        sr.verticalNormalizedPosition = 0;
    }

    void CurateTerminalOutput(int lines)
    { 
        if (msgList.transform.childCount > lines)
        {
            int overflow = msgList.transform.childCount - lines;
            for (int i = 0; i < overflow; i++)
            {
                msgList.GetComponent<RectTransform>().sizeDelta -= new Vector2(0, 35.0f);
                Destroy(msgList.transform.GetChild(i).gameObject);
            }
        }
    }

    public void DisplayText(List<string> lines)
    {
        AddInterpreterLines(lines);
        userInputLine.transform.SetAsLastSibling();
        ScrollToBottom(lines.Count);
    }

    void AddDirectoryLine(string userInput)
    {    
        GameObject msg = Instantiate(directoryLine, msgList.transform);
        msg.transform.SetSiblingIndex(msgList.transform.childCount - 1);
        var texts = msg.GetComponentsInChildren<TMP_Text>();
        texts[1].text = userInput;
        texts[0].text = "G:/" + fileManager.GetCurrentFolderPath() + ">";
        AdjustRectTransform(texts[1]);
        AdjustRectTransform(texts[0]);
    }
}
