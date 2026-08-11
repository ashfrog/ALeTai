using UnityEngine;
using UnityEngine.UI;

public class ConfirmationDialog : MonoBehaviour
{
    public GameObject dialogBox;
    public Text messageText;
    public Button confirmButton;
    public Button cancelButton;

    private System.Action onConfirm;
    private System.Action onCancel;

    // Display the dialog with the specified message and actions
    public void Show(string message, System.Action onConfirmAction, System.Action onCancelAction)
    {
        messageText.text = message;
        onConfirm = onConfirmAction;
        onCancel = onCancelAction;
        dialogBox.SetActive(true);
    }

    // Called when the confirm button is clicked
    public void OnConfirmButtonClicked()
    {
        dialogBox.SetActive(false);
        onConfirm?.Invoke();
    }

    // Called when the cancel button is clicked
    public void OnCancelButtonClicked()
    {
        dialogBox.SetActive(false);
        onCancel?.Invoke();
    }
}