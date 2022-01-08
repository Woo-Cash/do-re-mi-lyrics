using System.Windows;
using Do_Re_Mi_Lyrics.ViewModels;

namespace Do_Re_Mi_Lyrics.Views;

public partial class AboutWindow : Window
{
    private readonly AboutWindowViewModel _viewModel;

    public AboutWindow()
    {
        InitializeComponent();
        _viewModel = new AboutWindowViewModel(WebBrowser);
        DataContext = _viewModel;
    }

    private void LicenseClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowLicense();
    }

    private void ChangelogClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowChangelog();
    }

    private void HelpClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowHelp();
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}