import { Component, NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-turns',
  standalone: false,
  template: `
    <div class="page">
      <div class="page-header"><h1>Asignación de turnos</h1></div>
      <div class="empty">Pantalla en construcción.</div>
    </div>
  `,
})
export class TurnsComponent {}

@NgModule({
  declarations: [TurnsComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: TurnsComponent }])],
})
export class TurnsModule {}
