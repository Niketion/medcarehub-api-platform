import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ConfigService } from './config.service';
import { Observable } from 'rxjs';

export interface PrestazioneDto {
  id: string;
  name: string;
  durationMinutes?: number | null;
  description?: string | null;
  basePrice: number;
  createdAt: string;
}

export interface SlotDto {
  id: string;
  doctorId: string;
  prestazioneId?: string | null;
  prestazioneName?: string | null;
  prestazioneBasePrice?: number | null;
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
  bookedPrice: number;
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

export interface DoctorEconomicsDto {
  doctorId: string;
  confirmedBookings: number;
  completedBookings: number;
  estimatedRevenue: number;
  realizedRevenue: number;
}

export interface PrestazioneEconomicsDto {
  prestazioneId?: string | null;
  prestazioneName: string;
  confirmedBookings: number;
  completedBookings: number;
  estimatedRevenue: number;
  realizedRevenue: number;
}

export interface DashboardEconomicsDto {
  estimatedRevenue: number;
  realizedRevenue: number;
  averageTicket: number;
  confirmedBookings: number;
  completedBookings: number;
  byDoctor: DoctorEconomicsDto[];
  byPrestazione: PrestazioneEconomicsDto[];
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  constructor(private http: HttpClient, private cfg: ConfigService) {}

  private get base(): string {
    return this.cfg.required.apiBaseUrl.replace(/\/$/, '');
  }

  getPrestazioni(): Observable<PrestazioneDto[]> {
    return this.http.get<PrestazioneDto[]>(`${this.base}/prestazioni`);
  }

  createPrestazione(req: {
    name: string;
    durationMinutes?: number | null;
    description?: string | null;
    basePrice: number;
  }): Observable<PrestazioneDto> {
    return this.http.post<PrestazioneDto>(`${this.base}/prestazioni`, req);
  }

  getSlots(): Observable<SlotDto[]> {
    return this.http.get<SlotDto[]>(`${this.base}/slots`);
  }

  createSlot(req: {
    doctorId: string;
    prestazioneId?: string | null;
    startsAt: string;
    endsAt: string;
  }): Observable<any> {
    return this.http.post(`${this.base}/slots`, req);
  }

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

  getEconomics(from?: string | null, to?: string | null, doctorId?: string | null): Observable<DashboardEconomicsDto> {
    let params = new HttpParams();

    if (from?.trim()) params = params.set('from', from.trim());
    if (to?.trim()) params = params.set('to', to.trim());
    if (doctorId?.trim()) params = params.set('doctorId', doctorId.trim());

    return this.http.get<DashboardEconomicsDto>(`${this.base}/analytics/economics`, { params });
  }
}