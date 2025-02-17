using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text;

public class ConsoleFileSystem : MonoBehaviour
{
    
    public ConsoleFolder rootFolder;
    public LockedFolderPair lockedFolderPair;

    //list of prefixes and suffixes for random fodler name generation
    List<string> prefixes = new List<string> { "Data", "Temp", "Net", "Secure", "Program", "Common"};
    List<string> suffixes = new List<string> { "Files", "Storage", "Bin", "Docs", "Logs", "Cache",};

    
    //list of folders always present in every system
    string[] baseFolders = new string[] { "System", "User", "Apps", "Documents" };

    //list of locked folders and their accompanying password files
    List<LockedFolderPair> lockedFolderPairs = new List<LockedFolderPair>
    {
        new LockedFolderPair("SystemUpgrade", "string", "DataTypes.txt", "","Which data type is used for a sequence of characters?"),
        new LockedFolderPair("SystemUpgrade", "character", "DataTypes.txt", "","Which data type is used to store a single character?"),
        new LockedFolderPair("SystemUpgrade", "integer", "DataTypes.txt", "","Which data type is used to store whole numbers?"),
        new LockedFolderPair("SystemUpgrade", "float", "DataTypes.txt", "","Which data type is used for numbers that contain decimal points?"),
        new LockedFolderPair("SystemUpgrade", "boolean", "DataTypes.txt", "","Which data type is used where data is restricted to True/False options?"),
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
          
    }

    public void GenerateFileSystem(int difficulty)
    {
        rootFolder = new ConsoleFolder("root");

        foreach (var baseFolder in baseFolders)
        {
            ConsoleFolder folder = new ConsoleFolder(baseFolder);
            PopulateFolder(folder, difficulty);
            rootFolder.AddChild(folder);
        }

        Debug.Log("File System Generated.");
    }

    public ConsoleFolder GetRootFolder()
    {
        return rootFolder;
    }

    void PopulateFolder(ConsoleFolder folder, int difficulty)
    {
        string content = GenerateRandomFileContent(1);
        string fileName = GeneratePrefixSuffixName() + ".txt"; 
        ConsoleFile file = new ConsoleFile(fileName, content);
        folder.AddFile(file);
        
        // Change to adjust difficulty baseline
        int baseFolders = 1 + difficulty;

        for (int i = 0; i < baseFolders; i++)
        {
            string folderName = GeneratePrefixSuffixName();

            ConsoleFolder subFolder = new ConsoleFolder(folderName);
            folder.AddChild(subFolder);

            if (UnityEngine.Random.value > 0.10f) // Random chance to add more depth
            {
                PopulateFolder(subFolder, difficulty - 1); // Recursively add depth
            }
        }

    }
    private string GeneratePrefixSuffixName()
    {
        string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Count)];
        string suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Count)];
        return prefix + suffix;
    }

    string GenerateRandomFileContent(int sentenceCount)
    {
        StringBuilder fileContent = new StringBuilder();
        for (int i = 0; i < sentenceCount; i++)
        {
            fileContent.AppendLine(sentencePool[Random.Range(0, sentencePool.Count)]);
        }
        return fileContent.ToString();
    }

    public List<ConsoleFolder> GetUnlockedFolders()
    {
        List<ConsoleFolder> unlockedFolders = new List<ConsoleFolder>();
        AddUnlockedFoldersToList(rootFolder, unlockedFolders);
        return unlockedFolders;
    }
    
    public void AddUnlockedFoldersToList(ConsoleFolder folder, List<ConsoleFolder> list)
    {
        if (!folder.IsLocked)
        {
            list.Add(folder);
            foreach (var child in folder.Children.Values)
            {
                AddUnlockedFoldersToList(child, list);
            }
        }
    }

    


    

}
