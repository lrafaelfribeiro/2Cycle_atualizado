using API.DTOs.Routes.Requests;
using FluentValidation;

namespace API.Validators.Route
{
    public class RouteRoundTripSuggestionRequestValidator : AbstractValidator<RouteRoundTripSuggestionRequest>
    {
        public RouteRoundTripSuggestionRequestValidator()
        {
            RuleFor(x => x.OriginLatitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.OriginLongitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.DesiredDistanceMeters)
                .InclusiveBetween(500, 100_000)
                .WithMessage("A distância deve estar entre 500m e 100km.");
            RuleFor(x => x.OriginLatitude).Must(double.IsFinite);
            RuleFor(x => x.OriginLongitude).Must(double.IsFinite);
            RuleFor(x => x.DesiredDistanceMeters).Must(double.IsFinite);
        }
    }
}
