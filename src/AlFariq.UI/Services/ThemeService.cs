namespace AlFariq.UI.Services;

public enum ThemePreference
{
    Auto,
    Light,
    Dark,
}

/// <summary>Préférence de thème : suit l'appareil par défaut (Auto) ou force clair / sombre.</summary>
public sealed class ThemeService
{
    private readonly ShellInterop _interop;

    public ThemeService(ShellInterop interop) => _interop = interop;

    public ThemePreference Current { get; private set; } = ThemePreference.Auto;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var stored = await _interop.InvokeAsync<string>("getTheme");
        Current = Parse(stored);
        Changed?.Invoke();
    }

    public async Task SetAsync(ThemePreference preference)
    {
        Current = preference;
        await _interop.InvokeVoidAsync("setTheme", ToKey(preference));
        Changed?.Invoke();
    }

    /// <summary>Valeur inconnue ou absente : « Auto ».</summary>
    public static ThemePreference Parse(string? key) => key switch
    {
        "light" => ThemePreference.Light,
        "dark" => ThemePreference.Dark,
        _ => ThemePreference.Auto,
    };

    public static string ToKey(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        _ => "auto",
    };
}
