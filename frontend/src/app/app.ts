import { Component, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth';
import { ApiService } from './api.service';
import { NotificationItemDto } from './models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet],
  templateUrl: './app.html'
})
export class AppComponent implements OnInit {
  notifications: NotificationItemDto[] = [];
  unreadCount: number = 0;
  showNotificationsMenu: boolean = false;
  eventsMap: Map<number, string> = new Map();

  constructor(public authService: AuthService, public apiService: ApiService) {}

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.fetchNotifications();
      this.loadEventTitles();
    }
  }

  loadEventTitles() {
    this.apiService.getPublicEvents({}).subscribe({
      next: (events) => {
        if (events) {
          events.forEach(e => this.eventsMap.set(e.eventId, e.title));
        }
      }
    });
  }

  fetchNotifications() {
    if (!this.authService.isAuthenticated()) return;
    this.loadEventTitles();
    this.apiService.getUnreadNotificationCount().subscribe({
      next: (res) => { if (res.success) this.unreadCount = res.data; }
    });
    this.apiService.getNotifications(false, 1, 10).subscribe({
      next: (res) => { if (res.success && res.data) this.notifications = res.data.data; }
    });
  }

  getFormattedNotificationMessage(msg: string): string {
    if (!msg) return '';
    return msg.replace(/event #(\d+)/gi, (match, idStr) => {
      const id = parseInt(idStr, 10);
      const title = this.eventsMap.get(id);
      return title ? `event '${title}' (#${id})` : match;
    });
  }

  toggleNotifications() {
    this.showNotificationsMenu = !this.showNotificationsMenu;
    if (this.showNotificationsMenu) {
      this.fetchNotifications();
    }
  }

  markRead(notificationId: number) {
    this.apiService.markNotificationRead(notificationId).subscribe({
      next: () => this.fetchNotifications()
    });
  }

  markAllRead() {
    this.apiService.markAllNotificationsRead().subscribe({
      next: () => this.fetchNotifications()
    });
  }

  logout() {
    this.showNotificationsMenu = false;
    this.authService.logout();
  }
}
