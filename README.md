# Prueba técnica .NET Core – Angular (Viamatica)

Sistema de gestión de caja y turnos para una empresa que brinda servicios de internet.

**Autor:** Bryan Dueñas

> Estado: en desarrollo. Este README se actualiza conforme avanza la prueba.

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| [Backend/](Backend/) | API REST en .NET 8 con arquitectura Onion |
| [Frontend/](Frontend/) | Aplicación Angular con Angular Material |
| [AI/](AI/) | Prompts y skills utilizados con agentes de IA durante el desarrollo |

## Stack definido

| Pieza | Tecnología |
|---|---|
| Backend | .NET 8 Web API |
| Arquitectura | Onion (Domain, Application, Infrastructure, Api) |
| Acceso a datos | Entity Framework Core |
| Base de datos | PostgreSQL |
| Seguridad | JWT con claims encriptadas |
| Logs | Serilog |
| Documentación de la API | Swagger |
| Frontend | Angular con módulos y lazy loading |
| UI | Angular Material |
| Contenedores | Docker y docker-compose |

## Cómo ejecutar

Las instrucciones de ejecución con Docker se agregan al completar la dockerización.
Cada proyecto tiene su propio README con el detalle.
