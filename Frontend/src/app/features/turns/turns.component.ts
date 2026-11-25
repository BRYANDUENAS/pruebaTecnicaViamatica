import { Component, OnInit } from '@angular/core';
import { CashesApi, CatalogsApi, TurnsApi } from '../../core/api.services';
import { AttentionType, Cash, CashierOption, TURN_STATUS, Turn } from '../../core/models';
import { NotifyService } from '../../core/ui.services';
import { ConfirmService } from '../../shared/confirm-dialog.component';

const MAX_CASHIERS = 2;
const STATUS_BADGE: Record<number, string> = { 1: 'warn', 2: 'info', 3: 'ok', 4: 'bad' };

/** Fecha local en formato yyyy-MM-dd, el que espera la API y el campo de fecha. */
function today(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
}

/** Pantalla del gestor: asigna cajeros a las cajas y turnos a cada caja. */
@Component({
  selector: 'app-turns',
  standalone: false,
  templateUrl: './turns.component.html',
  styleUrl: './turns.component.scss',
})
export class TurnsComponent implements OnInit {
  readonly status = TURN_STATUS;
  readonly columns = ['description', 'type', 'cash', 'status', 'cashier', 'time', 'actions'];

  cashes: Cash[] = [];
  cashiers: CashierOption[] = [];
  attentionTypes: AttentionType[] = [];
  turns: Turn[] = [];

  date = today();
  newTurn: { cashId: number | null; attentionTypeId: string | null } = { cashId: null, attentionTypeId: null };

  constructor(
    private cashesApi: CashesApi,
    private turnsApi: TurnsApi,
    private catalogs: CatalogsApi,
    private confirm: ConfirmService,
    private notify: NotifyService,
  ) {}

  ngOnInit(): void {
    this.catalogs.attentionTypes().subscribe((types) => (this.attentionTypes = types));
    this.loadCashes();
    this.loadTurns();
  }

  loadCashes(): void {
    this.cashesApi.list().subscribe((cashes) => (this.cashes = cashes));
    this.cashesApi.cashiers().subscribe((cashiers) => (this.cashiers = cashiers));
  }

  loadTurns(): void {
    this.turnsApi.list({ date: this.date }).subscribe((turns) => (this.turns = turns));
  }

  // ----- Cajas y cajeros -----

  /** Cajeros activos que todavía no tienen caja: un cajero atiende una sola. */
  get freeCashiers(): CashierOption[] {
    return this.cashiers.filter((cashier) => cashier.cashId === null);
  }

  canAssign(cash: Cash): boolean {
    return cash.active && cash.users.length < MAX_CASHIERS;
  }

  assign(cash: Cash, userId: number): void {
    this.cashesApi.assign(cash.cashId, userId).subscribe(() => {
      this.notify.success(`Cajero asignado a ${cash.cashDescription}.`);
      this.loadCashes();
    });
  }

  unassign(cash: Cash, userId: number, username: string): void {
    this.confirm
      .ask({
        title: 'Retirar cajero',
        message: `¿Retirar a ${username} de ${cash.cashDescription}? Quedará disponible para otra caja.`,
        action: 'Retirar',
      })
      .subscribe(() =>
        this.cashesApi.unassign(cash.cashId, userId).subscribe(() => {
          this.notify.success('Cajero retirado de la caja.');
          this.loadCashes();
        }),
      );
  }

  // ----- Turnos -----

  /** Solo reciben turnos las cajas activas que tienen al menos un cajero. */
  get availableCashes(): Cash[] {
    return this.cashes.filter((cash) => cash.active && cash.users.length > 0);
  }

  createTurn(): void {
    const { cashId, attentionTypeId } = this.newTurn;
    if (cashId === null || attentionTypeId === null) return;
    this.turnsApi.create(cashId, attentionTypeId).subscribe((turn) => {
      this.notify.success(`Turno ${turn.description} asignado a ${turn.cashDescription}.`);
      this.date = today();
      this.loadTurns();
    });
  }

  otherCashes(turn: Turn): Cash[] {
    return this.availableCashes.filter((cash) => cash.cashId !== turn.cashId);
  }

  move(turn: Turn, cash: Cash): void {
    this.turnsApi.move(turn.turnId, cash.cashId).subscribe(() => {
      this.notify.success(`Turno ${turn.description} movido a ${cash.cashDescription}.`);
      this.loadTurns();
    });
  }

  remove(turn: Turn): void {
    this.confirm
      .ask({ title: 'Eliminar turno', message: `¿Eliminar el turno ${turn.description}?`, action: 'Eliminar' })
      .subscribe(() =>
        this.turnsApi.delete(turn.turnId).subscribe(() => {
          this.notify.success(`Turno ${turn.description} eliminado.`);
          this.loadTurns();
        }),
      );
  }

  badge(turn: Turn): string {
    return STATUS_BADGE[turn.statusId] ?? '';
  }

  count(statusId: number): number {
    return this.turns.filter((turn) => turn.statusId === statusId).length;
  }
}
