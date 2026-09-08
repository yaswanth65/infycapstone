import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from './api.service';
import {
  EventResponseDto,
  EventCreateDto,
  EventUpdateDto,
  RegistrationRequestResponseDto,
  AttendanceResponseDto,
  EventRosterResponseDto,
  CategoryResponseDto,
  VenueResponseDto,
  RecurringEventCreateDto,
  CapacityAlertResponseDto,
  FeedbackSummaryDto
} from './models';

@Component({
  selector: 'app-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manager.component.html'
})
export class ManagerComponent implements OnInit {
  activeTab: 'events' | 'create' | 'recurring' | 'roster' | 'requests' | 'attendance' = 'events';

  constructor(public apiService: ApiService) {}

  // Events list
  events: EventResponseDto[] = [];
  statusFilter = '';
  isLoadingEvents = false;

  // Master Reference Data
  categories: CategoryResponseDto[] = [];
  venues: VenueResponseDto[] = [];

  // Create / Edit form model
  isEditing = false;
  editingEventId: number | null = null;
  formModel: EventCreateDto = {
    title: '',
    description: '',
    venue: '',
    venueId: undefined,
    startAtUtc: '',
    endAtUtc: '',
    capacity: 50,
    categoryIds: [],
    isVirtual: false,
    virtualMeetingUrl: ''
  };
  eventFormSubmitted = false;
  selectedCategoryIds: number[] = [];
  selectedVenueId: number | null = null;
  venueCheckMessage: string = '';
  venueCheckAvailable: boolean | null = null;
  venueConflictError: string = '';

  // Approval Submission Modal State
  submitApprovalModalData: { eventId: number; remarks: string } | null = null;

  // View Feedback Modal State
  selectedFeedbackEventId: number | null = null;
  selectedFeedbackEventTitle: string = '';
  eventFeedbackSummary: FeedbackSummaryDto | null = null;
  isLoadingEventFeedback: boolean = false;

  // Recurring Event Form Model
  recurringModel: RecurringEventCreateDto = {
    title: '',
    description: '',
    venue: '',
    venueId: undefined,
    firstStartAtUtc: '',
    firstEndAtUtc: '',
    capacity: 50,
    recurrencePattern: 'Weekly',
    recurrenceInterval: 1,
    recurrenceEndDateUtc: '',
    isVirtual: false,
    virtualMeetingUrl: ''
  };

  // Capacity Alerts Modal State
  selectedAlertEventId: number | null = null;
  alertThreshold: number = 80;
  capacityAlerts: CapacityAlertResponseDto[] = [];
  isLoadingAlerts = false;

  // Registration Requests
  requests: RegistrationRequestResponseDto[] = [];
  onBehalfEventId: number = 0;
  onBehalfAttendeeUserId: number = 0;
  requestFilterEventId: number = 0;

  // Event Roster (registrations & waitlist)
  rosterEventId: number = 0;
  roster: EventRosterResponseDto | null = null;
  isLoadingRoster = false;

  // Attendance Redesign State
  selectedAttendanceEventId: number = 0;
  attendanceRoster: EventRosterResponseDto | null = null;
  attendanceRecords: AttendanceResponseDto[] = [];
  isLoadingAttendance: boolean = false;

  correctingRegistrationId: number | null = null;
  correctionStatus: string = 'Attended';
  correctionReason: string = '';

  message = '';
  errorMessage = '';

  ngOnInit() {
    this.loadEvents();
    this.loadCategories();
    this.loadVenues();
  }

  loadCategories() {
    this.apiService.getCategories(true).subscribe({
      next: (res) => this.categories = res.data || [],
      error: () => {}
    });
  }

  loadVenues() {
    this.apiService.getVenues(true).subscribe({
      next: (res) => this.venues = res.data || [],
      error: () => {}
    });
  }

  toggleCategorySelection(catId: number) {
    const idx = this.selectedCategoryIds.indexOf(catId);
    if (idx >= 0) {
      this.selectedCategoryIds.splice(idx, 1);
    } else {
      this.selectedCategoryIds.push(catId);
    }
  }

  isCategorySelected(catId: number): boolean {
    return this.selectedCategoryIds.includes(catId);
  }

  setTab(tab: 'events' | 'create' | 'recurring' | 'roster' | 'requests' | 'attendance') {
    this.activeTab = tab;
    this.message = '';
    this.errorMessage = '';
    if (tab === 'events') this.loadEvents();
    if (tab === 'requests') this.loadRequests();
    if (tab === 'recurring') this.openRecurringForm();
    if (tab === 'roster') {
      if (!this.rosterEventId && this.events.length > 0) {
        this.rosterEventId = this.events[0].eventId;
      }
      if (this.rosterEventId) this.loadRoster();
    }
    if (tab === 'attendance') {
      if (!this.selectedAttendanceEventId && this.events.length > 0) {
        this.selectedAttendanceEventId = this.events[0].eventId;
      }
      if (this.selectedAttendanceEventId) this.onAttendanceEventSelect();
    }
  }

