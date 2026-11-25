import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CashesApi, ServicesApi } from '../../core/api.services';
import { Cash, InternetService } from '../../core/models';

@Component({
  selector: 'app-cash-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>{{ cash ? 'Editar caja' : 'Nueva caja' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-grid" id="cash-form" (ngSubmit)="save()">
        <mat-form-field class="full">
          <mat-label>Descripción</mat-label>
          <input matInput formControlName="cashDescription" maxlength="50" />
          <mat-error>Ingrese la descripción de la caja.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Estado</mat-label>
          <mat-select formControlName="active">
            <mat-option [value]="true">Activa</mat-option>
            <mat-option [value]="false">Inactiva</mat-option>
          </mat-select>
          <mat-hint>Una caja inactiva no recibe cajeros ni turnos.</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button type="submit" form="cash-form" [disabled]="form.invalid || saving">
        {{ cash ? 'Guardar cambios' : 'Crear caja' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class CashDialogComponent {
  saving = false;
  readonly form;

  constructor(
    fb: FormBuilder,
    private api: CashesApi,
    private ref: MatDialogRef<CashDialogComponent, Cash>,
    @Inject(MAT_DIALOG_DATA) public cash: Cash | null,
  ) {
    this.form = fb.nonNullable.group({
      cashDescription: [cash?.cashDescription ?? '', [Validators.required, Validators.maxLength(50)]],
      active: [cash?.active ?? true],
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const body = this.form.getRawValue();
    const request = this.cash ? this.api.update(this.cash.cashId, body) : this.api.create(body);
    request.subscribe({ next: (cash) => this.ref.close(cash), error: () => (this.saving = false) });
  }
}

@Component({
  selector: 'app-service-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>{{ service ? 'Editar servicio' : 'Nuevo servicio' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-grid" id="service-form" (ngSubmit)="save()">
        <mat-form-field class="full">
          <mat-label>Nombre del servicio</mat-label>
          <input matInput formControlName="serviceName" maxlength="100" />
          <mat-error>Ingrese el nombre del servicio.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Descripción</mat-label>
          <input matInput formControlName="serviceDescription" maxlength="150" />
          <mat-error>Ingrese la descripción.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Velocidad (Mbps)</mat-label>
          <input matInput formControlName="speedMbps" type="number" min="1" />
          <mat-error>Debe ser mayor a 0.</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Precio mensual (USD)</mat-label>
          <input matInput formControlName="price" type="number" min="0.01" step="0.01" />
          <mat-error>Debe ser mayor a 0.</mat-error>
        </mat-form-field>
        <mat-form-field class="full">
          <mat-label>Equipos</mat-label>
          <input matInput formControlName="devices" />
          <mat-hint>Separe los equipos con comas. Ejemplo: Router WiFi 6, ONT de fibra</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button type="submit" form="service-form" [disabled]="form.invalid || saving">
        {{ service ? 'Guardar cambios' : 'Crear servicio' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ServiceDialogComponent {
  saving = false;
  readonly form;

  constructor(
    fb: FormBuilder,
    private api: ServicesApi,
    private ref: MatDialogRef<ServiceDialogComponent, InternetService>,
    @Inject(MAT_DIALOG_DATA) public service: InternetService | null,
  ) {
    this.form = fb.nonNullable.group({
      serviceName: [service?.serviceName ?? '', [Validators.required, Validators.maxLength(100)]],
      serviceDescription: [service?.serviceDescription ?? '', [Validators.required, Validators.maxLength(150)]],
      speedMbps: [service?.speedMbps ?? 100, [Validators.required, Validators.min(1)]],
      price: [service?.price ?? 0, [Validators.required, Validators.min(0.01)]],
      devices: [service?.devices.join(', ') ?? ''],
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const { devices, ...values } = this.form.getRawValue();
    const body = {
      ...values,
      devices: devices.split(',').map((device) => device.trim()).filter((device) => device.length > 0),
    };
    const request = this.service ? this.api.update(this.service.serviceId, body) : this.api.create(body);
    request.subscribe({ next: (service) => this.ref.close(service), error: () => (this.saving = false) });
  }
}
