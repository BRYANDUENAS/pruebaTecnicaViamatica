import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { AssistantChatComponent } from './assistant-chat.component';
import { ShellComponent } from './shell.component';

/** Estructura de la aplicación autenticada: menú lateral y barra superior. */
@NgModule({
  declarations: [ShellComponent, AssistantChatComponent],
  imports: [SharedModule, RouterModule],
  exports: [ShellComponent],
})
export class LayoutModule {}
