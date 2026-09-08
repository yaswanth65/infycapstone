namespace EventManagementSystemServiceLayer.DTOs.Brownfield
{
    // Categories
    public record CategoryCreateDto(string CategoryName, string? Description);
    public record CategoryUpdateDto(int CategoryId, string CategoryName, string? Description, bool IsActive);
    public record CategoryResponseDto(int CategoryId, string CategoryName, string? Description, bool IsActive, DateTime CreatedAtUtc);

    // Venues
    public record VenueCreateDto(string Name, string? Address, int Capacity, string? ContactDetails);
    public record VenueUpdateDto(long VenueId, string Name, string? Address, int Capacity, string? ContactDetails, bool IsActive);
    public record VenueResponseDto(long VenueId, string Name, string? Address, int Capacity, string? ContactDetails, bool IsActive, DateTime CreatedAtUtc);
    public record VenueAvailabilityCheckDto(long VenueId, DateTime StartAtUtc, DateTime EndAtUtc, long? ExcludeEventId);
    public record VenueAvailabilityResultDto(long VenueId, bool IsAvailable, string Message);

    // Recurring Events
    public record RecurringEventCreateDto(
        string Title,
        string? Description,
        string Venue,
        long? VenueId,
        DateTime FirstStartAtUtc,
        DateTime FirstEndAtUtc,
        int Capacity,
        string RecurrencePattern, // Daily, Weekly, Monthly
        int RecurrenceInterval,
        int? DaysOfWeekMask,
        DateTime RecurrenceEndDateUtc,
        List<int>? CategoryIds,
        bool IsVirtual,
        string? VirtualMeetingUrl
    );

    public record RecurringEventSeriesDto(long SeriesId, string RecurrencePattern, int OccurrencesCount, List<long> CreatedEventIds);

    // Approval Workflow
    public record EventApprovalSubmitDto(long EventId, string? Remarks);
    public record EventApprovalReviewDto(long ApprovalRequestId, bool Approve, string? Remarks);
    public record EventApprovalResponseDto(long ApprovalRequestId, long EventId, string EventTitle, long RequestedByUserId, string RequestedByUserName, string Status, string? Remarks, DateTime RequestedAtUtc, DateTime? ReviewedAtUtc);

    // Capacity Alerts
    public record CapacityAlertConfigDto(long EventId, int ThresholdPercentage);
    public record CapacityAlertResponseDto(long AlertConfigId, long EventId, int ThresholdPercentage, bool IsTriggered, DateTime? TriggeredAtUtc, DateTime CreatedAtUtc);

    // Feedback & Ratings
    public record FeedbackCreateDto(long EventId, int Rating, string? Comments);
    public record FeedbackItemDto(long FeedbackId, long EventId, long AttendeeUserId, string AttendeeName, int Rating, string? Comments, DateTime CreatedAtUtc);
    public record FeedbackSummaryDto(long EventId, double AverageRating, int TotalCount, Dictionary<int, int> StarDistribution, List<FeedbackItemDto> RecentReviews);
    public record FeedbackHistoryItemDto(long FeedbackId, long EventId, string EventTitle, int Rating, string? Comments, DateTime CreatedAtUtc);

    // Attendee Recommendations & Preferences
    public record AttendeeCategoryPreferenceDto(int CategoryId, decimal Weight);
    public record SetPreferencesDto(List<AttendeeCategoryPreferenceDto> Preferences);
    public record RecommendedEventDto(long EventId, string Title, string? Description, string Venue, DateTime StartAtUtc, DateTime EndAtUtc, int Capacity, int AvailableCapacity, List<string> Categories, bool IsVirtual);

    // Calendar Export
    public record CalendarEventDetailsDto(long EventId, string Title, string? Description, string Venue, DateTime StartAtUtc, DateTime EndAtUtc, bool IsVirtual, string? VirtualMeetingUrl);
}
