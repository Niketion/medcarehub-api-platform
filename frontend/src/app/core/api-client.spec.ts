import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ApiClient } from './api-client';
import { ConfigService } from './config.service';
import { AppConfig } from './app-config';

class MockConfigService {
  get required(): AppConfig {
    return {
      apiBaseUrl: '/api',
      keycloak: {
        url: 'http://localhost:8081',
        realm: 'medcarehub',
        clientId: 'medcarehub-web'
      }
    };
  }
}

describe('ApiClient', () => {
  let service: ApiClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ApiClient,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ConfigService, useClass: MockConfigService }
      ]
    });

    service = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls GET /api/slots', () => {
    service.getSlots().subscribe();

    const req = httpMock.expectOne('/api/slots');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('calls POST /api/bookings with slotId payload', () => {
    service.createBooking('slot-123').subscribe();

    const req = httpMock.expectOne('/api/bookings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ slotId: 'slot-123' });
    req.flush({
      id: 'b1',
      slotId: 'slot-123',
      slotStartsAt: '2026-04-02T10:00:00Z',
      slotEndsAt: '2026-04-02T10:30:00Z',
      slotDoctorId: 'doctor-1',
      bookedPrice: 50,
      status: 'confirmed',
      paymentStatus: 'unpaid',
      createdAt: '2026-04-02T09:00:00Z'
    });
  });

  it('calls GET /api/analytics/economics with query params', () => {
    service.getEconomics('2026-04-01', '2026-04-30', 'doctor-1').subscribe();

    const req = httpMock.expectOne(r =>
      r.url === '/api/analytics/economics' &&
      r.params.get('from') === '2026-04-01' &&
      r.params.get('to') === '2026-04-30' &&
      r.params.get('doctorId') === 'doctor-1'
    );

    expect(req.request.method).toBe('GET');
    req.flush({
      estimatedRevenue: 0,
      realizedRevenue: 0,
      paidRevenue: 0,
      averageTicket: 0,
      confirmedBookings: 0,
      completedBookings: 0,
      paidBookings: 0,
      byDoctor: [],
      byPrestazione: [],
      revenueTrend: []
    });
  });

  it('builds multipart form for report upload', () => {
    const file = new File(['pdf'], 'report.pdf', { type: 'application/pdf' });

    service.uploadReport('booking-1', file, {
      reportType: 'Dimissione',
      documentDate: '2026-04-02'
    }).subscribe();

    const req = httpMock.expectOne('/api/reports/upload');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);

    const form = req.request.body as FormData;
    expect(form.get('bookingId')).toBe('booking-1');
    expect(form.get('reportType')).toBe('Dimissione');
    expect(form.get('documentDate')).toBe('2026-04-02');
    expect((form.get('file') as File).name).toBe('report.pdf');

    req.flush({
      id: 'r1',
      bookingId: 'booking-1',
      fileName: 'report.pdf',
      contentType: 'application/pdf',
      sizeBytes: 3,
      createdAt: '2026-04-02T09:00:00Z'
    });
  });
});