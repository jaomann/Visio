using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace Visio.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		try
		{
			LibVLCSharp.Shared.Core.Initialize();
			Debug.WriteLine("[Windows] LibVLC Core inicializado com sucesso");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[Windows] Erro ao inicializar LibVLC: {ex.Message}");
		}

		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

