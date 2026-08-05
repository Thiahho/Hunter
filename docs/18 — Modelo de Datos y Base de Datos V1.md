📘 18 — Modelo de Datos y Base de Datos V1

Producto: DIFRANI | Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Objetivo: Definir la estructura de datos necesaria para soportar prospectos, campañas, mensajería, IA, leads, ventas y métricas.

1. Objetivo

La base de datos debe soportar el flujo:

PROSPECTO
    ↓
CAMPAÑA
    ↓
CONTACTO
    ↓
MENSAJE
    ↓
RESPUESTA
    ↓
IA
    ↓
LEAD
    ↓
HUMANO
    ↓
VENTA

Además:

COSTOS
MÉTRICAS
AUDITORÍA
SUPPRESSION LIST

La arquitectura debe permitir que la V1 funcione inicialmente con Difrani, pero sin diseñar la base de datos exclusivamente para una única empresa.

2. Arquitectura Multi-Tenant

La entidad central será:

Organization

Ejemplo:

Difrani

Posteriormente:

Tauro Parts

Todas las entidades comerciales deberán estar vinculadas a:

OrganizationId

Estructura:

Organization
    │
    ├── Users
    ├── Prospects
    ├── Campaigns
    ├── Templates
    ├── Leads
    ├── Sales
    ├── Costs
    └── Metrics
3. Entidades Principales

La V1 tendrá:

Organization
User
Role

Prospect
ProspectSource
ImportBatch

Campaign
CampaignRecipient
Template

Message
MessageResponse

Lead
LeadActivity
FollowUp

Sale

Suppression

Cost

AuditLog
4. Organization

Representa una empresa que utiliza Hunter.

Campos
Id
Name
LegalName
TaxId
Phone
Email
Address
City
Province
Country
IsActive
CreatedAt
UpdatedAt
Ejemplo
Id:
UUID

Name:
Difrani

Country:
Argentina
5. User

Representa un usuario del sistema.

Campos
Id
OrganizationId
FirstName
LastName
Email
Phone
PasswordHash
IsActive
CreatedAt
UpdatedAt

Relación:

Organization
    │
    └── 1:N Users
6. Role

Roles iniciales:

ADMIN
MANAGER
SELLER

Se recomienda inicialmente manejar roles mediante:

UserRole
UserRole
UserId
RoleId

Relación:

User
    │
    └── N:N Role
7. Prospect

Representa una empresa o persona potencialmente compradora.

Campos
Id
OrganizationId

Name
BusinessName

Phone
WhatsApp
Email

Address
City
Province
Country

Latitude
Longitude

Category
BusinessSize
LocationType
RecurrencePotential

Source
SourceUrl
ExternalId

Status

CreatedAt
UpdatedAt
8. Prospect — Categoría

La categoría debe ser flexible.

Ejemplos:

DISTRIBUTOR
AUTO_PARTS_STORE
WORKSHOP
MECHANIC
LUBRICENTER
TIRE_SHOP
RETAILER
OTHER

Se recomienda utilizar un campo:

Category

y posteriormente migrar a una tabla configurable si se requiere mayor flexibilidad.

9. Prospect — Tamaño
SMALL
MEDIUM
LARGE
UNKNOWN

Importante:

La V1 no debe descartar automáticamente prospectos por tamaño.

El objetivo comercial es:

MAXIMIZAR VENTAS

Por lo tanto:

Small
Medium
Large

todos pueden entrar en campañas.

10. Prospect — Potencial de Recurrencia
HIGH
MEDIUM
LOW
UNKNOWN

Este campo inicialmente será:

Manual

o:

Inferido

No deberá impedir el contacto.

11. Prospect Status
NEW
VALIDATED
READY
CONTACTED
RESPONDED
LEAD
CUSTOMER
SUPPRESSED
INVALID
12. ProspectSource

Permite conocer de dónde proviene el prospecto.

Campos
Id
OrganizationId

Name
Type
Url

CreatedAt

Tipos:

GOOGLE_PLACES
MANUAL
CSV
EXTERNAL_API
PUBLIC_DIRECTORY
OTHER
13. ImportBatch

Representa una importación masiva.

Campos
Id
OrganizationId

FileName
SourceId

