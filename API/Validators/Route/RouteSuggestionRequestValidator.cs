using API.DTOs.Routes.Requests;
using FluentValidation;

namespace API.Validators.Route
{
    public class RouteSuggestionRequestValidator : AbstractValidator<RouteSuggestionRequest>
    {
        private const int MaxWaypoints = 8;
        public RouteSuggestionRequestValidator()
        {
            RuleFor(x => x.OriginLatitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.OriginLongitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.DestinationLatitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.DestinationLongitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.OriginLatitude).Must(double.IsFinite);
            RuleFor(x => x.OriginLongitude).Must(double.IsFinite);
            RuleFor(x => x.DestinationLatitude).Must(double.IsFinite);
            RuleFor(x => x.DestinationLongitude).Must(double.IsFinite);
            RuleFor(x => x.Waypoints)
                .Must(w => w == null || w.Count <= MaxWaypoints)
                .WithMessage($"Máximo de {MaxWaypoints} paragens intermédias.");
        }
    }
}
