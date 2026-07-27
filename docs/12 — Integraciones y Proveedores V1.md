📘 12 — Integraciones y Proveedores V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Definir las integraciones externas necesarias para operar el MVP hasta octubre de 2026, priorizando bajo costo, velocidad de implementación y capacidad de validación comercial.

Nota: Los precios y condiciones de proveedores externos pueden cambiar. Las cifras de este documento deben considerarse una referencia operativa y verificarse antes de pasar a producción.

1. Principio de arquitectura

Hunter V1 no debe depender de un único proveedor para toda la operación.

La arquitectura será:

                    ┌──────────────────┐
                    │ FUENTES DE DATOS │
                    └────────┬─────────┘
                             │
                             ▼
                         ┌───────┐
                         │  n8n  │
                         └───┬───┘
                             │
                             ▼
                     ┌───────────────┐
                     │ ASP.NET CORE  │
                     └───────┬───────┘
                             │
                             ▼
                       PostgreSQL
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
          Mensajería        IA         Frontend
              │              │              │
              └──────────────┼──────────────┘
                             ▼
                           HUMANO

La regla principal será:

Los proveedores externos se integran mediante adaptadores para poder reemplazarlos en V2.

2. Integraciones V1

El MVP necesitará:

1. Fuente de prospectos
2. Motor de automatización
3. Canal de mensajería
4. Proveedor de IA
5. Hosting
6. Base de datos
7. Sistema de backups
8. HTTPS
3. Fuente de Prospectos
Primera opción: Google Places API

Google Places puede utilizarse para descubrir empresas mediante búsquedas geográficas y por categorías.

Ejemplos:

Casa de repuestos
Distribuidora de repuestos
Taller mecánico
Gomería
Lubricentro
Casa de suspensión
Casa de frenos

El modelo recomendado es:

n8n
  ↓
Consulta API
  ↓
Resultados
  ↓
Normalización
  ↓
Validación
  ↓
Hunter API
  ↓
Prospect Pool

Google Places utiliza un esquema de pago por uso y la facturación depende del SKU y de los campos solicitados. Google recomienda utilizar FieldMask para pedir únicamente los datos necesarios y controlar costos.

V1

Se recomienda almacenar únicamente:

businessName
address
city
state
country
latitude
longitude
website
phone
googlePlaceId
source

No solicitar información innecesaria.

4. Estrategia de descubrimiento

El sistema deberá trabajar por zonas.

Ejemplo:

Zona Oeste
    ↓
Moreno
    ↓
Merlo
    ↓
Ituzaingó
    ↓
Morón
    ↓
Castelar

Y por categorías:

Casa de repuestos
Distribuidora
Taller
Gomería
Lubricentro

Esto permite generar campañas específicas:

Campaña:
Distribuidores Zona Oeste

Campaña:
Casas de Repuestos

Campaña:
Talleres Mecánicos
5. Problema: Google Places no es una base comercial completa

No debemos asumir que Google Places siempre proporcionará:

WhatsApp
Email
Persona de contacto
Cargo

Por eso el proceso será:

Google Places
       ↓
Empresa
       ↓
Teléfono
       ↓
Validación
       ↓
¿Es WhatsApp?
       │
    ┌──┴──┐
    │     │
   SI    NO
    │     │
    ▼     ▼
 Campaña  Otro canal

La V1 debe trabajar principalmente con datos disponibles públicamente y obtenidos de fuentes cuyo uso sea compatible con sus términos.

6. Scraping

Para la V1:

No se recomienda construir un scraper masivo como fuente principal.

Sí puede existir un módulo posterior de enriquecimiento.

Ejemplo:

Prospecto
    ↓
Website público
    ↓
Buscar datos comerciales públicos
    ↓
Email
Teléfono
WhatsApp
Redes
    ↓
Actualizar Prospect

Debe evitarse:

Scraping de plataformas cerradas
Extracción de datos privados
Bypass de sistemas anti-bot
Extracción masiva de datos personales
7. Módulo Extra: Captura desde la Calle

Este módulo queda fuera del MVP V1.

La idea:

Persona en la calle
       ↓
Foto del local
       ↓
Bot
       ↓
OCR + IA
       ↓
Extraer:
Nombre
Teléfono
Dirección
Cartel
       ↓
Hunter API
       ↓
Prospect Pool

Ejemplo:

📷 Foto

"Repuestos El Toro
Av. X 1234
Tel: 11-XXXX-XXXX"

Resultado:

{
  "businessName": "Repuestos El Toro",
  "phone": "54911...",
  "address": "Av. X 1234",
  "source": "FIELD_CAPTURE"
}

Este módulo será parte potencial de V2.

8. n8n

n8n será el orquestador.

Responsabilidades:

Discovery
Import
Campaign
Messaging
Webhooks
IA
Notifications

La lógica comercial crítica permanece en ASP.NET.

La arquitectura será:

n8n
  ↓
