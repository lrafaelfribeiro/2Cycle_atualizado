// API/Data/Conversions/UtcDateTimeConverter.cs
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data.Conversions
{
    /// <summary>
    /// Garante que todo o DateTime persistido no Postgres tem Kind=Utc.
    /// Necessário porque o Npgsql rejeita Kind=Unspecified/Local para colunas
    /// "timestamp with time zone", e o Kind original (Unspecified) chega
    /// tipicamente de DateTimes desserializados de JSON sem sufixo "Z".
    /// </summary>
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            toProvider => toProvider.Kind == DateTimeKind.Utc
                ? toProvider
                : DateTime.SpecifyKind(toProvider, DateTimeKind.Utc),
            fromProvider => DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc))
        {
        }
    }

    public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter() : base(
            toProvider => toProvider.HasValue && toProvider.Value.Kind != DateTimeKind.Utc
                ? DateTime.SpecifyKind(toProvider.Value, DateTimeKind.Utc)
                : toProvider,
            fromProvider => fromProvider.HasValue
                ? DateTime.SpecifyKind(fromProvider.Value, DateTimeKind.Utc)
                : fromProvider)
        {
        }
    }
}