using System.Diagnostics;
using System.Windows.Input;

namespace GameDeck.Infrastructure;

/// <summary>Keeps async-void ICommand exceptions inside the UI error boundary.</summary>
public sealed class AsyncRelayCommand(
    Func<object?, Task> execute,
    Func<Exception, Task> onError,
    Predicate<object?>? canExecute = null) : ICommand
{
    private bool _isExecuting;
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (canExecute?.Invoke(parameter) ?? true);
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter)) return;
        _isExecuting = true;
        NotifyCanExecuteChanged();
        try
        {
            await execute(parameter);
        }
        catch (Exception ex)
        {
            try { await onError(ex); }
            catch (Exception dialogError) { Debug.WriteLine(dialogError); }
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }
}
