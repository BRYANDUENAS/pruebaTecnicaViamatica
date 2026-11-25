import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';
import { CashesApi, CatalogsApi, ClientsApi, ContractsApi, TurnsApi } from '../../core/api.services';
import { AuthService } from '../../core/auth.service';
import {
  Cash, Client, Contract, InternetService, MethodPayment, OPEN_CONTRACT_STATUSES, Payment, TURN_STATUS, Turn,
} from '../../core/models';
import { NotifyService } from '../../core/ui.services';
import { ClientDialogComponent } from '../../shared/client-dialog.component';
import { ConfirmService } from '../../shared/confirm-dialog.component';

/**
 * Procesos de caja del cajero. Todo exige tener la caja abierta: atender
 * turnos, contratar, cobrar, cambiar servicio o forma de pago y cancelar.
 */
@Component({
  selector: 'app-cashier',
  standalone: false,
  templateUrl: './cashier.component.html',
  styleUrl: './cashier.component.scss',
})
export class CashierComponent implements OnInit {
  readonly status = TURN_STATUS;
  readonly paymentColumns = ['date', 'service', 'method', 'amount'];

  loaded = false;
  cash: Cash | null = null;
  turns: Turn[] = [];
  services: InternetService[] = [];
  methods: MethodPayment[] = [];

  // Cliente en atención
  identification = '';
  notFound = false;
  client: Client | null = null;
  contracts: Contract[] = [];
  payments: Payment[] = [];

  // Selecciones de los formularios de proceso
  serviceId: number | null = null;
  methodId: number | null = null;
  newServiceId: number | null = null;
  newMethodId: number | null = null;
  payMethodId: number | null = null;

  constructor(
    private cashesApi: CashesApi,
    private turnsApi: TurnsApi,
    private clientsApi: ClientsApi,
    private contractsApi: ContractsApi,
    private catalogs: CatalogsApi,
    private dialog: MatDialog,
    private confirm: ConfirmService,
    private notify: NotifyService,
    private auth: AuthService,
  ) {}

  ngOnInit(): void {
    forkJoin([this.catalogs.services(), this.catalogs.methodPayments()]).subscribe(([services, methods]) => {
      this.services = services;
      this.methods = methods;
    });
    this.loadCash();
  }

  // ----- Caja -----

  get isOpenByMe(): boolean {
    return !!this.cash && this.cash.currentUserId === this.auth.user()?.userId;
  }

  get openByOther(): boolean {
    return !!this.cash?.currentUserId && !this.isOpenByMe;
  }

  loadCash(): void {
    this.cashesApi.mine().subscribe((cash) => {
      this.cash = cash;
      this.loaded = true;
      if (cash) this.loadTurns();
    });
  }

  openCash(): void {
    this.cashesApi.open().subscribe({
      next: (cash) => {
        this.cash = cash;
        this.notify.success(`${cash.cashDescription} abierta.`);
        this.loadTurns();
      },
      error: () => this.loadCash(),
    });
  }

  closeCash(): void {
    this.cashesApi.close().subscribe((cash) => {
      this.cash = cash;
      this.clearClient();
      this.notify.success(`${cash.cashDescription} cerrada.`);
    });
  }

  // ----- Turnos -----

  get currentTurn(): Turn | null {
    return this.turns.find((turn) => turn.statusId === TURN_STATUS.inProgress) ?? null;
  }

  get pendingTurns(): Turn[] {
    return this.turns.filter((turn) => turn.statusId === TURN_STATUS.pending);
  }

  get attendedCount(): number {
    return this.turns.filter((turn) => turn.statusId === TURN_STATUS.attended).length;
  }

  loadTurns(): void {
    this.turnsApi.list().subscribe((turns) => (this.turns = turns));
  }

  startTurn(turn: Turn): void {
    this.turnsApi.start(turn.turnId).subscribe(() => {
      this.notify.success(`Atendiendo el turno ${turn.description}.`);
      this.loadTurns();
    });
  }

  /** Finaliza el turno y lo asocia al cliente atendido, si se buscó uno. */
  finishTurn(turn: Turn): void {
    this.turnsApi.finish(turn.turnId, this.client?.clientId ?? null).subscribe(() => {
      this.notify.success(`Turno ${turn.description} finalizado.`);
      this.clearClient();
      this.loadTurns();
    });
  }

