using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrawRightNow.Core.ViewModels;
using DrawRightNow.Core.Models;
using DrawRightNow.Core.Models.Tools;
using DrawRightNow.App.Services;

// Строгие псевдонимы для WPF, исключающие конфликты с Windows Forms
using UserControl = System.Windows.Controls.UserControl;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Application = System.Windows.Application;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;

namespace DrawRightNow.App.Views;

public partial class OptionsView : UserControl
{
    private string? _activeGlobalAction = null;
    private ToolType? _activeToolAction = null;

    public OptionsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => RefreshHotkeyLabels();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.IsOptionsOpen = false;
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

        // Глобальные
        Btn_Over.Content = h["ToggleOverlay"].DisplayText;
        Btn_Draw.Content = h["ToggleDrawing"].DisplayText;
        Btn_Undo.Content = h["Undo"].DisplayText;
        Btn_Redo.Content = h["Redo"].DisplayText;
        Btn_Copy.Content = h["Copy"].DisplayText;

        // Локальные инструменты
        Btn_T_Pencil.Content = t[ToolType.Pencil].ToString();
        Btn_T_Brush.Content = t[ToolType.Brush].ToString();
        Btn_T_Marker.Content = t[ToolType.Marker].ToString();
        Btn_T_Eraser.Content = t[ToolType.Eraser].ToString();
        Btn_T_Rect.Content = t[ToolType.Rectangle].ToString();
        Btn_T_Ellipse.Content = t[ToolType.Ellipse].ToString();
        Btn_T_Line.Content = t[ToolType.Line].ToString();
        Btn_T_Arrow.Content = t[ToolType.Arrow].ToString();
        Btn_T_Text.Content = t[ToolType.Text].ToString();
        Btn_T_Knife.Content = t[ToolType.KnifeDelete].ToString();
        Btn_T_Move.Content = t[ToolType.Move].ToString();
        Btn_T_Blur.Content = t[ToolType.Blur].ToString();
        Btn_T_Eye.Content = t[ToolType.Eyedropper].ToString();
    }

    // --- Обработка Глобальных Хоткеев ---
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
        e.Handled = true;
        UnregisterKeyCapture(sender, Window_GlobalHotkeyCaptureKeyDown);

        if (_activeGlobalAction == null || DataContext is not MainViewModel vm) return;

        var modifiers = Keyboard.Modifiers;
        uint win32Modifiers = 0;
        string display = "";

        if (modifiers.HasFlag(ModifierKeys.Control)) { win32Modifiers |= 2; display += "Ctrl + "; }
        if (modifiers.HasFlag(ModifierKeys.Alt)) { win32Modifiers |= 1; display += "Alt + "; }
        if (modifiers.HasFlag(ModifierKeys.Shift)) { win32Modifiers |= 4; display += "Shift + "; }

        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        display += key.ToString();

        if (vm.Settings.Hotkeys.TryGetValue(_activeGlobalAction, out var cfg))
        {
            cfg.Modifiers = win32Modifiers;
            cfg.VirtualKey = vk;
            cfg.DisplayText = display;
            vm.Settings.Save();
        }

        _activeGlobalAction = null;
        RefreshHotkeyLabels();

        if (sender is MainWindow mw) mw.UpdateGlobalHotkeys();
    }

    // --- Обработка Локальных Хоткеев Инструментов ---
    private void ToolHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && Enum.TryParse<ToolType>(btn.Tag.ToString(), out var toolType))
        {
            _activeToolAction = toolType;
            SetButtonToListenState(btn);
            RegisterKeyCapture(Window_ToolHotkeyCaptureKeyDown);
        }
    }

    private void Window_ToolHotkeyCaptureKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        UnregisterKeyCapture(sender, Window_ToolHotkeyCaptureKeyDown);

        if (_activeToolAction == null || DataContext is not MainViewModel vm) return;

        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

        // Сохраняем как строку (ToString)
        vm.Settings.ToolHotkeys[_activeToolAction.Value] = key.ToString();
        vm.Settings.Save();

        _activeToolAction = null;
        RefreshHotkeyLabels();
    }

    // --- Вспомогательные методы ---
    private void SetButtonToListenState(Button btn)
    {
        if (Application.Current.Resources["Opt_PressKey"] is string str)
            btn.Content = str;
        else
            btn.Content = "???";
    }

    private void RegisterKeyCapture(KeyEventHandler handler)
    {
        if (Window.GetWindow(this) is Window window) window.KeyDown += handler;
    }

    private void UnregisterKeyCapture(object sender, KeyEventHandler handler)
    {
        if (sender is Window window) window.KeyDown -= handler;
    }
}