# Frontend

Aplicación Angular 21 con Angular Material para el sistema de gestión de caja y turnos.

## Ejecutar

Con Docker, desde la raíz del repositorio: `docker compose up --build`. La aplicación queda en http://localhost:8080.

Sin Docker, con la API corriendo en `http://localhost:5080`:

```bash
cd Frontend
npm install
npm start
```

La aplicación queda en http://localhost:4200. Las peticiones a `/api` se reenvían a la API con `proxy.conf.json`; en Docker lo hace nginx.

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/app/core/` | Modelos, sesión, servicios de API, guards e interceptores |
| `src/app/shared/` | `SharedModule`: Angular Material y componentes reutilizables (confirmación, carga masiva, formulario de cliente) |
| `src/app/layout/` | Estructura autenticada: sidebar, barra superior y asistente |
| `src/app/features/` | Un módulo por funcionalidad |

La aplicación usa NgModules. Cada ruta carga su módulo con `loadChildren`, así que el navegador descarga cada pantalla solo cuando se visita.

## Módulos y rutas

| Ruta | Módulo | Rol |
|---|---|---|
| `/login`, `/login/recuperar` | `AuthModule` | Público |
| `/inicio` | `HomeModule` | Todos |
| `/dashboard` | `DashboardModule` | Administrador |
| `/usuarios` | `UsersModule` | Administrador, Gestor |
| `/mantenimiento` | `MaintenanceModule` | Administrador |
| `/turnos` | `TurnsModule` | Gestor |
| `/caja` | `CashierModule` | Cajero |
| `/clientes` | `ClientsModule` | Cajero |

## Seguridad

- **`authGuard`** exige una sesión vigente y **`roleGuard`** valida el rol declarado en `data.roles` de cada ruta.
- **`AuthInterceptor`** adjunta el JWT y las cabeceras a todas las peticiones.
- **`HttpStateInterceptor`** controla la barra de carga global, muestra los errores y cierra la sesión si el token venció.
- **El menú llega del backend** según el rol; no hay ningún menú definido en el código.
- Como las claims del JWT viajan cifradas, los datos del usuario se toman de la respuesta del login y no del token.

## Pantallas

| Pantalla | Qué permite |
|---|---|
| Login | Ingreso con validación previa y recuperación de contraseña |
| Bienvenida | Datos del usuario, turnos atendidos del día según el rol y, para el gestor, sus usuarios pendientes de aprobación |
| Dashboard | Indicadores de usuarios y de cajas, cajeros y gestores con filtro de fechas |
| Usuarios | Alta, edición, estados, aprobación, eliminación y carga masiva |
| Cajas y servicios | Mantenimiento de cajas y de planes de internet |
| Asignación de turnos | Cajeros por caja, creación de turnos y seguimiento del día |
| Procesos de caja | Apertura y cierre, atención de turnos, contratación, pagos, cambios y cancelación |
| Clientes | Alta, edición, estado, eliminación y carga masiva |

## Carga masiva

Acepta archivos `.xlsx` y `.csv`. El botón **Plantilla** descarga un CSV con los encabezados esperados:

| Pantalla | Columnas |
|---|---|
| Usuarios | `username`, `password`, `firstName`, `lastName`, `email`, `identification`, `rol` (Gestor o Cajero) |
| Clientes | `name`, `lastName`, `identification`, `email`, `phoneNumber`, `address`, `referenceAddress` |

En Excel, la identificación y el teléfono deben tener formato de texto para conservar el cero inicial. Al terminar se muestra cuántos registros se crearon y el error de cada fila que falló.

## Diseño

- Tema de Angular Material con azul como color primario y turquesa como acento.
- Formularios reactivos con las mismas reglas del backend y el error bajo cada campo.
- Mensajes de éxito y de error con `MatSnackBar` y barra de progreso mientras hay peticiones en curso.
- Diseño adaptable: en pantallas angostas el menú lateral se superpone.
- Los íconos y la fuente se cargan desde Google Fonts, por lo que se necesita conexión a internet.
