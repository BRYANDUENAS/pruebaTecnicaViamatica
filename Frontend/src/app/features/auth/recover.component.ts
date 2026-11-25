import { Component } from '@angular/core';
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

    <a routerLink="/login" class="link">Volver al inicio de sesión</a>
  `,
  styleUrl: './auth-forms.scss',
})
export class RecoverComponent {
  sending = false;

  readonly form;

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
