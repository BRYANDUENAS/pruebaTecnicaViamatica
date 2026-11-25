import { Component, NgModule, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DashboardApi } from '../../core/api.services';
import { ROLES, Welcome } from '../../core/models';
import { SharedModule } from '../../shared/shared.module';

/** Pantalla de bienvenida: datos del usuario y turnos del día según su rol. */
@Component({
  selector: 'app-home',
  standalone: false,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  data: Welcome | null = null;
  readonly roles = ROLES;
  readonly pendingColumns = ['username', 'name', 'rol', 'created'];

  constructor(private api: DashboardApi) {}

  ngOnInit(): void {
    this.api.welcome().subscribe((data) => (this.data = data));
  }

  /** Describe el alcance del conteo de turnos, que cambia con el rol. */
  get scope(): string {
    switch (this.data?.user.rolName) {
      case ROLES.admin: return 'Total de todas las cajas';
      case ROLES.gestor: return 'De los turnos que usted asignó';
      default: return 'Atendidos por usted';
    }
  }
}

@NgModule({
  declarations: [HomeComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: HomeComponent }])],
})
export class HomeModule {}
