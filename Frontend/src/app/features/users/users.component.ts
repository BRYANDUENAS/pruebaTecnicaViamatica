import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { UsersApi } from '../../core/api.services';
import { AuthService } from '../../core/auth.service';
import { BulkResult, ROLES, User } from '../../core/models';
import { NotifyService } from '../../core/ui.services';
import { ConfirmService } from '../../shared/confirm-dialog.component';
import { ASSIGNABLE_ROLES, UserDialogComponent } from './user-dialog.component';

const STATUS_BADGE: Record<string, string> = { ACT: 'ok', INA: '', BLO: 'bad', PEN: 'warn' };

/**
 * Mantenimiento de gestores y cajeros. El administrador gestiona todo; el
 * gestor solo crea usuarios (individual o masivamente) y consulta los suyos.
 */
@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  readonly isAdmin: boolean;
  readonly roles = ASSIGNABLE_ROLES;
  readonly statuses = [
    { id: 'ACT', label: 'Activo' },
    { id: 'INA', label: 'Inactivo' },
    { id: 'BLO', label: 'Bloqueado' },
    { id: 'PEN', label: 'Pendiente de aprobación' },
  ];
  readonly bulkColumns = ['username', 'password', 'firstName', 'lastName', 'email', 'identification', 'rol'];
  readonly columns: string[];

  readonly data = new MatTableDataSource<User>([]);
  search = '';
  rolId: number | null = null;
  statusId: string | null = null;
  bulkResult: BulkResult | null = null;

  constructor(
    private api: UsersApi,
    private dialog: MatDialog,
    private confirm: ConfirmService,
    private notify: NotifyService,
    auth: AuthService,
  ) {
    this.isAdmin = auth.hasRole([ROLES.admin]);
    this.columns = ['username', 'name', 'email', 'identification', 'rol', 'status', 'session'];
    if (this.isAdmin) this.columns.push('actions');

    this.data.filterPredicate = (user, term) =>
      [user.username, user.firstName, user.lastName, user.email, user.identification]
        .join(' ').toLowerCase().includes(term);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list({ rolId: this.rolId, statusId: this.statusId }).subscribe((users) => (this.data.data = users));
  }

  applySearch(): void {
    this.data.filter = this.search.trim().toLowerCase();
  }

  badge(user: User): string {
    return STATUS_BADGE[user.statusId] ?? '';
  }

  openDialog(user?: User): void {
    this.dialog
      .open(UserDialogComponent, { data: { user, isAdmin: this.isAdmin } })
      .afterClosed()
      .subscribe((saved?: User) => {
        if (!saved) return;
        this.notify.success(
          user ? 'Usuario actualizado.'
            : saved.statusId === 'PEN' ? 'Usuario creado. Queda pendiente de aprobación.' : 'Usuario creado.',
        );
        this.load();
      });
  }

  approve(user: User): void {
    this.api.approve(user.userId).subscribe(() => {
      this.notify.success(`Usuario ${user.username} aprobado.`);
      this.load();
    });
  }

  changeStatus(user: User, statusId: string): void {
    this.api.changeStatus(user.userId, statusId).subscribe((updated) => {
      this.notify.success(`Estado de ${user.username}: ${updated.statusDescription}.`);
      this.load();
    });
  }

  remove(user: User): void {
    this.confirm
      .ask({
        title: 'Eliminar usuario',
        message: `¿Eliminar a ${user.username}? Dejará de aparecer en el sistema y se liberará su caja.`,
        action: 'Eliminar',
      })
      .subscribe(() =>
        this.api.delete(user.userId).subscribe(() => {
          this.notify.success('Usuario eliminado.');
          this.load();
        }),
      );
  }

  /** Convierte las filas del archivo al formato de la API y las envía. */
  upload(rows: Record<string, string>[]): void {
    const body = rows.map(({ rol, ...row }) => ({ ...row, rolId: this.toRolId(rol) }));
    this.api.bulk(body).subscribe((result) => {
      this.bulkResult = result;
      result.failed
        ? this.notify.error(`Carga finalizada: ${result.created} creados y ${result.failed} con error.`)
        : this.notify.success(`Carga finalizada: ${result.created} usuarios creados.`);
      this.load();
    });
  }

  /** Acepta el nombre del rol (Gestor, Cajero) o su código (2, 3). */
  private toRolId(value: string): number {
    const match = this.roles.find((rol) => rol.rolName.toLowerCase() === value.toLowerCase());
    return match?.rolId ?? (Number(value) || 0);
  }
}
