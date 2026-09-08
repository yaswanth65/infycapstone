import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from './api.service';
import { AuthService } from './auth';
import {
  UserResponseDto,
  UserCreateDto,
  RoleResponse,
  AuditLogResponseDto,
  PerformanceMetricsDto,
  CategoryResponseDto,
  VenueResponseDto,
  EventApprovalResponseDto
} from './models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html'
})
export class AdminComponent implements OnInit {
  activeTab: 'users' | 'create-user' | 'categories' | 'venues' | 'approvals' | 'audit' | 'metrics' = 'users';

  constructor(public apiService: ApiService, public authService: AuthService) {}

  // Users
  users: UserResponseDto[] = [];
  roles: RoleResponse[] = [];
  isLoadingUsers = false;

  // Categories (Brownfield)
  categories: CategoryResponseDto[] = [];
  newCategoryName = '';
  newCategoryDesc = '';
  isLoadingCategories = false;

  // Venues (Brownfield)
  venues: VenueResponseDto[] = [];
  newVenueName = '';
  newVenueAddress = '';
  newVenueCapacity = 100;
  newVenueContact = '';
  isLoadingVenues = false;

  // Approvals (Brownfield)
  pendingApprovals: EventApprovalResponseDto[] = [];
  approvalRemarks: Record<number, string> = {};
  isLoadingApprovals = false;

  // Create User Model
  userModel: UserCreateDto = {
    email: '',
    userName: '',
    displayName: '',
    phoneNumber: '',
    roleId: 0
  };

  // Audit Logs
  auditLogs: AuditLogResponseDto[] = [];
  filterActorUserId: number | null = null;
  filterActionType: string = '';
  isLoadingAudit = false;

  // Metrics
  metrics: PerformanceMetricsDto | null = null;
  isLoadingMetrics = false;

  message = '';
  errorMessage = '';

  ngOnInit() {
    this.loadRoles();
    this.loadUsers();
    this.loadCategories();
    this.loadVenues();
    this.loadApprovals();
  }

  setTab(tab: 'users' | 'create-user' | 'categories' | 'venues' | 'approvals' | 'audit' | 'metrics') {
    this.activeTab = tab;
    this.message = '';
    if (tab === 'users') this.loadUsers();
    if (tab === 'categories') this.loadCategories();
    if (tab === 'venues') this.loadVenues();
    if (tab === 'approvals') this.loadApprovals();
    if (tab === 'audit') this.loadAuditLogs();
    if (tab === 'metrics') this.loadMetrics();
  }

  loadCategories() {
    this.isLoadingCategories = true;
    this.apiService.getCategories(false).subscribe({
      next: (res) => {
        this.categories = res.data || [];
        this.isLoadingCategories = false;
      },
      error: () => this.isLoadingCategories = false
    });
  }

