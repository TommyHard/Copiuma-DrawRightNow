using System.Globalization;
using System.Windows;
using Application = System.Windows.Application;

namespace DrawRightNow.App.Services;

/// <summary>
/// Подменяет merged-словарь со строками EN/RU в Application.Resources
/// </summary>
public static class LocalizationManager
{
    public const string LangRu = "ru";
    public const string LangEn = "en";

    public static string CurrentLanguage { get; private set; } = LangRu;

    public static void Initialize()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == LangRu
            ? LangRu : LangEn;
        SetLanguage(lang);
    }

    public static void Toggle()
        => SetLanguage(CurrentLanguage == LangRu ? LangEn : LangRu);

    public static void SetLanguage(string lang)
    {
        if (Application.Current is null) return;
        var resources = Application.Current.Resources.MergedDictionaries;

        var existing = resources
            .Where(d => d.Source is not null && d.Source.OriginalString.Contains("Strings."))
            .ToList();
        foreach (var d in existing) resources.Remove(d);

        var uri = new Uri(
            $"pack://application:,,,/Copiuma.DrawRightNow;component/Resources/Strings.{lang}.xaml",
            UriKind.Absolute);
        var dict = new ResourceDictionary { Source = uri };
        resources.Add(dict);

        CurrentLanguage = lang;

        Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
    }
}