using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Alfred.App.Sync;

internal sealed class CalendarSync
{
    private const string CalendarName = "Alfred";
    private const string KeyPropertyId = "String {66f5a359-4659-4830-9070-00047ec6ac6e} Name AlfredKey";

    private static readonly Uri GraphBase = new("https://graph.microsoft.com/v1.0/");
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<CancellationToken, Task<string?>> _accessTokenProvider;
    private string? _calendarId;

    public CalendarSync(Func<CancellationToken, Task<string?>> accessTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(accessTokenProvider);

        _accessTokenProvider = accessTokenProvider;
    }

    public async Task<SyncResult> SyncAsync(IReadOnlyList<SyncItem> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        string? accessToken = await _accessTokenProvider(cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("A Microsoft account access token is not available.");
        }

        string calendarId = await EnsureCalendarIdAsync(accessToken, cancellationToken);
        List<ExistingEvent> existingEvents = await ListEventsAsync(accessToken, calendarId, cancellationToken);
        return await ReconcileAsync(new GraphSession(accessToken, calendarId), items, existingEvents, cancellationToken);
    }

    private async Task<string> EnsureCalendarIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (_calendarId is not null)
        {
            return _calendarId;
        }

        string calendarId = await FindCalendarIdAsync(accessToken, cancellationToken)
            ?? await CreateCalendarAsync(accessToken, cancellationToken);
        _calendarId = calendarId;
        return calendarId;
    }

    private static async Task<SyncResult> ReconcileAsync(GraphSession session, IReadOnlyList<SyncItem> items, List<ExistingEvent> existingEvents, CancellationToken cancellationToken)
    {
        (Dictionary<string, ExistingEvent> eventsByKey, List<ExistingEvent> duplicateEvents) = IndexEventsByKey(existingEvents);

        int created = 0;
        int updated = 0;
        var itemKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (SyncItem item in items)
        {
            itemKeys.Add(item.Key);
            if (!eventsByKey.TryGetValue(item.Key, out ExistingEvent? existingEvent))
            {
                await SendAsync(session.AccessToken, new GraphRequest(HttpMethod.Post, BuildEventCollectionUri(session.CalendarId), CreateEventBody(item)), cancellationToken);
                created++;
                continue;
            }

            if (!RequiresUpdate(existingEvent, item))
            {
                continue;
            }

            await SendAsync(session.AccessToken, new GraphRequest(HttpMethod.Patch, BuildEventUri(session.CalendarId, existingEvent.Id), CreateEventBody(item)), cancellationToken);
            updated++;
        }

        var removedEvents = new List<ExistingEvent>(duplicateEvents);
        foreach (KeyValuePair<string, ExistingEvent> entry in eventsByKey)
        {
            if (!itemKeys.Contains(entry.Key))
            {
                removedEvents.Add(entry.Value);
            }
        }

        foreach (ExistingEvent removedEvent in removedEvents)
        {
            await SendAsync(session.AccessToken, new GraphRequest(HttpMethod.Delete, BuildEventUri(session.CalendarId, removedEvent.Id), null), cancellationToken);
        }

        return new SyncResult(created, updated, removedEvents.Count);
    }

    private static (Dictionary<string, ExistingEvent> ByKey, List<ExistingEvent> Duplicates) IndexEventsByKey(List<ExistingEvent> existingEvents)
    {
        var eventsByKey = new Dictionary<string, ExistingEvent>(StringComparer.Ordinal);
        var duplicateEvents = new List<ExistingEvent>();
        foreach (ExistingEvent existingEvent in existingEvents)
        {
            if (existingEvent.Key is null)
            {
                continue;
            }

            if (!eventsByKey.TryAdd(existingEvent.Key, existingEvent))
            {
                duplicateEvents.Add(existingEvent);
            }
        }

        return (eventsByKey, duplicateEvents);
    }

    private static bool RequiresUpdate(ExistingEvent existingEvent, SyncItem item)
        => !string.Equals(existingEvent.Subject, item.Title, StringComparison.Ordinal) || existingEvent.Date != item.Date;

    private static async Task<string?> FindCalendarIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        Uri requestUri = new(GraphBase, "me/calendars?$filter=" + Uri.EscapeDataString($"name eq '{CalendarName}'"));
        string payload = await SendAsync(accessToken, new GraphRequest(HttpMethod.Get, requestUri, null), cancellationToken);
        using JsonDocument document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("value", out JsonElement calendars) || calendars.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement calendar in calendars.EnumerateArray())
        {
            if (IsAlfredCalendar(calendar) && calendar.TryGetProperty("id", out JsonElement idElement))
            {
                return idElement.GetString();
            }
        }

        return null;
    }

    private static bool IsAlfredCalendar(JsonElement calendar)
        => calendar.TryGetProperty("name", out JsonElement name)
            && string.Equals(name.GetString(), CalendarName, StringComparison.OrdinalIgnoreCase);

    private static async Task<string> CreateCalendarAsync(string accessToken, CancellationToken cancellationToken)
    {
        Uri requestUri = new(GraphBase, "me/calendars");
        string payload = await SendAsync(accessToken, new GraphRequest(HttpMethod.Post, requestUri, new CalendarBody(CalendarName)), cancellationToken);
        using JsonDocument document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("id", out JsonElement idElement) && idElement.GetString() is string calendarId)
        {
            return calendarId;
        }

        throw new HttpRequestException($"Calendar creation response did not include an id: {payload}");
    }

    private static async Task<List<ExistingEvent>> ListEventsAsync(string accessToken, string calendarId, CancellationToken cancellationToken)
    {
        var events = new List<ExistingEvent>();
        string keyFilter = Uri.EscapeDataString($"id eq '{KeyPropertyId}'");
        Uri? requestUri = new Uri(
            GraphBase,
            $"me/calendars/{Uri.EscapeDataString(calendarId)}/events?$select=id,subject,start&$expand=singleValueExtendedProperties($filter={keyFilter})&$top=200");
        while (requestUri is not null)
        {
            string payload = await SendAsync(accessToken, new GraphRequest(HttpMethod.Get, requestUri, null), cancellationToken);
            requestUri = ReadEventsPage(payload, events);
        }

        return events;
    }

    private static Uri? ReadEventsPage(string payload, List<ExistingEvent> events)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("value", out JsonElement page) || page.ValueKind != JsonValueKind.Array)
        {
            return ReadNextLink(root);
        }

        foreach (JsonElement element in page.EnumerateArray())
        {
            if (ReadEvent(element) is ExistingEvent existingEvent)
            {
                events.Add(existingEvent);
            }
        }

        return ReadNextLink(root);
    }

    private static Uri? ReadNextLink(JsonElement root)
        => root.TryGetProperty("@odata.nextLink", out JsonElement nextLink) && nextLink.GetString() is string next
            ? new Uri(next)
            : null;

    private static ExistingEvent? ReadEvent(JsonElement element)
    {
        if (!element.TryGetProperty("id", out JsonElement idElement) || idElement.GetString() is not string id)
        {
            return null;
        }

        string? subject = element.TryGetProperty("subject", out JsonElement subjectElement) ? subjectElement.GetString() : null;
        return new ExistingEvent(id, subject, ReadStartDate(element), ReadKey(element));
    }

    private static DateOnly? ReadStartDate(JsonElement element)
    {
        if (!element.TryGetProperty("start", out JsonElement start) || start.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!start.TryGetProperty("dateTime", out JsonElement dateTime) || dateTime.GetString() is not string value || value.Length < 10)
        {
            return null;
        }

        return DateOnly.TryParseExact(value.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : null;
    }

    private static string? ReadKey(JsonElement element)
    {
        if (!element.TryGetProperty("singleValueExtendedProperties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement property in properties.EnumerateArray())
        {
            if (property.TryGetProperty("id", out JsonElement idElement)
                && string.Equals(idElement.GetString(), KeyPropertyId, StringComparison.OrdinalIgnoreCase)
                && property.TryGetProperty("value", out JsonElement valueElement))
            {
                return valueElement.GetString();
            }
        }

        return null;
    }

    private static EventBody CreateEventBody(SyncItem item)
    {
        return new EventBody(
            item.Title,
            true,
            new EventDate(FormatMidnight(item.Date), "UTC"),
            new EventDate(FormatMidnight(item.Date.AddDays(1)), "UTC"),
            [new ExtendedProperty(KeyPropertyId, item.Key)]);
    }

    private static string FormatMidnight(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00";

    private static Uri BuildEventCollectionUri(string calendarId)
        => new(GraphBase, $"me/calendars/{Uri.EscapeDataString(calendarId)}/events");

    private static Uri BuildEventUri(string calendarId, string eventId)
        => new(GraphBase, $"me/calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}");

    private static async Task<string> SendAsync(string accessToken, GraphRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(request.Method, request.RequestUri);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (request.Body is not null)
        {
            message.Content = new StringContent(JsonSerializer.Serialize(request.Body, SerializerOptions), Encoding.UTF8, "application/json");
        }

        using HttpResponseMessage response = await Http.SendAsync(message, cancellationToken);
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return payload;
        }

        throw new HttpRequestException($"{request.Method} {request.RequestUri} failed with {(int)response.StatusCode}: {payload}");
    }

    private sealed record GraphSession(string AccessToken, string CalendarId);

    private sealed record GraphRequest(HttpMethod Method, Uri RequestUri, object? Body);

    private sealed record ExistingEvent(string Id, string? Subject, DateOnly? Date, string? Key);

    private sealed record CalendarBody(string Name);

    private sealed record EventBody(
        string Subject,
        bool IsAllDay,
        EventDate Start,
        EventDate End,
        IReadOnlyList<ExtendedProperty> SingleValueExtendedProperties);

    private sealed record EventDate(string DateTime, string TimeZone);

    private sealed record ExtendedProperty(string Id, string Value);
}
