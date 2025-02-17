using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TerminalManager : MonoBehaviour
{
     public GameObject directoryLine;
     public GameObject responseLine;

     public TMP_InputField terminalInput;
     public TMP_Text directoryLineMain;
     public GameObject userInputLine;
     public ScrollRect sr;
     public GameObject msgList;

     Interpreter interpreter;
     private FileManager fileManager;

     private void Start()
     {
          interpreter = GetComponent<Interpreter>();
          fileManager = GetComponent<FileManager>();

          //focus input line
          terminalInput.ActivateInputField();
          terminalInput.Select();

          DisplayStartupMessage(); // Display title or welcome message at startup
     }

     // use to display a boot up sequence 
     public void DisplayStartupMessage()
     {
          //find out what level we are on
          int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

          //display terminal bootup message
          List<string> startupMessages = interpreter.LoadTitle("ascii.txt", "red", 1);
          AddInterpreterLines(startupMessages);

          List<string> TerminalNotification = new List<string>   {"Connected to 192.168.1.1",
                                                                 "@?&*!~# Has Connected..."};

          List<string> helper = new List<string>{ "<???>Hello! Sorry for breaking in but I'm here to assist!", 
                                                  "<???>Try typing 'help' and get a feel for things.", 
                                                  "<???>Theres a system upgrade for you somewhere on this system.",
                                                  "<???>You should look for locked folders and some files might",
                                                  "<???>be able to help crack open folders. Also, dude who owns",
                                                  "<???>this system was crazy, obsessed with data types of",
                                                  "<???>all things, keep your eyes peeled, good luck!"};

          // if this is the first level
          if (currentSceneIndex == 1)
          {
               AddInterpreterLines(TerminalNotification);
               AddHelperLines(helper);
          }

          // if this is the second level
          if (currentSceneIndex == 2)
          {
               List<string> TerminalNotification2 = new List<string>{"Connected to 192.168.1.1"};
               helper = new List<string>{    "<???>See?!", 
                                             "<???>Told you I was here to help!", 
                                             "<Askie>My name is Askie, think of me like your assistant.", 
                                             "<Askie>Anyway now you can take notes to keep track of stuff!", 
                                             "<Askie>Just a tip, this system seems to be part of a small",
                                             "<Askie>network, maybe there's more to find than meets the eye." };
               AddInterpreterLines(TerminalNotification2);
               AddHelperLines(helper);
               
          }
          
          // set user input line to the bottom of the list
          userInputLine.transform.SetAsLastSibling();
     }

     private void OnGUI()
     {
          if(terminalInput.isFocused && terminalInput.text != "" && Input.GetKeyDown(KeyCode.Return))
          {
                    //store user input
                    string userInput = terminalInput.text;
                    
                    //clear input field
                    ClearInputField();

                    //instantiate game object with directory prefix
                    AddDirectoryLine(userInput);

                    // add the interpretation lines
                    int lines = AddInterpreterLines(interpreter.Interpret(userInput));

                    //scroll to the bottome of the scroll rect
                    ScrollToBottom(lines);

                    //move user input to the bottom
                    userInputLine.transform.SetAsLastSibling();

                    AdjustRectTransform(directoryLineMain);

                    //delete excess text
                    CurateTerminalOutput(200);

                    //refocus input line
                    terminalInput.ActivateInputField();
                    terminalInput.Select();
          }
     }
   
     void ClearInputField()
     {
          terminalInput.text = "";
     }

     void AddDirectoryLine(string userInput)
     {    
          //instantiate directory line
          GameObject msg = Instantiate(directoryLine, msgList.transform);

          //set child index
          msg.transform.SetSiblingIndex(msgList.transform.childCount - 1);

          //set text of new game object
          msg.GetComponentsInChildren<TMP_Text>()[1].text = userInput;
          msg.GetComponentsInChildren<TMP_Text>()[0].text = "G:/" + fileManager.GetCurrentFolderPath() + ">";
               
          // and resize
          AdjustRectTransform(msg.GetComponentsInChildren<TMP_Text>()[1]);
          AdjustRectTransform(msg.GetComponentsInChildren<TMP_Text>()[0]);         
     }  

     int AddInterpreterLines(List<string> interpretation)
     {
          for(int i = 0; i < interpretation.Count; i++)
          {
               //instantiate response line
               GameObject res = Instantiate(responseLine, msgList.transform);

               //set it to the end of all the messages
               res.transform.SetAsLastSibling();

               //set the text of the response line to the text in the interpreter
               res.GetComponentInChildren<TMP_Text>().text = interpretation[i];

          }

          return interpretation.Count;
     }

     int AddHelperLines(List<string> interpretation)
     {
          for(int i = 0; i < interpretation.Count; i++)
          {
               //instantiate response line
               GameObject res = Instantiate(responseLine, msgList.transform);

               //set it to the end of all the messages
               res.transform.SetAsLastSibling();

               //set the text of the response line to the text in the interpreter
               res.GetComponentInChildren<TMP_Text>().text = fileManager.ColorString(interpretation[i], "purple");

          }

          return interpretation.Count;
     }

     //scroll to bottom of the screen based on lines being displayed
     void ScrollToBottom(int lines)
     {
          Canvas.ForceUpdateCanvases(); // Force the Canvas to update

          sr.verticalNormalizedPosition = 0; // Scroll immediately to the bottom
     }

     //trims overflow when there are too many lines that cant be seen
     void CurateTerminalOutput(int lines)
          { 
               if(msgList.transform.childCount > lines)
               {
                    int overflow = msgList.transform.childCount - lines;
                    
                    for(int i = 0; i < overflow; i++)
                    {
                         //Decrease the size of the list by one message height (currently 35 units)
                         msgList.GetComponent<RectTransform>().sizeDelta = msgList.GetComponent<RectTransform>().sizeDelta - new Vector2(0, 35.0f);
                         Destroy(msgList.transform.GetChild(i).gameObject);

                    }

               }

          }

     // This method allows external components to display text in the terminal
     public void DisplayText(List<string> lines)
     {
          // Add the lines to the terminal
          AddInterpreterLines(lines);

          // Optionally scroll to the bottom if needed
          ScrollToBottom(lines.Count);
     }

     void AdjustRectTransform(TMP_Text textComponent)
     {    
          // Force the text component to update
          textComponent.ForceMeshUpdate();

          float maxWidth = Screen.width * 0.8f; // Maximum width 80% of the screen width

          // Get the preferred values for the current text content
          Vector2 preferredValues = textComponent.GetPreferredValues(float.PositiveInfinity, float.PositiveInfinity);

          // Calculate the new width, ensuring it doesn't exceed maxWidth
          float adjustedWidth = Mathf.Min(preferredValues.x, maxWidth);

          // Adjust width of the RectTransform
          textComponent.rectTransform.sizeDelta = new Vector2(adjustedWidth, textComponent.rectTransform.sizeDelta.y);

          LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
     }

}
