namespace PlannerPro.Web.Services.Holidays;

public interface IHolidayApiClient
{
    Task<HolidayFetchResult> GetHolidaysAsync(
        string countryCode,
        int year,
        CancellationToken cancellationToken = default);
}
