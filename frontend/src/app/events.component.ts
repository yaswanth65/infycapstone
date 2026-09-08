import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { AuthService } from './auth';
import { PublicEventDto } from './models';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './events.component.html'
})
export class EventsComponent implements OnInit {
  events: PublicEventDto[] = [];
  recommendedEvents: any[] = [];
  categories: any[] = [];
  selectedCategory = '';
  selectedCategoryId: number | null = null;
  userPreferenceCategoryIds: Set<number> = new Set();
  filterOnlyPreferences = false;

  // Feedback viewing
  feedbackSummary: any = null;
  isLoadingFeedback = false;

  constructor(
    public apiService: ApiService,
    public authService: AuthService,
    public router: Router
  ) {}
  selectedEvent: PublicEventDto | null = null;
  isLoading = false;

  // Filter params
  keyword = '';
  location = '';
  onlyAvailable = false;
  hideRegistered = false;

  registeredEventIds: Set<number> = new Set();

  actionMessage = '';
  actionError = '';

  ngOnInit() {
    this.loadCategories();
    this.loadUserPreferences();
    this.loadMyRegistrations();
    this.loadEvents();
    this.loadRecommendations();
  }

  loadCategories() {
    this.apiService.getCategories(true).subscribe({
      next: (res) => {
        if (res?.data) this.categories = res.data;
      }
    });
  }

  loadUserPreferences() {
    if (!this.authService.isAuthenticated() || this.authService.getRole() !== 'Attendee') return;
    this.apiService.getPreferences().subscribe({
      next: (res) => {
        const prefSet = new Set<number>();
        (res?.data || []).forEach(p => prefSet.add(p.categoryId));
        this.userPreferenceCategoryIds = prefSet;
      }
    });
  }

  loadRecommendations() {
    if (!this.authService.isAuthenticated() || this.authService.getRole() !== 'Attendee') return;
    this.apiService.getRecommendations(6).subscribe({
      next: (res) => {
        if (res?.data) this.recommendedEvents = res.data;
      }
    });
  }

  loadMyRegistrations() {
    if (!this.authService.isAuthenticated()) return;
    this.apiService.getMyEvents().subscribe({
      next: (res) => {
        if (res) {
          const ids = new Set<number>();
          (res.active || []).forEach(item => ids.add(item.eventId));
          (res.past || []).forEach(item => ids.add(item.eventId));
          this.registeredEventIds = ids;
        }
      }
    });
  }

  get displayedEvents(): PublicEventDto[] {
    let list = this.events;
    if (this.hideRegistered && this.registeredEventIds.size > 0) {
      list = list.filter(e => !this.registeredEventIds.has(e.eventId));
    }
    if (this.filterOnlyPreferences && this.userPreferenceCategoryIds.size > 0) {
      list = list.filter(e => e.categoryIds?.some(id => this.userPreferenceCategoryIds.has(id)));
    }
    return list;
  }

  isRegistered(eventId: number): boolean {
    return this.registeredEventIds.has(eventId);
  }

  selectCategory(catId: number | null) {
    this.filterOnlyPreferences = false;
    this.selectedCategoryId = catId;
    this.loadEvents();
  }

  togglePreferenceFilter() {
    this.filterOnlyPreferences = !this.filterOnlyPreferences;
    if (this.filterOnlyPreferences) {
      this.selectedCategoryId = null;
    }
  }

  loadEvents() {
    this.isLoading = true;
    this.apiService.getPublicEvents({
      keyword: this.keyword,
      location: this.location,
      onlyAvailable: this.onlyAvailable,
      categoryId: this.selectedCategoryId || undefined
    }).subscribe({
      next: (data) => {
        this.events = data || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  resetFilters() {
    this.keyword = '';
    this.location = '';
    this.onlyAvailable = false;
    this.hideRegistered = false;
    this.selectedCategoryId = null;
    this.filterOnlyPreferences = false;
    this.loadEvents();
  }

  viewDetails(eventId: number) {
    this.feedbackSummary = null;
    this.apiService.getPublicEventById(eventId).subscribe({
      next: (data) => {
        this.selectedEvent = data;
        this.loadEventFeedback(eventId);
      }
    });
  }

  loadEventFeedback(eventId: number) {
    this.isLoadingFeedback = true;
    this.apiService.getEventFeedbackSummary(eventId).subscribe({
      next: (res) => {
        this.feedbackSummary = res.data;
        this.isLoadingFeedback = false;
      },
      error: () => {
        this.isLoadingFeedback = false;
      }
    });
  }

  downloadCalendar(eventId: number) {
    this.apiService.exportEventCalendar(eventId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `event-${eventId}.ics`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.actionError = 'Failed to export calendar.';
      }
    });
  }

  closeModal() {
    this.selectedEvent = null;
    this.feedbackSummary = null;
    this.actionMessage = '';
    this.actionError = '';
  }

  registerForEvent(eventId: number) {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    if (this.authService.getRole() !== 'Attendee') {
      this.actionError = 'Only Attendees can self-register for events.';
      return;
    }

    this.actionMessage = '';
    this.actionError = '';

    this.apiService.registerEvent({ eventId }).subscribe({
      next: (res) => {
        if (res.outcome === 'Confirmed') {
          this.actionMessage = '🎉 Registration Confirmed successfully!';
        } else if (res.outcome === 'Waitlisted') {
          this.actionMessage = `⏳ Capacity full. You have been added to the waitlist (Position #${res.waitlistPosition}).`;
        } else {
          this.actionError = res.message || 'Unable to register for this event.';
        }
        this.loadMyRegistrations();
        this.loadEvents();
        if (this.selectedEvent) {
          this.viewDetails(this.selectedEvent.eventId);
        }
      },
      error: (err) => {
        this.actionError = err?.error?.message || 'Registration failed.';
      }
    });
  }
}
