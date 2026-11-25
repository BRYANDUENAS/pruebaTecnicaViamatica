import { Component } from '@angular/core';

/** Marco de las pantallas públicas: panel de marca a la izquierda y formulario a la derecha. */
@Component({
  selector: 'app-auth-layout',
  standalone: false,
  template: `
    <div class="auth">
      <section class="hero">
        <div class="brand">
          <mat-icon>wifi</mat-icon>
          <strong>NetCaja</strong>
        </div>

        <div class="pitch">
          <h1>Internet que conecta, atención que resuelve.</h1>
          <p>Gestione turnos, contratos y pagos de sus clientes desde un solo lugar.</p>
          <ul>
            <li><mat-icon>router</mat-icon> Contratación de planes de fibra óptica</li>
            <li><mat-icon>confirmation_number</mat-icon> Turnos por caja en tiempo real</li>
            <li><mat-icon>payments</mat-icon> Pagos y cambios de servicio</li>
          </ul>
        </div>

        <!-- Ondas de señal decorativas -->
        <svg class="signal" viewBox="0 0 400 400" aria-hidden="true">
          <circle cx="400" cy="400" r="90" />
          <circle cx="400" cy="400" r="170" />
          <circle cx="400" cy="400" r="250" />
          <circle cx="400" cy="400" r="330" />
        </svg>
      </section>

      <section class="panel">
        <div class="panel-inner">
          <router-outlet></router-outlet>
        </div>
      </section>
    </div>
  `,
  styleUrl: './auth-layout.component.scss',
})
export class AuthLayoutComponent {}
