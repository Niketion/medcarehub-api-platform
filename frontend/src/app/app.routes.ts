import { Routes } from '@angular/router';
import { HomePageComponent } from './pages/home.page';
import { DashboardPageComponent } from './pages/dashboard.page';
import { SlotsPageComponent } from './pages/slots.page';
import { BookingsPageComponent } from './pages/bookings.page';
import { ReportsPageComponent } from './pages/reports.page';
import { AuthGuard } from './core/auth.guard';
import { RoleGuard } from './core/role.guard';

export const routes: Routes = [
  { path: '', component: HomePageComponent },

  { path: 'dashboard', component: DashboardPageComponent, canActivate: [AuthGuard] },

  { path: 'slots', component: SlotsPageComponent, canActivate: [AuthGuard] },
  { path: 'bookings', component: BookingsPageComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['patient'] } },
  { path: 'reports', component: ReportsPageComponent, canActivate: [AuthGuard] },

  { path: '**', redirectTo: '' }
];