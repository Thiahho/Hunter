📘 05 — Modelo de Dominio y Base de Datos — MVP V1

Producto: DIFRANI | Hunter CRM AI
Versión: 1.0
Estado: Diseño inicial
Objetivo: Definir las entidades, relaciones y reglas de negocio necesarias para implementar el MVP.

1. Objetivo

Definir el modelo de dominio que representa el funcionamiento comercial de DIFRANI | Hunter CRM AI durante la V1.

El modelo debe cubrir el flujo:

ORGANIZACIÓN
      ↓
PROSPECTO
      ↓
CAMPAÑA
      ↓
CONTACTO
      ↓
RESPUESTA
      ↓
INTERÉS
      ↓
LEAD
      ↓
GESTIÓN HUMANA
      ↓
RESULTADO

El diseño debe ser:

Multiempresa.
Modular.
Extensible.
Compatible con EF Core.
Compatible con PostgreSQL.
Preparado para futuras versiones.
2. Principios del Modelo
2.1 Multi-Tenant

Todas las entidades relacionadas con una empresa deberán estar aisladas mediante OrganizationId.

Organization A
    │
    ├── Prospects
    ├── Campaigns
    ├── Leads
    └── Users

Organization B
    │
    ├── Prospects
    ├── Campaigns
    ├── Leads
    └── Users

Nunca se deberá asumir que el sistema pertenece exclusivamente a Difrani.

2.2 Separación Prospect / Lead

Esta separación es fundamental.

Prospect

Es una empresa o contacto potencial descubierto por Hunter.

Todavía no demostró interés.

Lead

Es un prospecto que realizó una acción que demuestra interés comercial y requiere intervención humana.

Prospect
   ↓
Contacto
   ↓
Respuesta
   ↓
Interés
   ↓
Lead

Un prospecto puede existir sin convertirse en Lead.

3. Entidades Principales

El MVP tendrá las siguientes entidades:

Organization
User
Role
Prospect
ProspectContact
ProspectSource
ProspectTag
Campaign
CampaignRecipient
Message
MessageTemplate
Lead
Interaction
LeadActivity
Channel
Tag
OrganizationSettings
AuditLog
4. Organization

Representa una empresa que utiliza Hunter.

Ejemplo:

Difrani

Pero el sistema también podrá tener:

Empresa A
Empresa B
Empresa C
Campos principales
Id
Name
LegalName
TaxId
Email
Phone
Country
Timezone
Status
CreatedAt
UpdatedAt
Responsabilidad

Es la raíz del aislamiento multi-tenant.

5. User

Representa un usuario de la plataforma.

Puede ser:

Administrador.
Vendedor.
Supervisor.
Campos
Id
OrganizationId
Name
Email
PasswordHash
Status
CreatedAt
UpdatedAt

Relación:

Organization
    │
    └── Users
6. Role

Define los permisos del usuario.

Roles iniciales:

OWNER
ADMIN
MANAGER
SELLER

Para el MVP no se requiere un sistema extremadamente complejo de permisos.

Debe permitir:

OWNER / ADMIN
    ↓
Configuración
Usuarios
Campañas
Prospectos
Leads

SELLER
    ↓
Leads asignados
Prospectos
Conversaciones
Seguimiento
7. Prospect

Es la entidad central del Prospect Pool.

Representa una oportunidad comercial potencial.

Campos
Id
OrganizationId

BusinessName
BusinessType

Address
City
State
Country
PostalCode

Website

Status

Score

CreatedAt
UpdatedAt
LastContactedAt
Estado
DISCOVERED
VALIDATED
READY
CONTACTED
RESPONDED
INTERESTED
CONVERTED
LOST

La V1 puede comenzar con un conjunto reducido:

DISCOVERED
READY
CONTACTED
RESPONDED
INTERESTED
CONVERTED
LOST
8. ProspectContact

Un prospecto puede tener múltiples formas de contacto.

Por eso no recomiendo guardar todo directamente en Prospect.

Ejemplo:

Prospect
   │
   ├── WhatsApp
   ├── Teléfono
   ├── Email
   └── Instagram
Campos
Id
ProspectId

Channel
Value

IsPrimary
IsVerified
IsActive

CreatedAt
UpdatedAt

Ejemplo:

WhatsApp
+5491122334455

Esto permitirá posteriormente agregar nuevos canales sin modificar Prospect.

9. ProspectSource

Indica de dónde provino el prospecto.

Ejemplos:

GOOGLE_PLACES
OPEN_STREET_MAP
DIRECTORY
PUBLIC_WEBSITE
CSV_IMPORT
MANUAL
Campos
Id
ProspectId

