using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    List<string> response = new List<string>();

    //virtual machine variables
    public VMManager virtualMachineManager;

    //file system variables
    public ConsoleFileSystem consoleFileSystem;
    private ConsoleFolder root;
    private ConsoleFolder currentFolder;

    // locking unlocking variables
    public bool awaitingPasswordInput = false;
    private ConsoleFolder folderAwaitingUnlock;

    [SerializeField] private TerminalManager terminalManager; // Using SerializeField to keep it private but assignable in the Inspector

    Dictionary<string,string> colors = new Dictionary<string,string>()
    {
        {"black", "#021b21"},
        {"gray", "#555d71"},
        {"red", "#ff5879"},
        {"yellow", "#f2f1b9"},
        {"blue", "#9ed9d8"},
        {"purple", "#d926ff"},
        {"orange", "#ef5847"}
    };

    // Start is called before the first frame update
    void Start()
    {
       // if (virtualMachineManager == null) Debug.LogError("virtualMachineManager is null");
       // else if (virtualMachineManager.currentVM == null) Debug.LogError("currentVM is null");
       // else if (virtualMachineManager.currentVM.FileSystem == null) Debug.LogError("FileSystem is null");

        consoleFileSystem = virtualMachineManager.currentVM.FileSystem;
        root = consoleFileSystem.GetRootFolder();
        currentFolder = consoleFileSystem.GetRootFolder();
    }

    public List<string> ProcessCommand(string command, string[] args)
    {
        List<string> response = new List<string>();

        switch (command.ToLower()) // Ensure command processing is case-insensitive
        {
            case "help":
                StartCoroutine(LoadFile("help.txt", "red", 1, (helpText) => {
                    terminalManager.DisplayText(helpText);
                }));
                break;
            case "dir":
                //first check if empty
                if (currentFolder.Children.Count == 0 && currentFolder.Files.Count == 0)
                {
                    // If there are no children folders or files, display <empty>
                    response.Add("<empty>");
                }
                else
                {
                    // List all child folders with indication if they are locked or not
                    foreach (var folder in currentFolder.Children)
                    {
                        // Check if the folder is locked
                        string folderStatus = folder.Value.IsLocked ? "<LockedFolder>" : "<Folder>";
                        response.Add($"{folder.Key} {folderStatus}"); // Adjusted to include folder status
                    }
                    // Then, list all files in the current folder
                    foreach (var file in currentFolder.Files)
                    {
                        response.Add($"{file.Key} <File>"); // Assuming file.Key is the file name
                    }
                }
                break;
            case "cd":
                if (args.Length > 1)
                {
                    string folderName = args[1]; // Keep the input as is for potential display
                    string folderNameLower = folderName.ToLower(); // Use a lowercase version for comparison

                    switch (folderNameLower)
                    {
                        case "root":
                            // Navigate directly to the root folder
                            currentFolder = root;
                            terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
                            break;

                        case "..":
                            if (currentFolder.Parent != null)
                            {
                                // Go up to the parent folder, if not root
                                currentFolder = currentFolder.Parent;
                                terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";
                            }
                            break;

                        default:
                            // Handle navigation to a specific folder
                            bool folderFound = false;
                            foreach (var child in currentFolder.Children)
                            {
                                if (child.Key.ToLower() == folderNameLower) // Case-insensitive comparison
                                {
                                    if (child.Value.IsLocked)
                                    {
                                        if (child.Value.IsLocked)
                                        {
                                            awaitingPasswordInput = true;
                                            folderAwaitingUnlock = child.Value;
                                            response.Add("hmm, this folder seems to be corrupted.");
                                            response.Add("I can fix it but I need to know:");
                                            
                                            // Use the security question from the folder
                                            string question = child.Value.SecurityQuestion; // Access the question from the folder
                                            response.Add(question); // Display the question

                                            folderFound = true; // Mark as found to prevent "Folder not found" message
                                            break; // Exit the loop once the locked folder is found
                                        }
                                    }
                                    else
                                    {
                                        // The folder is not locked, navigate into it
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
                break;

            case "open":
                if (args.Length > 1)
                {   
                    string fileNameLower = args[1].ToLower(); // Convert filename to lowercase for comparison
                    if (currentFolder.Files.ContainsKey(fileNameLower))
                    {
                        var file = currentFolder.Files[fileNameLower]; // Access the file object
                        if (fileNameLower.EndsWith(".exe"))
                        {
                            virtualMachineManager.CompleteLevel();
                        }
                        else
                        // Check if the file has direct content specified
                        if (!string.IsNullOrEmpty(file.Content))
                        {
                            // Display file content
                            OpenAndDisplayFile(args[1]);
                            response.Add("This information seems random....");
                        }
                        else
                        {
                            // Display file content
                            OpenAndDisplayFile(args[1]);
                            response.Add("This information seems important...");
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
                break;
            case "connect" :
                if (args.Length > 2)
                {   
                    string ipAddress = args[1];
                    string password = args[2]; // Assuming a password is required
                    if(virtualMachineManager.ConnectToVM(ipAddress, password))
                    {
                        // Connection successful, update consoleFileSystem
                        consoleFileSystem = virtualMachineManager.currentVM.FileSystem;
                        root = consoleFileSystem.GetRootFolder(); // Update root
                        currentFolder = root; // Reset currentFolder to the new root
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
                break;
            
            default:
                response.Add("Command not recognized in FileManager.");
                break;
        }

        return response;
    }

 
    // LoadFile method to return a list of colored and formatted strings
    public IEnumerator LoadFile(string fileName, string color, int spacing, System.Action<List<string>> callback)
    {
        List<string> formattedLines = new List<string>();
        
        // Modified path handling with proper conditional compilation
        string fullPath;
        #if UNITY_WEBGL && !UNITY_EDITOR
        fullPath = $"{Application.streamingAssetsPath}/{fileName}";
        #else
        fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        #endif

        Debug.Log($"Attempting to load file from: {fullPath}");

        // Add initial spacing
        for (int i = 0; i < spacing; i++) formattedLines.Add("");

        using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string[] lines = request.downloadHandler.text.Split('\n');
                foreach (string line in lines)
                {
                    formattedLines.Add(ColorString(line.Trim(), colors[color]));
                }
            }
            else
            {
                Debug.LogError($"Error loading {fileName}: {request.error}");
                formattedLines.Add(ColorString("Error loading file.", colors["red"]));
            }
        }

        // Add trailing spacing
        for (int i = 0; i < spacing; i++) formattedLines.Add("");

        callback?.Invoke(formattedLines);
    }
 
    public string ColorString(string s, string colorCode)
    {
        return $"<color={colorCode}>{s}</color>";
    }

    private void OpenAndDisplayFile(string fileName)
    {
        fileName = fileName.ToLower();
        if (currentFolder.Files.TryGetValue(fileName, out ConsoleFile file))
        {
            if (!string.IsNullOrEmpty(file.Content))
            {
                var content = new List<string> { ColorString(file.Content, colors["blue"]) };
                terminalManager.DisplayText(content);
            }
            else
            {
                StartCoroutine(LoadFile(file.Name, "blue", 1, (formattedLines) => {
                    terminalManager.DisplayText(formattedLines);
                }));
            }
        }
        else
        {
            terminalManager.DisplayText(new List<string> { "File not found." });
        }
    }

    public string GetCurrentFolderPath()
    {
        string path = "";
        ConsoleFolder folder = currentFolder;

        // Build the path by prepending each folder's name as we move up the hierarchy
        while (folder != null)
        {
            path = folder.Name + (path == "" ? "" : "/" + path);
            folder = folder.Parent;
        }

        return path;
    }

    public List<string> ProcessPasswordAttempt(string userInput)
    {
        response.Clear(); // Clear any existing messages
        
        if (folderAwaitingUnlock.Unlock(userInput))
        {
                response.Add("Folder unlocked successfully.");
                // Navigate into the folder or perform desired action after unlocking
                currentFolder = folderAwaitingUnlock;
                terminalManager.directoryLineMain.text = "G:/" + GetCurrentFolderPath() + ">";

                // Reset flags
                awaitingPasswordInput = false;
                folderAwaitingUnlock = null;

                response.Add("You did it!");
                response.Add("That sorted out the corrupted folder nicely,");
                response.Add("you should be able to access it now ");
        }
        else
        {
            response.Add("Incorrect password.");
            response.Add("That didn't work,");
            response.Add("there must be a file somewhere");
            response.Add("that could give us a hint");

            // Reset flags
            awaitingPasswordInput = false;
            folderAwaitingUnlock = null;
        }
        return response;
    }

}
