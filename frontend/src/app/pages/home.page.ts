import { Component, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  imports: [NgIf, NgFor, RouterLink],
  template: `
  <div class="page">
    <div class="card">
      <div class="page-head">
        <div class="page-title">
          <h1>Portale MedCare Hub</h1>
          <div class="sub">Accesso sicuro (Keycloak) e gestione end-to-end: slot → prenotazioni → referti.</div>
        </div>

        <div class="row" style="justify-content:flex-end;">
          <button class="btn primary" *ngIf="!auth.isAuthenticated()" (click)="auth.login()">Login con Keycloak</button>
          <button class="btn" *ngIf="auth.isAuthenticated()" (click)="auth.logout()">Logout</button>
        </div>
      </div>

      <div style="height:14px;"></div>

      <div class="grid-2">
        <div class="panel">
          <h3>Azioni rapide</h3>
          <div style="height:10px;"></div>

          <div class="stack">
            <a class="btn" routerLink="/slots">Vai agli slot</a>
            <a class="btn" routerLink="/bookings" *ngIf="auth.hasRole('patient')">Le mie prenotazioni</a>
            <a class="btn" routerLink="/reports">Referti</a>
          </div>

          <div style="height:10px;"></div>
          <div class="help">
            Suggerimento: per la demo usa <b>patient1</b> (paziente) oppure <b>operator1</b> (staff).
          </div>
        </div>

        <div class="panel">
          <h3>Stato sessione</h3>
          <div style="height:10px;"></div>

          <ng-container *ngIf="auth.isAuthenticated(); else notLogged">
            <div class="small muted">Utente</div>
            <div style="font-weight:800; margin-top:4px;">
              {{ auth.profile()?.preferred_username ?? auth.profile()?.email }}
            </div>

            <div style="height:10px;"></div>
            <div class="small muted">Ruoli</div>
            <div class="row" style="margin-top:6px;">
              <span class="badge" *ngFor="let r of auth.roles()">{{ r }}</span>
            </div>

            <div style="height:10px;"></div>
            <div class="help">
              Le pagine si adattano al ruolo: il paziente prenota e scarica, lo staff crea slot e carica referti.
            </div>
          </ng-container>

          <ng-template #notLogged>
            <div class="empty">
              <div class="empty-title">Non sei autenticato</div>
              <div class="empty-sub">Esegui il login per usare le funzionalità del portale.</div>
            </div>
          </ng-template>
        </div>
      </div>
    </div>
  </div>
  `
})
export class HomePageComponent {
  auth = inject(AuthService);
}