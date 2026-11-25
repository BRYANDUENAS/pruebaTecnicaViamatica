import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { NotifyService } from '../../core/ui.services';

function samePassword(group: AbstractControl): ValidationErrors | null {
  return group.get('newPassword')?.value === group.get('confirm')?.value ? null : { mismatch: true };
}

/** Recuperación de contraseña: el usuario valida su identidad y define una nueva. */
@Component({
  selector: 'app-recover',
  standalone: false,
  template: `
    <h2>Recuperar contraseña</h2>

    <!-- Por correo: disponible si el servidor tiene configurado el envío. -->
    <ng-container *ngIf="mode === 'email'">
      <p class="muted">Le enviaremos una contraseña temporal a su correo registrado.</p>
      <form [formGroup]="emailForm" (ngSubmit)="sendEmail()" class="auth-form">
        <mat-form-field>
          <mat-label>Usuario</mat-label>
          <input matInput formControlName="username" maxlength="20" />
          <mat-error>Ingrese su usuario.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Correo registrado</mat-label>
          <input matInput formControlName="email" type="email" />
          <mat-icon matPrefix>mail</mat-icon>
          <mat-error>Ingrese un correo válido.</mat-error>
        </mat-form-field>
        <button mat-flat-button type="submit" class="submit" [disabled]="emailForm.invalid || sending">
          Enviar contraseña temporal
        </button>
      </form>
      <a class="link" href="#" (click)="setMode('identity', $event)">No tengo acceso a mi correo</a><br />
    </ng-container>

    <ng-container *ngIf="mode === 'identity'">
    <p class="muted">Confirme sus datos para definir una nueva contraseña.</p>

    <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
      <mat-form-field>
        <mat-label>Usuario</mat-label>
        <input matInput formControlName="username" maxlength="20" />
        <mat-error>Ingrese su usuario.</mat-error>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Correo registrado</mat-label>
        <input matInput formControlName="email" type="email" />
        <mat-error>Ingrese un correo válido.</mat-error>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Identificación</mat-label>
        <input matInput formControlName="identification" maxlength="13" inputmode="numeric" />
        <mat-error>Entre 10 y 13 dígitos, solo números.</mat-error>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Nueva contraseña</mat-label>
        <input matInput formControlName="newPassword" type="password" maxlength="30" autocomplete="new-password" />
        <mat-hint>8 a 30 caracteres, con un número y una mayúscula.</mat-hint>
        <mat-error>8 a 30 caracteres, con al menos un número y una mayúscula.</mat-error>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Confirmar contraseña</mat-label>
        <input matInput formControlName="confirm" type="password" maxlength="30" autocomplete="new-password" />
        <mat-error>Confirme la contraseña.</mat-error>
      </mat-form-field>
      <p class="form-error" *ngIf="form.hasError('mismatch') && form.controls.confirm.touched">
        Las contraseñas no coinciden.
      </p>

      <button mat-flat-button type="submit" class="submit" [disabled]="form.invalid || sending">
        Actualizar contraseña
      </button>
    </form>
    <ng-container *ngIf="emailEnabled">
      <a class="link" href="#" (click)="setMode('email', $event)">Recibir una contraseña temporal por correo</a><br />
    </ng-container>
    </ng-container>

    <a routerLink="/login" class="link">Volver al inicio de sesión</a>
  `,
  styleUrl: './auth-forms.scss',
})
export class RecoverComponent implements OnInit {
  sending = false;
  emailEnabled = false;
  mode: 'email' | 'identity' = 'identity';

  readonly form;
  readonly emailForm;

  constructor(fb: FormBuilder, private auth: AuthService, private router: Router, private notify: NotifyService) {
    this.form = fb.nonNullable.group(
      {
        username: ['', [Validators.required, Validators.maxLength(20)]],
        email: ['', [Validators.required, Validators.email]],
        identification: ['', [Validators.required, Validators.pattern(/^\d{10,13}$/)]],
        newPassword: ['', [Validators.required, Validators.pattern(/^(?=.*\d)(?=.*[A-Z]).{8,30}$/)]],
        confirm: ['', Validators.required],
      },
      { validators: samePassword },
    );
    this.emailForm = fb.nonNullable.group({
      username: ['', [Validators.required, Validators.maxLength(20)]],
      email: ['', [Validators.required, Validators.email]],
    });
  }

  ngOnInit(): void {
    this.auth.recoveryOptions().subscribe((options) => {
      this.emailEnabled = options.emailEnabled;
      if (options.emailEnabled) this.mode = 'email';
    });
  }

  setMode(mode: 'email' | 'identity', event: Event): void {
    event.preventDefault();
    this.mode = mode;
  }

  sendEmail(): void {
    if (this.emailForm.invalid) return;
    this.sending = true;
    this.auth.forgotPassword(this.emailForm.getRawValue()).subscribe({
      next: (response) => {
        this.notify.success(response.message);
        this.router.navigate(['/login']);
      },
      error: () => (this.sending = false),
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.sending = true;
    const { confirm, ...body } = this.form.getRawValue();
    this.auth.recoverPassword(body).subscribe({
      next: (response) => {
        this.notify.success(response.message);
        this.router.navigate(['/login']);
      },
      error: () => (this.sending = false),
    });
  }
}
