import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, BookingDto, ReportDto } from '../core/api-client';
import { AuthService } from '../core/auth.service';

type Toast = { type: 'success' | 'error'; message: string };

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="page">
    <div class="page-head">
      <div class="page-title">
        <h1>Referti</h1>
        <div class="sub">Paziente: lista e download · Staff: upload del referto associato a una prenotazione.</div>
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

    <div class="grid-2" *ngIf="isStaff(); else patientView">
      <!-- upload -->
      <div class="card">
        <h2 style="font-size:18px;">Upload referto (staff)</h2>
        <div class="small muted" style="margin-top:4px;">
          Seleziona la prenotazione e carica un file (PDF consigliato).
        </div>

        <div style="height:12px;"></div>

        <div class="stack">
          <div class="field" style="min-width:auto;">
            <div class="label">Prenotazione</div>
            <select class="input" [(ngModel)]="bookingId" [disabled]="loadingBookings">
              <option value="">— Seleziona —</option>
              <option *ngFor="let b of staffBookings; trackBy: trackById" [value]="b.id">
                {{ b.slotDoctorId }} · {{ b.slotStartsAt | date:'dd/MM HH:mm' }} → {{ b.slotEndsAt | date:'HH:mm' }} · {{ b.status }}
              </option>
            </select>
            <div class="help" *ngIf="loadingBookings">Carico prenotazioni…</div>
          </div>

          <div class="row">
            <div class="field" style="flex:1; min-width:220px;">
              <div class="label">Tipo referto</div>
              <input class="input" [(ngModel)]="reportType" placeholder="es. visita, analisi..." />
            </div>

            <div class="field" style="min-width:220px;">
              <div class="label">Data documento</div>
              <input class="input" type="date" [(ngModel)]="documentDate" />
            </div>
          </div>

          <div class="field" style="min-width:auto;">
            <div class="label">File</div>
            <input class="input" type="file" (change)="onFile($event)" />
            <div class="help" *ngIf="file">Selezionato: <b>{{ file.name }}</b> ({{ formatBytes(file.size) }})</div>
          </div>

          <div class="row" style="justify-content:flex-end;">
            <button class="btn primary" (click)="upload()" [disabled]="uploading">
              {{ uploading ? 'Upload…' : 'Carica referto' }}
            </button>
          </div>

          <div class="small muted" *ngIf="uploadError">{{ uploadError }}</div>
        </div>
      </div>

      <!-- list -->
      <div class="card">
        <h2 style="font-size:18px;">Lista referti</h2>
        <div class="small muted" style="margin-top:4px;">Vista staff: include tutti i referti.</div>

        <div style="height:12px;"></div>

        <ng-container *ngIf="loading; else staffTable">
          <div class="stack">
            <div class="skeleton" style="height:18px; width:40%;"></div>
            <div class="skeleton" style="height:14px; width:85%;"></div>
            <div class="skeleton" style="height:14px; width:76%;"></div>
          </div>
        </ng-container>

        <ng-template #staffTable>
          <div class="table-wrap" *ngIf="items.length; else empty">
            <table class="table">
              <thead>
                <tr>
                  <th>Creato</th>
                  <th>BookingId</th>
                  <th>Tipo</th>
                  <th>Data doc</th>
                  <th>File</th>
                  <th>Size</th>
                  <th style="width:200px;"></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let r of items; trackBy: trackById">
                  <td>{{ r.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td class="small">{{ r.bookingId }}</td>
                  <td>{{ r.reportType || '-' }}</td>
                  <td>{{ r.documentDate ? (r.documentDate | date:'dd/MM/yyyy') : '-' }}</td>
                  <td>
                    <div style="font-weight:700">{{ r.fileName }}</div>
                    <div class="small muted">{{ r.contentType }}</div>
                    <div class="small muted" *ngIf="r.authorRole">Autore: {{ r.authorRole }} · Firma: {{ r.signedAt ? 'Sì' : 'No' }}</div>
                  </td>
                  <td>{{ formatBytes(r.sizeBytes) }}</td>
                  <td>
                    <button class="btn" (click)="download(r)" [disabled]="downloadingId===r.id">
                      {{ downloadingId===r.id ? 'Scarico…' : 'Download' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <ng-template #empty>
            <div class="empty">
              <div class="empty-title">Nessun referto</div>
              <div class="empty-sub">Carica il primo referto dalla colonna a sinistra.</div>
            </div>
          </ng-template>

          <div class="small muted" *ngIf="error" style="margin-top:10px;">{{ error }}</div>
        </ng-template>
      </div>
    </div>

    <ng-template #patientView>
      <div class="card">
        <div class="page-head">
          <div class="page-title">
            <h2 style="font-size:18px;">I miei referti</h2>
            <div class="sub">Scarica i documenti associati alle tue prenotazioni.</div>
          </div>
        </div>

        <div style="height:12px;"></div>

        <ng-container *ngIf="loading; else patientTable">
          <div class="stack">
            <div class="skeleton" style="height:18px; width:40%;"></div>
            <div class="skeleton" style="height:14px; width:85%;"></div>
            <div class="skeleton" style="height:14px; width:76%;"></div>
          </div>
        </ng-container>

        <ng-template #patientTable>
          <div class="table-wrap" *ngIf="items.length; else emptyPatient">
            <table class="table">
              <thead>
                <tr>
                  <th>Creato</th>
                  <th>Tipo</th>
                  <th>File</th>
                  <th>Size</th>
                  <th style="width:200px;"></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let r of items; trackBy: trackById">
                  <td>{{ r.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td>{{ r.reportType || '-' }}</td>
                  <td>
                    <div style="font-weight:700">{{ r.fileName }}</div>
                    <div class="small muted">{{ r.contentType }}</div>
                  </td>
                  <td>{{ formatBytes(r.sizeBytes) }}</td>
                  <td>
                    <button class="btn" (click)="download(r)" [disabled]="downloadingId===r.id">
                      {{ downloadingId===r.id ? 'Scarico…' : 'Download' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <ng-template #emptyPatient>
            <div class="empty">
              <div class="empty-title">Nessun referto disponibile</div>
              <div class="empty-sub">Quando lo staff carica un documento, comparirà qui.</div>
            </div>
          </ng-template>

          <div class="small muted" *ngIf="error" style="margin-top:10px;">{{ error }}</div>
        </ng-template>
      </div>
    </ng-template>
  </div>
  `
})
export class ReportsPageComponent {
  private api = inject(ApiClient);
  private auth = inject(AuthService);

  items: ReportDto[] = [];
  loading = false;
  error: string | null = null;

  bookingId = '';
  reportType = '';
  documentDate = ''; // yyyy-mm-dd
  file?: File;

  uploading = false;
  uploadError: string | null = null;

  downloadingId: string | null = null;

  staffBookings: BookingDto[] = [];
  loadingBookings = false;

  toast: Toast | null = null;

  trackById(_: number, x: any) { return x.id; }

  isStaff() {
    return this.auth.hasRole('operator') || this.auth.hasRole('doctor') || this.auth.hasRole('admin');
  }

  async ngOnInit() {
    await this.load();
    if (this.isStaff()) await this.loadBookings();
  }

  async load() {
    this.loading = true;
    this.error = null;
    try {
      this.items = this.isStaff()
        ? await firstValueFrom(this.api.getAllReports())
        : await firstValueFrom(this.api.myReports());
    } catch {
      this.items = [];
      this.error = 'Errore caricamento referti (permessi o backend).';
    } finally {
      this.loading = false;
    }
  }

  onFile(ev: Event) {
    const input = ev.target as HTMLInputElement;
    this.file = input.files?.[0] ?? undefined;
  }

  private docDateToIsoOrNull(d: string): string | null {
    const s = (d ?? '').trim();
    if (!s) return null;
    const iso = new Date(s + 'T00:00:00Z');
    if (Number.isNaN(iso.getTime())) return null;
    return iso.toISOString();
  }

  async upload() {
    this.uploading = true;
    this.uploadError = null;
    this.toast = null;

    if (!this.bookingId || !this.file) {
      this.uploadError = 'Prenotazione e file sono obbligatori.';
      this.uploading = false;
      return;
    }

    try {
      await firstValueFrom(this.api.uploadReport(this.bookingId, this.file, {
        reportType: this.reportType || undefined,
        documentDate: this.docDateToIsoOrNull(this.documentDate)
      }));
      this.bookingId = '';
      this.reportType = '';
      this.documentDate = '';
      this.file = undefined;

      await this.load();
      this.toast = { type: 'success', message: 'Upload completato.' };
    } catch (e: any) {
      this.uploadError = e?.error?.detail || 'Errore upload (bookingId errato, permessi o backend).';
      this.toast = { type: 'error', message: 'Impossibile completare l’upload.' };
    } finally {
      this.uploading = false;
    }
  }

  async download(r: ReportDto) {
    this.downloadingId = r.id;
    this.toast = null;

    try {
      const blob = await firstValueFrom(this.api.downloadReport(r.id));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = r.fileName || 'referto';
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      this.toast = { type: 'error', message: 'Errore download.' };
    } finally {
      this.downloadingId = null;
    }
  }

  async loadBookings() {
    this.loadingBookings = true;
    try {
      this.staffBookings = await firstValueFrom(this.api.getAllBookings());
    } catch {
      this.staffBookings = [];
    } finally {
      this.loadingBookings = false;
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