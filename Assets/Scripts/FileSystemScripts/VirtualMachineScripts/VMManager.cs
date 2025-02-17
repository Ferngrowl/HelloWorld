using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;


public class VMManager : MonoBehaviour
{
    public int currentDifficulty = 1;
    public List<VirtualMachine> virtualMachines;
    public VirtualMachine currentVM; // The VM the player is currently interacting with
    public ConsoleFolder rootFolder;
    public LockedFolderPair lockedFolderPair;

    //list of prefixes and suffixes for random fodler name generation
    List<string> prefixes = new List<string> { "Data", "Temp", "Net", "Secure", "Program", "Common"};
    List<string> suffixes = new List<string> { "Files", "Storage", "Bin", "Docs", "Logs", "Cache",};

    
    //list of folders always present in every system
    string[] baseFolders = new string[] { "System", "User", "Apps", "Documents" };

    //list of locked folders and their accompanying password files
    List<LockedFolderPair> lockedFolderPairsData = new List<LockedFolderPair>
    {
        new LockedFolderPair("SystemUpgrade", "string", "DataTypes.txt", "","Which data type is used for a sequence of characters?"),
        new LockedFolderPair("SystemUpgrade", "character", "DataTypes.txt", "","Which data type is used to store a single character?"),
        new LockedFolderPair("SystemUpgrade", "integer", "DataTypes.txt", "","Which data type is used to store whole numbers?"),
        new LockedFolderPair("SystemUpgrade", "float", "DataTypes.txt", "","Which data type is used for numbers that contain decimal points?"),
        new LockedFolderPair("SystemUpgrade", "boolean", "DataTypes.txt", "","Which data type is used where data is restricted to True/False options?"),
        // Add more pairs as needed
    };

    List<LockedFolderPair> lockedFolderPairsBinary = new List<LockedFolderPair>
    {
        new LockedFolderPair("PasswordVault", "0101", "Binary.txt", "","What is 5 in binary?"),
        new LockedFolderPair("PasswordVault", "15", "Binary.txt", "","What is 1111 in decimal?"),
        new LockedFolderPair("PasswordVault", "0111", "Binary.txt", "","What is 7 in binary?"),
        new LockedFolderPair("PasswordVault", "3", "Binary.txt", "","What is 0011 in decimal?"),
        new LockedFolderPair("PasswordVault", "12", "Binary.txt", "","What is 1100 in decimal?"),
        // Add more pairs as needed
    };

    List<string> sentencePool = new List<string>
    {
        "The quick brown fox jumps over the lazy dog.",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
        "She sells sea shells by the sea shore.",
        "How much wood would a woodchuck chuck if a woodchuck could chuck wood?",
        "A watched pot never boils.",
        "This is a randomly generated sentence."
    };

    //list of used pairs in the current system
    private HashSet<LockedFolderPair> usedPairs = new HashSet<LockedFolderPair>();

