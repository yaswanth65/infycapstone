import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from './api.service';
import { MyEventItemDto, CategoryResponseDto, AttendeeCategoryPreferenceDto } from './models';

@Component({
  selector: 'app-attendee',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './attendee.component.html'
})
export class AttendeeComponent implements OnInit {
  activeEvents: MyEventItemDto[] = [];
  pastEvents: MyEventItemDto[] = [];
  isLoading = false;

  // Active Tab
  activeTab: 'registrations' | 'calendar' | 'preferences' | 'personalized' = 'registrations';

  // Category Preferences
  allCategories: CategoryResponseDto[] = [];
  userPreferences: Record<number, boolean> = {};

  // Personalized Recommended Events
  personalizedEvents: any[] = [];
  isLoadingPersonalized = false;

  // Feedback Submission Modal
  feedbackEvent: MyEventItemDto | null = null;
  feedbackRating = 5;
  feedbackComment = '';
  modalFeedbackError = '';
  reviewedEventIds: Set<number> = new Set<number>();

  // Virtual Access Link
  virtualMeetingUrl = '';
  virtualEventTitle = '';

  constructor(public apiService: ApiService) {}

  actionMessage = '';
  actionError = '';

  ngOnInit() {
    this.loadMyEvents();
    this.loadCategoriesAndPreferences();
    this.loadReviewedEventIds();
  }

  loadReviewedEventIds() {
    this.apiService.getMyReviewedEventIds().subscribe({
      next: (res) => {
        if (res?.data) {
          this.reviewedEventIds = new Set<number>(res.data);
        }
      },
      error: () => {}
    });
  }

  loadMyEvents() {
    this.isLoading = true;
    this.apiService.getMyEvents().subscribe({
      next: (res) => {
        this.activeEvents = res.active || [];
        this.pastEvents = res.past || [];
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.actionError = err?.error?.message || 'Failed to load your registrations.';
      }
    });
  }

  loadCategoriesAndPreferences() {
    this.apiService.getCategories(true).subscribe({
      next: (catRes) => {
        this.allCategories = catRes.data || [];
        this.apiService.getPreferences().subscribe({
          next: (prefRes) => {
            const prefs: Record<number, boolean> = {};
            (prefRes.data || []).forEach(p => {
              prefs[p.categoryId] = true;
            });
            this.userPreferences = prefs;
          }
        });
      }
    });
  }

  savePreferences() {
    const list: AttendeeCategoryPreferenceDto[] = [];
    Object.keys(this.userPreferences).forEach(k => {
      if (this.userPreferences[Number(k)]) {
        list.push({ categoryId: Number(k), weight: 1.0 });
      }
    });

    this.apiService.setPreferences({ preferences: list }).subscribe({
      next: () => {
        this.actionMessage = 'Interest preferences updated successfully! Recommendations will adapt.';
        this.activeTab = 'registrations';
      },
      error: () => {
        this.actionError = 'Failed to save preferences.';
      }
    });
  }

  exportCalendar(registrationId?: number) {
    if (!registrationId) return;
    this.apiService.exportRegistrationCalendar(registrationId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `registration-${registrationId}.ics`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.actionError = 'Failed to export calendar.';
      }
    });
  }

  getVirtualLink(eventId: number, title: string) {
    this.apiService.getVirtualAccessLink(eventId).subscribe({
      next: (res) => {
        if (res?.virtualMeetingUrl) {
          this.virtualMeetingUrl = res.virtualMeetingUrl;
          this.virtualEventTitle = title;
        } else {
          alert('No virtual meeting link has been configured for this event yet.');
        }
      },
      error: () => {
        this.actionError = 'Only confirmed attendees can access this virtual link.';
      }
    });
  }

  loadPersonalizedEvents() {
    this.isLoadingPersonalized = true;
    this.apiService.getRecommendations(20).subscribe({
      next: (res) => {
        this.personalizedEvents = res.data || [];
        this.isLoadingPersonalized = false;
      },
      error: () => {
        this.isLoadingPersonalized = false;
      }
    });
  }

  openFeedbackModal(item: MyEventItemDto) {
    this.feedbackEvent = item;
    this.feedbackRating = 5;
    this.feedbackComment = '';
    this.modalFeedbackError = '';
  }

  closeFeedbackModal() {
    this.feedbackEvent = null;
    this.modalFeedbackError = '';
  }

  submitFeedback() {
    if (!this.feedbackEvent) return;
    const eventId = this.feedbackEvent.eventId;
    this.modalFeedbackError = '';
    this.apiService.submitFeedback({
      eventId: eventId,
      rating: Number(this.feedbackRating),
      comments: this.feedbackComment
    }).subscribe({
      next: () => {
        this.reviewedEventIds.add(eventId);
        this.actionMessage = 'Thank you! Your feedback and rating have been recorded.';
        this.closeFeedbackModal();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to submit feedback.';
        this.modalFeedbackError = msg;
        if (msg.includes('already submitted')) {
          this.reviewedEventIds.add(eventId);
        }
      }
    });
  }

  cancelRegistration(registrationId?: number) {
    if (!registrationId) return;

    if (!confirm('Are you sure you want to cancel your registration for this event?')) {
      return;
    }

    this.actionMessage = '';
    this.actionError = '';

    this.apiService.cancelRegistration(registrationId).subscribe({
      next: (res) => {
        if (res.cancelled) {
          this.actionMessage = 'Registration cancelled successfully. Event capacity has been freed.';
          this.loadMyEvents();
        } else {
          this.actionError = res.reason || 'Failed to cancel registration.';
        }
      },
      error: (err) => {
        this.actionError = err?.error?.reason || 'Cancellation failed.';
      }
    });
  }
}
