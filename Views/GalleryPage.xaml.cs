namespace Visio.Views;

public partial class GalleryPage : ContentPage
{
	public GalleryPage(ViewModels.GalleryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
