public class LockedFolderPair
{
    public string FolderName { get; set; }
    public string Password { get; set; }
    public string FileName { get; set; }
    public string FileContent { get; set; }
    public string SecurityQuestion { get; set; }
    

    public LockedFolderPair(string folderName, string password, string fileName, string fileContent, string securityQuestion = "")
    {
        FolderName = folderName;
        Password = password;
        FileName = fileName;
        FileContent = fileContent;
        SecurityQuestion = securityQuestion;
    }
}

