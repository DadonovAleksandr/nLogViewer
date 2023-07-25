namespace nLogViewer.Services.UserDialogService;

internal interface IUserDialogService
{
    void ShowInformation(string message, string title);
    void ShowWarning(string message, string title);
    void ShowError(string message, string title);
    bool Confirm(string message, string title, bool exclamation = false);
}