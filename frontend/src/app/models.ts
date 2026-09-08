// ============================================================
// EventHub - Master Frontend Data Models & Interfaces
// ============================================================

export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data: T;
  errors: Record<string, string[]> | null;
}

export interface PaginatedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface LoginRequest {
  emailOrUserName?: string;
  password?: string;
}

export interface SignupRequest {
  email: string;
  userName: string;
  displayName: string;
  phoneNumber?: string;
  password?: string;
}

export interface LoginResponse {
  success: boolean;
  accessToken: string;
  expiresAtUtc: string;
  userId: number;
  userName: string;
  displayName: string;
  email: string;
  roleName: string;
  message?: string;
}

export interface RoleResponse {
  roleId: number;
  roleName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface WaitlistAttendeeItem {
  attendeeUserId: number;
  displayName: string;
  position: number;
  requestedAtUtc: string;
}

export interface PublicEventDto {
  eventId: number;
  title: string;
  description?: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  registrationOpenAtUtc?: string;
  registrationCloseAtUtc?: string;
  capacity: number;
  confirmedCount: number;
  availableCapacity: number;
  waitlistCount: number;
  waitlistAvailable: boolean;
  status: string;
  waitlistAttendees?: WaitlistAttendeeItem[];
  capacityMessage?: string;
}

export interface RegisterEventDto {
  eventId: number;
}

export interface RegistrationResponseDto {
  outcome: string;
  registrationId?: number;
  waitlistEntryId?: number;
  waitlistPosition?: number;
  message?: string;
}

export interface CancellationResponseDto {
  cancelled: boolean;
  reason?: string;
  promotedRegistrationId?: number;
  promotedAttendeeUserId?: number;
}

export interface MyEventItemDto {
  eventId: number;
  title: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  registrationId?: number;
  waitlistEntryId?: number;
  itemType: string;
  status: string;
  waitlistPosition?: number;
  attendanceStatus?: string;
  isPast: boolean;
}

export interface MyEventsDto {
  active: MyEventItemDto[];
  past: MyEventItemDto[];
}

export interface EventCreateDto {
  title: string;
  description?: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  registrationOpenAtUtc?: string;
  registrationCloseAtUtc?: string;
  capacity: number;
}

export interface EventUpdateDto extends EventCreateDto {
  eventId: number;
}

export interface EventStatusTransitionDto {
  remarks?: string;
}

export interface EventResponseDto {
  eventId: number;
  title: string;
  description?: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  registrationOpenAtUtc?: string;
  registrationCloseAtUtc?: string;
  capacity: number;
  status: string;
  approvalStatus?: string;
  rejectionReason?: string;
  organizerUserId: number;
  organizerDisplayName?: string;
  publishedAtUtc?: string;
  closedAtUtc?: string;
  cancelledAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
  confirmedRegistrations: number;
  waitlistCount: number;
}

export interface AttendanceRecordDto {
  registrationId: number;
  attendanceStatus: string;
  finalize: boolean;
}

export interface AttendanceCorrectionDto {
  attendanceStatus: string;
  correctionReason?: string;
}

export interface AttendanceResponseDto {
  attendanceRecordId: number;
  registrationId: number;
  eventId: number;
  attendanceStatus: string;
  recordedByUserId: number;
  recordedAtUtc: string;
  isFinalized: boolean;
  finalizedAtUtc?: string;
  correctedByUserId?: number;
  correctedAtUtc?: string;
  correctionReason?: string;
  revisionNo: number;
}

export interface RegistrationRequestCreateDto {
  eventId: number;
  attendeeUserId: number;
}

export interface RegistrationRequestDecisionDto {
  decision: string;
  responseComment?: string;
}

export interface RegistrationRequestResponseDto {
  registrationRequestId: number;
  eventId: number;
  attendeeUserId: number;
  attendeeDisplayName: string;
  requestedByUserId: number;
  requestType: string;
  requestStatus: string;
  requestedAtUtc: string;
  respondedAtUtc?: string;
  responseComment?: string;
  linkedRegistrationId?: number;
}

export interface EventRosterItemDto {
  registrationId: number;
  eventId: number;
  attendeeUserId: number;
  attendeeDisplayName: string;
  attendeeEmail?: string;
  registrationStatus: string;
  source?: string;
  registeredAtUtc: string;
  attendanceStatus?: string;
  attendanceFinalized: boolean;
}

export interface EventWaitlistItemDto {
  waitlistEntryId: number;
  eventId: number;
  attendeeUserId: number;
  attendeeDisplayName: string;
  attendeeEmail?: string;
  position: number;
  queuedAtUtc: string;
}

export interface EventRosterResponseDto {
  eventId: number;
  eventTitle: string;
  capacity: number;
  confirmedCount: number;
  registrations: EventRosterItemDto[];
  waitlist: EventWaitlistItemDto[];
}

export interface UserCreateDto {
  email: string;
  userName: string;
  displayName: string;
  phoneNumber?: string;
  roleId: number;
}

export interface UserResponseDto {
  userId: number;
  email: string;
  userName: string;
  displayName: string;
  phoneNumber?: string;
  roleId: number;
  roleName?: string;
  isActive: boolean;
  deactivatedAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface RoleUpdateDto {
  userId: number;
  roleId: number;
}

export interface AuditLogFilterDto {
  actorUserId?: number;
  actionType?: string;
  startDate?: string;
  endDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface AuditLogResponseDto {
  auditRecordId: number;
  actorUserId?: number;
  actorUserName?: string;
  actionType: string;
  targetEntity: string;
  targetEntityId?: number;
  eventId?: number;
  outcome: string;
  ipAddress?: string;
  metadataJson?: string;
  createdAtUtc: string;
}

export interface PerformanceMetricsDto {
  capturedAtUtc: string;
  totalActiveUsers: number;
  totalAuditRecords: number;
  auditRecordsLast24Hours: number;
  memoryUsageMb: number;
  uptimeSeconds: number;
  healthStatus: string;
}

export interface DashboardMetricsDto {
  capturedAtUtc: string;
  totalEvents: number;
  activeEvents: number;
  totalRegistrations: number;
  totalConfirmedRegistrations: number;
  overallCapacityUtilizationRate: number;
  overallAttendanceRate: number;
  registrationTrends: RegistrationTrendDto[];
}

export interface RegistrationTrendDto {
  dateLabel: string;
  count: number;
}

export interface EventSummaryDto {
  eventId: number;
  title: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  status: string;
  capacity: number;
  confirmedCount: number;
  cancelledCount: number;
  capacityUtilizationRate: number;
  presentCount: number;
  absentCount: number;
  noShowCount: number;
  attendanceRate: number;
}

export interface NotificationItemDto {
  notificationId: number;
  notificationType: string;
  title: string;
  message: string;
  relatedEventId?: number;
  relatedRegistrationId?: number;
  relatedRequestId?: number;
  deliveryStatus: string;
  sentAtUtc?: string;
  readAtUtc?: string;
  createdAtUtc: string;
}

// ============================================================
// Phase 1 Brownfield Models
// ============================================================

export interface CategoryResponseDto {
  categoryId: number;
  categoryName: string;
  description?: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CategoryCreateDto {
  categoryName: string;
  description?: string;
}

export interface CategoryUpdateDto {
  categoryId: number;
  categoryName: string;
  description?: string;
  isActive: boolean;
}

export interface VenueResponseDto {
  venueId: number;
  name: string;
  address?: string;
  capacity: number;
  contactDetails?: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface VenueCreateDto {
  name: string;
  address?: string;
  capacity: number;
  contactDetails?: string;
}

export interface VenueUpdateDto {
  venueId: number;
  name: string;
  address?: string;
  capacity: number;
  contactDetails?: string;
  isActive: boolean;
}

export interface VenueAvailabilityCheckDto {
  venueId: number;
  startAtUtc: string;
  endAtUtc: string;
  excludeEventId?: number;
}

export interface VenueAvailabilityResultDto {
  venueId: number;
  isAvailable: boolean;
  message: string;
}

export interface RecurringEventCreateDto {
  title: string;
  description?: string;
  venue: string;
  venueId?: number;
  firstStartAtUtc: string;
  firstEndAtUtc: string;
  capacity: number;
  recurrencePattern: string;
  recurrenceInterval: number;
  daysOfWeekMask?: number;
  recurrenceEndDateUtc: string;
  categoryIds?: number[];
  isVirtual: boolean;
  virtualMeetingUrl?: string;
}

export interface RecurringEventSeriesDto {
  seriesId: number;
  recurrencePattern: string;
  occurrencesCount: number;
  createdEventIds: number[];
}

export interface EventApprovalResponseDto {
  approvalRequestId: number;
  eventId: number;
  eventTitle: string;
  requestedByUserId: number;
  requestedByUserName: string;
  status: string;
  remarks?: string;
  requestedAtUtc: string;
  reviewedAtUtc?: string;
}

export interface EventApprovalSubmitDto {
  eventId: number;
  remarks?: string;
}

export interface EventApprovalReviewDto {
  approvalRequestId: number;
  approve: boolean;
  remarks?: string;
}

export interface CapacityAlertConfigDto {
  eventId: number;
  thresholdPercentage: number;
}

export interface CapacityAlertResponseDto {
  alertConfigId: number;
  eventId: number;
  thresholdPercentage: number;
  isTriggered: boolean;
  triggeredAtUtc?: string;
  createdAtUtc: string;
}

export interface FeedbackCreateDto {
  eventId: number;
  rating: number;
  comments?: string;
}

export interface FeedbackItemDto {
  feedbackId: number;
  eventId: number;
  attendeeUserId: number;
  attendeeName: string;
  rating: number;
  comments?: string;
  createdAtUtc: string;
}

export interface FeedbackSummaryDto {
  eventId: number;
  averageRating: number;
  totalCount: number;
  starDistribution: Record<number, number>;
  recentReviews: FeedbackItemDto[];
}

export interface FeedbackHistoryItemDto {
  feedbackId: number;
  eventId: number;
  eventTitle: string;
  rating: number;
  comments?: string;
  createdAtUtc: string;
}

export interface AttendeeCategoryPreferenceDto {
  categoryId: number;
  weight: number;
}

export interface SetPreferencesDto {
  preferences: AttendeeCategoryPreferenceDto[];
}

export interface RecommendedEventDto {
  eventId: number;
  title: string;
  description?: string;
  venue: string;
  startAtUtc: string;
  endAtUtc: string;
  capacity: number;
  availableCapacity: number;
  categories: string[];
  isVirtual: boolean;
}

export interface VirtualAccessLinkDto {
  eventId: number;
  title: string;
  isVirtual: boolean;
  virtualMeetingUrl?: string;
}

export interface CategoryPopularityDto {
  categoryName: string;
  eventCount: number;
  totalRegistrations: number;
}

export interface MonthlyTrendDto {
  monthYear: string;
  eventsCount: number;
  registrationsCount: number;
}

export interface AdvancedAnalyticsDto {
  totalEvents: number;
  totalRegistrations: number;
  totalAttendeesCheckedIn: number;
  totalCancellations: number;
  overallAttendanceRatePercentage: number;
  overallNoShowRatePercentage: number;
  averageSystemRating: number;
  categoryBreakdown: CategoryPopularityDto[];
  monthlyTrends: MonthlyTrendDto[];
}
