using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Ensure the script is attached to a GameObject with a TMP_InputField component
[RequireComponent(typeof(TMP_InputField))]
public class NotepadManager : TMP_InputField
{
    public override void OnSelect(BaseEventData eventData)
    {
        // Call the base class version of OnSelect to ensure basic functionality is preserved
        base.OnSelect(eventData);
        
        // Deselect the text by setting the caret position to the end of the text
        // This prevents the text from being highlighted
        this.caretPosition = this.text.Length;
        this.selectionAnchorPosition = this.text.Length;
        this.selectionFocusPosition = this.text.Length;
    }
}
