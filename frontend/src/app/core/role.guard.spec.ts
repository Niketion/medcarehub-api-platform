import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { RoleGuard } from './role.guard';
import { AuthService } from './auth.service';

describe('RoleGuard', () => {
  function routeWithRoles(roles?: string[]): ActivatedRouteSnapshot {
    return {
      data: roles ? { roles } : {}
    } as ActivatedRouteSnapshot;
  }

  it('returns true when no roles are required', () => {
    const auth = {
      hasRole: jest.fn()
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jest.fn()
    } as unknown as Router;

    const guard = new RoleGuard(auth, router);

    expect(guard.canActivate(routeWithRoles())).toBe(true);
    expect(auth.hasRole).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('returns true when at least one required role is present', () => {
    const auth = {
      hasRole: jest.fn((r: string) => r === 'patient')
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jest.fn()
    } as unknown as Router;

    const guard = new RoleGuard(auth, router);

    expect(guard.canActivate(routeWithRoles(['patient', 'operator']))).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('redirects to root and returns false when role is missing', () => {
    const auth = {
      hasRole: jest.fn(() => false)
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jest.fn()
    } as unknown as Router;

    const guard = new RoleGuard(auth, router);

    expect(guard.canActivate(routeWithRoles(['patient']))).toBe(false);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });
});