using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Interpreter : MonoBehaviour
{

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

    List<string> response = new List<string>();

     private FileManager fileManager;

     void Start()
    {
        fileManager = GetComponent<FileManager>();
        if (fileManager == null)
        {
            Debug.LogError("FileManager component not found on " + gameObject.name);
        }

    }

    public List<string> Interpret(string userInput)
    {
        response.Clear();

         // Check if awaiting password input before splitting the command
        if (fileManager.awaitingPasswordInput == true) 
        {
            // Directly forward the userInput as a password attempt
            response.AddRange(fileManager.ProcessPasswordAttempt(userInput));
        }
        else
        {
            string[] args = userInput.Split();

            switch (args[0])
            {
                case "help":
                    if (fileManager != null)
                    {
                        response.AddRange(fileManager.ProcessCommand(args[0], args));
                    }
                    break;
                
                case "dir":
                    if (fileManager != null)
                    {
                        response.AddRange(fileManager.ProcessCommand(args[0], args));
                    }
                    break;

                case "cd":
                    if (fileManager != null)
                    {
                        response.AddRange(fileManager.ProcessCommand(args[0], args));
                    }
                    break;

                case "open":
                    if (fileManager != null)
                    {
                        response.AddRange(fileManager.ProcessCommand(args[0], args));
                    }
                    break;
                
                case "connect":
                    if (fileManager != null)
                    {
                        response.AddRange(fileManager.ProcessCommand(args[0], args));
                    }
                    break;

                // Handle other non-file system commands
                case "displaytitle":
                    //load title
                    LoadTitle("ascii.txt", "red", 2);
                    break;

                case "exit":
                    Application.Quit();
                    break;

                default:
                    response.Add("Command not recognized.");
                    response.Add("Type \"help\" for a list of commands.");
                    break;
            } 
        }
        return response;
    }

    public string ColorString(string s, string color)
    {
        string leftTag = "<color=" + color + ">";
        string rightTag = "</color>";

        return leftTag + s + rightTag;
    }

    public List<string> LoadTitle(string path, string color, int spacing)
    {
        StreamReader file = new StreamReader(Path.Combine(Application.streamingAssetsPath, path));

        while(!file.EndOfStream)
        {
            response.Add(ColorString(file.ReadLine(), colors[color]));
        }

        for(int i = 0; i < spacing; i++)
        {
            response.Add("");
        }
        
        file.Close();
        return response;
    }

}
