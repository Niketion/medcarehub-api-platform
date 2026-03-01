import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const required = (route.data['roles'] as string[] | undefined) ?? [];
    if (!required.length) return true;

    const ok = required.some(r => this.auth.hasRole(r));
    if (ok) return true;

    this.router.navigateByUrl('/');
    return false;
  }
}
