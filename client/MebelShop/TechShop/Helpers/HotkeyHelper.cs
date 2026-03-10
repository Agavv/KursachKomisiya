using System.Windows;
using System.Windows.Input;
using MebelShop.Auth;
using MebelShop.Helpers;

namespace MebelShop.Helpers
{
    public static class HotkeyHelper
    {
        public static void AttachHotkey(FrameworkElement element, Key key, ModifierKeys modifiers, Action action)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var gesture = new KeyGesture(key, modifiers);
            var command = new RoutedCommand();
            command.InputGestures.Add(gesture);

            element.Loaded += (_, __) =>
            {
                var window = Window.GetWindow(element);
                if (window != null)
                {
                    window.CommandBindings.Add(new CommandBinding(command, (s, e) => action()));
                }
            };
        }
        public static void AttachHotkey(FrameworkElement element, Key key, Action action)
        {
            AttachHotkey(element, key, ModifierKeys.None, action);
        }

        public static void AttachThemeHotkey(Window window)
        {
            var toggleThemeGesture = new KeyGesture(Key.T, ModifierKeys.Control);
            var toggleThemeCommand = new RoutedCommand();
            toggleThemeCommand.InputGestures.Add(toggleThemeGesture);

            window.CommandBindings.Add(new CommandBinding(toggleThemeCommand, ToggleThemeCommandExecuted));
        }

        public static void ToggleThemeCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
        }

        public static void AttachLogoutHotkey(Window window)
        {
            var logoutGesture = new KeyGesture(Key.L, ModifierKeys.Control);
            var logoutCommand = new RoutedCommand();

            logoutCommand.InputGestures.Add(logoutGesture);

            window.CommandBindings.Add(new CommandBinding(logoutCommand, LogoutCommandExecuted));
        }

        public async static void LogoutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await SettingsService.LogoutAsync(sender as Window);
        }
    }
}
