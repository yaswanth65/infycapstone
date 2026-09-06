import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from './api.service';
import { MyEventItemDto } from './models';

@Component({
  selector: 'app-attendee',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './attendee.component.html'
})
export class AttendeeComponent implements OnInit {
  activeEvents: MyEventItemDto[] = [];
  pastEvents: MyEventItemDto[] = [];
  isLoading = false;

  constructor(public apiService: ApiService) {}

  actionMessage = '';
  actionError = '';

  ngOnInit() {
    this.loadMyEvents();
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
