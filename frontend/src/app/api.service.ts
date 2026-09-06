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
  EventRosterResponseDto
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
}
