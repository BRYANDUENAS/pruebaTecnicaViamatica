import { Component, EventEmitter, Input, Output } from '@angular/core';
import * as XLSX from 'xlsx';
import { BulkResult } from '../core/models';
import { NotifyService } from '../core/ui.services';

/**
 * Carga masiva desde Excel (.xlsx) o CSV. Lee la primera hoja, valida que
 * existan las columnas esperadas y emite las filas para enviarlas a la API.
 */
@Component({
  selector: 'app-bulk-upload',
  standalone: false,
  template: `
    <input #file type="file" accept=".xlsx,.csv" hidden (change)="onFile($event)" />
    <button mat-stroked-button type="button" (click)="file.click()">
      <mat-icon>upload_file</mat-icon> Carga masiva
    </button>
    <button mat-button type="button" (click)="downloadTemplate()" matTooltip="Descargar plantilla CSV">
      <mat-icon>download</mat-icon> Plantilla
    </button>

    <mat-card *ngIf="result" class="bulk-result">
      <mat-card-content>
        <div class="row between">
          <strong>Resultado: {{ result.created }} de {{ result.total }} registros creados</strong>
          <button mat-icon-button type="button" (click)="result = null" aria-label="Cerrar resultado">
            <mat-icon>close</mat-icon>
          </button>
        </div>
        <table class="plain" *ngIf="failedRows.length">
          <tr><th>Fila</th><th>Registro</th><th>Error</th></tr>
          <tr *ngFor="let row of failedRows">
            <td>{{ row.row }}</td><td>{{ row.key }}</td><td>{{ row.error }}</td>
          </tr>
        </table>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`.bulk-result { margin-top: 12px; }`],
})
export class BulkUploadComponent {
  /** Columnas esperadas en la primera fila del archivo. */
  @Input({ required: true }) columns: string[] = [];
  @Input() templateName = 'plantilla';
  /** Resultado devuelto por la API, para mostrar los errores por fila. */
  @Input() result: BulkResult | null = null;
  @Output() rows = new EventEmitter<Record<string, string>[]>();

  constructor(private notify: NotifyService) {}

  get failedRows() {
    return this.result?.rows.filter((row) => !row.success) ?? [];
  }

  async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    try {
      // raw: true conserva los ceros iniciales de identificaciones y teléfonos en CSV.
      const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array', raw: true });
      const sheet = workbook.Sheets[workbook.SheetNames[0]];
      const data = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: '', raw: false });

      if (!data.length) return this.notify.error('El archivo no tiene filas.');
      const missing = this.columns.filter((column) => !(column in data[0]));
      if (missing.length) return this.notify.error(`Faltan columnas en el archivo: ${missing.join(', ')}.`);

      this.rows.emit(
        data.map((row) => Object.fromEntries(this.columns.map((column) => [column, String(row[column]).trim()]))),
      );
    } catch {
      this.notify.error('No se pudo leer el archivo. Use un .xlsx o .csv válido.');
    }
  }

  downloadTemplate(): void {
    const blob = new Blob([this.columns.join(',') + '\n'], { type: 'text/csv;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${this.templateName}.csv`;
    link.click();
    URL.revokeObjectURL(link.href);
  }
}