  createCategory() {
    if (!this.newCategoryName.trim()) return;
    this.apiService.createCategory({
      categoryName: this.newCategoryName.trim(),
      description: this.newCategoryDesc.trim()
    }).subscribe({
      next: () => {
        this.message = 'Category created successfully.';
        this.newCategoryName = '';
        this.newCategoryDesc = '';
        this.loadCategories();
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to create category.'
    });
  }

  isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  isValidPhone(phone: string): boolean {
    return /^\d{7,15}$/.test(phone.replace(/[\s\-\(\)]/g, ''));
  }

  toggleCategory(c: CategoryResponseDto) {
    this.apiService.updateCategory(c.categoryId, {
      categoryId: c.categoryId,
      categoryName: c.categoryName,
      description: c.description,
      isActive: !c.isActive
    }).subscribe({
      next: () => this.loadCategories(),
      error: () => this.errorMessage = 'Failed to update category.'
    });
  }

  loadVenues() {
    this.isLoadingVenues = true;
    this.apiService.getVenues(false).subscribe({
      next: (res) => {
        this.venues = res.data || [];
        this.isLoadingVenues = false;
      },
      error: () => this.isLoadingVenues = false
    });
  }

  createVenue() {
    if (!this.newVenueName.trim()) return;
    this.apiService.createVenue({
      name: this.newVenueName.trim(),
      address: this.newVenueAddress.trim(),
      capacity: Number(this.newVenueCapacity),
      contactDetails: this.newVenueContact.trim()
    }).subscribe({
      next: () => {
        this.message = 'Venue registered successfully.';
        this.newVenueName = '';
        this.newVenueAddress = '';
        this.newVenueContact = '';
        this.loadVenues();
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to register venue.'
    });
  }

  toggleVenue(v: VenueResponseDto) {
    this.apiService.updateVenue(v.venueId, {
      venueId: v.venueId,
      name: v.name,
      address: v.address,
      capacity: v.capacity,
      contactDetails: v.contactDetails,
      isActive: !v.isActive
    }).subscribe({
      next: () => this.loadVenues(),
      error: () => this.errorMessage = 'Failed to update venue.'
    });
  }

  loadApprovals() {
    this.isLoadingApprovals = true;
    this.apiService.getPendingApprovals().subscribe({
      next: (res) => {
        this.pendingApprovals = res.data || [];
        this.isLoadingApprovals = false;
      },
      error: () => this.isLoadingApprovals = false
    });
  }

  reviewApproval(requestId: number, approve: boolean) {
    if (!approve && !(this.approvalRemarks[requestId] || '').trim()) {
      this.errorMessage = 'A rejection reason is required. Please add review notes before rejecting.';
      return;
    }
    const remarks = this.approvalRemarks[requestId] || (approve ? 'Approved by Admin.' : 'Needs revision.');
    this.apiService.reviewApproval({
      approvalRequestId: requestId,
      approve,
      remarks
    }).subscribe({
      next: () => {
        this.message = `Event has been ${approve ? 'approved' : 'rejected'}. Organizer notified.`;
        this.loadApprovals();
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Review action failed.'
    });
  }

  loadRoles() {
    this.apiService.getRoles().subscribe({
      next: (res) => {
        if (res.success) {
          this.roles = res.data;
          // Don't rely on a hardcoded role ID; default to the first active role.
          if (!this.userModel.roleId && this.roles.length > 0) {
            this.userModel.roleId = this.roles[0].roleId;
          }
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to load roles.'
    });
  }

  loadUsers() {
    this.isLoadingUsers = true;
    this.apiService.getAllUsers(1, 20).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.users = res.data.data;
        }
        this.isLoadingUsers = false;
      },
      error: () => this.isLoadingUsers = false
    });
  }

  createUserSubmit() {
    if (!this.userModel.email || !this.userModel.userName || !this.userModel.displayName || !this.userModel.roleId) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    this.apiService.createUser(this.userModel).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = `User '${res.data.userName}' created successfully.`;
          this.setTab('users');
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to create user.'
    });
  }

  getRoleName(roleId: number): string {
    const r = this.roles.find(x => x.roleId === Number(roleId));
    return r ? r.roleName : 'Unknown';
  }

  changeRole(userId: number, roleId: number) {
    if (userId === this.authService.getUserId()) {
      this.errorMessage = 'You cannot change your own role. Have another Administrator adjust it.';
      this.loadUsers();
      return;
    }

    this.apiService.updateUserRole({ userId, roleId: Number(roleId) }).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = 'User role updated successfully.';
          this.loadUsers();
        } else {
          this.errorMessage = res.message || 'Failed to update user role.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to update user role.'
    });
  }

  deactivate(userId: number, userName: string) {
    if (userId === this.authService.getUserId()) {
      this.errorMessage = 'You cannot deactivate your own account.';
      return;
    }
    if (!confirm(`Are you sure you want to deactivate user '${userName}'?`)) return;
    this.apiService.deactivateUser(userId).subscribe({
      next: (res) => {
        if (res.success) {
          this.message = `User '${userName}' deactivated successfully.`;
          this.loadUsers();
        } else {
          this.errorMessage = res.message || 'Failed to deactivate user.';
        }
      },
      error: (err) => this.errorMessage = err?.error?.message || 'Failed to deactivate user.'
    });
  }

  // Audit Logs
  loadAuditLogs() {
    this.isLoadingAudit = true;
    this.apiService.getAuditLogs({
      actorUserId: this.filterActorUserId ? Number(this.filterActorUserId) : undefined,
      actionType: this.filterActionType || undefined,
      pageNumber: 1,
      pageSize: 20
    }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.auditLogs = res.data.data;
        }
        this.isLoadingAudit = false;
      },
      error: () => this.isLoadingAudit = false
    });
  }

  // Metrics
  loadMetrics() {
    this.isLoadingMetrics = true;
    this.apiService.getPerformanceMetrics().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.metrics = res.data;
        }
        this.isLoadingMetrics = false;
      },
      error: () => this.isLoadingMetrics = false
    });
  }

  // Authenticated Report File Downloads
  downloadExport(type: 'registrations' | 'attendance') {
    const obs = type === 'registrations' 
      ? this.apiService.exportRegistrationReportBlob() 
      : this.apiService.exportAttendanceReportBlob();

    obs.subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${type}_report_${Date.now()}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.message = `${type.toUpperCase()} report exported successfully.`;
      },
      error: () => {
        this.errorMessage = `Failed to export ${type} report.`;
      }
    });
  }
}