Ejecuta workflow
  ↓
ASP.NET API
  ↓
Regla de negocio
  ↓
PostgreSQL

n8n actualmente ofrece Cloud y Self-hosted. En sus planes Cloud, el modelo se basa en ejecuciones del workflow; el plan Starter incluye 2.500 ejecuciones mensuales y 5 ejecuciones concurrentes, mientras que las opciones superiores amplían estos límites.

Para este proyecto, la opción recomendada para V1 es:

Self-hosted n8n

dentro del mismo VPS o infraestructura controlada.

Esto evita añadir un costo mensual de n8n Cloud durante la etapa inicial.

9. WhatsApp

Este es el punto más sensible del proyecto.

La arquitectura debe diferenciar:

WhatsApp oficial
        vs
Soluciones no oficiales

Para una plataforma comercial seria, la V1 debe priorizar un canal oficial.

La decisión final del proveedor debe hacerse antes de producción, considerando:

Costo por mensaje
Costo por conversación
Categoría del mensaje
Plantillas
Ventana de atención
Límites de envío
Calidad del número
Riesgo de bloqueo
API disponible
Webhooks
10. Estrategia V1 de Mensajería

La V1 debe evitar construir el sistema suponiendo que:

1000 mensajes
=
1000 mensajes gratuitos

El costo debe calcularse por:

Prospectos contactados
×
Costo real del mensaje
+
Proveedor
+
Infraestructura

La fórmula será:

Costo de adquisición
=
Prospección
+
Mensajería
+
IA
+
Infraestructura

Y:

CAC
=
Costo total
/
Ventas generadas
11. Flujo de Mensajería V1
Prospecto
    ↓
Validación
    ↓
Campaign Recipient
    ↓
n8n
    ↓
Proveedor WhatsApp
    ↓
Mensaje
    ↓
Respuesta
    ↓
Webhook
    ↓
n8n
    ↓
IA
    ↓
Clasificación
    ↓
INTERESTED
    ↓
Lead
    ↓
Humano
12. IA

La IA será utilizada exclusivamente para clasificación en V1.

Ejemplo:

Entrada:

"Sí, pasame información."

Salida:

{
  "intent": "INTERESTED",
  "confidence": 0.96
}

Categorías:

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
13. Proveedor de IA

Se recomienda utilizar una API de modelos de lenguaje con capacidad suficiente para clasificación y extracción estructurada.

No se necesita un modelo premium para cada mensaje.

La lógica:

Mensaje
   ↓
Prompt corto
   ↓
Modelo económico
   ↓
JSON estructurado

La plataforma de OpenAI ofrece modelos con precios por tokens; por ejemplo, la página oficial de API muestra distintos niveles de costo de entrada y salida según el modelo. Para una tarea de clasificación corta, el consumo puede mantenerse bajo si se utiliza un modelo adecuado y prompts pequeños.

14. Optimización del costo de IA

No enviar conversaciones completas innecesariamente.

En V1:

Mensaje recibido
       ↓
Último mensaje
       +
Contexto mínimo
       ↓
IA

No:

Toda la conversación histórica
+
Toda la información del cliente
+
Todo el CRM

Esto reduce:

Tokens.
Latencia.
Costos.
15. IA como Clasificador

La IA no debe responder automáticamente en V1.

Su función será:

IA
 ↓
"¿Hay interés?"
 ↓
Sí / No / Duda

El sistema entonces:

INTERESTED
     ↓
Crear Lead
     ↓
Avisar humano

Esto reduce significativamente el riesgo de:

Respuestas incorrectas.
Promesas comerciales.
Precios erróneos.
Información inventada.
16. Hosting

La arquitectura inicial:

VPS
│
├── Nginx
│
├── ASP.NET API
│
├── n8n
│
├── PostgreSQL
│
└── Frontend

Se recomienda:

Docker
Docker Compose
Nginx
HTTPS
17. Base de Datos

PostgreSQL será la base de datos principal.

Responsabilidades:

Organizations
Users
Prospects
Contacts
Campaigns
Recipients
Messages
Leads
Activities

No utilizar Google Sheets como base principal.

Puede utilizarse como:

Exportación
Importación manual
Backup auxiliar

Pero no como fuente principal de verdad.

18. Backups

V1:

Backup diario

Con retención recomendada:

7 días

Posteriormente:

30 días

El backup debe almacenarse fuera del servidor principal cuando sea posible.

Arquitectura:

PostgreSQL
     ↓
Backup
     ↓
Storage externo
19. HTTPS

Todos los servicios públicos deben utilizar:

HTTPS

Especialmente:

API
Frontend
Webhooks
n8n

Los webhooks de proveedores deben utilizar URLs seguras.

