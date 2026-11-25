# Uso de IA en el desarrollo

Esta carpeta registra los prompts y skills utilizados con agentes de IA, tal como lo solicita la prueba.

## Herramienta

- **Agente:** Claude Code (extensión de VS Code), de Anthropic.

## Reparto del trabajo

| Autor | Agente |
|---|---|
| Lectura y análisis del enunciado | Contraste del plan y de los puntos obligatorios |
| Decisión del stack: .NET 8, PostgreSQL, Angular con Angular Material | Propuesta de alternativas y sus compromisos |
| Dirección del orden de trabajo y validación de cada etapa antes de continuar | Generación del código bajo esas decisiones |
| Definición de las convenciones del proyecto (skills) | Aplicación de las convenciones y pruebas de cada endpoint |
| Entorno, repositorio y comunicación de la entrega | Asistencia en la instalación de herramientas |

## Forma de trabajo

1. Con el enunciado ya analizado, se revisó con el agente el giro del negocio (gestión de caja y turnos de un proveedor de internet) para acordar el flujo: roles, aprobación de usuarios, uso de las cajas y ciclo de vida del contrato.
2. Se definieron el stack y la arquitectura Onion antes de escribir código.
3. Se diseñó el modelo de datos a partir del esquema de ejemplo, con los ajustes necesarios: eliminación lógica, menú por rol, cajero activo por caja y velocidad del servicio.
4. Se construyó la base del backend: seguridad, persistencia, funciones de PostgreSQL, login y usuarios.
5. Se escribieron las convenciones del proyecto como skills, que el agente aplica desde ese punto.
6. El desarrollo continúa por etapas, cada una probada y validada antes del commit.

## Skills

Skills propios del proyecto, en [skills/](skills/). Se definieron después de la base del backend y rigen el trabajo desde entonces.

| Skill | Para qué sirve |
|---|---|
| [backend-conventions](skills/backend-conventions/SKILL.md) | Reglas de capas, validaciones, eliminación lógica, seguridad y logs del backend |
| [frontend-conventions](skills/frontend-conventions/SKILL.md) | Estructura modular, guards, interceptores y lineamientos de diseño del frontend |

## Prompts

- [prompts.md](prompts.md): registro cronológico y resumido de los prompts enviados al agente.
