import { AuthGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('AuthGuard', () => {
  it('returns true when user is authenticated', () => {
    const auth = {
      isAuthenticated: jest.fn(() => true),
      login: jest.fn()
    } as unknown as AuthService;

    const guard = new AuthGuard(auth);

    expect(guard.canActivate()).toBe(true);
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('calls login and returns false when user is not authenticated', () => {
    const auth = {
      isAuthenticated: jest.fn(() => false),
      login: jest.fn()
    } as unknown as AuthService;

    const guard = new AuthGuard(auth);

    expect(guard.canActivate()).toBe(false);
    expect(auth.login).toHaveBeenCalledTimes(1);
  });
});