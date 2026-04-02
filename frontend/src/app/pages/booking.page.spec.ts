import { TestBed } from '@angular/core/testing';
import { BookingsPageComponent } from './bookings.page';
import { ApiClient } from '../core/api-client';
import { of } from 'rxjs';

describe('BookingsPageComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookingsPageComponent],
      providers: [
        {
          provide: ApiClient,
          useValue: {
            myBookings: jest.fn(() => of([])),
            cancelBooking: jest.fn(() => of(void 0))
          }
        }
      ]
    }).compileComponents();
  });

  it('allows cancel only for confirmed bookings', () => {
    const fixture = TestBed.createComponent(BookingsPageComponent);
    const component = fixture.componentInstance;

    expect(component.canCancel({ status: 'confirmed' } as any)).toBe(true);
    expect(component.canCancel({ status: 'cancelled' } as any)).toBe(false);
    expect(component.canCancel({ status: 'completed' } as any)).toBe(false);
  });

  it('maps booking labels correctly', () => {
    const fixture = TestBed.createComponent(BookingsPageComponent);
    const component = fixture.componentInstance;

    expect(component.bookingLabel('confirmed')).toBe('Confermata');
    expect(component.bookingLabel('cancelled')).toBe('Annullata');
    expect(component.bookingLabel('completed')).toBe('Completata');
    expect(component.bookingLabel('other')).toBe('other');
  });

  it('maps payment labels correctly', () => {
    const fixture = TestBed.createComponent(BookingsPageComponent);
    const component = fixture.componentInstance;

    expect(component.paymentLabel('paid')).toBe('Pagata');
    expect(component.paymentLabel('unpaid')).toBe('Da pagare');
    expect(component.paymentLabel(undefined)).toBe('Da pagare');
  });

  it('formats euro values', () => {
    const fixture = TestBed.createComponent(BookingsPageComponent);
    const component = fixture.componentInstance;

    expect(component.formatEuro(12)).toBe('€ 12.00');
    expect(component.formatEuro(12.5)).toBe('€ 12.50');
    expect(component.formatEuro(undefined)).toBe('€ 0.00');
  });
});