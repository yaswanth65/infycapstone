import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiResponse,
  PaginatedResponse,
  LoginRequest,
  SignupRequest,
  LoginResponse,
  RoleResponse,
  PublicEventDto,
  RegisterEventDto,
  RegistrationResponseDto,
  CancellationResponseDto,
  MyEventsDto,
  EventCreateDto,
  EventUpdateDto,
  EventResponseDto,
  AttendanceRecordDto,
  AttendanceCorrectionDto,
  AttendanceResponseDto,
  RegistrationRequestCreateDto,
  RegistrationRequestDecisionDto,
  RegistrationRequestResponseDto,
  UserCreateDto,
  UserResponseDto,
  RoleUpdateDto,
  AuditLogFilterDto,
  AuditLogResponseDto,
  PerformanceMetricsDto,
  DashboardMetricsDto,
  EventSummaryDto,
  NotificationItemDto,
  EventRosterResponseDto,
  CategoryResponseDto,
  CategoryCreateDto,
  CategoryUpdateDto,
  VenueResponseDto,
  VenueCreateDto,
  VenueUpdateDto,
  VenueAvailabilityCheckDto,
  VenueAvailabilityResultDto,
  RecurringEventCreateDto,
  RecurringEventSeriesDto,
  EventApprovalResponseDto,
  EventApprovalSubmitDto,
  EventApprovalReviewDto,
  CapacityAlertConfigDto,
  CapacityAlertResponseDto,
  FeedbackCreateDto,
  FeedbackItemDto,
  FeedbackSummaryDto,
  AttendeeCategoryPreferenceDto,
  SetPreferencesDto,
  RecommendedEventDto,
  VirtualAccessLinkDto,
  AdvancedAnalyticsDto
} from './models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5195/api/v1';

  constructor(private http: HttpClient) {}

  // 1. Auth
  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/auth/login`, request);
  }

  signup(request: SignupRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/auth/signup`, request);
  }

  // 2. Roles
  getRoles(): Observable<ApiResponse<RoleResponse[]>> {
    return this.http.get<ApiResponse<RoleResponse[]>>(`${this.baseUrl}/roles`);
  }

  // 3. Public Events
  getPublicEvents(params?: { keyword?: string; fromUtc?: string; toUtc?: string; location?: string; onlyAvailable?: boolean }): Observable<PublicEventDto[]> {
    let httpParams = new HttpParams();
    if (params?.keyword) httpParams = httpParams.set('keyword', params.keyword);
    if (params?.fromUtc) httpParams = httpParams.set('fromUtc', params.fromUtc);
    if (params?.toUtc) httpParams = httpParams.set('toUtc', params.toUtc);
    if (params?.location) httpParams = httpParams.set('location', params.location);
    if (params?.onlyAvailable !== undefined && params?.onlyAvailable !== null) {
      httpParams = httpParams.set('onlyAvailable', params.onlyAvailable.toString());
    }

    return this.http.get<PublicEventDto[]>(`${this.baseUrl}/public/events`, { params: httpParams });
  }

  getPublicEventById(eventId: number): Observable<PublicEventDto> {
    return this.http.get<PublicEventDto>(`${this.baseUrl}/public/events/${eventId}`);
  }

  // 4. Attendee Portal
  registerEvent(dto: RegisterEventDto): Observable<RegistrationResponseDto> {
    return this.http.post<RegistrationResponseDto>(`${this.baseUrl}/attendee/registrations`, dto);
  }

  cancelRegistration(registrationId: number): Observable<CancellationResponseDto> {
    return this.http.delete<CancellationResponseDto>(`${this.baseUrl}/attendee/registrations/${registrationId}`);
  }

  getMyEvents(): Observable<MyEventsDto> {
    return this.http.get<MyEventsDto>(`${this.baseUrl}/attendee/my-events`);
  }

  // 5. Event Manager Operations
  createEvent(dto: EventCreateDto): Observable<ApiResponse<EventResponseDto>> {
    return this.http.post<ApiResponse<EventResponseDto>>(`${this.baseUrl}/event-manager/events`, dto);
  }

  updateEvent(dto: EventUpdateDto): Observable<ApiResponse<EventResponseDto>> {
    return this.http.put<ApiResponse<EventResponseDto>>(`${this.baseUrl}/event-manager/events`, dto);
  }

  publishEvent(eventId: number, remarks?: string): Observable<ApiResponse<EventResponseDto>> {
    return this.http.post<ApiResponse<EventResponseDto>>(`${this.baseUrl}/event-manager/events/${eventId}/publish`, { remarks });
  }

  closeEvent(eventId: number, remarks?: string): Observable<ApiResponse<EventResponseDto>> {
    return this.http.post<ApiResponse<EventResponseDto>>(`${this.baseUrl}/event-manager/events/${eventId}/close`, { remarks });
  }

  cancelEvent(eventId: number, remarks?: string): Observable<ApiResponse<EventResponseDto>> {
    return this.http.post<ApiResponse<EventResponseDto>>(`${this.baseUrl}/event-manager/events/${eventId}/cancel`, { remarks });
  }

  getOrganizerEvents(status?: string, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<EventResponseDto>>> {
    let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PaginatedResponse<EventResponseDto>>>(`${this.baseUrl}/event-manager/events`, { params });
  }

  getEventRoster(eventId: number): Observable<ApiResponse<EventRosterResponseDto>> {
    return this.http.get<ApiResponse<EventRosterResponseDto>>(`${this.baseUrl}/event-manager/events/${eventId}/registrations`);
  }

  createRegistrationRequest(dto: RegistrationRequestCreateDto): Observable<ApiResponse<RegistrationRequestResponseDto>> {
    return this.http.post<ApiResponse<RegistrationRequestResponseDto>>(`${this.baseUrl}/event-manager/registration-requests`, dto);
  }

  getRegistrationRequests(eventId?: number, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<RegistrationRequestResponseDto>>> {
    let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    if (eventId) params = params.set('eventId', eventId.toString());
    return this.http.get<ApiResponse<PaginatedResponse<RegistrationRequestResponseDto>>>(`${this.baseUrl}/event-manager/registration-requests`, { params });
  }

  decideRegistrationRequest(requestId: number, dto: RegistrationRequestDecisionDto): Observable<ApiResponse<RegistrationRequestResponseDto>> {
    return this.http.post<ApiResponse<RegistrationRequestResponseDto>>(`${this.baseUrl}/event-manager/registration-requests/${requestId}/decide`, dto);
  }

  recordAttendance(dto: AttendanceRecordDto): Observable<ApiResponse<AttendanceResponseDto>> {
    return this.http.post<ApiResponse<AttendanceResponseDto>>(`${this.baseUrl}/event-manager/attendance`, dto);
  }

  correctAttendance(registrationId: number, dto: AttendanceCorrectionDto): Observable<ApiResponse<AttendanceResponseDto>> {
    return this.http.put<ApiResponse<AttendanceResponseDto>>(`${this.baseUrl}/event-manager/attendance/${registrationId}/correct`, dto);
  }

  getAttendanceForEvent(eventId: number): Observable<ApiResponse<AttendanceResponseDto[]>> {
    return this.http.get<ApiResponse<AttendanceResponseDto[]>>(`${this.baseUrl}/event-manager/attendance/event/${eventId}`);
  }

  // 6. Administrator Operations
  createUser(dto: UserCreateDto): Observable<ApiResponse<UserResponseDto>> {
    return this.http.post<ApiResponse<UserResponseDto>>(`${this.baseUrl}/admin/users`, dto);
  }

  getAllUsers(pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<UserResponseDto>>> {
    const params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    return this.http.get<ApiResponse<PaginatedResponse<UserResponseDto>>>(`${this.baseUrl}/admin/users`, { params });
  }

  updateUserRole(dto: RoleUpdateDto): Observable<ApiResponse<UserResponseDto>> {
    return this.http.put<ApiResponse<UserResponseDto>>(`${this.baseUrl}/admin/users/role`, dto);
  }

  deactivateUser(userId: number): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/admin/users/${userId}/deactivate`, {});
  }

  getAuditLogs(filter?: AuditLogFilterDto): Observable<ApiResponse<PaginatedResponse<AuditLogResponseDto>>> {
    let params = new HttpParams();
    if (filter?.actorUserId) params = params.set('actorUserId', filter.actorUserId.toString());
    if (filter?.actionType) params = params.set('actionType', filter.actionType);
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    return this.http.get<ApiResponse<PaginatedResponse<AuditLogResponseDto>>>(`${this.baseUrl}/admin/audit-logs`, { params });
  }

  getPerformanceMetrics(): Observable<ApiResponse<PerformanceMetricsDto>> {
    return this.http.get<ApiResponse<PerformanceMetricsDto>>(`${this.baseUrl}/admin/metrics`);
  }

  exportRegistrationReportBlob(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/admin/reports/registrations/export?format=excel`, { responseType: 'blob' });
  }

  exportAttendanceReportBlob(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/admin/reports/attendance/export?format=excel`, { responseType: 'blob' });
  }

  // 7. Notifications
  getNotifications(unreadOnly: boolean = false, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<NotificationItemDto>>> {
    const params = new HttpParams().set('unreadOnly', unreadOnly.toString()).set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    return this.http.get<ApiResponse<PaginatedResponse<NotificationItemDto>>>(`${this.baseUrl}/notifications`, { params });
  }

  getUnreadNotificationCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/notifications/unread-count`);
  }

  markNotificationRead(notificationId: number): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/notifications/${notificationId}/read`, {});
  }

  markAllNotificationsRead(): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/notifications/read-all`, {});
  }

  // 8. Business Management
  getBusinessDashboard(): Observable<ApiResponse<DashboardMetricsDto>> {
    return this.http.get<ApiResponse<DashboardMetricsDto>>(`${this.baseUrl}/business/dashboard`);
  }

  getBusinessRegistrationReports(startDate?: string, endDate?: string, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<EventSummaryDto>>> {
    let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<ApiResponse<PaginatedResponse<EventSummaryDto>>>(`${this.baseUrl}/business/reports/registrations`, { params });
  }

  getBusinessAttendanceReports(startDate?: string, endDate?: string, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<EventSummaryDto>>> {
    let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<ApiResponse<PaginatedResponse<EventSummaryDto>>>(`${this.baseUrl}/business/reports/attendance`, { params });
  }

  getBusinessCompletionReports(startDate?: string, endDate?: string, pageNumber: number = 1, pageSize: number = 20): Observable<ApiResponse<PaginatedResponse<EventSummaryDto>>> {
    let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<ApiResponse<PaginatedResponse<EventSummaryDto>>>(`${this.baseUrl}/business/reports/completion`, { params });
  }

  // 9. Brownfield Phase 1 Methods

  // Custom Categories
  getCategories(onlyActive: boolean = true): Observable<ApiResponse<CategoryResponseDto[]>> {
    const params = new HttpParams().set('onlyActive', onlyActive.toString());
    return this.http.get<ApiResponse<CategoryResponseDto[]>>(`${this.baseUrl}/categories`, { params });
  }

  createCategory(dto: CategoryCreateDto): Observable<ApiResponse<CategoryResponseDto>> {
    return this.http.post<ApiResponse<CategoryResponseDto>>(`${this.baseUrl}/categories`, dto);
  }

  updateCategory(categoryId: number, dto: CategoryUpdateDto): Observable<ApiResponse<CategoryResponseDto>> {
    return this.http.put<ApiResponse<CategoryResponseDto>>(`${this.baseUrl}/categories/${categoryId}`, dto);
  }

  // Venues
  getVenues(onlyActive: boolean = true): Observable<ApiResponse<VenueResponseDto[]>> {
    const params = new HttpParams().set('onlyActive', onlyActive.toString());
    return this.http.get<ApiResponse<VenueResponseDto[]>>(`${this.baseUrl}/venues`, { params });
  }

  createVenue(dto: VenueCreateDto): Observable<ApiResponse<VenueResponseDto>> {
    return this.http.post<ApiResponse<VenueResponseDto>>(`${this.baseUrl}/venues`, dto);
  }

  updateVenue(venueId: number, dto: VenueUpdateDto): Observable<ApiResponse<VenueResponseDto>> {
    return this.http.put<ApiResponse<VenueResponseDto>>(`${this.baseUrl}/venues/${venueId}`, dto);
  }

  checkVenueAvailability(dto: VenueAvailabilityCheckDto): Observable<ApiResponse<VenueAvailabilityResultDto>> {
    return this.http.post<ApiResponse<VenueAvailabilityResultDto>>(`${this.baseUrl}/venues/check-availability`, dto);
  }

  // Recurring Events
  createRecurringEvent(dto: RecurringEventCreateDto): Observable<ApiResponse<RecurringEventSeriesDto>> {
    return this.http.post<ApiResponse<RecurringEventSeriesDto>>(`${this.baseUrl}/event-manager/events/recurring`, dto);
  }

  // Event Approval Workflow
  submitEventForApproval(dto: EventApprovalSubmitDto): Observable<ApiResponse<EventApprovalResponseDto>> {
    return this.http.post<ApiResponse<EventApprovalResponseDto>>(`${this.baseUrl}/approvals/submit`, dto);
  }

  getPendingApprovals(): Observable<ApiResponse<EventApprovalResponseDto[]>> {
    return this.http.get<ApiResponse<EventApprovalResponseDto[]>>(`${this.baseUrl}/approvals/pending`);
  }

  reviewApproval(dto: EventApprovalReviewDto): Observable<ApiResponse<EventApprovalResponseDto>> {
    return this.http.post<ApiResponse<EventApprovalResponseDto>>(`${this.baseUrl}/approvals/review`, dto);
  }

  // Capacity Alerts
  setCapacityAlert(dto: CapacityAlertConfigDto): Observable<ApiResponse<CapacityAlertResponseDto>> {
    return this.http.post<ApiResponse<CapacityAlertResponseDto>>(`${this.baseUrl}/event-manager/events/${dto.eventId}/capacity-alerts`, dto);
  }

  getCapacityAlerts(eventId: number): Observable<ApiResponse<CapacityAlertResponseDto[]>> {
    return this.http.get<ApiResponse<CapacityAlertResponseDto[]>>(`${this.baseUrl}/event-manager/events/${eventId}/capacity-alerts`);
  }

  // Virtual Event Link
  getVirtualAccessLink(eventId: number): Observable<VirtualAccessLinkDto> {
    return this.http.get<VirtualAccessLinkDto>(`${this.baseUrl}/attendee/events/${eventId}/access-link`);
  }

  // Feedback and Ratings
  submitFeedback(dto: FeedbackCreateDto): Observable<ApiResponse<FeedbackItemDto>> {
    return this.http.post<ApiResponse<FeedbackItemDto>>(`${this.baseUrl}/feedback`, dto);
  }

  getEventFeedbackSummary(eventId: number): Observable<ApiResponse<FeedbackSummaryDto>> {
    return this.http.get<ApiResponse<FeedbackSummaryDto>>(`${this.baseUrl}/feedback/events/${eventId}/summary`);
  }

  // Recommendations & Preferences
  getRecommendations(limit: number = 10): Observable<ApiResponse<RecommendedEventDto[]>> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<ApiResponse<RecommendedEventDto[]>>(`${this.baseUrl}/recommendations`, { params });
  }

  getPreferences(): Observable<ApiResponse<AttendeeCategoryPreferenceDto[]>> {
    return this.http.get<ApiResponse<AttendeeCategoryPreferenceDto[]>>(`${this.baseUrl}/recommendations/preferences`);
  }

  setPreferences(dto: SetPreferencesDto): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/recommendations/preferences`, dto);
  }

  // Advanced Analytics
  getAdvancedAnalytics(fromUtc?: string, toUtc?: string): Observable<ApiResponse<AdvancedAnalyticsDto>> {
    let params = new HttpParams();
    if (fromUtc) params = params.set('fromUtc', fromUtc);
    if (toUtc) params = params.set('toUtc', toUtc);
    return this.http.get<ApiResponse<AdvancedAnalyticsDto>>(`${this.baseUrl}/business/analytics`, { params });
  }

  // Calendar Export
  exportRegistrationCalendar(registrationId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/calendar/registrations/${registrationId}/export`, { responseType: 'blob' });
  }

  exportEventCalendar(eventId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/calendar/events/${eventId}/export`, { responseType: 'blob' });
  }
}