TotalRecords
ValidRecords
DuplicateRecords
InvalidRecords
SuppressedRecords

Status

CreatedBy
CreatedAt
CompletedAt
14. ImportBatch Status
PROCESSING
PREVIEW
CONFIRMED
COMPLETED
FAILED
CANCELLED
15. ImportBatchRecord

Para poder auditar cada fila importada.

Campos
Id
ImportBatchId

RowNumber

RawData
NormalizedData

Status
ErrorMessage

ProspectId

Estados:

VALID
DUPLICATE
INVALID
SUPPRESSED
IMPORTED
16. Campaign

Representa una campaña comercial.

Campos
Id
OrganizationId

Name
Description

Channel

TemplateId

Status

MaxMessages
MessagesPerMinute
MessagesPerHour
MessagesPerDay

StartDate
EndDate

CreatedBy

CreatedAt
UpdatedAt
17. Campaign Status
DRAFT
READY
RUNNING
PAUSED
COMPLETED
CANCELLED
18. CampaignRecipient

Esta entidad es fundamental.

Representa la relación entre:

Campaign

y:

Prospect
Campos
Id

CampaignId
ProspectId

Status

Attempts
LastAttemptAt

FirstMessageId
LastMessageId

CreatedAt
UpdatedAt
19. CampaignRecipient Status
PENDING
QUEUED
SENT
RESPONDED
INTERESTED
NOT_INTERESTED
STOPPED
FAILED
COMPLETED

Esto permitirá medir cada campaña individualmente.

20. Template

Representa una plantilla de mensaje.

Campos
Id
OrganizationId

Name
Content

Channel

Version

IsActive

CreatedBy
CreatedAt
UpdatedAt
21. Template Variables

Ejemplo:

{{business_name}}

{{city}}

{{category}}

{{sender_name}}

La plantilla no debe contener lógica comercial.

La lógica se ejecutará en el sistema.

22. Message

Representa cada mensaje enviado.

Campos
Id

OrganizationId

CampaignId
CampaignRecipientId
ProspectId

TemplateId

Channel
Provider

Content

ExternalMessageId

Status

SentAt
DeliveredAt
ReadAt
FailedAt

Cost

CreatedAt
23. Message Status
PENDING
SENT
DELIVERED
READ
FAILED
CANCELLED
24. MessageResponse

Representa la respuesta recibida.

Campos
Id

OrganizationId

ProspectId
CampaignId
MessageId

Content

ReceivedAt

Classification
Confidence

ProcessedAt
25. Message Classification
INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
26. IA Classification

La IA debe devolver:

Classification
Confidence

Ejemplo:

Classification:
INTERESTED

Confidence:
0.94

La respuesta original debe conservarse:

Content

Nunca se debe almacenar únicamente la interpretación de la IA.

27. AI Processing

Para mantener trazabilidad, se recomienda agregar:

AIModel
AIPromptVersion
AIResponse
ProcessedAt

Esto permitirá saber:

¿Qué modelo clasificó?
¿Qué versión del prompt utilizó?
¿Qué respondió?

Puede implementarse inicialmente dentro de MessageResponse.

28. Lead

Representa una oportunidad comercial.

Campos
Id

OrganizationId

ProspectId
CampaignId

AssignedUserId

Status
Priority

Source

CreatedAt
FirstResponseAt
LastActivityAt
ClosedAt
29. Lead Status
NEW
IN_PROGRESS
WON
LOST
30. Lead Priority
HIGH
MEDIUM
LOW
31. LeadActivity

Representa cualquier interacción comercial humana.

Campos
Id

LeadId
UserId

Type

Description

CreatedAt

Tipos:

CONTACT
CALL
WHATSAPP
QUOTE
NOTE
FOLLOW_UP
32. FollowUp

Representa una tarea futura.

Campos
Id

LeadId
UserId

ScheduledAt

Status

Notes

CompletedAt
CreatedAt

Estados:

PENDING
COMPLETED
CANCELLED
33. Sale

Representa una venta cerrada.

Campos
Id

OrganizationId

LeadId
CampaignId
ProspectId
SellerId

Amount
Currency

Margin

ProductCategory

Date

CreatedAt
34. Sale Status

Se recomienda agregar:

WON
CANCELLED