SourceType
ExternalId
SourceUrl

CreatedAt

Un prospecto puede tener varias fuentes.

Ejemplo:

Google Places
+
Web pública
+
Carga manual

Esto también ayuda a detectar duplicados.

10. ProspectTag

Permite clasificar prospectos.

Ejemplo:

Taller
Distribuidora
Casa de Repuestos
Mayorista
Buenos Aires
Alta Prioridad

Relación:

Prospect
    │
    ├── Tag
    ├── Tag
    └── Tag
11. Tag

Entidad reutilizable.

Id
OrganizationId
Name
Color
CreatedAt

Ejemplo:

TALLER
DISTRIBUIDOR
CASA_REPUESTOS
ALTA_PRIORIDAD
12. Campaign

Representa una campaña de prospección.

Ejemplo:

Campaña:
Distribuidores Buenos Aires
Campos
Id
OrganizationId

Name
Description

Status

Channel

MessageTemplateId

StartedAt
FinishedAt

CreatedAt
UpdatedAt

Estados:

DRAFT
READY
RUNNING
PAUSED
COMPLETED
CANCELLED
13. CampaignRecipient

Representa la participación de un prospecto en una campaña específica.

Es necesaria porque un mismo prospecto puede participar en varias campañas.

Prospect
    │
    ├── Campaign A
    ├── Campaign B
    └── Campaign C
Campos
Id
CampaignId
ProspectId

Status

SentAt
RespondedAt

CreatedAt
UpdatedAt

Estados:

PENDING
SENT
DELIVERED
RESPONDED
FAILED
SKIPPED
14. Message

Representa un mensaje enviado o recibido.

Campos
Id

OrganizationId
ProspectId
CampaignId
CampaignRecipientId

Channel

Direction

Content

ExternalMessageId

Status

SentAt
ReceivedAt
CreatedAt

Dirección:

OUTBOUND
INBOUND

Esto permite registrar:

Bot → Prospecto

Prospecto → Bot

Vendedor → Prospecto

Prospecto → Vendedor
15. MessageTemplate

Plantillas reutilizables.

Ejemplo:

Hola, ¿cómo va?

Somos [EMPRESA].
Estamos buscando ampliar nuestra red comercial.

¿Trabajan con distribución de repuestos?
Campos
Id
OrganizationId

Name
Content

Channel

IsActive

CreatedAt
UpdatedAt

La V1 utilizará mensajes simples.

Posteriormente podremos agregar variables:

{{business_name}}

{{city}}

{{seller_name}}
16. Interaction

Representa una interacción comercial.

Puede ser:

Mensaje
Respuesta
Cambio de estado
Nota
Llamada
Campos
Id
OrganizationId
ProspectId
LeadId

Type

Description

CreatedByUserId

CreatedAt

Esto permite construir una línea de tiempo.

10:00
Prospecto descubierto

10:05
Mensaje enviado

10:15
Respuesta recibida

10:16
IA detecta interés

10:16
Lead creado

10:20
Vendedor toma el lead
17. Lead

Representa una oportunidad comercial que requiere intervención humana.

Campos
Id

OrganizationId
ProspectId

AssignedToUserId

Status

SourceCampaignId

CreatedAt
UpdatedAt

FirstContactAt
LastInteractionAt
ClosedAt

Estados:

NEW
ASSIGNED
IN_PROGRESS
QUALIFIED
QUOTED
WON
LOST

Para el MVP podemos comenzar con:

NEW
IN_PROGRESS
WON
LOST
18. LeadActivity

Registra las acciones realizadas por el vendedor.

Ejemplos:

Llamada
Mensaje
Cotización
Seguimiento
Nota
Campos
Id
LeadId
UserId

Type
Description

CreatedAt

Esto permitirá conocer qué ocurrió después de que la IA entregó el lead al humano.

19. Channel

Representa el canal de comunicación.

Inicialmente:

WHATSAPP

Posteriormente:

EMAIL
TELEGRAM
SMS
INSTAGRAM
FACEBOOK

El canal debe ser una abstracción del sistema.

Esto permitirá cambiar proveedores sin modificar el dominio.

20. OrganizationSettings

Configuraciones específicas de cada empresa.

Ejemplos:

Nombre comercial
Mensaje inicial
Horarios de contacto
Canales habilitados
Configuración de IA
Reglas de campañas
Campos
Id
OrganizationId

Key
Value

CreatedAt
UpdatedAt

