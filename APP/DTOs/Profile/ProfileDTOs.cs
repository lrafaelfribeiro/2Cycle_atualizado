using System;
using System.Collections.Generic;
using System.Text;

namespace APP.DTOs.Profile
{
        public sealed record CreateProfileRequestDTO(
            DateOnly BirthDate,
            double HeightCm,
            double InitialWeightKg,
            double TargetWeightKg,
            int ActivityDaysPerWeek,
            int Sex // 0 = Masculino, 1 = Feminino 
        );

        public sealed record ProfileResponseDTO(
            string Name,
            DateOnly BirthDate,
            double HeightCm,
            double? CurrentWeightKg,
            double TargetWeightKg,
            int ActivityDaysPerWeek,
            int Sex,
            double? Bmi
        );
}
