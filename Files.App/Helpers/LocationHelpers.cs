// Copyright(c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace Files.App.Helpers;

public static class LocationHelpers
{
    public static async Task<string> GetAddressFromCoordinatesAsync(double? Lat, double? Lon)
    {
        if (!Lat.HasValue || !Lon.HasValue)
        {
            return null!;
        }

        if (string.IsNullOrEmpty(MapService.ServiceToken))
        {
            try
            {
                MapService.ServiceToken = Constants.AutomatedWorkflowInjectionKeys.BingMapsSecret;
            }
            catch (Exception)
            {
                return null!;
            }
        }

        var location = new BasicGeoposition
        {
            Latitude = Lat.Value,
            Longitude = Lon.Value
        };
        var pointToReverseGeocode = new Geopoint(location);

        // Reverse geocode the specified geographic location.

        var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
        // CHANGE: Use property instead of Linq Enumerable method FirstOrDefault().
        return result?.Locations?.Count > 0 ? result.Locations[0].DisplayName : default!;
    }
}
