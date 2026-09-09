using APP.DTOs.Profile;
using APP.Services.Profile;
using APP.Services.Toast;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APP.ViewModels
{
    public partial class CreateProfileViewModel : ObservableObject
    {
        private readonly IProfileService _profileService;
        private readonly IToastService _toastService;

        public CreateProfileViewModel(IProfileService profileService, IToastService toastService)
        {
            _profileService = profileService;
            _toastService = toastService;
        }

        [ObservableProperty] private DateTime birthDate = DateTime.Today.AddYears(-18);
        [ObservableProperty] private string heightCm = string.Empty;
        [ObservableProperty] private string initialWeightKg = string.Empty;
        [ObservableProperty] private string targetWeightKg = string.Empty;
        [ObservableProperty] private int activityDaysPerWeek = 3;
        [ObservableProperty] private int selectedSexIndex; // 0 = Masculino, 1 = Feminino

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        private bool IsNotBusy => !IsBusy;

        [ObservableProperty] private string? errorMessage;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync(CancellationToken cancellationToken)
        {
            if (IsBusy) return;

            if (!double.TryParse(HeightCm, out var height) || height <= 0 || height > 300)
            {
                ErrorMessage = "Altura inválida.";
                return;
            }

            if (!double.TryParse(InitialWeightKg, out var initialWeight) || initialWeight <= 0 || initialWeight > 600)
            {
                ErrorMessage = "Peso inválido.";
                return;
            }

            if (!double.TryParse(TargetWeightKg, out var targetWeight) || targetWeight <= 0 || targetWeight > 600)
            {
                ErrorMessage = "Peso pretendido inválido.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var request = new CreateProfileRequestDTO(
                    DateOnly.FromDateTime(BirthDate),
                    height,
                    initialWeight,
                    targetWeight,
                    ActivityDaysPerWeek,
                    SelectedSexIndex
                );

                var result = await _profileService.CreateProfileAsync(request, cancellationToken);

                if (result.IsSuccess)
                {
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    ErrorMessage = result.Error!.Message;
                    await _toastService.Show(ErrorMessage);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SkipAsync()
        {
            // Permite continuar sem perfil por agora — pode completá-lo mais tarde.
            // Sem isto, um utilizador que desista a meio deste ecrã fica preso, sem forma de entrar na app.
            await Shell.Current.GoToAsync("//main");
        }

        private bool CanSave() => !IsBusy;
    }
}
