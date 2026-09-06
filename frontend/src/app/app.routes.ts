import { Routes } from '@angular/router';
import { LoginComponent } from './login.component';
import { EventsComponent } from './events.component';
import { AttendeeComponent } from './attendee.component';
import { ManagerComponent } from './manager.component';
import { BusinessComponent } from './business.component';
import { AdminComponent } from './admin.component';
import { roleGuard } from './auth';

export const routes: Routes = [
  { path: '', redirectTo: 'events', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'events', component: EventsComponent },
  {
    path: 'attendee',
    component: AttendeeComponent,
    canActivate: [roleGuard(['Attendee'])]
  },
  {
    path: 'manager',
    component: ManagerComponent,
    canActivate: [roleGuard(['EventManager', 'Administrator'])]
  },
  {
    path: 'business',
    component: BusinessComponent,
    canActivate: [roleGuard(['BusinessManagement', 'Administrator'])]
  },
  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [roleGuard(['Administrator'])]
  },
  { path: '**', redirectTo: 'events' }
];
