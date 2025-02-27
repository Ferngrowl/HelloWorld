using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    // Container for building response output.
    private List<string> response = new List<string>();

    // References to the virtual machine and file system.
    public VMManager virtualMachineManager;
    public ConsoleFileSystem consoleFileSystem;
    private ConsoleFolder root;
    private ConsoleFolder currentFolder;

    // Variables for folder unlocking.
    public bool awaitingPasswordInput = false;
    private ConsoleFolder folderAwaitingUnlock;

    // Reference to TerminalManager for updating the UI.
    [SerializeField] private TerminalManager terminalManager;

    // Dictionary for rich text color codes.
    private Dictionary<string, string> colors = new Dictionary<string, string>()
    {
        {"black", "#021b21"},
        {"gray", "#555d71"},
        {"red", "#ff5879"},
        {"yellow", "#f2f1b9"},
        {"blue", "#9ed9d8"},
        {"purple", "#d926ff"},
        {"orange", "#ef5847"}
    };

    /// <summary>
    /// Initializes the file system at startup.
    /// </summary>
    void Start()
    {
        // Assumes that virtualMachineManager.currentVM.FileSystem is valid.
        consoleFileSystem = virtualMachineManager.currentVM.FileSystem;
        root = consoleFileSystem.GetRootFolder();
        currentFolder = root;
    }

    /// <summary>
    /// Processes a command asynchronously and returns a list of formatted strings via callback.
    /// </summary>
    public IEnumerator ProcessCommandAsync(string command, string[] args, System.Action<List<string>> callback)
    {
        // Clear the response list for the new command.
        List<string> response = new List<string>();

        switch (command.ToLower())
        {
            case "help":
                // Load help text from file, display it, then return via callback.
                yield return StartCoroutine(LoadFile("help.txt", "red", 1, helpText =>
                {
                    terminalManager.DisplayText(helpText);
                    callback(helpText);
                }));
                break;

            case "dir":
                Debug.Log("Executing DIR command. Current Folder: " + currentFolder.Name);
                response.Clear();

                // If there are no child folders or files, add a formatted <empty> message.
                if (currentFolder.Children.Count == 0 && currentFolder.Files.Count == 0)
                {
                    response.Add(ColorString("<empty>", colors["gray"]));
                }
                else
                {
                    // Process and format each child folder.
                    foreach (var folderEntry in currentFolder.Children)
                    {
                        ConsoleFolder folder = folderEntry.Value;
                        string status = folder.IsLocked 
                            ? ColorString("<LockedFolder>", colors["red"]) 
                            : ColorString("<Folder>", colors["yellow"]);
                        string line = ColorString(folder.Name, colors["purple"]) + " " + status;
                        response.Add(line);
                    }
                    // Process and format each file.
                    foreach (var fileEntry in currentFolder.Files)
                    {
                        ConsoleFile file = fileEntry.Value;
                        string line = ColorString(file.Name, colors["blue"]) + " " + ColorString("<File>", colors["orange"]);
                        response.Add(line);
                    }
                }
                // Display the constructed response using TerminalManager's response line prefabs.
                terminalManager.DisplayText(response);
                callback(response);
                break;

            case "cd":
                if (args.Length > 1)
                {
                    string folderName = args[1];
                    string folderNameLower = folderName.ToLower();

                    switch (folderNameLower)
                    {
                        case "root":
                            currentFolder = root;
                            terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
                            break;
                        case "..":
                            if (currentFolder.Parent != null)
                            {
                                currentFolder = currentFolder.Parent;
                                terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
                            }
                            break;
                        default:
                            bool folderFound = false;
                            foreach (var child in currentFolder.Children)
                            {
                                if (child.Key.ToLower() == folderNameLower)
                                {
                                    if (child.Value.IsLocked)
                                    {
                                        // Locked folder: prompt for security question.
                                        awaitingPasswordInput = true;
                                        folderAwaitingUnlock = child.Value;
                                        response.Add("Folder is locked. Please answer the security question:");
                                        response.Add(child.Value.SecurityQuestion);
                                        folderFound = true;
                                    }
                                    else
                                    {
                                        // Navigate into the folder.
                                        currentFolder = child.Value;
                                        terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
                                        folderFound = true;
                                    }
                                    break;
                                }
                            }
                            if (!folderFound)
                            {
                                response.Add("Folder not found.");
                            }
                            break;
                    }
                }
                else
                {
                    response.Add("No directory specified.");
                }
                // Display cd command output.
                terminalManager.DisplayText(response);
                callback(response);
                break;

            case "open":
                if (args.Length > 1)
                {
                    string fileNameLower = args[1].ToLower();
                    if (currentFolder.Files.ContainsKey(fileNameLower))
                    {
                        var file = currentFolder.Files[fileNameLower];
                        if (fileNameLower.EndsWith(".exe"))
                        {
                            // Execute the file (e.g., complete level).
                            virtualMachineManager.CompleteLevel();
                            response.Add("Executing program...");
                        }
                        else
                        {
                            // Open file and display its content without adding extra messages.
                            yield return StartCoroutine(OpenAndDisplayFileAsync(args[1], () => { }));
                        }
                    }
                    else
                    {
                        response.Add("File not found.");
                    }
                }
                else
                {
                    response.Add("No file specified.");
                }
                terminalManager.DisplayText(response);
                callback(response);
                break;

            case "connect":
                if (args.Length > 2)
                {
                    string ipAddress = args[1];
                    string password = args[2];
                    if (virtualMachineManager.ConnectToVM(ipAddress, password))
                    {
                        consoleFileSystem = virtualMachineManager.currentVM.FileSystem;
                        root = consoleFileSystem.GetRootFolder();
                        currentFolder = root;
                        response.Add("Successfully connected to " + ipAddress + ".");
                    }
                    else
                    {
                        response.Add("Failed to connect: Invalid IP address or incorrect password.");
                    }
                }
                else
                {
                    response.Add("Usage: connect <IP Address> <Password>");
                }
                terminalManager.DisplayText(response);
                callback(response);
                break;

            default:
                response.Add("Command not recognized.");
                terminalManager.DisplayText(response);
                callback(response);
                break;
        }
    }

    /// <summary>
    /// Loads a file from StreamingAssets, applies color formatting, and returns the content as a list of strings.
    /// </summary>
    public IEnumerator LoadFile(string fileName, string color, int spacing, System.Action<List<string>> callback)
    {
        List<string> formattedLines = new List<string>();

        // Determine the full file path based on the platform.
        string fullPath;
#if UNITY_WEBGL && !UNITY_EDITOR
        fullPath = $"{Application.streamingAssetsPath}/{fileName}";
#else
        fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#endif

        Debug.Log("Loading file from: " + fullPath);

        // Add leading blank lines for spacing.
        for (int i = 0; i < spacing; i++)
            formattedLines.Add("");

        using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Split file content into lines, trim, and format with color.
                string[] lines = request.downloadHandler.text.Split('\n');
                foreach (string line in lines)
                {
                    formattedLines.Add(ColorString(line.Trim(), colors[color]));
                }
            }
            else
            {
                Debug.LogError("Error loading file: " + request.error);
                formattedLines.Add(ColorString("Error loading file.", colors["red"]));
            }
        }

        // Add trailing blank lines.
        for (int i = 0; i < spacing; i++)
            formattedLines.Add("");

        callback(formattedLines);
    }

    /// <summary>
    /// Wraps a string with Unity rich text color tags.
    /// </summary>
    public string ColorString(string s, string colorCode)
    {
        return $"<color={colorCode}>{s}</color>";
    }

    /// <summary>
    /// Opens a file and displays its content using TerminalManager.
    /// </summary>
    private IEnumerator OpenAndDisplayFileAsync(string fileName, System.Action callback)
    {
        fileName = fileName.ToLower();
        if (currentFolder.Files.TryGetValue(fileName, out ConsoleFile file))
        {
            if (!string.IsNullOrEmpty(file.Content))
            {
                List<string> content = new List<string> { ColorString(file.Content, colors["blue"]) };
                terminalManager.DisplayText(content);
            }
            else
            {
                yield return StartCoroutine(LoadFile(file.Name, "blue", 1, lines =>
                {
                    terminalManager.DisplayText(lines);
                }));
            }
        }
        else
        {
            terminalManager.DisplayText(new List<string> { "File not found." });
        }
        callback?.Invoke();
    }

    /// <summary>
    /// Constructs the full folder path by traversing up the folder hierarchy.
    /// </summary>
    public string GetCurrentFolderPath()
    {
        string path = "";
        ConsoleFolder folder = currentFolder;
        while (folder != null)
        {
            path = folder.Name + (string.IsNullOrEmpty(path) ? "" : "/" + path);
            folder = folder.Parent;
        }
        return path;
    }

    /// <summary>
    /// Processes a password attempt for unlocking a locked folder.
    /// </summary>
    public List<string> ProcessPasswordAttempt(string userInput)
    {
        response.Clear();
        if (folderAwaitingUnlock.Unlock(userInput))
        {
            response.Add("Folder unlocked successfully.");
            currentFolder = folderAwaitingUnlock;
            terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
            awaitingPasswordInput = false;
            folderAwaitingUnlock = null;
            response.Add("You did it!");
            response.Add("Folder is now accessible.");
        }
        else
        {
            response.Add("Incorrect password. Please try again.");
            awaitingPasswordInput = false;
            folderAwaitingUnlock = null;
        }
        return response;
    }
}