    void Awake()
    {
        // Initialize your network here
        virtualMachines = new List<VirtualMachine>
        {
            new VirtualMachine("192.168.1.1", "1000"),
            new VirtualMachine("192.168.1.2", "0101")
            // Add more VMs as needed
        };

        
        
        

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log(SceneManager.GetActiveScene().buildIndex);

        if(currentSceneIndex == 1){
            
            var availablePairs = lockedFolderPairsData.Where(p => !usedPairs.Contains(p)).ToList();
            if (availablePairs.Count == 0) return;

            // start on the first VM
            currentVM = virtualMachines[0];
            currentVM.FileSystem.GenerateFileSystem(currentDifficulty);

            // Select a random pair
            var pair = availablePairs[Random.Range(0, availablePairs.Count)];

            //find a random unlocked folder
            ConsoleFolder targetFolder = GetRandomUnlockedFolder(currentVM.FileSystem);

            // Create and add the locked folder
            ConsoleFolder lockedFolder = new ConsoleFolder(pair.FolderName, pair.Password, pair.SecurityQuestion);
            targetFolder.AddChild(lockedFolder);
            //define and place level trigger file into locked folder
            ConsoleFile levelTrigger = new ConsoleFile("SystemUpgrade.exe", "System Update");
            lockedFolder.AddFile(levelTrigger);

            //re rondomise target folder
            targetFolder = GetRandomUnlockedFolder(currentVM.FileSystem);

            // Create and add the password file to an unlocked folder
            ConsoleFile passwordFile = new ConsoleFile(pair.FileName, pair.FileContent);
            //place the file in another unlocked folder
            targetFolder.AddFile(passwordFile);

        }
        
        if(currentSceneIndex == 2)
        {
            // start on the first VM
            currentVM = virtualMachines[0];
            currentVM.FileSystem.GenerateFileSystem(currentDifficulty);

            virtualMachines[1].FileSystem.GenerateFileSystem(currentDifficulty);

            var availablePairs = lockedFolderPairsBinary.Where(p => !usedPairs.Contains(p)).ToList();
            if (availablePairs.Count == 0) return;

            // Select a random pair
            var pair = availablePairs[Random.Range(0, availablePairs.Count)];

            //find a random unlocked folder in the first vm
            ConsoleFolder targetFolder = GetRandomUnlockedFolder(virtualMachines[0].FileSystem);
            
            //create a file containing the IP of vm2
            ConsoleFile FileIP = new ConsoleFile("ProxyIP.txt", "");
            targetFolder.AddFile(FileIP);
            
            // Creating a locked folder in VM A
            ConsoleFolder lockedFolderEnd = new ConsoleFolder("TheSecret", "9", "what is (0111 + 0101) - 0011 in decimal?");
            virtualMachines[0].FileSystem.rootFolder.AddChild(lockedFolderEnd);
            ConsoleFile levelTrigger = new ConsoleFile("YouAreNotReal.exe", "Congratulations");
            lockedFolderEnd.AddFile(levelTrigger);

            //re randomise target folder for a folder in vm2
            targetFolder = GetRandomUnlockedFolder(virtualMachines[1].FileSystem);

            // Creating a locked file and placing it in VM 2
            ConsoleFolder lockedFolderBinary = new ConsoleFolder(pair.FolderName, pair.Password, pair.SecurityQuestion);
            targetFolder.AddChild(lockedFolderBinary);
            ConsoleFile FilePassword = new ConsoleFile("DoNotForget.txt", "");
            lockedFolderBinary.AddFile(FilePassword); //password for vm 1 secret folder

            //re randomise target folder for a folder in vm2
            targetFolder = GetRandomUnlockedFolder(virtualMachines[1].FileSystem);

            //paired resource file with locked folder binary
            ConsoleFile lockedFile = new ConsoleFile(pair.FileName, pair.FileContent);
            targetFolder.AddFile(lockedFile);

            
        }
   
    }

    public bool ConnectToVM(string ipAddress, string password = "")
    {
        foreach (var vm in virtualMachines)
        {
            if (vm.IPAddress == ipAddress)
            {
                if (vm.CheckAccess(password))
                {
                    currentVM = vm;
                    Debug.Log($"Connected to {vm.IPAddress}");
                    return true;
                }
                else
                {
                    Debug.Log("Access Denied");
                    return false;
                }
            }
        }

        Debug.Log("VM not found");
        return false;
    }
    public ConsoleFolder GetRandomUnlockedFolder(ConsoleFileSystem fileSystem)
    {
        List<ConsoleFolder> unlockedFolders = fileSystem.GetUnlockedFolders();
        if (unlockedFolders.Count == 0)
        {
            return null; // No unlocked folders available
        }
        return unlockedFolders[Random.Range(0, unlockedFolders.Count)];
    }

    public void CompleteLevel()
    {
        
        currentDifficulty++;
        currentVM.FileSystem.GenerateFileSystem(currentDifficulty);
        // Code to update the game UI and state
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        currentSceneIndex++;

        // Check if the nextSceneIndex is within the range of available scenes
        if (currentSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(currentSceneIndex);
        }
        else
        {
            // If there are no more levels
            // Reload the last level
            SceneManager.LoadScene(SceneManager.sceneCountInBuildSettings - 1);
            
            //todo
            //load a specific scene that indicates game completion:
            
        }
    }  

}


