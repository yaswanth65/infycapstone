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
  PerformanceMetricsDto
} from './models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html'
})
export class AdminComponent implements OnInit {
  activeTab: 'users' | 'create-user' | 'audit' | 'metrics' = 'users';

  constructor(public apiService: ApiService, public authService: AuthService) {}

  // Users
  users: UserResponseDto[] = [];
  roles: RoleResponse[] = [];
  isLoadingUsers = false;

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
  }

  setTab(tab: 'users' | 'create-user' | 'audit' | 'metrics') {
    this.activeTab = tab;
    this.message = '';
    this.errorMessage = '';
    if (tab === 'users') this.loadUsers();
    if (tab === 'audit') this.loadAuditLogs();
    if (tab === 'metrics') this.loadMetrics();
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
