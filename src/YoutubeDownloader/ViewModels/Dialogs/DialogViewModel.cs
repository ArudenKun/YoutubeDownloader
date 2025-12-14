using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace YoutubeDownloader.ViewModels.Dialogs;

[AutoInterface(Inheritance = [typeof(IViewModel)])]
public abstract partial class DialogViewModel : ViewModel, IDialogViewModel
{
    public DialogViewModel()
    {
        Dialog = new SukiDialog();
    }

    protected ISukiDialog Dialog { get; set; }

    /// <summary>
    /// Gets the title of the dialog.
    /// </summary>
    public virtual string DialogTitle => string.Empty;

    [RelayCommand]
    public void CloseDialog()
    {
        Dialog.Dismiss();
    }

    public void SetDialog(ISukiDialog dialog)
    {
        Dialog = dialog;
    }
}
