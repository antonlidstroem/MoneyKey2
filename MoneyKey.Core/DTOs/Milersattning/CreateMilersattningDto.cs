namespace MoneyKey.Core.DTOs.Milersattning;

public record CreateMilersattningDto(
    DateTime TripDate,
    string   FromLocation,
    string   ToLocation,
    decimal  DistanceKm,
    bool     IsRoundTrip,
    decimal  RatePerKm,
    string?  Purpose,
    string?  PayerName);
