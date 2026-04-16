namespace MoneyKey.Core.DTOs.Milersattning;
public record CreateMilersattningDto(DateTime TripDate, string FromLocation, string ToLocation, decimal DistanceKm, decimal RatePerKm, string? Purpose);
