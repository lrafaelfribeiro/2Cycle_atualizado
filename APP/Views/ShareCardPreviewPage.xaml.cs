namespace APP.Views;

public partial class ShareCardPreviewPage : ContentPage
{
    private readonly string _imagePath;
    private readonly string _shareTitle;

    public ShareCardPreviewPage(string imagePath, string shareTitle)
    {
        InitializeComponent();

        _imagePath = imagePath;
        _shareTitle = shareTitle;
        PreviewImage.Source = ImageSource.FromFile(imagePath);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.Navigation.PopModalAsync();

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = _shareTitle,
            File = new ShareFile(_imagePath)
        });
    }
}