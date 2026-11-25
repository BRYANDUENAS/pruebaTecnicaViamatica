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

## Requisitos del entorno

| Herramienta | Versión usada | Para qué |
|---|---|---|
| Docker Desktop | 29.9 (Compose v5.7) | Ejecutar la base de datos, la API y el frontend |
| .NET SDK | 8.0 | Compilar el backend fuera de Docker |
| Node.js | 22 | Compilar el frontend fuera de Docker |
| dotnet-ef | 9.0 | Generar migraciones |

Para ejecutar la entrega solo se necesita Docker; el resto es para desarrollo.

En desarrollo, la base de datos corre en un contenedor en el puerto `5544`:

```bash
docker run -d --name viamatica-dev-db -e POSTGRES_USER=viamatica -e POSTGRES_PASSWORD=viamatica_dev -e POSTGRES_DB=viamatica -p 5544:5432 postgres:16-alpine
```

## Cómo ejecutar

Las instrucciones de ejecución con Docker se agregan al completar la dockerización.
Cada proyecto tiene su propio README con el detalle.