  // ----- Cliente -----

  get validIdentification(): boolean {
    return /^\d{10,13}$/.test(this.identification);
  }

  searchClient(): void {
    if (!this.validIdentification) return;
    this.notFound = false;
    this.clientsApi.byIdentification(this.identification).subscribe({
      next: (client) => this.selectClient(client),
      error: (error) => {
        this.client = null;
        this.notFound = error.status === 404;
      },
    });
  }

  registerClient(): void {
    this.dialog
      .open(ClientDialogComponent, { data: { identification: this.identification } })
      .afterClosed()
      .subscribe((client?: Client) => {
        if (!client) return;
        this.notify.success('Cliente registrado.');
        this.selectClient(client);
      });
  }

  clearClient(): void {
    this.client = null;
    this.identification = '';
    this.notFound = false;
    this.contracts = [];
    this.payments = [];
  }

  private selectClient(client: Client): void {
    this.client = client;
    this.identification = client.identification;
    this.notFound = false;
    this.loadClientData();
  }

  private loadClientData(): void {
    if (!this.client) return;
    const id = this.client.clientId;
    forkJoin([this.contractsApi.byClient(id), this.contractsApi.payments(id)]).subscribe(([contracts, payments]) => {
      this.contracts = contracts;
      this.payments = payments;
      this.serviceId = this.newServiceId = this.newMethodId = null;
      this.methodId = null;
      this.payMethodId = this.contract?.methodPaymentId ?? null;
    });
  }

  // ----- Procesos de contrato -----

  /** Contrato vigente del cliente; solo puede haber uno. */
  get contract(): Contract | null {
    return this.contracts.find((contract) => OPEN_CONTRACT_STATUSES.includes(contract.statusId)) ?? null;
  }

  get history(): Contract[] {
    return this.contracts.filter((contract) => !OPEN_CONTRACT_STATUSES.includes(contract.statusId));
  }

  get otherServices(): InternetService[] {
    return this.services.filter((service) => service.serviceId !== this.contract?.serviceId);
  }

  get otherMethods(): MethodPayment[] {
    return this.methods.filter((method) => method.methodPaymentId !== this.contract?.methodPaymentId);
  }

  service(id: number | null): InternetService | undefined {
    return this.services.find((service) => service.serviceId === id);
  }

  hire(): void {
    if (!this.client || this.serviceId === null || this.methodId === null) return;
    this.contractsApi.create(this.client.clientId, this.serviceId, this.methodId).subscribe((contract) => {
      this.notify.success(`Servicio ${contract.serviceName} contratado.`);
      this.loadClientData();
    });
  }

  pay(): void {
    if (!this.contract) return;
    this.contractsApi.pay(this.contract.contractId, this.payMethodId).subscribe((payment) => {
      this.notify.success(`Pago de $${payment.amount.toFixed(2)} registrado.`);
      this.loadClientData();
    });
  }

  changeService(): void {
    if (!this.contract || this.newServiceId === null) return;
    this.contractsApi.changeService(this.contract.contractId, this.newServiceId).subscribe((contract) => {
      this.notify.success(`Servicio cambiado a ${contract.serviceName}.`);
      this.loadClientData();
    });
  }

  changeMethod(): void {
    if (!this.contract || this.newMethodId === null) return;
    this.contractsApi.changeMethodPayment(this.contract.contractId, this.newMethodId).subscribe((contract) => {
      this.notify.success(`Forma de pago cambiada a ${contract.methodPaymentDescription}.`);
      this.loadClientData();
    });
  }

  cancelContract(): void {
    const contract = this.contract;
    if (!contract) return;
    this.confirm
      .ask({
        title: 'Cancelar contrato',
        message: `¿Cancelar el contrato de ${contract.serviceName}? El servicio termina hoy y no se puede deshacer.`,
        action: 'Cancelar contrato',
      })
      .subscribe(() =>
        this.contractsApi.cancel(contract.contractId).subscribe(() => {
          this.notify.success('Contrato cancelado.');
          this.loadClientData();
        }),
      );
  }
}