20. Integraciones V1
Integración	V1
ASP.NET Core	✅
PostgreSQL	✅
n8n	✅
Google Places	✅
WhatsApp / proveedor oficial	✅
IA	✅
Docker	✅
Nginx	✅
HTTPS	✅
Backup	✅
OCR desde calle	❌
GPS	❌
IA conversacional	❌
21. Arquitectura de Integraciones
                 ┌───────────────┐
                 │ Google Places │
                 └───────┬───────┘
                         │
                         ▼
                       n8n
                         │
                         ▼
                 ┌───────────────┐
                 │ ASP.NET Core  │
                 └───────┬───────┘
                         │
                         ▼
                    PostgreSQL
                         │
                         ▼
                     Campaign
                         │
                         ▼
                       n8n
                         │
                         ▼
                    WhatsApp
                         │
                         ▼
                     Prospecto
                         │
                         ▼
                      Webhook
                         │
                         ▼
                       n8n
                         │
                         ▼
                         IA
                         │
                         ▼
                       Lead
                         │
                         ▼
                      Humano
22. Costos V1

El costo total debe separarse en:

Costos fijos
VPS
Dominio
Backups
Costos variables
Google Places
WhatsApp
IA
Costos opcionales
n8n Cloud
Proveedor externo de enriquecimiento
Email
Servicios adicionales
23. Modelo de Costos

La fórmula operativa será:

Costo mensual Hunter
=
Hosting
+
Backups
+
n8n
+
Google Places
+
WhatsApp
+
IA

Y:

Costo por prospecto contactado
=
Costo total de prospección
/
Prospectos contactados

Y:

Costo por Lead
=
Costo total
/
Leads generados

Y:

Costo por Venta
=
Costo total
/
Ventas cerradas
24. Estrategia Hasta Octubre

La V1 debe aprovechar la etapa inicial para:

Construir
     ↓
Probar
     ↓
Medir
     ↓
Optimizar
     ↓
Validar ventas

No se debe optimizar prematuramente para:

100.000 mensajes

Antes hay que comprobar:

¿100 mensajes generan ventas?

Luego:

¿500 mensajes generan ventas?

Finalmente:

¿1000 mensajes son rentables?
25. Estrategia Post-Octubre

Después de octubre se debe recalcular:

Costo por mensaje
+
Costo por prospecto
+
Costo IA
+
Costo infraestructura

Y decidir:

¿Escalar?
¿Cambiar proveedor?
¿Reducir mensajes?
¿Segmentar mejor?
¿Automatizar seguimiento?

La V2 deberá priorizar rentabilidad por venta, no volumen de mensajes.

26. Recomendación Técnica V1

La arquitectura recomendada queda:

┌────────────────────────────────────────┐
│              HUNTER V1                 │
├────────────────────────────────────────┤
│                                        │
│ React + TypeScript                     │
│        │                               │
│        ▼                               │
│ ASP.NET Core API                       │
│        │                               │
│        ▼                               │
│ PostgreSQL                             │
│                                        │
│ n8n Self-hosted                        │
│   ├── Prospect Discovery               │
│   ├── Import                           │
│   ├── Campaigns                        │
│   ├── Messaging                        │
│   ├── Webhooks                         │
│   └── IA                               │
│                                        │
│ Google Places                          │
│ WhatsApp / Proveedor oficial           │
│ API de IA                              │
│                                        │
└────────────────────────────────────────┘
27. Decisiones Pendientes

Antes de comenzar el desarrollo productivo quedan cuatro decisiones importantes:

1. Fuente de prospectos

Definir:

Google Places

como fuente inicial y comprobar el costo real según el volumen esperado.

2. WhatsApp

Definir:

Meta Cloud API

o un BSP/proveedor oficial.

Esta es la decisión más importante porque afecta directamente el modelo económico.

3. IA

Definir el modelo específico para clasificación.

4. Infraestructura

Definir el VPS definitivo y si:

API
+
n8n
+
PostgreSQL

vivirán en un único servidor durante la V1.

28. Decisión Recomendada para V1

Mi recomendación es:

V1
│
├── ASP.NET Core
├── PostgreSQL
├── React
├── n8n Self-hosted
├── Google Places
├── WhatsApp oficial
├── IA económica
├── Docker
├── Nginx
└── Backups

Y mantener fuera:

OCR
Scraping avanzado
IA conversacional
Multi-canal
Scoring predictivo
Automatización de seguimiento

hasta validar el modelo comercial.

29. Punto Crítico

Hay una modificación importante respecto a la planificación inicial:

No debemos diseñar la V1 suponiendo que el costo de WhatsApp será cero o despreciable.

El sistema debe incorporar desde el comienzo un módulo de Cost Tracking, aunque sea básico:

Message
    ↓
Provider
    ↓
Category
    ↓
Cost
    ↓
Campaign
    ↓
Lead
    ↓
Sale

Así podremos medir:

Campaña A
1000 contactos
$X costo
20 Leads
5 ventas
$Y facturación

Y saber si realmente es rentable.

Esta decisión será fundamental para la documentación 15 — KPIs y Modelo de Medición.