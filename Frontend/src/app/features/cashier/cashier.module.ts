import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CashierComponent } from './cashier.component';

@NgModule({
  declarations: [CashierComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: CashierComponent }])],
})
export class CashierModule {}
