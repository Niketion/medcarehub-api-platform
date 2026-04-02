import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  ApiClient,
  BookingDto,
  DashboardEconomicsDto,
  DoctorEconomicsDto,
  PrestazioneEconomicsDto,
  ReportDto,
  RevenueTrendPointDto,
  SlotDto
} from '../core/api-client';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.css']
})
export class DashboardPageComponent {
  private api = inject(ApiClient);
  auth = inject(AuthService);

  loading = false;
  error: string | null = null;

  economicsLoading = false;
  economicsError: string | null = null;

  economics: DashboardEconomicsDto | null = null;
  economicsByDoctor: DoctorEconomicsDto[] = [];
  economicsByPrestazione: PrestazioneEconomicsDto[] = [];
  revenueTrend: RevenueTrendPointDto[] = [];

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

  payingId: string | null = null;
  paymentError: string | null = null;

  trackById(_: number, x: any) { return x.id; }
  trackByLabel(_: number, x: RevenueTrendPointDto) { return x.label; }

  isStaff() {
    return this.auth.hasRole('operator') || this.auth.hasRole('doctor') || this.auth.hasRole('admin');
  }

  async ngOnInit() {
    await this.load();
  }

  async load() {
    this.loading = true;
    this.error = null;
    this.completeError = null;
    this.paymentError = null;
    this.economicsError = null;

    try {
      this.slots = await firstValueFrom(this.api.getSlots());
      this.bookings = this.isStaff()
        ? await firstValueFrom(this.api.getAllBookings())
        : await firstValueFrom(this.api.myBookings());
      this.reports = this.isStaff()
        ? await firstValueFrom(this.api.getAllReports())
        : await firstValueFrom(this.api.myReports());

      this.applyFilters(false);

      if (this.isStaff()) {
        await this.loadEconomics();
      }
    } catch {
      this.error = 'Errore caricamento dati (permessi o backend).';
      this.slots = [];
      this.bookings = [];
      this.reports = [];
      this.applyFilters(false);
    } finally {
      this.loading = false;
    }
  }

  async loadEconomics() {
    if (!this.isStaff()) return;

    this.economicsLoading = true;
    this.economicsError = null;

    try {
      const from = this.fromDate ? new Date(this.fromDate + 'T00:00:00').toISOString() : null;
      const to = this.toDate ? new Date(this.toDate + 'T23:59:59').toISOString() : null;

      this.economics = await firstValueFrom(
        this.api.getEconomics(from, to, this.doctorQuery || null)
      );

      this.economicsByDoctor = this.economics.byDoctor ?? [];
      this.economicsByPrestazione = this.economics.byPrestazione ?? [];
      this.revenueTrend = this.economics.revenueTrend ?? [];
    } catch {
      this.economics = null;
      this.economicsByDoctor = [];
      this.economicsByPrestazione = [];
      this.revenueTrend = [];
      this.economicsError = 'Errore caricamento KPI economici.';
    } finally {
      this.economicsLoading = false;
    }
  }

  applyFilters(reloadEconomics = true) {
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

    this.slotTotal = this.vSlots.length;
    this.slotAvailable = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'available').length;
    this.slotBooked = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'booked').length;
    this.slotCancelled = this.vSlots.filter(s => (s.status ?? '').toLowerCase() === 'cancelled').length;
    this.slotAvailabilityPct = this.slotTotal ? (this.slotAvailable / this.slotTotal) * 100 : 0;

    this.bookingTotal = this.vBookings.length;
    this.bookingConfirmed = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'confirmed').length;
    this.bookingCancelled = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'cancelled').length;
    this.bookingCompleted = this.vBookings.filter(b => (b.status ?? '').toLowerCase() === 'completed').length;
    this.bookingConfirmedPct = this.bookingTotal ? (this.bookingConfirmed / this.bookingTotal) * 100 : 0;

    this.reportTotal = this.vReports.length;
    const now = new Date();
    const d30 = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    this.reportLast30 = this.vReports.filter(r => new Date(r.createdAt) >= d30).length;

    const upcoming = this.vBookings
      .filter(b => (b.status ?? '').toLowerCase() !== 'cancelled')
      .slice()
      .sort((a, b) => +new Date(a.slotStartsAt) - +new Date(b.slotStartsAt));

    this.upcomingBookings = upcoming.slice(0, 10);

    this.recentReports = this.vReports
      .slice()
      .sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt))
      .slice(0, 10);

    if (reloadEconomics && this.isStaff()) {
      void this.loadEconomics();
    }
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

  paymentLabel(status: string | null | undefined) {
    const s = (status ?? '').toLowerCase();
    if (s === 'paid') return 'Pagata';
    return 'Da pagare';
  }

  paymentBadgeClass(status: string | null | undefined) {
    const s = (status ?? '').toLowerCase();
    if (s === 'paid') return 'success';
    return 'warning';
  }

  canMarkPaid(b: BookingDto) {
    const status = (b.status ?? '').toLowerCase();
    const payment = (b.paymentStatus ?? '').toLowerCase();
    return status !== 'cancelled' && payment !== 'paid';
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

  async markPaid(b: BookingDto) {
    this.payingId = b.id;
    this.paymentError = null;

    try {
      await firstValueFrom(this.api.markBookingPaid(b.id));
      await this.load();
    } catch (e: any) {
      this.paymentError = e?.error?.detail || 'Errore aggiornamento pagamento.';
    } finally {
      this.payingId = null;
    }
  }

  visibleTrend(): RevenueTrendPointDto[] {
    return (this.revenueTrend ?? []).slice(-8);
  }

  trendMax() {
    const values = this.visibleTrend().flatMap(x => [x.realizedRevenue ?? 0, x.paidRevenue ?? 0]);
    const max = Math.max(...values, 0);
    return max <= 0 ? 1 : max;
  }

  trendBarHeight(value: number) {
    const max = this.trendMax();
    return Math.max(6, (value / max) * 100);
  }

  outstandingRevenue() {
    if (!this.economics) return 0;
    return Math.max((this.economics.realizedRevenue ?? 0) - (this.economics.paidRevenue ?? 0), 0);
  }

  collectionRate() {
    if (!this.economics) return 0;
    const realized = Math.max(this.economics.realizedRevenue ?? 0, 0);
    if (realized === 0) return 0;
    return Math.max(0, Math.min(100, ((this.economics.paidRevenue ?? 0) / realized) * 100));
  }

  donutBackground() {
    const paidPct = this.collectionRate();
    return `conic-gradient(#111827 0 ${paidPct}%, rgba(17, 24, 39, 0.14) ${paidPct}% 100%)`;
  }

  formatBytes(n: number) {
    if (!n && n !== 0) return '-';
    const units = ['B', 'KB', 'MB', 'GB'];
    let v = n;
    let i = 0;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return `${v.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
  }

  formatEuro(n: number | null | undefined) {
    const v = Number(n ?? 0);
    return `€ ${v.toFixed(2)}`;
  }
}