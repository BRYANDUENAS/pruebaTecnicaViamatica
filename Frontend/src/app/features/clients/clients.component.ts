import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ClientsApi } from '../../core/api.services';
import { BulkResult, Client } from '../../core/models';
import { NotifyService } from '../../core/ui.services';
import { ClientDialogComponent } from '../../shared/client-dialog.component';
import { ConfirmService } from '../../shared/confirm-dialog.component';

/** Mantenimiento de clientes del cajero: alta, edición, estado, eliminación y carga masiva. */
@Component({
  selector: 'app-clients',
  standalone: false,
  templateUrl: './clients.component.html',
})
export class ClientsComponent implements OnInit {
  readonly columns = ['identification', 'name', 'email', 'phone', 'address', 'status', 'actions'];
  readonly bulkColumns = ['name', 'lastName', 'identification', 'email', 'phoneNumber', 'address', 'referenceAddress'];

  readonly data = new MatTableDataSource<Client>([]);
  search = '';
  active: boolean | null = null;
  bulkResult: BulkResult | null = null;

  constructor(
    private api: ClientsApi,
    private dialog: MatDialog,
    private confirm: ConfirmService,
    private notify: NotifyService,
  ) {
    this.data.filterPredicate = (client, term) =>
      [client.identification, client.name, client.lastName, client.email, client.phoneNumber]
        .join(' ').toLowerCase().includes(term);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list({ active: this.active }).subscribe((clients) => (this.data.data = clients));
  }

  applySearch(): void {
    this.data.filter = this.search.trim().toLowerCase();
  }

  openDialog(client?: Client): void {
    this.dialog
      .open(ClientDialogComponent, { data: { client } })
      .afterClosed()
      .subscribe((saved?: Client) => {
        if (!saved) return;
        this.notify.success(client ? 'Cliente actualizado.' : 'Cliente registrado.');
        this.load();
      });
  }

  toggleStatus(client: Client): void {
    this.api.changeStatus(client.clientId, !client.active).subscribe((updated) => {
      this.notify.success(`Cliente ${updated.active ? 'activado' : 'inactivado'}.`);
      this.load();
    });
  }

  remove(client: Client): void {
    this.confirm
      .ask({
        title: 'Eliminar cliente',
        message: `¿Eliminar a ${client.name} ${client.lastName}? Su identificación no podrá registrarse de nuevo.`,
        action: 'Eliminar',
      })
      .subscribe(() =>
        this.api.delete(client.clientId).subscribe(() => {
          this.notify.success('Cliente eliminado.');
          this.load();
        }),
      );
  }

  upload(rows: Record<string, string>[]): void {
    this.api.bulk(rows).subscribe((result) => {
      this.bulkResult = result;
      result.failed
        ? this.notify.error(`Carga finalizada: ${result.created} creados y ${result.failed} con error.`)
        : this.notify.success(`Carga finalizada: ${result.created} clientes creados.`);
      this.load();
    });
  }
}
