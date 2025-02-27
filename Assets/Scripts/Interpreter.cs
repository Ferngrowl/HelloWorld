using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpreter : MonoBehaviour
{
    // Dictionary for rich text colors (used for formatting if needed).
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

    // Reference to the FileManager component for command dispatch.
    private FileManager fileManager;

    /// <summary>
    /// Initializes the Interpreter by retrieving the FileManager component.
    /// </summary>
    void Start()
    {
        fileManager = GetComponent<FileManager>();
        if (fileManager == null)
        {
            Debug.LogError("FileManager component not found on " + gameObject.name);
        }
    }

    /// <summary>
    /// Interprets the user's input command asynchronously and returns a list of response strings.
    /// </summary>
    /// <param name="userInput">The full command entered by the user.</param>
    /// <param name="callback">Callback to deliver the response lines.</param>
    public IEnumerator InterpretAsync(string userInput, Action<List<string>> callback)
    {
        List<string> response = new List<string>();
        bool errorOccurred = false;
        Exception caughtException = null;

        // Execute the main interpretation coroutine with error handling.
        yield return StartCoroutine(InterpretCoroutine(userInput, response, ex =>
        {
            errorOccurred = true;
            caughtException = ex;
        }));

        if (errorOccurred)
        {
            Debug.LogError($"Command error: {caughtException}");
            response.Add("System error: Command failed to execute");
        }

        callback?.Invoke(response);
    }

    /// <summary>
    /// Main interpreter coroutine that dispatches commands to the FileManager.
    /// If awaiting a password, it handles that separately.
    /// </summary>
    private IEnumerator InterpretCoroutine(string userInput, List<string> response, Action<Exception> errorHandler)
    {
        // If the FileManager is waiting for a password, process it immediately.
        if (fileManager.awaitingPasswordInput)
        {
            try
            {
                response = fileManager.ProcessPasswordAttempt(userInput);
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
            yield break;
        }

        // Split the input into command arguments while removing any extra spaces.
        string[] args = userInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0)
            yield break;

        // Dispatch the command to FileManager and capture the response.
        IEnumerator commandCoroutine = fileManager.ProcessCommandAsync(
            args[0],
            args,
            commandResponse => { response = commandResponse; }
        );

        yield return StartCoroutine(RunWithErrorHandling(commandCoroutine, errorHandler));
    }

    /// <summary>
    /// Wraps a coroutine execution with error handling.
    /// </summary>
    private IEnumerator RunWithErrorHandling(IEnumerator coroutine, Action<Exception> errorHandler)
    {
        while (true)
        {
            bool moveNext;
            try
            {
                moveNext = coroutine.MoveNext();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
                yield break;
            }

            if (!moveNext)
                yield break;

            yield return coroutine.Current;
        }
    }

    /// <summary>
    /// Wraps a string with Unity rich text color tags.
    /// </summary>
    public string ColorString(string s, string color)
    {
        return $"<color={color}>{s}</color>";
    }

    /// <summary>
    /// Loads a title file via the FileManager's file-loading method.
    /// </summary>
    public IEnumerator LoadTitle(string path, string color, int spacing, Action<List<string>> callback)
    {
        List<string> result = new List<string>();
        yield return StartCoroutine(fileManager.LoadFile(path, color, spacing, lines =>
        {
            result = lines;
            callback?.Invoke(result);
        }));
    }
}
