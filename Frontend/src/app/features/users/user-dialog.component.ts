import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { UsersApi } from '../../core/api.services';
import { User } from '../../core/models';

export const ASSIGNABLE_ROLES = [
  { rolId: 2, rolName: 'Gestor' },
  { rolId: 3, rolName: 'Cajero' },
];

/**
 * Alta y edición de gestores y cajeros. El usuario y la contraseña solo se
 * piden al crear; al editar se actualizan los datos personales y el rol.
 */
@Component({
  selector: 'app-user-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>{{ user ? 'Editar usuario' : 'Nuevo usuario' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-grid" id="user-form" (ngSubmit)="save()">
        <ng-container *ngIf="!user">
          <mat-form-field>
            <mat-label>Nombre de usuario</mat-label>
            <input matInput formControlName="username" maxlength="20" autocomplete="off" />
            <mat-hint>8 a 20 caracteres, letras y al menos un número.</mat-hint>
            <mat-error>8 a 20 caracteres, solo letras y números, con al menos un número.</mat-error>
          </mat-form-field>
          <mat-form-field>
            <mat-label>Contraseña</mat-label>
            <input matInput formControlName="password" type="password" maxlength="30" autocomplete="new-password" />
            <mat-hint>8 a 30 caracteres, con un número y una mayúscula.</mat-hint>
            <mat-error>8 a 30 caracteres, con al menos un número y una mayúscula.</mat-error>
          </mat-form-field>
        </ng-container>

        <mat-form-field>
          <mat-label>Nombres</mat-label>
          <input matInput formControlName="firstName" maxlength="50" />
          <mat-error>Ingrese los nombres.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Apellidos</mat-label>
          <input matInput formControlName="lastName" maxlength="50" />
          <mat-error>Ingrese los apellidos.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Identificación</mat-label>
          <input matInput formControlName="identification" maxlength="13" inputmode="numeric" />
          <mat-error>Entre 10 y 13 dígitos, solo números.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Rol</mat-label>
          <mat-select formControlName="rolId">
            <mat-option *ngFor="let rol of roles" [value]="rol.rolId">{{ rol.rolName }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Correo</mat-label>
          <input matInput formControlName="email" type="email" maxlength="100" />
          <mat-error>Ingrese un correo válido.</mat-error>
        </mat-form-field>
      </form>
      <p class="muted" *ngIf="!user && !isAdmin">
        El usuario quedará pendiente hasta que un administrador lo apruebe.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button type="submit" form="user-form" [disabled]="form.invalid || saving">
        {{ user ? 'Guardar cambios' : 'Crear usuario' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class UserDialogComponent {
  readonly roles = ASSIGNABLE_ROLES;
  readonly user?: User;
  readonly isAdmin: boolean;
  saving = false;

  readonly form;

  constructor(
    fb: FormBuilder,
    private api: UsersApi,
    private ref: MatDialogRef<UserDialogComponent, User>,
    @Inject(MAT_DIALOG_DATA) data: { user?: User; isAdmin: boolean },
  ) {
    this.user = data.user;
    this.isAdmin = data.isAdmin;
    const onCreate = (validators: unknown[]) => (this.user ? [] : validators) as never[];

    this.form = fb.nonNullable.group({
      username: ['', onCreate([Validators.required, Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,20}$/)])],
      password: ['', onCreate([Validators.required, Validators.pattern(/^(?=.*\d)(?=.*[A-Z]).{8,30}$/)])],
      firstName: [this.user?.firstName ?? '', [Validators.required, Validators.maxLength(50)]],
      lastName: [this.user?.lastName ?? '', [Validators.required, Validators.maxLength(50)]],
      identification: [this.user?.identification ?? '', [Validators.required, Validators.pattern(/^\d{10,13}$/)]],
      rolId: [this.user?.rolId ?? 3, Validators.required],
      email: [this.user?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(100)]],
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const { username, password, ...profile } = this.form.getRawValue();
    const request = this.user
      ? this.api.update(this.user.userId, profile)
      : this.api.create({ username, password, ...profile });
    request.subscribe({
      next: (user) => this.ref.close(user),
      error: () => (this.saving = false),
    });
  }
}
