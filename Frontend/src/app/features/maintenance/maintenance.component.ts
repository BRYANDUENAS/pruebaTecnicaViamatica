import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CashesApi, ServicesApi } from '../../core/api.services';
import { Cash, InternetService } from '../../core/models';
import { NotifyService } from '../../core/ui.services';
import { ConfirmService } from '../../shared/confirm-dialog.component';
import { CashDialogComponent, ServiceDialogComponent } from './maintenance-dialogs.component';

/** Mantenimiento del administrador: cajas y planes de internet. */
@Component({
  selector: 'app-maintenance',
  standalone: false,
  templateUrl: './maintenance.component.html',
})
export class MaintenanceComponent implements OnInit {
  readonly cashColumns = ['description', 'status', 'use', 'cashiers', 'actions'];
  readonly serviceColumns = ['name', 'speed', 'price', 'devices', 'actions'];

  cashes: Cash[] = [];
  services: InternetService[] = [];

  constructor(
    private cashesApi: CashesApi,
    private servicesApi: ServicesApi,
    private dialog: MatDialog,
    private confirm: ConfirmService,
    private notify: NotifyService,
  ) {}

  ngOnInit(): void {
    this.loadCashes();
    this.loadServices();
  }

  loadCashes(): void {
    this.cashesApi.list().subscribe((cashes) => (this.cashes = cashes));
  }

  loadServices(): void {
    this.servicesApi.list().subscribe((services) => (this.services = services));
  }

  cashierNames(cash: Cash): string {
    return cash.users.map((user) => user.fullName).join(', ') || 'Sin cajeros';
  }

  openCash(cash?: Cash): void {
    this.dialog
      .open(CashDialogComponent, { data: cash ?? null })
      .afterClosed()
      .subscribe((saved?: Cash) => {
        if (!saved) return;
        this.notify.success(cash ? 'Caja actualizada.' : 'Caja creada.');
        this.loadCashes();
      });
  }

  removeCash(cash: Cash): void {
    this.confirm
      .ask({
        title: 'Eliminar caja',
        message: `¿Eliminar ${cash.cashDescription}? Sus cajeros quedarán sin caja asignada.`,
        action: 'Eliminar',
      })
      .subscribe(() =>
        this.cashesApi.delete(cash.cashId).subscribe(() => {
          this.notify.success('Caja eliminada.');
          this.loadCashes();
        }),
      );
  }

  openService(service?: InternetService): void {
    this.dialog
      .open(ServiceDialogComponent, { data: service ?? null })
      .afterClosed()
      .subscribe((saved?: InternetService) => {
        if (!saved) return;
        this.notify.success(service ? 'Servicio actualizado.' : 'Servicio creado.');
        this.loadServices();
      });
  }

  removeService(service: InternetService): void {
    this.confirm
      .ask({
        title: 'Eliminar servicio',
        message: `¿Eliminar ${service.serviceName}? Ya no podrá contratarse.`,
        action: 'Eliminar',
      })
      .subscribe(() =>
        this.servicesApi.delete(service.serviceId).subscribe(() => {
          this.notify.success('Servicio eliminado.');
          this.loadServices();
        }),
      );
  }
}
