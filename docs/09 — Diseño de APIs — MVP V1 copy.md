📗 07 — Backlog Técnico V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Construir una primera versión funcional antes de octubre de 2026, enfocada en generar prospectos, contactar potenciales clientes, detectar interés y transferir la oportunidad a una persona para cerrar la venta.

1. Priorización

Se utilizarán tres niveles:

P0 — Crítico

Necesario para que el MVP funcione.

P1 — Importante

Necesario para una experiencia comercial completa, pero puede desarrollarse después del núcleo.

P2 — Deseable

Puede incorporarse si el tiempo lo permite.

2. EPIC 01 — Setup del Proyecto
Objetivo

Crear la base técnica del sistema.

P0
Crear repositorio.
Configurar solución .NET.
Crear Hunter.Api.
Crear Hunter.Application.
Crear Hunter.Domain.
Crear Hunter.Infrastructure.
Crear Hunter.Shared.
Crear Hunter.Tests.
Configurar PostgreSQL.
Configurar EF Core.
Configurar Docker.
Configurar variables de entorno.
P1
Configurar Serilog.
Health Checks.
Swagger.
OpenAPI.
Criterio de aceptación

El proyecto debe ejecutar correctamente:

ASP.NET Core API
+
PostgreSQL
+
EF Core

mediante entorno local reproducible.

3. EPIC 02 — Core y Multi-Tenancy
Objetivo

Crear la base multiempresa.

P0

Crear:

Organization
User
Role
UserRole
OrganizationSettings

Implementar:

OrganizationId.
Contexto de organización.
Resolución desde usuario autenticado.
Query Filters.
Validación de acceso.
Criterio

Un usuario de Organización A nunca puede consultar datos de Organización B.

4. EPIC 03 — Autenticación
P0

Implementar:

Login.
Registro inicial de organización.
JWT.
Refresh Token.
Logout.
Password hashing.
P1
Recuperación de contraseña.
Cambio de contraseña.
Gestión de usuarios.
5. EPIC 04 — Prospect Factory

Este será uno de los módulos centrales.

Objetivo

Descubrir y registrar nuevos prospectos.

P0

Crear:

Prospect
ProspectContact
ProspectSource

Implementar:

Crear prospecto.
Actualizar prospecto.
Validar datos.
Normalizar información.
Registrar fuente.
Registrar contactos.
P1

Integrar:

Google Places.
OpenStreetMap.
Importación CSV.
P2
Enriquecimiento web.
Detección de redes sociales.
Validación avanzada de teléfonos.
6. EPIC 05 — Prospect Pool
Objetivo

Administrar todos los prospectos disponibles.

P0

Crear:

Listado.
Detalle.
Búsqueda.
Filtros.
Estados.
Etiquetas.

Filtros:

Ciudad
Provincia
Tipo
Estado
Score
Tag
Fuente
P1
Acciones masivas.
Importación CSV.
Exportación CSV.
Detección visual de duplicados.
7. EPIC 06 — Campaign Engine
Objetivo

Crear campañas de prospección.

P0

Crear:

Campaign
CampaignRecipient
MessageTemplate

Permitir:

Crear campaña.
Editar campaña.
Seleccionar prospectos.
Seleccionar plantilla.
Configurar canal.
Iniciar campaña.
Pausar campaña.
Finalizar campaña.

Estados:

DRAFT
READY
RUNNING
PAUSED
COMPLETED
CANCELLED
8. EPIC 07 — Messaging
Objetivo

Gestionar mensajes enviados y recibidos.

P0

Crear:

Message
Channel

Registrar:

Mensajes salientes.
Mensajes entrantes.
Estado.
Fecha.
Canal.
Identificador externo.
P1

Implementar adaptador de proveedor.

IMessageProvider

Ejemplo:

WhatsAppProvider

La aplicación no debe depender directamente del proveedor.

9. EPIC 08 — Interest Detector
Objetivo

Detectar cuándo un prospecto demuestra interés.

Ejemplo:

Bot:
Hola, ¿trabajan con distribución?

Prospecto:
Sí, pasame información.

Resultado:

INTERESTED
P0

Implementar:

Recepción de respuesta.
Clasificación.
Actualización del Prospect.
Registro de Interaction.
Creación de Lead.
P1

Integrar IA.

Mensaje
    ↓
IA
    ↓
Clasificación

Categorías iniciales:

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
10. EPIC 09 — Human Handoff
Objetivo

Transferir la oportunidad a una persona.

Flujo:

Prospecto
    ↓
Responde
    ↓
Interest Detector
    ↓
