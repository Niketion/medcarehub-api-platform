import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from './config.service';
import { Observable } from 'rxjs';

export interface PrestazioneDto {
  id: string;
  name: string;
  durationMinutes?: number | null;
  description?: string | null;
  createdAt: string;
}

export interface SlotDto {
  id: string;
  doctorId: string;
  prestazioneId?: string | null;
  prestazioneName?: string | null;
  startsAt: string;
  endsAt: string;
  status: string;
}

export interface BookingDto {
  id: string;
  slotId: string;
  slotStartsAt: string;
  slotEndsAt: string;
  slotDoctorId: string;
  slotPrestazioneId?: string | null;
  slotPrestazioneName?: string | null;
  status: string;
  createdAt: string;
}

export interface ReportDto {
  id: string;
  bookingId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;

  reportType?: string | null;
  documentDate?: string | null;
  authorSub?: string | null;
  authorRole?: string | null;
  signedAt?: string | null;

  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  constructor(private http: HttpClient, private cfg: ConfigService) {}

  private get base(): string {
    return this.cfg.required.apiBaseUrl.replace(/\/$/, '');
  }

  // Prestazioni
  getPrestazioni(): Observable<PrestazioneDto[]> {
    return this.http.get<PrestazioneDto[]>(`${this.base}/prestazioni`);
  }

  createPrestazione(req: { name: string; durationMinutes?: number | null; description?: string | null }): Observable<PrestazioneDto> {
    return this.http.post<PrestazioneDto>(`${this.base}/prestazioni`, req);
  }

  // Slots
  getSlots(): Observable<SlotDto[]> {
    return this.http.get<SlotDto[]>(`${this.base}/slots`);
  }

  createSlot(req: { doctorId: string; prestazioneId?: string | null; startsAt: string; endsAt: string; }): Observable<any> {
    return this.http.post(`${this.base}/slots`, req);
  }

  // Bookings
  myBookings(): Observable<BookingDto[]> {
    return this.http.get<BookingDto[]>(`${this.base}/bookings/my`);
  }

  getAllBookings(): Observable<BookingDto[]> {
    return this.http.get<BookingDto[]>(`${this.base}/bookings`);
  }

  createBooking(slotId: string): Observable<BookingDto> {
    return this.http.post<BookingDto>(`${this.base}/bookings`, { slotId });
  }

  cancelBooking(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/bookings/${id}`);
  }

  completeBooking(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/bookings/${id}/complete`, {});
  }

  // Reports
  myReports(): Observable<ReportDto[]> {
    return this.http.get<ReportDto[]>(`${this.base}/reports/my`);
  }

  getAllReports(): Observable<ReportDto[]> {
    return this.http.get<ReportDto[]>(`${this.base}/reports`);
  }

  uploadReport(bookingId: string, file: File, meta?: { reportType?: string; documentDate?: string | null }): Observable<ReportDto> {
    const form = new FormData();
    form.append('bookingId', bookingId);
    if (meta?.reportType?.trim()) form.append('reportType', meta.reportType.trim());
    if (meta?.documentDate?.trim()) form.append('documentDate', meta.documentDate.trim());
    form.append('file', file);
    return this.http.post<ReportDto>(`${this.base}/reports/upload`, form);
  }

  downloadReport(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/reports/${id}/download`, { responseType: 'blob' });
  }
}