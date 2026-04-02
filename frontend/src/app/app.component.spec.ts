import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { AuthService } from './core/auth.service';

describe('AppComponent', () => {
  const authMock = {
    isAuthenticated: jest.fn(),
    profile: jest.fn(),
    roles: jest.fn(),
    hasRole: jest.fn(),
    login: jest.fn(),
    logout: jest.fn()
  };

  beforeEach(async () => {
    jest.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock }
      ]
    }).compileComponents();
  });

  it('shows login button when user is not authenticated', () => {
    authMock.isAuthenticated.mockReturnValue(false);
    authMock.profile.mockReturnValue(null);
    authMock.roles.mockReturnValue([]);
    authMock.hasRole.mockReturnValue(false);

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Login');
    expect(el.textContent).not.toContain('Logout');
  });

  it('shows username and logout when user is authenticated', () => {
    authMock.isAuthenticated.mockReturnValue(true);
    authMock.profile.mockReturnValue({ preferred_username: 'patient1', email: 'patient1@example.local' });
    authMock.roles.mockReturnValue(['patient']);
    authMock.hasRole.mockImplementation((role: string) => role === 'patient');

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('patient1');
    expect(el.textContent).toContain('Logout');
    expect(el.textContent).toContain('Prenotazioni');
  });

  it('calls auth.login when login() is invoked', () => {
    authMock.isAuthenticated.mockReturnValue(false);
    authMock.profile.mockReturnValue(null);
    authMock.roles.mockReturnValue([]);
    authMock.hasRole.mockReturnValue(false);

    const fixture = TestBed.createComponent(AppComponent);
    const component = fixture.componentInstance;

    component.login();

    expect(authMock.login).toHaveBeenCalledTimes(1);
  });

  it('calls auth.logout when logout() is invoked', () => {
    authMock.isAuthenticated.mockReturnValue(true);
    authMock.profile.mockReturnValue({ preferred_username: 'patient1' });
    authMock.roles.mockReturnValue(['patient']);
    authMock.hasRole.mockReturnValue(true);

    const fixture = TestBed.createComponent(AppComponent);
    const component = fixture.componentInstance;

    component.logout();

    expect(authMock.logout).toHaveBeenCalledTimes(1);
  });
});