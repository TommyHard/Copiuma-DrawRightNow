using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrawRightNow.Core.Mvvm;

/// <summary>
/// Базовый класс для всех ViewModel / Model-объектов, поддерживающих INotifyPropertyChanged
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}