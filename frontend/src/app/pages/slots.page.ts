import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, PrestazioneDto, SlotDto } from '../core/api-client';
import { AuthService } from '../core/auth.service';

type Toast = { type: 'success' | 'error'; message: string };

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="page">
    <div class="page-head">
      <div class="page-title">
        <h1>Slot</h1>
        <div class="sub">Consulta disponibilità e, se sei paziente, prenota uno slot disponibile.</div>
      </div>

      <div class="row" style="justify-content:flex-end;">
        <button class="btn" (click)="load()" [disabled]="loading">
          {{ loading ? 'Carico…' : 'Ricarica' }}
        </button>
      </div>
    </div>

    <div *ngIf="toast" class="alert" [class.success]="toast.type==='success'" [class.error]="toast.type==='error'">
      <div>
        <div style="font-weight:800">{{ toast.type === 'success' ? 'Operazione completata' : 'Attenzione' }}</div>
        <div class="small muted" style="margin-top:2px;">{{ toast.message }}</div>
      </div>
      <button class="btn" (click)="toast=null">Chiudi</button>
    </div>

    <!-- STAFF: prestazioni -->
    <div class="card" *ngIf="isStaff()">
      <div class="page-head">
        <div class="page-title">
          <h2 style="font-size:18px;">Prestazioni (staff)</h2>
          <div class="sub">Catalogo minimo per associare una tipologia allo slot.</div>
        </div>
        <button class="btn" (click)="loadPrestazioni()" [disabled]="loadingPrestazioni">
          {{ loadingPrestazioni ? 'Carico…' : 'Ricarica' }}
        </button>
      </div>

      <div style="height:12px;"></div>

      <div class="row">
        <div class="field" style="min-width:260px;">
          <div class="label">Nome</div>
          <input class="input" [(ngModel)]="newPrestName" placeholder="es. Visita cardiologica" />
        </div>

        <div class="field" style="min-width:180px;">
          <div class="label">Durata (min)</div>
          <input class="input" type="number" [(ngModel)]="newPrestDuration" placeholder="30" />
        </div>

        <div class="field" style="flex:1; min-width:260px;">
          <div class="label">Descrizione</div>
          <input class="input" [(ngModel)]="newPrestDescription" placeholder="opzionale" />
        </div>

        <div style="align-self:end;">
          <button class="btn primary" (click)="createPrestazione()" [disabled]="creatingPrestazione">
            {{ creatingPrestazione ? 'Creo…' : 'Crea prestazione' }}
          </button>
        </div>
      </div>

      <div class="small muted" *ngIf="prestazioniError" style="margin-top:10px;">{{ prestazioniError }}</div>
    </div>

    <!-- STAFF: crea slot -->
    <div class="card" *ngIf="isStaff()">
      <div class="page-head">
        <div class="page-title">
          <h2 style="font-size:18px;">Crea slot (staff)</h2>
          <div class="sub">Inserisci medico e orari (locali) + prestazione (opzionale).</div>
        </div>
      </div>

      <div style="height:12px;"></div>

      <div class="row">
        <div class="field">
          <div class="label">DoctorId</div>
          <input class="input" [(ngModel)]="doctorId" placeholder="doctor-sub" />
        </div>

        <div class="field" style="min-width:260px;">
          <div class="label">Prestazione</div>
          <select class="input" [(ngModel)]="prestazioneId">
            <option value="">— Nessuna —</option>
            <option *ngFor="let p of prestazioni; trackBy: trackById" [value]="p.id">
              {{ p.name }} <span *ngIf="p.durationMinutes">({{ p.durationMinutes }}m)</span>
            </option>
          </select>
        </div>

        <div class="field">
          <div class="label">Inizio</div>
          <input class="input" type="datetime-local" [(ngModel)]="startsAtLocal" />
        </div>

        <div class="field">
          <div class="label">Fine</div>
          <input class="input" type="datetime-local" [(ngModel)]="endsAtLocal" />
        </div>

        <div style="align-self:end;">
          <button class="btn primary" (click)="create()" [disabled]="creating">
            {{ creating ? 'Creo…' : 'Crea slot' }}
          </button>
        </div>
      </div>

      <div class="small muted" *ngIf="createError" style="margin-top:10px;">{{ createError }}</div>
    </div>

    <!-- Filtri + tabella -->
    <div class="card">
      <div class="row" style="justify-content:space-between; align-items:flex-end;">
        <div class="row" style="align-items:flex-end;">
          <div class="field" style="min-width:260px; flex:0;">
            <div class="label">Filtra per DoctorId</div>
            <input class="input" [(ngModel)]="filterDoctorId" (input)="applyFilters()" placeholder="es. dr-rossi" />
          </div>

          <div class="field" style="min-width:220px; flex:0;">
            <div class="label">Stato</div>
            <select class="input" [(ngModel)]="filterStatus" (change)="applyFilters()">
              <option value="">Tutti</option>
              <option value="available">Disponibili</option>
              <option value="booked">Prenotati</option>
              <option value="cancelled">Cancellati</option>
            </select>
          </div>
        </div>

        <div class="small muted">
          {{ loading ? 'Caricamento in corso…' : (visibleSlots.length + ' risultati') }}
        </div>
      </div>

      <div style="height:12px;"></div>

      <ng-container *ngIf="loading; else tableOrEmpty">
        <div class="stack">
          <div class="skeleton" style="height:18px; width:55%;"></div>
          <div class="skeleton" style="height:14px; width:85%;"></div>
          <div class="skeleton" style="height:14px; width:76%;"></div>
        </div>
      </ng-container>

      <ng-template #tableOrEmpty>
        <div class="table-wrap" *ngIf="visibleSlots.length; else empty">
          <table class="table">
            <thead>
              <tr>
                <th>Inizio</th>
                <th>Fine</th>
                <th>DoctorId</th>
                <th>Prestazione</th>
                <th>Stato</th>
                <th style="width:200px;"></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of visibleSlots; trackBy: trackById">
                <td>{{ s.startsAt | date:'dd/MM/yyyy HH:mm' }}</td>
                <td>{{ s.endsAt   | date:'dd/MM/yyyy HH:mm' }}</td>
                <td>{{ s.doctorId }}</td>
                <td>{{ s.prestazioneName || '-' }}</td>
                <td>
                  <span class="badge" [ngClass]="statusBadgeClass(s.status)">
                    {{ statusLabel(s.status) }}
                  </span>
                </td>
                <td>
                  <button class="btn primary"
                    *ngIf="auth.hasRole('patient')"
                    (click)="book(s)"
                    [disabled]="bookingSlotId===s.id || !canBook(s)">
                    {{ bookingSlotId===s.id ? 'Prenoto…' : 'Prenota' }}
                  </button>
                  <span class="small muted" *ngIf="auth.hasRole('patient') && !canBook(s)">
                    Non disponibile
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <ng-template #empty>
          <div class="empty">
            <div class="empty-title">Nessuno slot trovato</div>
            <div class="empty-sub">Prova a cambiare filtri oppure ricarica l’elenco.</div>
          </div>
        </ng-template>

        <div class="small muted" *ngIf="error" style="margin-top:10px;">{{ error }}</div>
      </ng-template>
    </div>
  </div>
  `
})
export class SlotsPageComponent {
  private api = inject(ApiClient);
  auth = inject(AuthService);

  slots: SlotDto[] = [];
  visibleSlots: SlotDto[] = [];

  prestazioni: PrestazioneDto[] = [];
  loadingPrestazioni = false;
  prestazioniError: string | null = null;

  newPrestName = '';
  newPrestDuration: number | null = null;
  newPrestDescription = '';
  creatingPrestazione = false;

  loading = false;
  error: string | null = null;

  // staff create slot
  doctorId = '';
  prestazioneId = '';
  startsAtLocal = '';
  endsAtLocal = '';
  creating = false;
  createError: string | null = null;

  // filters
  filterDoctorId = '';
  filterStatus: '' | 'available' | 'booked' | 'cancelled' = '';

  bookingSlotId: string | null = null;
  toast: Toast | null = null;

  isStaff() {
    return this.auth.hasRole('operator') || this.auth.hasRole('doctor') || this.auth.hasRole('admin');
  }

  trackById(_: number, x: any) { return x.id; }

  async ngOnInit() {
    if (this.isStaff()) await this.loadPrestazioni();
    await this.load();
  }

  async loadPrestazioni() {
    this.loadingPrestazioni = true;
    this.prestazioniError = null;
    try {
      this.prestazioni = await firstValueFrom(this.api.getPrestazioni());
    } catch {
      this.prestazioni = [];
      this.prestazioniError = 'Errore caricamento prestazioni';
    } finally {
      this.loadingPrestazioni = false;
    }
  }

  async createPrestazione() {
    this.creatingPrestazione = true;
    this.prestazioniError = null;
    this.toast = null;

    const name = this.newPrestName.trim();
    if (!name) {
      this.prestazioniError = 'Nome prestazione obbligatorio.';
      this.creatingPrestazione = false;
      return;
    }

    try {
      await firstValueFrom(this.api.createPrestazione({
        name,
        durationMinutes: this.newPrestDuration ?? null,
        description: this.newPrestDescription?.trim() || null
      }));
      this.newPrestName = '';
      this.newPrestDuration = null;
      this.newPrestDescription = '';
      await this.loadPrestazioni();
      this.toast = { type: 'success', message: 'Prestazione creata.' };
    } catch {
      this.toast = { type: 'error', message: 'Errore creazione prestazione (nome duplicato o permessi).' };
    } finally {
      this.creatingPrestazione = false;
    }
  }

  async load() {
    this.loading = true;
    this.error = null;
    try {
      this.slots = await firstValueFrom(this.api.getSlots());
      this.applyFilters();
    } catch (e: any) {
      this.error = e?.message ?? 'Errore caricamento slot';
      this.slots = [];
      this.applyFilters();
    } finally {
      this.loading = false;
    }
  }

  applyFilters() {
    const qDoctor = this.filterDoctorId.trim().toLowerCase();
    const qStatus = this.filterStatus;

    this.visibleSlots = this.slots.filter(s => {
      const okDoctor = !qDoctor || (s.doctorId ?? '').toLowerCase().includes(qDoctor);
      const okStatus = !qStatus || (s.status ?? '').toLowerCase() === qStatus;
      return okDoctor && okStatus;
    });
  }

  private toIsoOrNull(local: string): string | null {
    if (!local?.trim()) return null;
    const d = new Date(local);
    if (Number.isNaN(d.getTime())) return null;
    return d.toISOString();
  }

  async create() {
    this.creating = true;
    this.createError = null;

    const startsAt = this.toIsoOrNull(this.startsAtLocal);
    const endsAt = this.toIsoOrNull(this.endsAtLocal);

    if (!this.doctorId.trim() || !startsAt || !endsAt) {
      this.createError = 'DoctorId, Inizio e Fine sono obbligatori.';
      this.creating = false;
      return;
    }

    try {
      await firstValueFrom(this.api.createSlot({
        doctorId: this.doctorId.trim(),
        prestazioneId: this.prestazioneId || null,
        startsAt,
        endsAt
      }));
      this.doctorId = '';
      this.prestazioneId = '';
      this.startsAtLocal = '';
      this.endsAtLocal = '';
      await this.load();
      this.toast = { type: 'success', message: 'Slot creato correttamente.' };
    } catch {
      this.createError = 'Errore creazione slot (controlla date, prestazione e permessi).';
      this.toast = { type: 'error', message: 'Impossibile creare lo slot.' };
    } finally {
      this.creating = false;
    }
  }

  canBook(s: SlotDto): boolean {
    return (s.status ?? '').toLowerCase() === 'available';
  }

  async book(slot: SlotDto) {
    this.bookingSlotId = slot.id;
    this.toast = null;
    try {
      await firstValueFrom(this.api.createBooking(slot.id));
      await this.load();
      this.toast = { type: 'success', message: 'Prenotazione creata.' };
    } catch (e: any) {
      const msg =
        e?.error?.detail ||
        e?.error?.title ||
        'Errore prenotazione (slot non disponibile o permessi).';
      this.toast = { type: 'error', message: msg };
    } finally {
      this.bookingSlotId = null;
    }
  }

  statusLabel(status: string) {
    const s = (status ?? '').toLowerCase();
    if (s === 'available') return 'Disponibile';
    if (s === 'booked') return 'Prenotato';
    if (s === 'cancelled') return 'Cancellato';
    return status || '-';
  }

  statusBadgeClass(status: string) {
    const s = (status ?? '').toLowerCase();
    if (s === 'available') return 'success';
    if (s === 'booked') return 'warning';
    if (s === 'cancelled') return 'danger';
    return '';
  }
}