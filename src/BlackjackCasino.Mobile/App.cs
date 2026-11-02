namespace BlackjackCasino.Mobile;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigation = new NavigationPage(new LobbyPage())
        {
            BarBackgroundColor = Color.FromArgb("#10251F"),
            BarTextColor = Color.FromArgb("#E7C879"),
        };
        return new Window(navigation);
    }
}
