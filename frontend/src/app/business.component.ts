import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from './api.service';
import { DashboardMetricsDto, EventSummaryDto, AdvancedAnalyticsDto } from './models';

@Component({
  selector: 'app-business',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './business.component.html'
})
export class BusinessComponent implements OnInit {
  activeTab: 'overview' | 'analytics' | 'registrations' | 'attendance' | 'completion' = 'overview';

  constructor(public apiService: ApiService) {}

  metrics: DashboardMetricsDto | null = null;
  summaries: EventSummaryDto[] = [];
  analytics: AdvancedAnalyticsDto | null = null;
  isLoading = false;

  ngOnInit() {
    this.loadDashboard();
  }

  setTab(tab: 'overview' | 'analytics' | 'registrations' | 'attendance' | 'completion') {
    this.activeTab = tab;
    if (tab === 'overview') this.loadDashboard();
    if (tab === 'analytics') this.loadAdvancedAnalytics();
    if (tab === 'registrations') this.loadRegistrationReports();
    if (tab === 'attendance') this.loadAttendanceReports();
    if (tab === 'completion') this.loadCompletionReports();
  }

  loadAdvancedAnalytics() {
    this.isLoading = true;
    this.apiService.getAdvancedAnalytics().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.analytics = res.data;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadDashboard() {
    this.isLoading = true;
    this.apiService.getBusinessDashboard().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.metrics = res.data;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadRegistrationReports() {
    this.isLoading = true;
    this.apiService.getBusinessRegistrationReports().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summaries = res.data.data;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadAttendanceReports() {
    this.isLoading = true;
    this.apiService.getBusinessAttendanceReports().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summaries = res.data.data;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadCompletionReports() {
    this.isLoading = true;
    this.apiService.getBusinessCompletionReports().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summaries = res.data.data;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
}
