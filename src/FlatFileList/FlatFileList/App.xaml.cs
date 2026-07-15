using System.Globalization;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Markup;

namespace FlatFileList;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        if(!FlatFileList.Properties.Settings.Default.IsUpgreated)
        {
            using(Disposable.Create(()=> { FlatFileList.Properties.Settings.Default.IsUpgreated = true; FlatFileList.Properties.Settings.Default.Save(); }))
            {
                FlatFileList.Properties.Settings.Default.Upgrade();
            }
        }

        if (FlatFileList.Properties.Settings.Default.CustomOpenProcess == null)
            FlatFileList.Properties.Settings.Default.CustomOpenProcess = [];

        if (FlatFileList.Properties.Settings.Default.ExcludeExtensions == null)
            FlatFileList.Properties.Settings.Default.ExcludeExtensions = [];
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
          typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
              XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        base.OnStartup(e);
    }
}

