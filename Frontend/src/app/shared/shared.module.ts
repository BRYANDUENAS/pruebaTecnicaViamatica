import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BulkUploadComponent } from './bulk-upload.component';
import { ClientDialogComponent } from './client-dialog.component';
import { ConfirmDialogComponent } from './confirm-dialog.component';

const MATERIAL = [
  MatButtonModule, MatCardModule, MatChipsModule, MatDialogModule, MatDividerModule, MatFormFieldModule,
  MatIconModule, MatInputModule, MatListModule, MatMenuModule, MatProgressBarModule, MatSelectModule,
  MatSidenavModule, MatSnackBarModule, MatTableModule, MatTabsModule, MatToolbarModule, MatTooltipModule,
];

/** Módulos y componentes reutilizables por todas las funcionalidades. */
@NgModule({
  declarations: [ConfirmDialogComponent, BulkUploadComponent, ClientDialogComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ...MATERIAL],
  exports: [
    CommonModule, FormsModule, ReactiveFormsModule, ...MATERIAL,
    ConfirmDialogComponent, BulkUploadComponent, ClientDialogComponent,
  ],
})
export class SharedModule {}