Aunque el Lead ya tenga:

WON

Esto permite diferenciar:

Lead ganado

de:

Venta posteriormente cancelada
35. LostReason

Para oportunidades perdidas:

PRICE
NO_STOCK
EXISTING_SUPPLIER
NO_RESPONSE
NOT_INTERESTED
LOGISTICS
COMMERCIAL_CONDITIONS
OTHER

Puede estar en:

Lead

como:

LostReason
36. Suppression

Representa un contacto que no debe volver a ser contactado.

Campos
Id

OrganizationId

Contact
ContactType

Reason

Source

CreatedAt
37. Suppression Reason
USER_REQUESTED
INVALID_NUMBER
BLOCKED
MANUAL
OTHER
38. Regla de Supresión

Antes de enviar:

Prospect
   ↓
Get Contact
   ↓
Check Suppression
   ↓
¿Existe?

Si:

YES

Entonces:

NO ENVIAR
39. Cost

Registra los costos generados por el sistema.

Campos
Id

OrganizationId

CampaignId

Type
Provider

ReferenceId

Amount
Currency

Date
CreatedAt
40. Cost Types
MESSAGING
AI
PROSPECTING
INFRASTRUCTURE
OTHER
41. AuditLog

Registra acciones importantes.

Campos
Id

OrganizationId

UserId

Action

EntityType
EntityId

OldValues
NewValues

CreatedAt
42. Audit Actions
LOGIN

CAMPAIGN_CREATED
CAMPAIGN_STARTED
CAMPAIGN_PAUSED
CAMPAIGN_CANCELLED

MESSAGE_SENT

LEAD_CREATED
LEAD_ASSIGNED
LEAD_UPDATED

SALE_CREATED

OPT_OUT
SUPPRESSION_CREATED
43. Relaciones

Modelo principal:

Organization
│
├── Users
│
├── Prospects
│   ├── ProspectSource
│   ├── CampaignRecipient
│   ├── Messages
│   ├── MessageResponses
│   ├── Leads
│   └── Sales
│
├── Campaigns
│   ├── CampaignRecipients
│   ├── Messages
│   ├── Leads
│   ├── Sales
│   └── Costs
│
├── Templates
│
├── Suppressions
│
├── Costs
│
└── AuditLogs
44. Flujo Relacional
Organization
     │
     ▼
Prospect
     │
     ▼
CampaignRecipient
     │
     ▼
Message
     │
     ▼
MessageResponse
     │
     ▼
Lead
     │
     ├── LeadActivity
     │
     ├── FollowUp
     │
     ▼
Sale
45. Índices Críticos
Prospect
INDEX OrganizationId
INDEX Phone
INDEX WhatsApp
INDEX Email
INDEX ExternalId
INDEX Status
CampaignRecipient
INDEX CampaignId
INDEX ProspectId
INDEX Status

Restricción:

UNIQUE
CampaignId + ProspectId

Esto evita agregar dos veces el mismo prospecto a la misma campaña.

Message
INDEX CampaignId
INDEX ProspectId
INDEX Status
INDEX ExternalMessageId
Lead
INDEX OrganizationId
INDEX ProspectId
INDEX AssignedUserId
INDEX Status
Suppression
INDEX OrganizationId
INDEX Contact

Recomendado:

UNIQUE
OrganizationId + Contact
46. Reglas de Integridad
Regla 1

Un prospecto pertenece a una organización.

Prospect.OrganizationId
NOT NULL
Regla 2

Una campaña pertenece a una organización.

Campaign.OrganizationId
NOT NULL
Regla 3

Un prospecto no puede ser enviado si está suprimido.

Suppression Check
Regla 4

Un Lead debe estar vinculado a un prospecto.

Lead.ProspectId
NOT NULL
Regla 5

Una venta debe estar asociada a un Lead.

Sale.LeadId
NOT NULL
47. Soft Delete

No se recomienda eliminar físicamente entidades comerciales importantes.

Aplicar:

IsDeleted
DeletedAt

a:

Prospect
Campaign
Template
Lead

Las entidades históricas:

Message
MessageResponse
Sale
AuditLog

no deberían eliminarse normalmente.

48. Auditoría Temporal

Las entidades principales tendrán:

CreatedAt
UpdatedAt