INTERESTED
    ↓
Lead
    ↓
Vendedor
P0
Crear Lead.
Asignar Lead.
Notificar vendedor.
Cambiar estado.
Registrar timestamp.
P1
Asignación automática.
Distribución round-robin.
Notificaciones avanzadas.
11. EPIC 10 — CRM de Leads
P0

Crear:

Lead
LeadActivity
Interaction

Funciones:

Listar Leads.
Ver detalle.
Asignar.
Cambiar estado.
Registrar actividad.
Agregar notas.

Estados:

NEW
IN_PROGRESS
WON
LOST
12. EPIC 11 — Dashboard
P0

Mostrar:

Prospectos encontrados
Prospectos contactados
Respuestas
Interesados
Leads
Ventas ganadas
Ventas perdidas
P1

Agregar métricas:

Tasa de respuesta
Tasa de interés
Conversión a Lead
Conversión a venta
13. EPIC 12 — Integraciones
P0

Crear arquitectura de adaptadores.

IProspectSource
IMessageProvider
IAiProvider

Esto permitirá reemplazar proveedores.

P1

Primera integración de descubrimiento:

Google Places

Primera integración de comunicación:

WhatsApp

Primera integración IA:

AI Provider
14. EPIC 13 — Automatización n8n
Objetivo

Utilizar n8n como orquestador.

P0

Workflow:

Trigger
   ↓
Obtener prospectos
   ↓
Validar
   ↓
Enviar API

Segundo flujo:

Webhook
   ↓
Recibir respuesta
   ↓
API Hunter
   ↓
Interest Detector
   ↓
Lead
15. EPIC 14 — Testing
P0

Unit Tests:

Prospect Service.
Deduplicación.
Campaign Service.
Lead Service.
Interest Detector.

Integration Tests:

PostgreSQL.
API.
Multi-tenancy.
Casos críticos
Usuario A
NO puede acceder
a datos de Usuario B.
Prospecto duplicado
NO genera nuevo registro.
Interés detectado
GENERA Lead.
16. EPIC 15 — Deploy
P0

Preparar:

Docker
PostgreSQL
API
Frontend
n8n

Configurar:

Variables de entorno.
HTTPS.
Backup.
Logs.
P1
CI/CD.
Staging.
Producción.
17. Backlog resumido
P0
│
├── Setup
├── Core
├── Multi-Tenancy
├── Auth
├── Prospects
├── Prospect Pool
├── Campaigns
├── Messaging
├── Interest Detector
├── Human Handoff
├── Leads
├── Testing
└── Deploy

P1
│
├── Google Places
├── WhatsApp Provider
├── IA
├── Dashboard avanzado
├── CSV
├── Automatización n8n
└── CI/CD

P2
│
├── Hunter Mobile
├── OCR
├── GPS
├── Scoring
└── Analytics avanzado
18. Orden recomendado de implementación

No recomiendo desarrollar los Epics en orden numérico estricto.

El orden real debería ser:

FASE 1 — CORE
    │
    ├── Setup
    ├── PostgreSQL
    ├── EF Core
    ├── Auth
    └── Multi-Tenancy

              ↓

FASE 2 — PROSPECTOS
    │
    ├── Prospect
    ├── Contacts
    ├── Sources
    ├── Deduplicación
    └── Prospect Pool

              ↓

FASE 3 — CAMPAÑAS
    │
    ├── Campaign
    ├── Recipients
    ├── Templates
    └── Messaging

              ↓

FASE 4 — RESPUESTAS
    │
    ├── Webhooks
    ├── Messages
    ├── Interactions
    └── Interest Detector

              ↓

FASE 5 — CRM
    │
    ├── Leads
    ├── Assignment
    ├── Human Handoff
    └── Lead Activities

              ↓

FASE 6 — AUTOMATIZACIÓN
    │
    ├── n8n
    ├── Fuentes
    ├── Comunicación
    └── IA

              ↓

FASE 7 — VALIDACIÓN
    │
    ├── Testing
    ├── Métricas
    └── Deploy
19. Criterio de finalización del MVP

La V1 estará técnicamente terminada cuando pueda ejecutar este flujo completo:

┌──────────────────────┐
│ Fuente de prospectos │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Prospect Factory     │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Prospect Pool        │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Campaign Engine      │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Mensaje              │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Respuesta            │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Interest Detector    │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Lead                 │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Humano               │
└──────────┬───────────┘
           ↓
┌──────────────────────┐
│ Venta                │
└──────────────────────┘

Ese es el MVP real.

Todo lo demás es secundario.