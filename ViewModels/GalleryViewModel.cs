using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Visio.Models;

namespace Visio.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ImageInfo> _images = new();

    [ObservableProperty]
    private ImageInfo? _selectedImage;

    [ObservableProperty]
    private int _imageCount;

    private readonly string _galleryPath;

    public GalleryViewModel()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _galleryPath = Path.Combine(picturesPath, "Visio");
        
        LoadImages();
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadImages();
    }

    private void LoadImages()
    {
        Images.Clear();

        if (!Directory.Exists(_galleryPath))
        {
            Directory.CreateDirectory(_galleryPath);
            ImageCount = 0;
            return;
        }

        var files = Directory.GetFiles(_galleryPath, "*.png")
            .OrderByDescending(f => File.GetCreationTime(f));

        foreach (var file in files)
        {
            Images.Add(new ImageInfo
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                DateTaken = File.GetCreationTime(file)
            });
        }

        ImageCount = Images.Count;
    }

    [RelayCommand]
    private async Task OpenImage(string filePath)
    {
        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Erro", 
                    $"Não foi possível abrir: {ex.Message}", 
                    "OK");
            }
        }
    }

    [RelayCommand]
    private async Task DeleteImage(string filePath)
    {
        if (Application.Current?.MainPage == null)
            return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Confirmar",
            "Deseja realmente deletar esta imagem?",
            "Sim",
            "Não");

        if (confirm)
        {
            try
            {
                File.Delete(filePath);
                LoadImages();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Erro",
                    $"Erro ao deletar: {ex.Message}",
                    "OK");
            }
        }
    }
}
