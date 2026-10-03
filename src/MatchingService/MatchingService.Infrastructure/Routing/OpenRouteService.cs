using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Routing;

public class OpenRouteService : IRoutingService
{
    private const string DirectionsEndpoint =
        "https://api.heigit.org/openrouteservice/v2/directions/driving-car";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenRouteService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<RouteResult> CalculateRouteAsync(
        Location origin,
        Location destination,
        CancellationToken cancellationToken = default)
    {
        // Lấy API key từ configuration
        var apiKey =
            _configuration["OpenRouteService:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenRouteService API key is not configured.");
        }

        // OpenRouteService yêu cầu:
        // longitude,latitude
        var start =
            $"{origin.Longitude.ToString(CultureInfo.InvariantCulture)}," +
            $"{origin.Latitude.ToString(CultureInfo.InvariantCulture)}";

        var end =
            $"{destination.Longitude.ToString(CultureInfo.InvariantCulture)}," +
            $"{destination.Latitude.ToString(CultureInfo.InvariantCulture)}";

        var url =
            $"{DirectionsEndpoint}" +
            $"?start={Uri.EscapeDataString(start)}" +
            $"&end={Uri.EscapeDataString(end)}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        // API key
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            apiKey);

        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/geo+json");

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        // API trả lỗi
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouteService returned " +
                $"{(int)response.StatusCode}: " +
                responseBody);
        }

        using var document =
            JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        // ==============================
        // features
        // ==============================

        if (!root.TryGetProperty(
                "features",
                out var features))
        {
            throw new InvalidOperationException(
                "OpenRouteService response does not contain 'features'.");
        }

        if (features.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "OpenRouteService returned no route.");
        }

        var feature = features[0];

        // ==============================
        // properties
        // ==============================

        if (!feature.TryGetProperty(
                "properties",
                out var properties))
        {
            throw new InvalidOperationException(
                "OpenRouteService route does not contain 'properties'.");
        }

        if (!properties.TryGetProperty(
                "summary",
                out var summary))
        {
            throw new InvalidOperationException(
                "OpenRouteService route does not contain 'summary'.");
        }

        // distance: meters
        var distanceMeters =
            summary.GetProperty("distance")
                   .GetDouble();

        // duration: seconds
        var durationSeconds =
            summary.GetProperty("duration")
                   .GetDouble();

        // ==============================
        // geometry
        // ==============================

        if (!feature.TryGetProperty(
                "geometry",
                out var geometry))
        {
            throw new InvalidOperationException(
                "OpenRouteService route does not contain 'geometry'.");
        }

        var geometryType =
            geometry.GetProperty("type")
                    .GetString();

        if (geometryType != "LineString")
        {
            throw new InvalidOperationException(
                $"Unsupported geometry type: {geometryType}");
        }

        var coordinates =
            geometry.GetProperty("coordinates");

        var routeCoordinates =
            new List<RouteCoordinate>();

        foreach (var coordinate in coordinates.EnumerateArray())
        {
            if (coordinate.GetArrayLength() < 2)
            {
                continue;
            }

            // OpenRouteService:
            // [longitude, latitude]

            var longitude =
                coordinate[0].GetDouble();

            var latitude =
                coordinate[1].GetDouble();

            routeCoordinates.Add(
                new RouteCoordinate
                {
                    Longitude = longitude,
                    Latitude = latitude
                });
        }

        // ==============================
        // Convert units
        // ==============================

        var distanceKm =
            distanceMeters / 1000.0;

        var durationMinutes =
            durationSeconds / 60.0;

        return new RouteResult
        {
            DistanceKm = distanceKm,
            DurationMinutes = durationMinutes,
            Coordinates = routeCoordinates
        };
    }
}