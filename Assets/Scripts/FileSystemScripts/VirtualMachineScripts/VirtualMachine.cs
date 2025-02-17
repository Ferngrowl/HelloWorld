public class VirtualMachine
{
    public string IPAddress { get; private set; }
    public string Hostname { get; private set; }
    public ConsoleFileSystem FileSystem { get; private set; }
    private string password; // Add a password field

    // Constructor now includes a password parameter
    public VirtualMachine(string ipAddress, string password)
    {
        IPAddress = ipAddress;
        this.password = password; // Initialize the password
        FileSystem = new ConsoleFileSystem(); // Initialize a new filesystem for this VM
    }

    // Method to check if the provided password is correct
    public bool CheckAccess(string inputPassword)
    {
        return inputPassword == password;
    }

}
