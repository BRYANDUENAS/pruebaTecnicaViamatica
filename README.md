# NetCaja · Prueba técnica .NET Core – Angular (Viamatica)

Sistema de gestión de caja y turnos para una empresa que brinda servicios de internet.

**Autor:** Bryan Dueñas

## Ejecutar con Docker

Solo se necesita Docker. Desde la raíz del repositorio:

```bash
docker compose up --build
```

| Qué | Dirección |
|---|---|
| Aplicación | http://localhost:8080 |
| Swagger de la API | http://localhost:5080/swagger |

Al iniciar, la API crea la base de datos, aplica las migraciones (incluidas las funciones de PostgreSQL) y carga datos de demostración. El frontend se comunica con la API a través de nginx, que reenvía `/api` al contenedor del backend.

Para empezar de cero: `docker compose down -v`.

## Usuarios de demostración

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin001` | `Admin1234` | Administrador |
| `gestor001` a `gestor003` | `Gestor1234` | Gestor |
| `cajero001` a `cajero010` | `Cajero1234` | Cajero, con caja asignada |

Además hay un cajero bloqueado (`cajero011`), uno inactivo (`cajero012`) y dos usuarios pendientes de aprobación, para ver esos estados en las pantallas. Los datos incluyen 10 cajas, 30 clientes, contratos, pagos y turnos de la última semana; todos son ficticios.

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| [Backend/](Backend/) | API REST en .NET 8 con arquitectura Onion. [README](Backend/README.md) |
| [Frontend/](Frontend/) | Aplicación Angular 21 con Angular Material. [README](Frontend/README.md) |
| [AI/](AI/) | Prompts y skills utilizados con agentes de IA. [README](AI/README.md) |

## Entregables

| Requisito | Dónde está |
|---|---|
| Script de las funciones de base de datos | [Backend/Database/functions.sql](Backend/Database/functions.sql) |
| Colección de Postman | [Backend/Postman/NetCaja.postman_collection.json](Backend/Postman/NetCaja.postman_collection.json) |
| Dockerización | [docker-compose.yml](docker-compose.yml), [Backend/Dockerfile](Backend/Dockerfile), [Frontend/Dockerfile](Frontend/Dockerfile) |
| Prompts y skills de IA | [AI/](AI/) |

## Cumplimiento del enunciado

| Requisito | Cómo se resolvió |
|---|---|
| Arquitectura orientada a capas | Onion: Domain, Application, Infrastructure y Api |
| Eliminaciones lógicas | Filtro global en EF Core; ninguna entidad se borra físicamente |
| Swagger | Disponible en `/swagger`, con autenticación por token |
| Entity Framework | EF Core 8 con migraciones sobre PostgreSQL |
| JWT con claims encriptadas | Cada claim viaja cifrada con AES dentro del token |
| Stored procedures o funciones | `fn_create_turn` y `fn_cash_indicators` |
| Contraseña, identificación y correo encriptados | Contraseña con hash BCrypt; correo e identificación con AES |
| Serilog | Creación, actualización, eliminación, errores, alertas e inicios de sesión |
| Desarrollo modularizado y rutas a módulos | Un NgModule por funcionalidad, cargado con lazy loading |
| Protección de rutas | Guards de sesión y de rol |
| Interceptor | Adjunta el JWT y las cabeceras; otro controla la carga y los errores |
| Menú desde la base de datos según el rol | Tablas `menu_items` y `rol_menus`; el frontend no define ningún menú |
| Carga masiva desde Excel o CSV | Usuarios y clientes, con resultado por fila |
| Docker | Base de datos, API y frontend comunicados por `docker compose` |

## Funciones adicionales (opcionales)

No las pide el enunciado. El sistema funciona completo sin ellas; se activan con variables de entorno.

| Función | Qué hace | Sin configurar |
|---|---|---|
| Recuperación por correo | Envía una contraseña temporal al correo registrado | Se usa la validación de identidad (usuario, correo e identificación) |
| Asistente con IA | Chat por rol: el cajero consulta un cliente por su identificación; el administrador y el gestor consultan el estado de cajas, usuarios y turnos | El chat indica que no está configurado |

Para activarlas, copie `.env.example` como `.env`, complete los valores y vuelva a levantar los contenedores:

```bash
cp .env.example .env
docker compose up --build
```

El asistente no consulta la base de datos: el servidor reúne solo los datos que el rol del usuario puede ver y se los entrega al modelo como contexto. Al consultar un cliente, sus datos se envían al proveedor del modelo (Google Gemini).

## Decisiones de diseño

- **Una caja, un cajero a la vez.** Cada caja admite 2 cajeros asignados, pero solo uno puede tenerla abierta. La apertura es un `UPDATE` condicional, por lo que la base de datos resuelve el caso de dos aperturas simultáneas.
- **Los procesos de caja exigen la caja abierta**: atender turnos, contratar, cobrar, cambiar servicio o forma de pago y cancelar.
- **Un cajero atiende una sola caja.**
- **El contrato dura 1 año** y cada cliente tiene un solo contrato vigente.
- **Cambio de servicio**: el contrato actual queda sustituido y se crea uno de renovación que conserva la fecha final.
- **La identificación del cliente no se edita** y, al ser lógica la eliminación, no puede volver a registrarse.
- **Cifrado determinista** para correo e identificación de usuarios: permite buscar por esos campos sin descifrar la tabla.

## Requisitos para desarrollo

Para ejecutar la entrega basta Docker. Para desarrollar fuera de contenedores:

| Herramienta | Versión usada |
|---|---|
| Docker Desktop | 29.9 (Compose v5.7) |
| .NET SDK | 8.0 |
| Node.js | 22 |
| dotnet-ef | 9.0 |

Las instrucciones de cada proyecto están en su README.
