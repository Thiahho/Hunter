📘 08 — Flujos de Automatización n8n — MVP V1

Producto: Hunter CRM AI
Versión: MVP V1
Orquestador: n8n
Backend: ASP.NET Core
Base de datos: PostgreSQL
Objetivo: Automatizar la prospección y detección de oportunidades, dejando el cierre comercial en manos humanas.

1. Objetivo

Definir cómo funcionará la automatización del MVP y qué responsabilidad tendrá cada componente.

La arquitectura seguirá este principio:

n8n
ORQUESTA
    ↓
ASP.NET Core
CONTROLA EL NEGOCIO
    ↓
PostgreSQL
PERSISTE LOS DATOS
    ↓
IA
INTERPRETA
    ↓
HUMANO
CIERRA

La separación es fundamental.

n8n no será el cerebro del sistema.

Será el orquestador de procesos externos.

2. Responsabilidad de Cada Componente
2.1 ASP.NET Core

Responsable de:

Reglas de negocio.
Multi-tenancy.
Prospectos.
Campañas.
Leads.
Estados.
Deduplicación.
Persistencia.
Autenticación.
Autorización.
Métricas.
2.2 PostgreSQL

Responsable de almacenar:

Organizaciones.
Usuarios.
Prospectos.
Contactos.
Campañas.
Mensajes.
Leads.
Interacciones.
Actividades.
2.3 n8n

Responsable de:

Ejecutar workflows.
Conectar APIs.
Programar tareas.
Procesar lotes.
Recibir webhooks.
Invocar servicios externos.
Coordinar procesos.
2.4 IA

Responsable de:

Clasificar respuestas.
Detectar intención.
Detectar interés.
Identificar mensajes ambiguos.
Extraer información relevante.

No será responsable de:

Crear directamente registros en PostgreSQL.
Modificar estados sin pasar por la API.
Decidir precios.
Negociar.
Cerrar ventas.
2.5 Humano

Responsable de:

Tomar el Lead.
Contactar.
Cotizar.
Negociar.
Resolver dudas.
Cerrar la venta.
3. Flujo General
FUENTE
  ↓
n8n
  ↓
ASP.NET API
  ↓
VALIDACIÓN
  ↓
POSTGRESQL
  ↓
CAMPAÑA
  ↓
n8n
  ↓
PROVEEDOR DE MENSAJES
  ↓
PROSPECTO
  ↓
RESPUESTA
  ↓
WEBHOOK
  ↓
n8n
  ↓
ASP.NET API
  ↓
IA
  ↓
INTERÉS
  ↓
LEAD
  ↓
HUMANO
4. Workflow 01 — Descubrimiento de Prospectos
Objetivo

Obtener nuevos prospectos desde fuentes externas.

Ejemplo:

Google Places

Flujo:

Cron
  ↓
n8n
  ↓
API de búsqueda
  ↓
Resultados
  ↓
Normalización básica
  ↓
ASP.NET API
Entrada

Ejemplo:

{
  "query": "distribuidora de repuestos",
  "city": "Buenos Aires",
  "country": "Argentina"
}
Proceso
n8n inicia workflow.
Consulta fuente.
Recibe resultados.
Normaliza datos.
Envía cada prospecto a API.
API valida.
API deduplica.
API guarda.
Resultado
Prospect Factory
       ↓
Prospect Pool
5. Workflow 02 — Normalización y Deduplicación

La deduplicación será responsabilidad de ASP.NET Core, no de n8n.

n8n puede realizar una normalización básica.

Ejemplo:

+54 9 11 1234-5678

Convertir a:

5491112345678

Pero la decisión final será del backend.

Flujo
n8n
 ↓
POST /api/prospects/import
 ↓
ProspectService
 ↓
Normalizer
 ↓
DuplicateChecker
 ↓
¿Existe?
 ├── Sí → Actualizar / Ignorar
 └── No → Crear
6. Workflow 03 — Importación al Prospect Pool

Puede utilizarse para:

Google Places.
CSV.
Fuentes futuras.

Flujo:

Source
 ↓
n8n
 ↓
API
 ↓
ProspectService
 ↓
Prospect Pool

Respuesta:

{
  "created": 80,
  "updated": 15,
  "duplicated": 25,
  "invalid": 5
}
7. Workflow 04 — Preparación de Campaña
Objetivo

Preparar los prospectos que serán contactados.

Flujo:

Campaign
   ↓
n8n
   ↓
GET /campaigns/{id}/recipients
   ↓
Prospectos READY
   ↓
Validación
   ↓
CampaignRecipient

Validaciones:

Contacto válido.
No contactado recientemente.
No está bloqueado.
No pidió dejar de recibir mensajes.
No está en otra campaña incompatible.
8. Workflow 05 — Envío de Mensajes
Objetivo

Enviar mensajes iniciales.

Flujo:

Campaign
    ↓
n8n
    ↓
Obtener lote
    ↓
Message Template
    ↓
Personalización
    ↓
Proveedor
    ↓
Enviar
    ↓
Actualizar estado
Lotes

El sistema trabajará por lotes.

Ejemplo:

Batch 1
50 prospectos

Batch 2
50 prospectos

Batch 3
50 prospectos

Esto permitirá controlar:

Errores.
Límites.
Reintentos.
Costos.
Velocidad.
9. Control de Envíos

El workflow nunca debe enviar mensajes indefinidamente.

Debe existir:

