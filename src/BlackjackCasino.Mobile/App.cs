namespace BlackjackCasino.Mobile;

public sealed class App : Application
{
    private readonly Game.BlackjackSession session = new();
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigation = new NavigationPage(new LobbyPage(session))
        {
            BarBackgroundColor = Color.FromArgb("#10251F"),
            BarTextColor = Color.FromArgb("#E7C879"),
        };
        return new Window(navigation);
    }
}