Opcional:

CreatedBy
UpdatedBy
49. Multi-Tenant Query Filter

En EF Core:

OrganizationId

será utilizado para aislar datos.

Conceptualmente:

WHERE OrganizationId = CurrentOrganizationId

Esto debe aplicarse globalmente.

50. Seguridad Multi-Tenant

Un usuario de:

Difrani

no podrá acceder a:

Tauro Parts

aunque conozca:

UUID

de la entidad.

La API debe validar:

CurrentUser.OrganizationId
==
Entity.OrganizationId
51. Diagrama Simplificado
┌──────────────┐
│ Organization │
└──────┬───────┘
       │
       ├───────────────┐
       ▼               ▼
┌────────────┐   ┌───────────┐
│  Prospect  │   │ Campaign  │
└─────┬──────┘   └─────┬─────┘
      │                │
      └───────┬────────┘
              ▼
      ┌───────────────┐
      │CampaignRecipient│
      └───────┬───────┘
              ▼
         ┌─────────┐
         │ Message │
         └────┬────┘
              ▼
     ┌────────────────┐
     │ MessageResponse│
     └───────┬────────┘
             ▼
          ┌──────┐
          │ Lead │
          └──┬───┘
             │
       ┌─────┴──────┐
       ▼            ▼
┌────────────┐ ┌──────────┐
│ LeadActivity│ │ FollowUp │
└────────────┘ └──────────┘
             │
             ▼
          ┌──────┐
          │ Sale │
          └──────┘
52. Orden de Implementación

El desarrollo de la base de datos debe realizarse en este orden:

Fase 1
Organization
User
Role
Fase 2
Prospect
ProspectSource
ImportBatch
ImportBatchRecord
Fase 3
Campaign
CampaignRecipient
Template
Fase 4
Message
MessageResponse
Fase 5
Lead
LeadActivity
FollowUp
Fase 6
Sale
Fase 7
Suppression
Cost
AuditLog
53. Primera Migración

La primera migración debería contener únicamente la infraestructura mínima:

Organizations
Users
Roles
UserRoles

Prospects
ProspectSources

Campaigns
CampaignRecipients

Templates

Messages
MessageResponses

Leads
LeadActivities
FollowUps

Sales

Suppressions
Costs
AuditLogs
54. Recomendación para EF Core

Las entidades deben separarse por módulos:

Domain
│
├── Organizations
├── Identity
├── Prospects
├── Campaigns
├── Messaging
├── Leads
├── Sales
├── Costs
└── Auditing

Y cada módulo debería tener su configuración:

EntityTypeConfiguration

Ejemplo:

ProspectConfiguration
CampaignConfiguration
MessageConfiguration
LeadConfiguration

Esto evitará tener toda la configuración de EF Core concentrada en DbContext.

55. DbContext

Conceptualmente:

public class HunterDbContext : DbContext
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }

    public DbSet<Prospect> Prospects { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }

    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageResponse> MessageResponses { get; set; }

    public DbSet<Lead> Leads { get; set; }
    public DbSet<Sale> Sales { get; set; }

    public DbSet<Suppression> Suppressions { get; set; }
    public DbSet<Cost> Costs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
}

La implementación definitiva deberá incluir:

Global Query Filters
Indexes
Foreign Keys
DeleteBehavior
Value Converters
Enums
56. Decisión Importante: No Sobrediseñar la V1

La base de datos debe permitir evolucionar hacia:

V2
V3

pero no debemos construir desde el principio:

Machine Learning
Scoring complejo
Predicción
CRM Enterprise
ERP

La prioridad es:

Prospecto
↓
Campaña
↓
Mensaje
↓
Respuesta
↓
IA
↓
Lead
↓
Humano
↓
Venta
57. Resultado

Con este modelo, Hunter V1 tendrá una estructura suficientemente sólida para:

✓ Trabajar inicialmente con Difrani
✓ Incorporar Tauro Parts posteriormente
✓ Soportar múltiples organizaciones
✓ Importar grandes volúmenes de prospectos
✓ Ejecutar campañas
✓ Controlar mensajes
✓ Detectar interés
✓ Derivar Leads
✓ Medir ventas
✓ Calcular costos
✓ Auditar operaciones