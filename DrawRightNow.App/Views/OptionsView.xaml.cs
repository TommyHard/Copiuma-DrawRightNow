using DrawRightNow.App.Services;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.Core.ViewModels;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using Window = System.Windows.Window;

namespace DrawRightNow.App.Views;

public partial class OptionsView : Window
{
    private string? _activeGlobalAction = null;
    private ToolType? _activeToolAction = null;

    public OptionsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => RefreshHotkeyLabels();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void LangRu_Click(object sender, RoutedEventArgs e) => ChangeLanguage("ru");
    private void LangEn_Click(object sender, RoutedEventArgs e) => ChangeLanguage("en");

    private void ChangeLanguage(string lang)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.Settings.Language = lang;
        vm.Settings.Save();
        LocalizationManager.SetLanguage(lang);
    }

    private void RefreshHotkeyLabels()
    {
        if (DataContext is not MainViewModel vm) return;
        var h = vm.Settings.Hotkeys;
        var t = vm.Settings.ToolHotkeys;

        // Global
        Btn_Over.Content = h["ToggleOverlay"].DisplayText;
        Btn_Undo.Content = h["Undo"].DisplayText;
        Btn_Redo.Content = h["Redo"].DisplayText;

        Btn_T_Pencil.Content = t["Pencil"];
        Btn_T_Brush.Content = t["Brush"];
        Btn_T_Marker.Content = t["Marker"];
        Btn_T_Eraser.Content = t["Eraser"];
        Btn_T_Rect.Content = t["Rectangle"];
        Btn_T_Ellipse.Content = t["Ellipse"];
        Btn_T_Line.Content = t["Line"];
        Btn_T_Arrow.Content = t["Arrow"];
        Btn_T_Text.Content = t["Text"];
        Btn_T_Knife.Content = t["KnifeDelete"];
        Btn_T_Move.Content = t["Move"];
        Btn_T_Blur.Content = t["Blur"];
        Btn_T_Eye.Content = t["Eyedropper"];
    }

    // --- Global Hotkey's ---
    private void GlobalHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string actionName)
        {
            _activeGlobalAction = actionName;
            SetButtonToListenState(btn);
            RegisterKeyCapture(Window_GlobalHotkeyCaptureKeyDown);
        }
    }

    private void Window_GlobalHotkeyCaptureKeyDown(object sender, KeyEventArgs e)
    {
        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        if (IsModifierKey(key)) return;

        e.Handled = true;
        UnregisterKeyCapture(sender, Window_GlobalHotkeyCaptureKeyDown);

        if (_activeGlobalAction == null || DataContext is not MainViewModel vm) return;

        var modifiers = Keyboard.Modifiers;
        uint win32Modifiers = 0;
        string display = "";

        if (modifiers.HasFlag(ModifierKeys.Control)) { win32Modifiers |= 2; display += "Ctrl + "; }
        if (modifiers.HasFlag(ModifierKeys.Alt)) { win32Modifiers |= 1; display += "Alt + "; }
        if (modifiers.HasFlag(ModifierKeys.Shift)) { win32Modifiers |= 4; display += "Shift + "; }

        display += key.ToString();

        if (vm.Settings.Hotkeys.TryGetValue(_activeGlobalAction, out var cfg))
        {
            cfg.Modifiers = win32Modifiers;
            cfg.VirtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
            cfg.DisplayText = display;
            vm.Settings.Save();
        }

        _activeGlobalAction = null;
        RefreshHotkeyLabels();

        if (this.Owner is MainWindow mw) mw.UpdateGlobalHotkeys();
    }

    // --- Local HotKey's Инструментов ---
    private void ToolHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && Enum.TryParse<ToolType>(btn.Tag.ToString(), out var toolType))
        {
            _activeToolAction = toolType;
            SetButtonToListenState(btn);
            RegisterKeyCapture(Window_ToolHotkeyCaptureKeyDown);
        }
    }

    private bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LWin || key == Key.RWin ||
               key == Key.System || key == Key.DeadCharProcessed;
    }

    private void Window_ToolHotkeyCaptureKeyDown(object sender, KeyEventArgs e)
    {
        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        if (IsModifierKey(key)) return;

        e.Handled = true;
        UnregisterKeyCapture(sender, Window_ToolHotkeyCaptureKeyDown);

        if (_activeToolAction == null || DataContext is not MainViewModel vm) return;

        var modifiers = Keyboard.Modifiers;
        string display = "";

        if (modifiers.HasFlag(ModifierKeys.Control)) display += "Ctrl + ";
        if (modifiers.HasFlag(ModifierKeys.Alt)) display += "Alt + ";
        if (modifiers.HasFlag(ModifierKeys.Shift)) display += "Shift + ";

        display += key.ToString();

        var updatedHotkeys = new System.Collections.Generic.Dictionary<string, string>(vm.Settings.ToolHotkeys);
        updatedHotkeys[_activeToolAction.Value.ToString()] = display;

        vm.Settings.ToolHotkeys = updatedHotkeys;
        vm.Settings.Save();

        vm.NotifySettingsChanged();

        _activeToolAction = null;
        RefreshHotkeyLabels();
    }

    private void SetButtonToListenState(Button btn)
    {
        if (Application.Current.Resources["Opt_PressKey"] is string str)
        {
            btn.Content = str;
        }
        else
        {
            btn.Content = "???";
        }
    }

    private void RegisterKeyCapture(KeyEventHandler handler)
    {
        if (Window.GetWindow(this) is Window window) window.PreviewKeyDown += handler;
    }

    private void UnregisterKeyCapture(object sender, KeyEventHandler handler)
    {
        if (sender is Window window) window.PreviewKeyDown -= handler;
    }
}