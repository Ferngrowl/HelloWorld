using System.Collections.Generic;

public class ConsoleFolder
{
    public string Name { get; private set; }
    public string Type => "Folder";
    public Dictionary<string, ConsoleFolder> Children { get; private set; }
    public Dictionary<string, ConsoleFile> Files { get; private set; } = new Dictionary<string, ConsoleFile>();
    public ConsoleFolder Parent { get; private set; }
    private string password; // Store the password for locking
    public bool IsLocked { get; private set; } = false; // Indicate if the folder is locked
    public string SecurityQuestion { get; set; }

    // Constructor for an unlocked folder
    public ConsoleFolder(string name)
    {
        Name = name;
        Children = new Dictionary<string, ConsoleFolder>();
        
    }

    // Additional constructor to create a locked folder
    public ConsoleFolder(string name, string password, string securityQuestion) : this(name) // Call the base constructor
    {
        Lock(password); // Lock the folder with the given password
        SecurityQuestion = securityQuestion;
    }

    public void AddChild(ConsoleFolder child)
    {
        string key = child.Name; // Use original case
        if (!Children.ContainsKey(key))
        {
            Children.Add(key, child);
            child.Parent = this;
        }
    }

    public void AddFile(ConsoleFile file)
    {
        Files[file.Name.ToLower()] = file; // Store files, indexed by lowercase name for case-insensitive access
    }

    // Lock the folder with a password
    public void Lock(string password)
    {
        this.password = password;
        IsLocked = true;
    }

    // Attempt to unlock the folder with a password
    public bool Unlock(string password)
    {
        if (this.password == password)
        {
            IsLocked = false;
            return true;
        }
        else
        {
            return false;
        }
    }
}
