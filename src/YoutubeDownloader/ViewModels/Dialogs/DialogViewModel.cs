using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace YoutubeDownloader.ViewModels.Dialogs;

public abstract class DialogViewModel : DialogViewModel<bool>;

[AutoInterface(Inheritance = [typeof(IViewModel)])]
public abstract partial class DialogViewModel<TResult> : ViewModel, IDialogViewModel<TResult>
{
    public DialogViewModel()
    {
        Dialog = new SukiDialog();
    }

    public TaskCompletionSource<bool> Completion { get; } = new();

    protected ISukiDialog Dialog { get; private set; }

    [ObservableProperty]
    public partial TResult? DialogResult { get; private set; }

    /// <summary>
    /// Gets the title of the dialog.
    /// </summary>
    public virtual string DialogTitle => string.Empty;

    [RelayCommand]
    protected void Close(TResult? result = default)
    {
        DialogResult = result;
        Completion.SetResult(result is not null);
        Dialog.Dismiss();
    }

    public void SetDialog(ISukiDialog dialog)
    {
        Dialog = dialog;
    }
}
