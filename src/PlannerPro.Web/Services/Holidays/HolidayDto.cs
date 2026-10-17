using System.Text.Json.Serialization;

namespace PlannerPro.Web.Services.Holidays;

public sealed class HolidayDto
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; init; } = string.Empty;

    [JsonPropertyName("nationalHoliday")]
    public bool NationalHoliday { get; init; }

    [JsonPropertyName("holidayTypes")]
    public string[] HolidayTypes { get; init; } = [];
}
