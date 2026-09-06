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
    this.loadMyRegistrations();
    this.loadEvents();
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
    if (!this.hideRegistered || this.registeredEventIds.size === 0) {
      return this.events;
    }
    return this.events.filter(e => !this.registeredEventIds.has(e.eventId));
  }

  isRegistered(eventId: number): boolean {
    return this.registeredEventIds.has(eventId);
  }

  loadEvents() {
    this.isLoading = true;
    this.apiService.getPublicEvents({
      keyword: this.keyword,
      location: this.location,
      onlyAvailable: this.onlyAvailable
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
    this.loadEvents();
  }

  viewDetails(eventId: number) {
    this.apiService.getPublicEventById(eventId).subscribe({
      next: (data) => {
        this.selectedEvent = data;
      }
    });
  }

  closeModal() {
    this.selectedEvent = null;
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