Para configuraciones más complejas, posteriormente podemos utilizar JSONB en PostgreSQL.

21. AuditLog

Registra acciones importantes.

Ejemplos:

Prospecto creado
Prospecto modificado
Lead asignado
Campaña iniciada
Campaña pausada
Usuario creado
Campos
Id
OrganizationId

UserId

EntityType
EntityId

Action

OldValue
NewValue

CreatedAt
22. Relaciones

El modelo general:

Organization
│
├── Users
│
├── Settings
│
├── Tags
│
├── Prospects
│   │
│   ├── Contacts
│   ├── Sources
│   ├── Tags
│   └── Interactions
│
├── Campaigns
│   │
│   ├── Recipients
│   │      │
│   │      └── Messages
│   │
│   └── Templates
│
├── Leads
│   │
│   ├── Activities
│   └── Interactions
│
└── AuditLogs
23. Flujo de Datos
                    SOURCE
                      │
                      ▼
                 Prospect
                      │
                      ▼
               VALIDATION
                      │
                      ▼
                   READY
                      │
                      ▼
                  CAMPAIGN
                      │
                      ▼
             CampaignRecipient
                      │
                      ▼
                 Message
                      │
                      ▼
                 Response
                      │
                      ▼
              Interest Detector
                      │
                      ▼
                    Lead
                      │
                      ▼
                Human Handoff
                      │
                      ▼
                 LeadActivity
                      │
                      ▼
                  WON / LOST
24. Reglas de Negocio
Regla 1

Un prospecto pertenece a una única organización.

Regla 2

Un prospecto puede tener múltiples contactos.

Regla 3

Un prospecto puede participar en múltiples campañas.

Regla 4

Un prospecto no debe duplicarse dentro de una organización.

Regla 5

Un Lead siempre debe estar relacionado con un Prospect.

Regla 6

Un Lead debe pertenecer a la misma organización que su Prospect.

Regla 7

Un mensaje debe estar asociado a una organización.

Regla 8

Las campañas no pueden utilizar prospectos de otra organización.

Regla 9

Toda modificación crítica debe generar auditoría.

25. Deduplicación

La deduplicación será uno de los componentes más importantes del Prospect Factory.

La coincidencia podrá evaluarse mediante:

1. Teléfono
2. WhatsApp
3. ExternalId
4. Sitio web
5. Nombre + dirección
6. Nombre + ciudad

Ejemplo:

Google Places

Repuestos López
Av. Siempre Viva 123

+

Web

Repuestos López
Av. Siempre Viva 123

↓

Mismo Prospect

No se crearán dos registros.

26. Multi-Tenancy

La V1 utilizará un modelo:

Shared Database + OrganizationId

Ejemplo:

Prospects

Id | OrganizationId | Name
--------------------------------
1  | ORG-A          | Empresa A
2  | ORG-A          | Empresa B
3  | ORG-B          | Empresa C

EF Core utilizará filtros globales para garantizar aislamiento.

OrganizationId

Será obligatorio en las entidades tenant-aware.

27. Preparación para V2

El modelo debe poder evolucionar hacia:

AIConversation
AIMessage
LeadScore
ScoreFactor
AutomationRule
FollowUp
Integration
Provider

Estas entidades no forman parte del MVP inicial.

28. Modelo conceptual final
                    ORGANIZATION
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
     PROSPECT          CAMPAIGN          USER
        │                │
        │                ▼
        │         CAMPAIGN RECIPIENT
        │                │
        │                ▼
        │             MESSAGE
        │
        ├── CONTACT
        ├── SOURCE
        ├── TAG
        └── INTERACTION
                 │
                 ▼
                LEAD
                 │
                 ▼
           LEAD ACTIVITY
29. Decisión técnica recomendada

Para la V1 recomiendo no crear microservicios ni separar bases de datos por organización.

La arquitectura será:

ASP.NET Core
      │
      ▼
Modular Monolith
      │
      ▼
PostgreSQL
      │
      ▼
OrganizationId
      │
      ▼
EF Core Global Query Filters

Esto mantiene el desarrollo simple y permite escalar posteriormente.

30. Próximo documento

Con este modelo ya podemos pasar al documento:

06 — Modelo de Base de Datos Detallado

Ahí definiremos concretamente:

Tablas PostgreSQL.
Columnas.
Tipos de datos.
PK.
FK.
Índices.
Unique Constraints.
UUID vs BIGINT.
JSONB.
Enums.
Relaciones.
Índices para búsquedas.
Índices para deduplicación.
Estrategia de auditoría.
Configuración de EF Core.
Convenciones de nombres.