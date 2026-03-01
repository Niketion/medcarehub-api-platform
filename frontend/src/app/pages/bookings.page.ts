import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ApiClient, BookingDto } from '../core/api-client';

type Toast = { type: 'success' | 'error'; message: string };

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
  <div class="page">
    <div class="page-head">
      <div class="page-title">
        <h1>Prenotazioni</h1>
        <div class="sub">Area paziente: elenco prenotazioni e annullamento.</div>
      </div>
      <button class="btn" (click)="load()" [disabled]="loading">
        {{ loading ? 'Carico…' : 'Ricarica' }}
      </button>
    </div>

    <div *ngIf="toast" class="alert" [class.success]="toast.type==='success'" [class.error]="toast.type==='error'">
      <div>
        <div style="font-weight:800">{{ toast.type === 'success' ? 'Operazione completata' : 'Attenzione' }}</div>
        <div class="small muted" style="margin-top:2px;">{{ toast.message }}</div>
      </div>
      <button class="btn" (click)="toast=null">Chiudi</button>
    </div>

    <div class="card">
      <ng-container *ngIf="loading; else content">
        <div class="stack">
          <div class="skeleton" style="height:18px; width:40%;"></div>
          <div class="skeleton" style="height:14px; width:85%;"></div>
          <div class="skeleton" style="height:14px; width:76%;"></div>
        </div>
      </ng-container>

      <ng-template #content>
        <div class="table-wrap" *ngIf="items.length; else empty">
          <table class="table">
            <thead>
              <tr>
                <th>Creato</th>
                <th>Slot</th>
                <th>DoctorId</th>
                <th>Prestazione</th>
                <th>Stato</th>
                <th style="width:200px;"></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let b of items; trackBy: trackById">
                <td>{{ b.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                <td>
                  {{ b.slotStartsAt | date:'dd/MM/yyyy HH:mm' }} → {{ b.slotEndsAt | date:'HH:mm' }}
                </td>
                <td>{{ b.slotDoctorId }}</td>
                <td>{{ b.slotPrestazioneName || '-' }}</td>
                <td>
                  <span class="badge" [ngClass]="bookingBadgeClass(b.status)">
                    {{ bookingLabel(b.status) }}
                  </span>
                </td>
                <td>
                  <button class="btn danger"
                    (click)="cancel(b)"
                    [disabled]="cancelingId===b.id || !canCancel(b)">
                    {{ cancelingId===b.id ? 'Annullamento…' : 'Annulla' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <ng-template #empty>
          <div class="empty">
            <div class="empty-title">Nessuna prenotazione</div>
            <div class="empty-sub">Vai su “Slot” per prenotare una visita.</div>
          </div>
        </ng-template>

        <div class="small muted" *ngIf="error" style="margin-top:10px;">{{ error }}</div>
      </ng-template>
    </div>
  </div>
  `
})
export class BookingsPageComponent {
  private api = inject(ApiClient);

  items: BookingDto[] = [];
  loading = false;
  error: string | null = null;

  cancelingId: string | null = null;
  toast: Toast | null = null;

  trackById(_: number, x: BookingDto) { return x.id; }

  async ngOnInit() { await this.load(); }

  async load() {
    this.loading = true;
    this.error = null;
    this.toast = null;

    try {
      this.items = await firstValueFrom(this.api.myBookings());
    } catch {
      this.items = [];
      this.error = 'Errore caricamento prenotazioni';
    } finally {
      this.loading = false;
    }
  }

  canCancel(b: BookingDto) {
    const s = (b.status ?? '').toLowerCase();
    return s === 'confirmed';
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

  async cancel(b: BookingDto) {
    if (!confirm('Confermi l’annullamento della prenotazione?')) return;

    this.cancelingId = b.id;
    this.toast = null;

    try {
      await firstValueFrom(this.api.cancelBooking(b.id));
      await this.load();
      this.toast = { type: 'success', message: 'Prenotazione annullata.' };
    } catch (e: any) {
      const msg = e?.error?.detail || 'Errore annullamento.';
      this.toast = { type: 'error', message: msg };
    } finally {
      this.cancelingId = null;
    }
  }
}