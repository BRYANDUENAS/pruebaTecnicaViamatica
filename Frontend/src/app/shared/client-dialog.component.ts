import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ClientsApi } from '../core/api.services';
import { Client } from '../core/models';

export interface ClientDialogData {
  client?: Client;
  /** Identificación sugerida al registrar un cliente que no se encontró. */
  identification?: string;
}

/** Alta y edición de clientes, con las mismas reglas que valida el backend. */
@Component({
  selector: 'app-client-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>{{ editing ? 'Editar cliente' : 'Registrar cliente' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-grid" id="client-form" (ngSubmit)="save()">
        <mat-form-field>
          <mat-label>Nombres</mat-label>
          <input matInput formControlName="name" maxlength="50" />
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
          <mat-hint *ngIf="editing">La identificación no se puede modificar.</mat-hint>
          <mat-error>Entre 10 y 13 dígitos, solo números.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Teléfono</mat-label>
          <input matInput formControlName="phoneNumber" maxlength="13" inputmode="numeric" />
          <mat-error>Debe empezar con 09 y tener entre 10 y 13 dígitos.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Correo</mat-label>
          <input matInput formControlName="email" type="email" maxlength="120" />
          <mat-error>Ingrese un correo válido.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Dirección</mat-label>
          <input matInput formControlName="address" maxlength="100" />
          <mat-hint align="end">{{ form.value.address?.length || 0 }}/100</mat-hint>
          <mat-error>Entre 20 y 100 caracteres.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Referencia de la dirección</mat-label>
          <input matInput formControlName="referenceAddress" maxlength="100" />
          <mat-hint align="end">{{ form.value.referenceAddress?.length || 0 }}/100</mat-hint>
          <mat-error>Entre 20 y 100 caracteres.</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button type="submit" form="client-form" [disabled]="form.invalid || saving">
        {{ editing ? 'Guardar cambios' : 'Registrar' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ClientDialogComponent {
  readonly editing: boolean;
  saving = false;

  readonly form;

  constructor(
    fb: FormBuilder,
    private api: ClientsApi,
    private ref: MatDialogRef<ClientDialogComponent, Client>,
    @Inject(MAT_DIALOG_DATA) private data: ClientDialogData,
  ) {
    const client = data.client;
    this.editing = !!client;
    const address = [Validators.required, Validators.minLength(20), Validators.maxLength(100)];
    this.form = fb.nonNullable.group({
      name: [client?.name ?? '', [Validators.required, Validators.maxLength(50)]],
      lastName: [client?.lastName ?? '', [Validators.required, Validators.maxLength(50)]],
      identification: [
        { value: client?.identification ?? data.identification ?? '', disabled: this.editing },
        [Validators.required, Validators.pattern(/^\d{10,13}$/)],
      ],
      phoneNumber: [client?.phoneNumber ?? '', [Validators.required, Validators.pattern(/^09\d{8,11}$/)]],
      email: [client?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(120)]],
      address: [client?.address ?? '', address],
      referenceAddress: [client?.referenceAddress ?? '', address],
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const body = this.form.getRawValue();
    const request = this.data.client ? this.api.update(this.data.client.clientId, body) : this.api.create(body);
    request.subscribe({
      next: (client) => this.ref.close(client),
      error: () => (this.saving = false),
    });
  }
}