  loadEvents() {
    this.isLoadingEvents = true;
    this.apiService.getOrganizerEvents(this.statusFilter).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.events = res.data.data;
          if (this.events.length > 0) {
            const existsAtt = this.events.some(e => e.eventId === Number(this.selectedAttendanceEventId));
            if (!existsAtt) {
              this.selectedAttendanceEventId = this.events[0].eventId;
            }
            const existsRos = this.events.some(e => e.eventId === Number(this.rosterEventId));
            if (!existsRos) {
              this.rosterEventId = this.events[0].eventId;
            }
            const existsBehalf = this.events.some(e => e.eventId === Number(this.onBehalfEventId));
            if (!existsBehalf) {
              this.onBehalfEventId = this.events[0].eventId;
            }
          } else {
            this.selectedAttendanceEventId = 0;
            this.rosterEventId = 0;
            this.onBehalfEventId = 0;
          }
        }
        this.isLoadingEvents = false;
      },
      error: () => this.isLoadingEvents = false
    });
  }

  openCreateForm() {
    this.isEditing = false;
    this.editingEventId = null;
    this.eventFormSubmitted = false;
    this.selectedVenueId = null;
    this.selectedCategoryIds = [];
    this.venueCheckMessage = '';
    this.venueCheckAvailable = null;
    this.venueConflictError = '';
    const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 16);
    const dayAfter = new Date(Date.now() + 172800000).toISOString().slice(0, 16);
    this.formModel = {
      title: '',
      description: '',
      venue: '',
      venueId: undefined,
      startAtUtc: tomorrow,
      endAtUtc: dayAfter,
      capacity: 50,
      categoryIds: [],
      isVirtual: false,
      virtualMeetingUrl: ''
    };
    this.setTab('create');
  }

  openEditForm(event: EventResponseDto) {
    this.isEditing = true;
    this.editingEventId = event.eventId;
    this.eventFormSubmitted = false;
    this.selectedVenueId = event.venueId || null;
    this.selectedCategoryIds = event.categoryIds ? [...event.categoryIds] : [];
    this.venueCheckMessage = '';
    this.venueCheckAvailable = null;
    this.venueConflictError = '';
    this.formModel = {
      title: event.title,
      description: event.description || '',
      venue: event.venue,
      venueId: event.venueId || undefined,
      startAtUtc: event.startAtUtc ? event.startAtUtc.slice(0, 16) : '',
      endAtUtc: event.endAtUtc ? event.endAtUtc.slice(0, 16) : '',
      capacity: event.capacity,
      categoryIds: this.selectedCategoryIds,
      isVirtual: event.isVirtual || false,
      virtualMeetingUrl: event.virtualMeetingUrl || ''
    };
    if (this.selectedVenueId) {
      this.checkVenueAvailability();
    }
    this.setTab('create');
  }

  saveEvent() {
    this.eventFormSubmitted = true;
    this.venueConflictError = '';
    if (!this.formModel.title || !this.formModel.venue || !this.formModel.startAtUtc || !this.formModel.endAtUtc || !this.formModel.capacity || this.formModel.capacity <= 0) {
      return;
    }

    if (this.formModel.isVirtual && (!this.formModel.virtualMeetingUrl || !this.formModel.virtualMeetingUrl.trim())) {
      return;
    }

    const startDate = new Date(this.toUtcIso(this.formModel.startAtUtc));
    const endDate = new Date(this.toUtcIso(this.formModel.endAtUtc));

    if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
      this.errorMessage = 'Invalid start or end date selection.';
      return;
    }

    if (endDate <= startDate) {
      this.errorMessage = 'End Date must be strictly after Start Date.';
      return;
    }

    if (this.venueCheckAvailable === false) {
      this.venueConflictError = 'Venue conflict: The selected venue is not available for this time range. Please select another time or venue.';
      this.errorMessage = this.venueConflictError;
      return;
    }

    const payload = {
      ...this.formModel,
      categoryIds: this.selectedCategoryIds,
      venueId: this.selectedVenueId || undefined,
      startAtUtc: this.toUtcIso(this.formModel.startAtUtc),
      endAtUtc: this.toUtcIso(this.formModel.endAtUtc)
    };

    if (this.isEditing && this.editingEventId) {
      const updatePayload: EventUpdateDto = { ...payload, eventId: this.editingEventId };
      this.apiService.updateEvent(updatePayload).subscribe({
        next: (res) => {
          if (res.success) {
            this.message = 'Event details updated successfully.';
            this.eventFormSubmitted = false;
            this.setTab('events');
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          const msg = err?.error?.message || 'Failed to update event.';
          this.errorMessage = msg;
          if (msg.toLowerCase().includes('venue conflict') || msg.toLowerCase().includes('overlap') || msg.toLowerCase().includes('already booked')) {
            this.venueConflictError = msg;
          }
        }
      });
    } else {
      this.apiService.createEvent(payload).subscribe({
        next: (res) => {
          if (res.success) {
            this.message = 'Event created successfully in Draft status.';
            this.eventFormSubmitted = false;
            this.setTab('events');
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          const msg = err?.error?.message || 'Failed to create event.';
          this.errorMessage = msg;
          if (msg.toLowerCase().includes('venue conflict') || msg.toLowerCase().includes('overlap') || msg.toLowerCase().includes('already booked')) {
            this.venueConflictError = msg;
          }
        }
      });
    }
  }

  onVenueSelect(venueId: number | null) {
    this.selectedVenueId = venueId ? Number(venueId) : null;
    this.formModel.venueId = this.selectedVenueId || undefined;
    this.venueConflictError = '';
    if (this.selectedVenueId) {
      const v = this.venues.find(item => item.venueId === Number(this.selectedVenueId));
      if (v) {
        this.formModel.venue = v.name;
        this.checkVenueAvailability();
      }
    } else {
      this.venueCheckMessage = '';
      this.venueCheckAvailable = null;
    }
  }

  checkVenueAvailability() {
    this.venueConflictError = '';
    if (!this.selectedVenueId || !this.formModel.startAtUtc || !this.formModel.endAtUtc) return;
    this.apiService.checkVenueAvailability({
      venueId: this.selectedVenueId,
      startAtUtc: this.toUtcIso(this.formModel.startAtUtc),
      endAtUtc: this.toUtcIso(this.formModel.endAtUtc),
      excludeEventId: this.editingEventId || undefined
    }).subscribe({
      next: (res) => {
        this.venueCheckAvailable = res.data.isAvailable;
        this.venueCheckMessage = res.data.message;
        if (!res.data.isAvailable) {
          this.venueConflictError = res.data.message;
        }
      },
      error: () => {
        this.venueCheckAvailable = null;
        this.venueCheckMessage = '';
      }
    });
  }

  openSubmitApprovalModal(eventId: number) {
    this.submitApprovalModalData = {
      eventId,
      remarks: 'Draft event is ready and submitted for administrative approval.'
    };
  }

  closeSubmitApprovalModal() {
    this.submitApprovalModalData = null;
  }

  confirmSubmitApproval() {
    if (!this.submitApprovalModalData) return;
    const { eventId, remarks } = this.submitApprovalModalData;
    this.apiService.submitEventForApproval({ eventId, remarks }).subscribe({
      next: () => {
        this.message = 'Event submitted for administrator review and approval.';
        this.closeSubmitApprovalModal();
        this.loadEvents();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to submit event for approval.';
        this.closeSubmitApprovalModal();
      }
    });
  }

  openViewFeedbackModal(eventId: number, title: string) {
    this.selectedFeedbackEventId = eventId;
    this.selectedFeedbackEventTitle = title;
    this.isLoadingEventFeedback = true;
    this.eventFeedbackSummary = null;
    this.apiService.getEventFeedbackSummary(eventId).subscribe({
      next: (res) => {
        this.eventFeedbackSummary = res.data || null;
        this.isLoadingEventFeedback = false;
      },
      error: () => {
        this.isLoadingEventFeedback = false;
      }
    });
  }

  closeViewFeedbackModal() {
    this.selectedFeedbackEventId = null;
    this.eventFeedbackSummary = null;
  }

  // Capacity Alerts
  openCapacityAlertModal(eventId: number) {
    this.selectedAlertEventId = eventId;
    this.alertThreshold = 80;
    this.loadCapacityAlerts(eventId);
  }

  closeCapacityAlertModal() {
    this.selectedAlertEventId = null;
    this.capacityAlerts = [];
  }

  loadCapacityAlerts(eventId: number) {
    this.isLoadingAlerts = true;
    this.apiService.getCapacityAlerts(eventId).subscribe({
      next: (res) => {
        this.capacityAlerts = res.data || [];
        this.isLoadingAlerts = false;
      },
      error: () => this.isLoadingAlerts = false
    });
  }

  saveCapacityAlert() {
    if (!this.selectedAlertEventId || this.alertThreshold <= 0 || this.alertThreshold > 100) {
      this.errorMessage = 'Please enter a threshold percentage between 1 and 100.';
      return;
    }
    this.apiService.setCapacityAlert({
      eventId: this.selectedAlertEventId,
      thresholdPercentage: Number(this.alertThreshold)
    }).subscribe({
      next: () => {
        this.message = `Capacity alert threshold set at ${this.alertThreshold}%.`;
        this.loadCapacityAlerts(this.selectedAlertEventId!);
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to configure capacity alert.'
    });
  }

  // Recurring Event Series
  openRecurringForm() {
    const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 16);
    const dayAfter = new Date(Date.now() + 90000000).toISOString().slice(0, 16);
    const nextMonth = new Date(Date.now() + 86400000 * 30).toISOString().slice(0, 16);
    this.recurringModel = {
      title: '',
      description: '',
      venue: '',
      venueId: undefined,
      firstStartAtUtc: tomorrow,
      firstEndAtUtc: dayAfter,
      capacity: 50,
      recurrencePattern: 'Weekly',
      recurrenceInterval: 1,
      recurrenceEndDateUtc: nextMonth,
      isVirtual: false,
      virtualMeetingUrl: ''
    };
  }

  saveRecurringSeries() {
    if (!this.recurringModel.title || !this.recurringModel.venue || !this.recurringModel.firstStartAtUtc || !this.recurringModel.firstEndAtUtc || !this.recurringModel.recurrenceEndDateUtc) {
      this.errorMessage = 'Please fill all required fields for recurring series.';
      return;
    }

    const payload: RecurringEventCreateDto = {
      ...this.recurringModel,
      firstStartAtUtc: this.toUtcIso(this.recurringModel.firstStartAtUtc),
      firstEndAtUtc: this.toUtcIso(this.recurringModel.firstEndAtUtc),
      recurrenceEndDateUtc: this.toUtcIso(this.recurringModel.recurrenceEndDateUtc)
    };

    this.apiService.createRecurringEvent(payload).subscribe({
      next: (res) => {
        this.message = `Successfully generated ${res.data.occurrencesCount} occurrences in series #${res.data.seriesId}!`;
        this.setTab('events');
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to create recurring event series.'
    });
  }

  publish(eventId: number) {
    this.apiService.publishEvent(eventId, 'Published via Manager Portal').subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'Event published successfully! Attendees can now register.';
          this.loadEvents();
        } else {
          this.errorMessage = res.message || 'Failed to publish event.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to publish event.'
    });
  }

  close(eventId: number) {
    if (!confirm('Are you sure you want to close this event?')) return;
    this.apiService.closeEvent(eventId, 'Event completed and closed').subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'Event closed.';
          this.loadEvents();
        } else {
          this.errorMessage = res.message || 'Failed to close event.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to close event.'
    });
  }

  cancel(eventId: number) {
    if (!confirm('Are you sure you want to cancel this event? Registered attendees will be notified.')) return;
    this.apiService.cancelEvent(eventId, 'Event cancelled by organizer').subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'Event cancelled.';
          this.loadEvents();
        } else {
          this.errorMessage = res.message || 'Failed to cancel event.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to cancel event.'
    });
  }

  // Event Roster (Registrations & Waitlist)
  loadRoster() {
    if (!this.rosterEventId) {
      this.roster = null;
      return;
    }
    this.isLoadingRoster = true;
    this.roster = null;
    this.apiService.getEventRoster(Number(this.rosterEventId)).subscribe({
      next: (res) => {
        this.isLoadingRoster = false;
        if (res.success && res.data) {
          this.roster = res.data;
        } else {
          this.errorMessage = res.message || 'Failed to load event roster.';
        }
      },
      error: (err) => {
        this.isLoadingRoster = false;
        this.errorMessage = err?.error?.message || 'Failed to load event roster.';
      }
    });
  }

  // Registration Requests
  loadRequests() {
    this.apiService.getRegistrationRequests(this.requestFilterEventId ? Number(this.requestFilterEventId) : undefined).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.requests = res.data.data;
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to load registration requests.'
    });
  }

  submitOnBehalfRequest() {
    if (!this.onBehalfEventId || !this.onBehalfAttendeeUserId) {
      this.errorMessage = 'Please select an Event and enter Attendee User ID.';
      return;
    }
    this.apiService.createRegistrationRequest({
      eventId: Number(this.onBehalfEventId),
      attendeeUserId: Number(this.onBehalfAttendeeUserId)
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'On-behalf registration request submitted successfully.';
          this.loadRequests();
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Request failed.'
    });
  }

  decideRequest(requestId: number, decision: 'Accepted' | 'Rejected') {
    this.apiService.decideRegistrationRequest(requestId, { decision, responseComment: `Processed as ${decision}` }).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = `Request ${decision} successfully.`;
          this.loadRequests();
        } else {
          this.errorMessage = res.message || 'Failed to process request.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to process request.'
    });
  }

  // Redesigned Attendance Workflow Methods
  onAttendanceEventSelect() {
    if (!this.selectedAttendanceEventId || Number(this.selectedAttendanceEventId) === 0) {
      this.attendanceRoster = null;
      this.attendanceRecords = [];
      return;
    }
    this.isLoadingAttendance = true;
    this.correctingRegistrationId = null;
    this.errorMessage = '';

    const eventId = Number(this.selectedAttendanceEventId);

    this.apiService.getEventRoster(eventId).subscribe({
      next: (rosterRes) => {
        if (rosterRes.success && rosterRes.data) {
          this.attendanceRoster = rosterRes.data;
        }
        this.apiService.getAttendanceForEvent(eventId).subscribe({
          next: (attRes) => {
            if (attRes.success && attRes.data) {
              this.attendanceRecords = attRes.data;
            }
            this.isLoadingAttendance = false;
          },
          error: () => this.isLoadingAttendance = false
        });
      },
      error: (err) => {
        this.isLoadingAttendance = false;
        this.attendanceRoster = null;
        this.errorMessage = err?.error?.message || `Roster not available for Event #${eventId}.`;
      }
    });
  }

  getAttendanceRecordForReg(regId: number): AttendanceResponseDto | undefined {
    return this.attendanceRecords.find(r => r.registrationId === regId);
  }

  getAttendeeStatus(reg: any): string {
    const rec = this.getAttendanceRecordForReg(reg.registrationId);
    if (rec) return rec.attendanceStatus;
    if (reg.attendanceStatus) return reg.attendanceStatus;
    return 'Not Recorded';
  }

  get totalRegisteredCount(): number {
    return this.attendanceRoster?.registrations?.length || 0;
  }

  get attendedCount(): number {
    if (!this.attendanceRoster?.registrations) return 0;
    return this.attendanceRoster.registrations.filter(r => {
      const s = this.getAttendeeStatus(r);
      return s === 'Attended' || s === 'Present';
    }).length;
  }

  get absentCount(): number {
    if (!this.attendanceRoster?.registrations) return 0;
    return this.attendanceRoster.registrations.filter(r => {
      const s = this.getAttendeeStatus(r);
      return s === 'Absent' || s === 'No-show';
    }).length;
  }

  get pendingCount(): number {
    if (!this.attendanceRoster?.registrations) return 0;
    return this.attendanceRoster.registrations.filter(r => {
      const s = this.getAttendeeStatus(r);
      return s === 'Not Recorded' || s === 'Pending';
    }).length;
  }

  quickMarkAttendance(regId: number, status: 'Attended' | 'Absent') {
    this.message = '';
    this.errorMessage = '';
    this.apiService.recordAttendance({
      registrationId: regId,
      attendanceStatus: status,
      finalize: false
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = `Attendance marked as '${status}' successfully.`;
          this.onAttendanceEventSelect();
        } else {
          this.errorMessage = res.message || 'Failed to record attendance.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to record attendance.'
    });
  }

  openCorrection(regId: number, currentStatus: string) {
    this.correctingRegistrationId = regId;
    this.correctionStatus = (currentStatus === 'Not Recorded' || currentStatus === 'Pending') ? 'Attended' : currentStatus;
    this.correctionReason = '';
  }

  cancelCorrection() {
    this.correctingRegistrationId = null;
    this.correctionReason = '';
  }

  submitCorrection(regId: number) {
    if (!this.correctionReason || !this.correctionReason.trim()) {
      this.errorMessage = 'Correction reason is required for audit trail tracking.';
      return;
    }
    this.message = '';
    this.errorMessage = '';
    this.apiService.correctAttendance(regId, {
      attendanceStatus: this.correctionStatus,
      correctionReason: this.correctionReason
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'Attendance audit record corrected successfully.';
          this.correctingRegistrationId = null;
          this.onAttendanceEventSelect();
        } else {
          this.errorMessage = res.message || 'Failed to correct attendance.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to correct attendance.'
    });
  }

  private toUtcIso(value: string): string {
    if (!value) return value;
    return /([zZ]|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}:00Z`;
  }
}
