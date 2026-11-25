import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { NotifyService } from '../../core/ui.services';

@Component({
  selector: 'app-login',
  standalone: false,
  template: `
    <h2>Iniciar sesión</h2>
    <p class="muted">Ingrese con su usuario y contraseña.</p>

    <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
      <mat-form-field>
        <mat-label>Usuario</mat-label>
        <input matInput formControlName="username" autocomplete="username" maxlength="20" />
        <mat-icon matPrefix>person</mat-icon>
        <mat-error *ngIf="form.controls.username.hasError('required')">Ingrese su usuario.</mat-error>
        <mat-error *ngIf="form.controls.username.hasError('pattern')">
          Entre 8 y 20 caracteres, solo letras y números.
        </mat-error>
      </mat-form-field>

      <mat-form-field>
        <mat-label>Contraseña</mat-label>
        <input matInput formControlName="password" [type]="hide ? 'password' : 'text'" autocomplete="current-password" maxlength="30" />
        <mat-icon matPrefix>lock</mat-icon>
        <button mat-icon-button matSuffix type="button" (click)="hide = !hide" [attr.aria-label]="hide ? 'Mostrar contraseña' : 'Ocultar contraseña'">
          <mat-icon>{{ hide ? 'visibility' : 'visibility_off' }}</mat-icon>
        </button>
        <mat-error *ngIf="form.controls.password.hasError('required')">Ingrese su contraseña.</mat-error>
        <mat-error *ngIf="form.controls.password.hasError('minlength')">Mínimo 8 caracteres.</mat-error>
      </mat-form-field>

      <button mat-flat-button type="submit" class="submit" [disabled]="form.invalid || sending">
        {{ sending ? 'Ingresando…' : 'Ingresar' }}
      </button>
    </form>

    <a routerLink="recuperar" class="link">¿Olvidó su contraseña?</a>
  `,
  styleUrl: './auth-forms.scss',
})
export class LoginComponent {
  hide = true;
  sending = false;

  readonly form;

  constructor(fb: FormBuilder, private auth: AuthService, private router: Router, private notify: NotifyService) {
    // Se valida antes de consumir el servicio: sin datos válidos no hay petición.
    this.form = fb.nonNullable.group({
      username: ['', [Validators.required, Validators.pattern(/^[A-Za-z\d]{8,20}$/)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(30)]],
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.sending = true;
    const { username, password } = this.form.getRawValue();
    this.auth.login(username.trim(), password).subscribe({
      next: (response) => {
        this.notify.success(`Bienvenido, ${response.user.firstName}.`);
        this.router.navigate(['/inicio']);
      },
      error: () => (this.sending = false),
    });
  }
}
