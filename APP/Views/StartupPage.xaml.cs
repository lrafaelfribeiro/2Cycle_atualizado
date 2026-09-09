using AndroidX.Lifecycle;
using APP.ViewModels;

namespace APP.Views
{
	public partial class StartupPage : ContentPage
	{
		public StartupPage(StartupViewModel viewModel)
		{
			InitializeComponent();
			BindingContext = viewModel;
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			var viewModel = (StartupViewModel)BindingContext;

			Glow.Opacity = 0;
			Glow.Scale = 0.6;
			// Entrada do logo + brilho
			var animationTask =  Task.WhenAll(
				Glow.FadeToAsync(0.35, 900, Easing.CubicOut),
				Glow.ScaleToAsync(1, 900, Easing.CubicOut)
			);

			// Executar as 2 tarefas ao mesmo tempo
			var sessionTask = viewModel.CheckSessionAsync();
			await Task.WhenAll(animationTask, sessionTask);

			await viewModel.NavigateAsync(sessionTask.Result);
		}
	}
}