namespace PasswordKeeper.App.Services;

public interface IPreferencesService
{
    AppPreferences Current { get; }
    event EventHandler? Changed;

    void Load();
    void Save(AppPreferences updated);
}
