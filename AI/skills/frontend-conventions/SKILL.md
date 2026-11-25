---
name: frontend-conventions
description: Convenciones del frontend Angular de este proyecto (módulos, rutas, guards, diseño con Angular Material, mensajes y carga). Usar antes de crear o modificar módulos, componentes, servicios o estilos.
---

# Convenciones del frontend

## Estructura

- `core/`: servicios singleton, guards, interceptores y modelos. Se importa una sola vez.
- `shared/`: módulo con los componentes y módulos de Angular Material reutilizables.
- `features/<nombre>/`: un NgModule por funcionalidad, con su propio módulo de rutas.
- Las rutas principales cargan cada módulo con `loadChildren` (lazy loading).

## Seguridad

- `AuthGuard` protege todo lo que está detrás del login; `RoleGuard` valida el rol declarado en `data.roles`.
- Un interceptor adjunta el JWT y las cabeceras; otro controla el indicador de carga y los errores.
- Ante un 401 se cierra la sesión y se redirige al login.
- El menú se obtiene del backend según el rol; no se define en el código.

## Diseño

- Angular Material con un tema propio: primario azul, acento turquesa, coherente con una empresa de internet.
- Layout con `mat-sidenav` para el menú y `mat-toolbar` con el usuario y el cierre de sesión.
- Listados en `mat-table` con filtro; altas y ediciones en `mat-dialog`.
- Diseño adaptable: el menú lateral pasa a modo superpuesto en pantallas angostas.
- Textos de interfaz en español.

## Formularios y mensajes

- Formularios reactivos con las mismas reglas que valida el backend; el botón de envío se deshabilita si el formulario es inválido.
- Cada campo muestra su error con `mat-error`.
- Éxitos y errores se muestran con `MatSnackBar`; los errores de validación del backend se muestran por campo.
- Toda petición en curso muestra una barra de progreso global.
