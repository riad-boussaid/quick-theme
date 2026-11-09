using System.Runtime.ExceptionServices;
using QuickTheme.UI;

namespace QuickTheme.Tests;

[Collection("WinForms")]
public sealed class WinFormsSmokeTests
{
    [Fact]
    public void SettingsAndFlyout_CanBeCreatedAndDisposed()
    {
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                using var settings = new SettingsForm(
                    scheduleChanged: () => { },
                    checkForUpdates: () => { },
                    applyHotkeys: (_, _) => null);
                settings.CreateControl();
                _ = settings.Handle;
                settings.RefreshFromSettings();

                using var flyout = new ThemeFlyoutForm();
                flyout.CreateControl();
                _ = flyout.Handle;
                flyout.RefreshThemeState();
                flyout.RefreshScheduleStatus();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The WinForms smoke test timed out.");

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}

[CollectionDefinition("WinForms", DisableParallelization = true)]
public sealed class WinFormsCollection;
