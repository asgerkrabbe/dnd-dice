using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DiceRoller.Wpf.Commands;

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        var convertedParameter = ConvertParameter(parameter);
        return _canExecute?.Invoke(convertedParameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        var convertedParameter = ConvertParameter(parameter);
        _execute(convertedParameter);
    }

    private T? ConvertParameter(object? parameter)
    {
        if (parameter is T typedParameter)
        {
            return typedParameter;
        }

        if (parameter != null)
        {
            try
            {
                // Use TypeConverter to convert string parameters to target type
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(parameter.GetType()))
                {
                    return (T?)converter.ConvertFrom(parameter);
                }
            }
            catch
            {
                // Type conversion failed; return default value to gracefully handle
                // incompatible parameter types without breaking command execution
            }
        }

        return default;
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
