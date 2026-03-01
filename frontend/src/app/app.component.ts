import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIf, NgFor],
  template: `
  <header class="header">
    <div class="container">
      <div class="nav">
        <div class="nav-left">
          <div class="brand">
            <span class="brand-dot"></span>
            <span>MedCare Hub</span>
          </div>

          <nav class="nav-links">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Home</a>
            <a routerLink="/dashboard" routerLinkActive="active" *ngIf="isAuthenticated()">Dashboard</a>
            <a routerLink="/slots" routerLinkActive="active">Slot</a>
            <a routerLink="/bookings" routerLinkActive="active" *ngIf="hasRole('patient')">Prenotazioni</a>
            <a routerLink="/reports" routerLinkActive="active">Referti</a>
          </nav>
        </div>

        <div class="nav-right">
          <div *ngIf="isAuthenticated()" class="row" style="gap:8px;">
            <span class="small muted">
              {{ username() }}
            </span>
            <span class="badge" *ngFor="let r of roles()">{{ r }}</span>
          </div>

          <button class="btn primary" *ngIf="!isAuthenticated()" (click)="login()">Login</button>
          <button class="btn" *ngIf="isAuthenticated()" (click)="logout()">Logout</button>
        </div>
      </div>
    </div>
  </header>

  <main class="container content">
    <router-outlet></router-outlet>
  </main>

  <footer class="container footer">
    PW · Slot · Prenotazioni · Referti 
  </footer>
  `
})
export class AppComponent {
  private auth = inject(AuthService);

  isAuthenticated = () => this.auth.isAuthenticated();
  username = computed(() => this.auth.profile()?.preferred_username ?? this.auth.profile()?.email ?? 'utente');
  roles = computed(() => this.auth.roles());

  hasRole(role: string) {
    return this.auth.hasRole(role);
  }

  login() { this.auth.login(); }
  logout() { this.auth.logout(); }
}