import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ApiClient, BookingDto, ReportDto, SlotDto } from '../core/api-client';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="page">
    <div class="page-head">
      <div class="page-title">
        <h1>Dashboard</h1>
        <div class="sub">
          Panoramica operativa (slot, prenotazioni e referti) con filtri per periodo e medico.
        </div>
      </div>

      <div class="row" style="justify-content:flex-end;">
        <a class="btn" routerLink="/slots">Slot</a>
        <a class="btn" routerLink="/bookings" *ngIf="auth.hasRole('patient')">Prenotazioni</a>
        <a class="btn" routerLink="/reports">Referti</a>
        <button class="btn primary" (click)="load()" [disabled]="loading">
          {{ loading ? 'Aggiorno…' : 'Aggiorna' }}
        </button>
      </div>
    </div>

    <div *ngIf="error" class="alert error">
      <div>
        <div style="font-weight:800">Errore</div>
        <div class="small muted" style="margin-top:2px;">{{ error }}</div>
      </div>
      <button class="btn" (click)="error=null">Chiudi</button>
    </div>

    <!-- FILTRI -->
    <div class="card">
      <div class="page-head">
        <div class="page-title">
          <h2 style="font-size:18px;">Filtri</h2>
          <div class="sub">I KPI e le liste sotto rispettano i filtri selezionati.</div>
        </div>
      </div>

      <div style="height:12px;"></div>

      <div class="row" style="align-items:flex-end;">
        <div class="field" style="min-width:220px; flex:0;">
          <div class="label">Dal</div>
          <input class="input" type="date" [(ngModel)]="fromDate" (change)="applyFilters()" />
        </div>

        <div class="field" style="min-width:220px; flex:0;">
          <div class="label">Al</div>
          <input class="input" type="date" [(ngModel)]="toDate" (change)="applyFilters()" />
        </div>

        <div class="field" style="min-width:280px; flex:1;">
          <div class="label">DoctorId (contiene)</div>
          <input class="input" [(ngModel)]="doctorQuery" (input)="applyFilters()" placeholder="es. dr-rossi" />
        </div>

        <button class="btn" (click)="resetFilters()">Reset</button>

        <div class="small muted" style="margin-left:auto;">
          {{ loading ? 'Caricamento…' : summaryLine() }}
        </div>
      </div>
    </div>

    <!-- KPI -->
    <div class="grid-3">
      <div class="panel kpi">
        <div class="kpi-label">Slot (periodo)</div>
        <div class="kpi-value">{{ slotTotal }}</div>
        <div class="row" style="margin-top:8px;">
          <span class="badge success">{{ slotAvailable }} disponibili</span>
          <span class="badge warning">{{ slotBooked }} prenotati</span>
          <span class="badge danger">{{ slotCancelled }} cancellati</span>
        </div>

        <div style="height:10px;"></div>
        <div class="bar">
          <div class="bar-fill" [style.width.%]="slotAvailabilityPct"></div>
        </div>
        <div class="small muted" style="margin-top:6px;">
          Disponibilità: {{ slotAvailabilityPct | number:'1.0-0' }}%
        </div>
      </div>

      <div class="panel kpi">
        <div class="kpi-label">Prenotazioni (periodo)</div>
        <div class="kpi-value">{{ bookingTotal }}</div>
        <div class="row" style="margin-top:8px;">
          <span class="badge success">{{ bookingConfirmed }} confermate</span>
          <span class="badge warning">{{ bookingCompleted }} completate</span>
          <span class="badge danger">{{ bookingCancelled }} annullate</span>
        </div>

        <div style="height:10px;"></div>
        <div class="bar">
          <div class="bar-fill" [style.width.%]="bookingConfirmedPct"></div>
        </div>
        <div class="small muted" style="margin-top:6px;">
          Confermate: {{ bookingConfirmedPct | number:'1.0-0' }}%
        </div>
      </div>

      <div class="panel kpi">
        <div class="kpi-label">Referti</div>
        <div class="kpi-value">{{ reportTotal }}</div>
        <div class="row" style="margin-top:8px;">
          <span class="badge">{{ reportLast30 }} ultimi 30 giorni</span>
        </div>

        <div style="height:10px;"></div>
        <div class="small muted">
          Vista: {{ isStaff() ? 'staff (tutti)' : 'paziente (miei)' }}
        </div>
      </div>
    </div>

    <!-- LISTE -->
    <div class="grid-2">
      <div class="card">
        <div class="page-head">
          <div class="page-title">
            <h2 style="font-size:18px;">Prossimi appuntamenti</h2>
            <div class="sub">Ordinati per data/ora crescente.</div>
          </div>
        </div>

        <div style="height:12px;"></div>

        <ng-container *ngIf="loading; else upcomingBlock">
          <div class="stack">
            <div class="skeleton" style="height:18px; width:55%;"></div>
            <div class="skeleton" style="height:14px; width:85%;"></div>
            <div class="skeleton" style="height:14px; width:76%;"></div>
          </div>
        </ng-container>

        <ng-template #upcomingBlock>
          <div class="table-wrap" *ngIf="upcomingBookings.length; else emptyUpcoming">
            <table class="table">
              <thead>
                <tr>
                  <th>Quando</th>
                  <th>DoctorId</th>
                  <th>Prestazione</th>
                  <th>Stato</th>
                  <th style="width:180px;" *ngIf="isStaff()"></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let b of upcomingBookings; trackBy: trackById">
                  <td>{{ b.slotStartsAt | date:'dd/MM/yyyy HH:mm' }} → {{ b.slotEndsAt | date:'HH:mm' }}</td>
                  <td>{{ b.slotDoctorId }}</td>
                  <td>{{ b.slotPrestazioneName || '-' }}</td>
                  <td>
                    <span class="badge" [ngClass]="bookingBadgeClass(b.status)">
                      {{ bookingLabel(b.status) }}
                    </span>
                  </td>
                  <td *ngIf="isStaff()">
                    <button class="btn"
                      (click)="complete(b)"
                      [disabled]="completingId===b.id || (b.status||'').toLowerCase()!=='confirmed'">
                      {{ completingId===b.id ? 'Completo…' : 'Completa' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <ng-template #emptyUpcoming>
            <div class="empty">
              <div class="empty-title">Nessun appuntamento in arrivo</div>
              <div class="empty-sub">Prova a modificare i filtri, oppure crea/prenota uno slot.</div>
            </div>
          </ng-template>
        </ng-template>

        <div class="small muted" *ngIf="completeError" style="margin-top:10px;">{{ completeError }}</div>
      </div>

      <div class="card">
        <div class="page-head">
          <div class="page-title">
            <h2 style="font-size:18px;">Ultimi referti</h2>
            <div class="sub">Ordinati per creazione decrescente.</div>
          </div>
        </div>

        <div style="height:12px;"></div>

        <ng-container *ngIf="loading; else reportsBlock">
          <div class="stack">
            <div class="skeleton" style="height:18px; width:45%;"></div>
            <div class="skeleton" style="height:14px; width:85%;"></div>
            <div class="skeleton" style="height:14px; width:76%;"></div>
          </div>
        </ng-container>

        <ng-template #reportsBlock>
          <div class="table-wrap" *ngIf="recentReports.length; else emptyReports">
            <table class="table">
              <thead>
                <tr>
                  <th>Creato</th>
                  <th>File</th>
                  <th>Tipo</th>
                  <th>Size</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let r of recentReports; trackBy: trackById">
                  <td>{{ r.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td>
                    <div style="font-weight:800">{{ r.fileName }}</div>
                    <div class="small muted">{{ r.contentType }}</div>
                  </td>
                  <td>{{ r.reportType || '-' }}</td>
                  <td>{{ formatBytes(r.sizeBytes) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <ng-template #emptyReports>
            <div class="empty">
              <div class="empty-title">Nessun referto</div>
              <div class="empty-sub">Quando viene caricato un documento, comparirà qui.</div>
            </div>
          </ng-template>
        </ng-template>
      </div>
    </div>
  </div>
  `
})
export class DashboardPageComponent {
  private api = inject(ApiClient);
  auth = inject(AuthService);

  loading = false;
  error: string | null = null;

  slots: SlotDto[] = [];
  bookings: BookingDto[] = [];
  reports: ReportDto[] = [];

  vSlots: SlotDto[] = [];
  vBookings: BookingDto[] = [];
  vReports: ReportDto[] = [];

  fromDate = '';
  toDate = '';
  doctorQuery = '';

  upcomingBookings: BookingDto[] = [];
  recentReports: ReportDto[] = [];

  slotTotal = 0;
  slotAvailable = 0;
  slotBooked = 0;
  slotCancelled = 0;
  slotAvailabilityPct = 0;

  bookingTotal = 0;
  bookingConfirmed = 0;
  bookingCancelled = 0;
  bookingCompleted = 0;
  bookingConfirmedPct = 0;

  reportTotal = 0;
  reportLast30 = 0;

  completingId: string | null = null;
  completeError: string | null = null;

  trackById(_: number, x: any) { return x.id; }

  isStaff() {
    return this.auth.hasRole('operator') || this.auth.hasRole('doctor') || this.auth.hasRole('admin');
  }

  async ngOnInit() { await this.load(); }

  async load() {
    this.loading = true;
    this.error = null;
    this.completeError = null;

    try {
      this.slots = await firstValueFrom(this.api.getSlots());
      this.bookings = this.isStaff()
        ? await firstValueFrom(this.api.getAllBookings())
        : await firstValueFrom(this.api.myBookings());
      this.reports = this.isStaff()
        ? await firstValueFrom(this.api.getAllReports())
        : await firstValueFrom(this.api.myReports());

      this.applyFilters();
    } catch {
      this.error = 'Errore caricamento dati (permessi o backend).';
      this.slots = [];
      this.bookings = [];
      this.reports = [];
      this.applyFilters();
    } finally {
      this.loading = false;
    }
  }

  applyFilters() {
    const from = this.fromDate ? new Date(this.fromDate + 'T00:00:00') : null;
    const to = this.toDate ? new Date(this.toDate + 'T23:59:59') : null;
    const dq = this.doctorQuery.trim().toLowerCase();

    this.vSlots = this.slots.filter(s => {
      const d = (s.doctorId ?? '').toLowerCase();
      const okDoctor = !dq || d.includes(dq);
      const st = new Date(s.startsAt);
      const okFrom = !from || st >= from;
      const okTo = !to || st <= to;
      return okDoctor && okFrom && okTo;
    });

    this.vBookings = this.bookings.filter(b => {
      const d = (b.slotDoctorId ?? '').toLowerCase();
      const okDoctor = !dq || d.includes(dq);
      const st = new Date(b.slotStartsAt);
      const okFrom = !from || st >= from;
      const okTo = !to || st <= to;
      return okDoctor && okFrom && okTo;
    });

    this.vReports = this.reports;

    // KPI slots
    this.slotTotal = this.vSlots.length;
    this.slotAvailable = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'available').length;
    this.slotBooked = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'booked').length;
    this.slotCancelled = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'cancelled').length;
    this.slotAvailabilityPct = this.slotTotal ? (this.slotAvailable / this.slotTotal) * 100 : 0;

    // KPI bookings
    this.bookingTotal = this.vBookings.length;
    this.bookingConfirmed = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'confirmed').length;
    this.bookingCancelled = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'cancelled').length;
    this.bookingCompleted = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'completed').length;
    this.bookingConfirmedPct = this.bookingTotal ? (this.bookingConfirmed / this.bookingTotal) * 100 : 0;

    // KPI reports
    this.reportTotal = this.vReports.length;
    const now = new Date();
    const d30 = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    this.reportLast30 = this.vReports.filter(r => new Date(r.createdAt) >= d30).length;

    // lists
    const upcoming = this.vBookings
      .filter(b => (b.status ?? '').toLowerCase() !== 'cancelled')
      .slice()
      .sort((a, b) => +new Date(a.slotStartsAt) - +new Date(b.slotStartsAt));

    this.upcomingBookings = upcoming.slice(0, 10);

    this.recentReports = this.vReports
      .slice()
      .sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt))
      .slice(0, 10);
  }

  resetFilters() {
    this.fromDate = '';
    this.toDate = '';
    this.doctorQuery = '';
    this.applyFilters();
  }

  summaryLine() {
    return `${this.vSlots.length} slot · ${this.vBookings.length} prenotazioni · ${this.vReports.length} referti`;
  }

  bookingLabel(status: string) {
    const s = (status ?? '').toLowerCase();
    if (s === 'confirmed') return 'Confermata';
    if (s === 'cancelled') return 'Annullata';
    if (s === 'completed') return 'Completata';
    return status || '-';
  }

  bookingBadgeClass(status: string) {
    const s = (status ?? '').toLowerCase();
    if (s === 'confirmed') return 'success';
    if (s === 'cancelled') return 'danger';
    if (s === 'completed') return 'warning';
    return '';
  }

  async complete(b: BookingDto) {
    this.completingId = b.id;
    this.completeError = null;
    try {
      await firstValueFrom(this.api.completeBooking(b.id));
      await this.load();
    } catch (e: any) {
      this.completeError = e?.error?.detail || 'Errore completamento.';
    } finally {
      this.completingId = null;
    }
  }

  formatBytes(n: number) {
    if (!n && n !== 0) return '-';
    const units = ['B', 'KB', 'MB', 'GB'];
    let v = n;
    let i = 0;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return `${v.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
  }
}