MAX_MESSAGES_PER_RUN
MAX_MESSAGES_PER_DAY
DELAY_BETWEEN_MESSAGES
MAX_RETRIES

Ejemplo:

Máximo diario:
1000

Lote:
50

Delay:
Variable

Reintentos:
3

Estos valores deben ser configurables.

10. Workflow 06 — Recepción de Respuestas

Cuando el prospecto responde:

Prospecto
   ↓
Proveedor
   ↓
Webhook
   ↓
n8n

n8n recibe:

{
  "external_message_id": "123",
  "phone": "5491112345678",
  "message": "Sí, pasame información"
}

Luego:

n8n
 ↓
Buscar Prospect
 ↓
Guardar Message
 ↓
Crear Interaction
11. Workflow 07 — Detección de Interés

Este es el núcleo de la inteligencia del MVP.

Flujo:

Mensaje
   ↓
n8n
   ↓
IA
   ↓
Clasificación

La IA devuelve:

{
  "intent": "INTERESTED",
  "confidence": 0.96
}
12. Categorías de Clasificación
INTERESTED

Ejemplos:

Sí, pasame información.

Me interesa.

¿Qué productos manejan?

¿Cómo trabajan?

NOT_INTERESTED

Ejemplos:

No me interesa.

Gracias, pero no.

QUESTION

Ejemplos:

¿Qué marcas tienen?

¿De dónde son?

UNCLEAR

Ejemplos:

Puede ser.

Después vemos.

STOP

Ejemplos:

No me escriban más.

Sacame de la lista.

13. Regla de Confianza

La IA no debería crear un Lead ante cualquier respuesta ambigua.

Se utilizará:

Confidence >= 0.80

Entonces:

INTERESTED
      ↓
Crear Lead

Si:

Confidence < 0.80

Entonces:

UNCLEAR
      ↓
Revisión humana
14. Workflow 08 — Creación del Lead

Cuando se detecta interés:

INTERESTED
    ↓
POST /api/leads
    ↓
Crear Lead
    ↓
Actualizar Prospect
    ↓
Crear Interaction

Resultado:

Prospect
    ↓
INTERESTED

Lead
    ↓
NEW
15. Workflow 09 — Human Handoff

El objetivo es entregar rápidamente la oportunidad.

Lead NEW
   ↓
Asignación
   ↓
Vendedor
   ↓
Notificación

Canales posibles:

Dashboard
WhatsApp interno
Telegram
Email

Para el MVP se recomienda comenzar con:

Dashboard
+
Notificación simple
16. Workflow 10 — Seguimiento de Métricas

n8n ejecutará periódicamente:

Cron
 ↓
API
 ↓
Obtener métricas
 ↓
Registrar

Métricas:

Prospectos encontrados
Prospectos válidos
Mensajes enviados
Mensajes entregados
Respuestas
Interesados
Leads
Ventas
17. Flujo Completo del MVP
┌───────────────┐
│ FUENTE        │
│ Google Places │
└───────┬───────┘
        ↓
      n8n
        ↓
┌───────────────┐
│ ASP.NET API   │
│ Validación    │
│ Deduplicación │
└───────┬───────┘
        ↓
   PostgreSQL
        ↓
┌───────────────┐
│ Prospect Pool │
└───────┬───────┘
        ↓
     Campaign
        ↓
      n8n
        ↓
   WhatsApp/API
        ↓
    Prospecto
        ↓
    Respuesta
        ↓
     Webhook
        ↓
      n8n
        ↓
       IA
        ↓
┌───────────────┐
│ INTERESTED    │
└───────┬───────┘
        ↓
      Lead
        ↓
    Vendedor
        ↓
      Venta
18. Responsabilidad Final
Función	n8n	ASP.NET	IA	Humano
Ejecutar workflows	✅			
Consultar APIs externas	✅			
Guardar datos		✅		
Deduplicar		✅		
Reglas de negocio		✅		
Enviar mensajes	Orquesta	Controla		
Recibir respuestas	✅	✅		
Interpretar respuesta			✅	
Crear Lead	Orquesta	✅		
Asignar Lead		✅		
Cotizar				✅
Negociar				✅
Cerrar venta				✅
19. Principio de Arquitectura

La regla principal será:

n8n ejecuta; ASP.NET decide; PostgreSQL guarda; IA interpreta; el humano vende.

Esto evita que la lógica crítica quede dispersa entre workflows.

Por ejemplo, si mañana se reemplaza n8n por otro orquestador, el dominio principal de Hunter seguirá funcionando.

20. Workflow Especial — Captura Manual

Aunque no es parte del núcleo de automatización, la arquitectura deberá dejar preparado un flujo para el futuro módulo de captura desde la calle.

Foto del local
      ↓
OCR
      ↓
IA
      ↓
Extraer:
- Nombre
- Teléfono
- Dirección
- Ubicación
      ↓
Confirmación humana
      ↓
ASP.NET API
      ↓
Prospect Pool

Este módulo queda fuera del MVP.

21. MVP V1 — Flujo Mínimo Real

El flujo mínimo que debemos implementar y probar será:

1. Prospecto encontrado
        ↓
2. Prospecto guardado
        ↓
3. Campaña creada
        ↓
4. Mensaje enviado
        ↓
5. Prospecto responde
        ↓
6. IA detecta interés
        ↓
7. Lead creado
        ↓
8. Humano recibe Lead
        ↓
9. Humano cierra venta

Si este flujo funciona de punta a punta, tenemos un MVP comercialmente validable.