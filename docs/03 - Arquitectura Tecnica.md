📙 Documento 03 — Arquitectura Técnica

Versión: 1.0
Estado: Diseño Inicial
Objetivo: Definir la arquitectura técnica completa del producto.

Índice
1. Objetivo

2. Principios de Arquitectura

3. Arquitectura General

4. Módulos

5. Backend

6. Frontend

7. Base de Datos

8. Automatización (n8n)

9. IA

10. Integraciones

11. Seguridad

12. Escalabilidad

13. Observabilidad

14. Infraestructura

15. Roadmap Técnico
1. Objetivo

Definir una arquitectura escalable, modular y mantenible que permita evolucionar Hunter desde un MVP hasta una plataforma SaaS multiempresa.

La arquitectura debe permitir:

incorporar nuevos módulos sin modificar el núcleo.
soportar múltiples empresas.
integrar distintos canales.
cambiar proveedores (WhatsApp, IA, OCR, etc.) sin reescribir el sistema.
2. Principios de Arquitectura

Toda decisión técnica deberá respetar estos principios.

Modular

Cada módulo tiene una responsabilidad.

API First

Todo debe poder consumirse mediante API.

Multi Tenant

Una única plataforma.

Muchas empresas.

Event Driven (cuando tenga sentido)

Las acciones importantes generan eventos.

Ejemplo.

Lead generado

↓

Notificar vendedor

↓

Actualizar Dashboard

↓

Registrar Analytics
Clean Architecture

Separación clara entre.

Dominio

Aplicación

Infraestructura

Presentación
Provider Agnostic

Nunca depender de un proveedor.

Ejemplo.

No depender únicamente de:

WhatsApp
OpenAI
Google Places

Siempre habrá una interfaz.

3. Arquitectura General
                    React

                      │

               ASP.NET Core API

                      │

      ┌───────────────┼────────────────┐

      │               │                │

 PostgreSQL        n8n Engine      AI Services

      │               │                │

      └───────────────┼────────────────┘

                      │

                 Integraciones
4. Módulos
Core

Responsabilidad

Todo lo común.

autenticación
empresas
usuarios
permisos
configuración
Prospect Factory

Descubrir empresas.

Prospect Pool

Administrar prospectos.

Campaign Engine

Campañas.

CRM

Leads.

Ventas.

Seguimiento.

Analytics

Métricas.

AI

Clasificación.

Integrations

WhatsApp.

Google.

Telegram.

Email.

5. Backend

Tecnología

ASP.NET Core 9

Entity Framework Core

PostgreSQL

JWT

FluentValidation

AutoMapper

MediatR (opcional)

Serilog

Arquitectura.

API

↓

Application

↓

Domain

↓

Infrastructure
6. Frontend
React

Vite

TypeScript

Tailwind

React Query

Zustand

Axios

Organización.

Pages

Components

Layouts

Modules

Hooks

Services

Store
7. Base de Datos

Inicialmente PostgreSQL.

Esquema.

Organizations

Users

Roles

Prospects

Campaigns

Messages

Leads

Interactions

Tags

Settings

AuditLogs

No crear tablas específicas para Difrani.

8. Automatización

n8n.

No contendrá lógica de negocio.

Solo orquestación.

Ejemplo.

Google Places

↓

Normalizar

↓

API Hunter

↓

Guardar

Nunca.

Google Places

↓

Guardar directo BD

Toda escritura pasa por la API.

9. IA

La IA será un servicio.

Nunca mezclada con la lógica.

Application

↓

IA Interface

↓

OpenAI

↓

Respuesta

Mañana podría cambiarse por.

Claude.

Gemini.

Llama.

Sin modificar la aplicación.

10. Integraciones

Cada integración será independiente.

Ejemplo.

Interfaces

↓

WhatsApp Adapter

↓

Telegram Adapter

↓

Email Adapter

↓

SMS Adapter

No habrá código acoplado.

11. Seguridad

JWT.

Refresh Tokens.

Permisos.

Multiempresa.

Rate Limit.

Logs.

Auditoría.

12. Escalabilidad

Preparado para.

Millones de prospectos.

Miles de campañas.

Cientos de empresas.

13. Observabilidad

Logs.

Serilog.

Health Checks.

Métricas.

Trazabilidad.

14. Infraestructura

Inicialmente.

Docker.

PostgreSQL.

n8n.

ASP.NET.

React.

Más adelante.

Redis.

RabbitMQ.

Elastic.

Prometheus.

15. Roadmap Técnico
MVP

Monolito modular.

V2

Separar.

Workers.

Queue.

Cache.

V3

Microservicios si realmente son necesarios.

No antes.

Arquitectura física

Yo iría con un Monolito Modular, no con microservicios.

                     Hunter

                React Frontend

                       │

               ASP.NET Core API

                       │

       ┌───────────────┼───────────────┐

       │               │               │

 Prospect        Campaigns        CRM

       │               │               │

       └───────────────┼───────────────┘

                       │

                 PostgreSQL

¿Por qué?

Porque somos un equipo pequeño.

Los microservicios agregarían una complejidad innecesaria.

Organización del repositorio

Aquí quiero hacer una propuesta importante.

Hunter/

docs/

backend/

frontend/

automation/

docker/

database/

scripts/

research/

branding/

deployment/

Y dentro del backend.

src/

Hunter.Api

Hunter.Application

Hunter.Domain

Hunter.Infrastructure

Hunter.Shared

Hunter.Tests

Es prácticamente la misma arquitectura que vienes utilizando en tus proyectos con ASP.NET Core, por lo que aprovecharás experiencia y componentes existentes.

Una decisión estratégica

Hay una decisión que considero clave para el éxito del producto:

Toda automatización debe pasar por el Core.

Es decir:

Google Places
        │
        ▼
      n8n
        │
        ▼
 ASP.NET Core API
        │
        ▼
 PostgreSQL

Y nunca:

Google Places

↓

n8n

↓

PostgreSQL

¿Por qué?

Porque el Core debe ser el dueño absoluto del negocio.

Las validaciones, reglas, auditoría, deduplicación, permisos, multiempresa y eventos deben vivir en el Core, no en n8n.

Esto tiene varias ventajas:

Si en el futuro reemplazamos n8n por otra herramienta, el negocio no cambia.
Si desarrollamos una aplicación móvil o una API pública, reutilizamos exactamente las mismas reglas.
La lógica queda centralizada y es mucho más fácil de mantener y probar.

Creo que esta será una de las decisiones de arquitectura más importantes del proyecto y la que más impacto tendrá cuando Hunter evolucione de un MVP para Difrani a una plataforma SaaS para múltiples empresas.