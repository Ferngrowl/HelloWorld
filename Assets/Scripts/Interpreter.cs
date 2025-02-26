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

    public IEnumerator InterpretAsync(string userInput, System.Action<List<string>> callback)
    {
        List<string> response = new List<string>();
        bool errorOccurred = false;
        Exception caughtException = null;

        var enumerator = InterpretCoroutine(userInput, response, ex => {
            errorOccurred = true;
            caughtException = ex;
        });
        
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }

        if (errorOccurred)
        {
            Debug.LogError($"Command error: {caughtException}");
            response.Add("System error: Command failed to execute");
        }

        callback(response);
    }

    private IEnumerator InterpretCoroutine(string userInput, List<string> response, Action<Exception> errorHandler)
    {
        if (fileManager.awaitingPasswordInput)
        {
            try
            {
                response = fileManager.ProcessPasswordAttempt(userInput);
            }
            catch (Exception ex)
            {
                errorHandler(ex);
            }
            yield break;
        }

        string[] args = userInput.Split();
        if (args.Length == 0) yield break;

        IEnumerator commandCoroutine = fileManager.ProcessCommandAsync(
            args[0], 
            args, 
            commandResponse => response = commandResponse
        );

        yield return RunWithErrorHandling(commandCoroutine, errorHandler);
    }

    private IEnumerator RunWithErrorHandling(IEnumerator coroutine, Action<Exception> errorHandler)
    {
        while (true)
        {
            bool moveNext;
            Exception exception = null;
            try
            {
                moveNext = coroutine.MoveNext();
            }
            catch (Exception ex)
            {
                moveNext = false;
                exception = ex;
            }

            if (exception != null)
            {
                errorHandler(exception);
                yield break;
            }

            if (!moveNext)
                yield break;

            yield return coroutine.Current;
        }
    }

    public string ColorString(string s, string color)
    {
        string leftTag = "<color=" + color + ">";
        string rightTag = "</color>";

        return leftTag + s + rightTag;
    }

    public IEnumerator LoadTitle(string path, string color, int spacing, System.Action<List<string>> callback)
    {
        List<string> result = new List<string>();
        
        // Use FileManager's WebGL-compatible loading
        yield return fileManager.StartCoroutine(fileManager.LoadFile(path, color, spacing, lines => {
            result = lines;
            callback?.Invoke(result);
        }));
    }

}
