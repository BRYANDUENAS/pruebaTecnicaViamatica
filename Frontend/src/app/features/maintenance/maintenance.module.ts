import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CashDialogComponent, ServiceDialogComponent } from './maintenance-dialogs.component';
import { MaintenanceComponent } from './maintenance.component';

@NgModule({
  declarations: [MaintenanceComponent, CashDialogComponent, ServiceDialogComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: MaintenanceComponent }])],
})
export class MaintenanceModule {}
