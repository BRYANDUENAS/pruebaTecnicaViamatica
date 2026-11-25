import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ShellComponent } from './shell.component';

/** Estructura de la aplicación autenticada: menú lateral y barra superior. */
@NgModule({
  declarations: [ShellComponent],
  imports: [SharedModule, RouterModule],
  exports: [ShellComponent],
})
export class LayoutModule {}
