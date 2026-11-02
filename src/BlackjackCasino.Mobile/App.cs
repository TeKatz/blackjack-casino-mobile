using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page root;
        try
        {
            var store = new MobileSaveStore(FileSystem.AppDataDirectory);
            MobileState? saved = store.Load();
            BlackjackSession session = saved is null ? new BlackjackSession(persist: store.Save)
                : BlackjackSession.Restore(saved, store.Save);
            if (saved is null) { store.Save(session.Export()); }
            VisualMotion.Reduced = () => session.Settings.ReduceInterfaceMotion;
            root = new LobbyPage(session, store.RecoveryNotice);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            root = new ContentPage
            {
                Title = "Profile unavailable", BackgroundColor = Ink,
                Content = new VerticalStackLayout
                {
                    Padding = 28, Spacing = 24,
                    Children = { Text("Your profile needs attention", 28, Gold), Text(exception.Message, 17, Muted),
                        Text("Existing save files have been left in place. Close and reopen the app after resolving the problem. Reinstalling removes local data.", 16, Muted) },
                },
            };
        }
        return new Window(new NavigationPage(root) { BarBackgroundColor = Color.FromArgb("#0A1513"), BarTextColor = Gold });
    }
}
