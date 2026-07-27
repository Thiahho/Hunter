📘 14 — Operación Comercial V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Definir cómo utilizar el sistema para generar la mayor cantidad posible de oportunidades comerciales y convertirlas en ventas, independientemente del tamaño, ubicación o recurrencia del cliente.

1. Objetivo Comercial

Hunter V1 será utilizado por Difrani como una máquina de prospección y generación de oportunidades comerciales.

El sistema debe permitir contactar potencialmente a:

Micro clientes
Pequeños
Medianos
Grandes
Distribuidores
Casas de repuestos
Talleres
Gomerías
Lubricentros
Revendedores
Otros compradores B2B

No se descartará automáticamente un prospecto por:

Tamaño
Distancia
Volumen estimado
Recurrencia estimada

La prioridad será:

Generar la mayor cantidad posible de ventas rentables.

2. Modelo Comercial

El proceso completo será:

DESCUBRIR
    ↓
VALIDAR
    ↓
SEGMENTAR
    ↓
CONTACTAR
    ↓
DETECTAR INTERÉS
    ↓
CREAR LEAD
    ↓
ASIGNAR HUMANO
    ↓
COTIZAR
    ↓
NEGOCIAR
    ↓
CERRAR
    ↓
REGISTRAR VENTA
    ↓
SEGUIMIENTO
3. Rol de Hunter

Hunter será responsable de:

Buscar prospectos
Importar prospectos
Normalizar datos
Deduplicar
Segmentar
Crear campañas
Enviar mensajes
Recibir respuestas
Clasificar interés
Crear Leads
Notificar al equipo
Registrar métricas

Hunter no será responsable del cierre comercial en V1.

El cierre será responsabilidad humana.

4. Rol del Humano

El vendedor será responsable de:

Responder Lead
Entender necesidad
Detectar oportunidad
Presentar productos
Cotizar
Negociar
Confirmar pedido
Cerrar venta
Registrar resultado

La separación será:

BOT
"Conseguir y detectar"
        ↓
HUMANO
"Vender y cerrar"
5. Flujo Comercial Principal
                         PROSPECTO
                             │
                             ▼
                         VALIDACIÓN
                             │
                             ▼
                          CAMPAÑA
                             │
                             ▼
                         MENSAJE
                             │
                             ▼
                          RESPUESTA
                             │
                 ┌───────────┴───────────┐
                 │                       │
                 ▼                       ▼
              SIN INTERÉS             INTERÉS
                 │                       │
                 ▼                       ▼
               CERRAR                   LEAD
                                         │
                                         ▼
                                      HUMANO
                                         │
                                         ▼
                                      VENTA
6. Etapa 1 — Descubrimiento

El sistema buscará prospectos mediante las fuentes disponibles.

Ejemplo:

Zona:
Moreno

Categoría:
Casa de Repuestos

Resultado:

Prospecto A
Prospecto B
Prospecto C
Prospecto D

El sistema deberá almacenar:

Nombre
Teléfono
Dirección
Ubicación
Categoría
Fuente
Fecha de captura
7. Etapa 2 — Validación

Antes de incluir un prospecto en una campaña:

¿Tiene teléfono?
¿El teléfono es válido?
¿Está duplicado?
¿Está en Suppression List?
¿Tiene datos suficientes?

Resultado:

READY

o:

INVALID

o:

SUPPRESSED
8. Etapa 3 — Segmentación

La segmentación no debe utilizarse para descartar clientes.

Debe utilizarse para mejorar el mensaje.

Ejemplo:

Distribuidor
Mensaje:
orientado a volumen
Casa de repuestos
Mensaje:
orientado a disponibilidad y variedad
Taller
Mensaje:
orientado a rapidez y abastecimiento
Revendedor
Mensaje:
orientado a precio y margen
9. Segmentación por Tamaño

Se podrán utilizar:

SMALL
MEDIUM
LARGE
UNKNOWN

Pero:

UNKNOWN nunca significa DESCARTAR.

Un pequeño comercio puede generar una compra inmediata y convertirse posteriormente en cliente recurrente.

10. Segmentación por Ubicación

Se podrá registrar:

NEAR
MEDIUM
FAR
UNKNOWN

Esto sirve para:

Logística.
Costos de envío.
Prioridad comercial.

Pero tampoco debe utilizarse como filtro absoluto.

11. Segmentación por Recurrencia

Se puede estimar:

HIGH
MEDIUM
LOW
UNKNOWN

Sin embargo, el sistema no debe asumir la recurrencia real antes de tener historial.

Inicialmente:

UNKNOWN

Después de las primeras compras:

Compra 1
Compra 2
Compra 3

se puede calcular:

Recurrencia real
12. Etapa 4 — Creación de Campaña

El usuario selecciona:

Nombre
Segmento
Zona
Categoría
Cantidad
Plantilla
Canal

Ejemplo:

Campaña:
Casas de Repuestos — Zona Oeste

Segmento:
Casas de Repuestos

Zona:
Zona Oeste

Cantidad:
500

Plantilla:
Presentación Difrani B2B
13. Etapa 5 — Preparación

Antes de enviar:

Prospectos
     ↓
Deduplicación
     ↓
Suppression List
     ↓
Validación
     ↓
Rate Limit
     ↓
Campaign Queue

El sistema debe mostrar:

Prospectos seleccionados: 500
Válidos: 470
Duplicados: 15
Bloqueados: 10
Inválidos: 5
14. Etapa 6 — Contacto Inicial

El primer mensaje debe ser:

Corto
Directo
Humano
Comercial
Relevante

No debe ser:

Largo
Genérico
Agresivo
Spam

La intención del primer contacto es:

Abrir una conversación.

No cerrar una venta inmediatamente.

15. Estructura del Primer Mensaje
Presentación
+
Motivo
+
Propuesta de valor
+
Pregunta

Ejemplo conceptual:

Hola, ¿cómo va?

Soy [NOMBRE] de Difrani.

Estamos trabajando con comercios del rubro automotor y quería consultarte si actualmente compran repuestos para reventa o distribución.

Si te interesa, te paso información.

El CTA debe buscar una respuesta simple.

16. Respuesta del Prospecto

El sistema recibirá:

Respuesta

La IA clasificará:

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
17. Interés Detectado

Cuando se detecte:

INTERESTED

el sistema debe:

Crear Lead
    ↓
Asignar vendedor
    ↓
Notificar
    ↓
Cambiar estado

El bot deja de intentar vender.

18. Human Handoff

Flujo:

BOT
"¿Te paso información?"

PROSPECTO
"Sí, pasame."

SYSTEM
🟢 INTERESTED

VENDEDOR
Toma el contacto

El vendedor continúa la conversación.

19. SLA Comercial

El tiempo de respuesta humano es crítico.

Recomendación:

Lead recibido
    ↓
Respuesta ideal:
< 5 minutos

Objetivo máximo:

< 15 minutos

Fuera de horario:

Responder al inicio
del siguiente turno

El tiempo de respuesta debe registrarse.

20. Asignación de Leads

V1 puede utilizar:

Round Robin

Ejemplo:

Lead 1 → Vendedor A
Lead 2 → Vendedor B
Lead 3 → Vendedor C
Lead 4 → Vendedor A

También puede utilizar:

Asignación manual

La opción recomendada para iniciar:

Asignación manual o Round Robin simple.

No desarrollar todavía algoritmos complejos.

21. Gestión del Lead

El vendedor deberá utilizar:

NEW
IN_PROGRESS
WON
LOST

Flujo:

NEW
 ↓
IN_PROGRESS
 ↓
WON

o:

NEW
 ↓
IN_PROGRESS
 ↓
LOST
22. Cotización

La cotización se realiza fuera de Hunter V1.

Puede ser mediante:

WhatsApp
Teléfono
Email
Sistema comercial

Hunter registra:

Cotización enviada
Fecha
Monto estimado
Resultado
23. Venta Ganada

Cuando se concrete:

Lead
 ↓
Venta

El vendedor marca:

WON

Debe registrar:

Monto
Producto
Categoría
Fecha
Vendedor

Opcional:

Margen
24. Venta Perdida

Si no compra:

LOST

Se debe registrar motivo.

Ejemplos:

Precio
Sin stock
Ya tiene proveedor
No respondió
No le interesa
Distancia
Condiciones comerciales
Otro

Esto será fundamental para optimizar las campañas.

25. Seguimiento V1

Aunque la automatización avanzada queda para V2, la V1 debe permitir registrar:

Fecha último contacto
Próximo seguimiento
Nota

Ejemplo:

Lead:
Distribuidora Norte

Estado:
IN_PROGRESS

Nota:
"Pidió lista de precios."

Seguimiento:
28/07/2026
26. Recuperación de Leads

Los Leads que no compraron no deben desaparecer.

Estados:

LOST

pero con:

LostReason

Posteriormente podrán entrar en:

Campaña de reactivación

V2:

Venta perdida
    ↓
30 días
    ↓
Nuevo contacto

En V1 puede hacerse manualmente.

27. Operación Diaria

La jornada comercial debe comenzar con:

1. Abrir Dashboard
2. Revisar nuevos Leads
3. Atender Leads
4. Revisar seguimientos
5. Ver campañas
6. Revisar ventas

Prioridad:

LEADS NUEVOS
      ↓
SEGUIMIENTOS
      ↓
LEADS EN PROCESO
      ↓
NUEVAS CAMPAÑAS
28. Rutina Comercial
Inicio del día
Revisar Leads
Durante el día
Responder
Cotizar
Negociar
Cerrar
Final del día
Registrar:
Ventas
Perdidas
Seguimientos
29. Campañas Paralelas

Hunter debe permitir múltiples campañas.

Ejemplo:

Campaña A
Distribuidores

Campaña B
Casas de Repuestos

Campaña C
Talleres

Campaña D
Gomerías

Cada campaña tendrá métricas independientes.

30. Regla de No Saturación

Un mismo prospecto no debería recibir múltiples campañas simultáneamente.

Ejemplo:

Campaña A
    ↓
Prospecto X

No:

Campaña A → X
Campaña B → X
Campaña C → X

La plataforma debe controlar:

LastContactedAt

y:

NextEligibleContactAt
31. Priorización de Leads

V1 puede utilizar una prioridad simple:

HIGH
MEDIUM
LOW

Ejemplo:

INTERESTED
+
Pregunta concreta
=
HIGH
INTERESTED
+
Respuesta genérica
=
MEDIUM
UNCLEAR
=
LOW
32. Métrica Fundamental

La métrica principal no será:

Mensajes enviados

Será:

VENTAS CERRADAS

El embudo:

Prospectos
    ↓
Mensajes
    ↓
Respuestas
    ↓
Interesados
    ↓
Leads
    ↓
Cotizaciones
    ↓
Ventas
33. Modelo Comercial para Difrani

Difrani podrá trabajar con:

Distribuidores
Casas de Repuestos
Revendedores
Talleres
Comercios
Otros compradores

El sistema debe permitir definir campañas por:

Tipo de cliente
Zona
Producto
Tamaño
34. Campaña Ejemplo
CAMPAÑA

Nombre:
Distribuidores Automotores Zona Oeste

Segmento:
Distribuidores

Zona:
Zona Oeste

Prospectos:
1.000

Objetivo:
Generar nuevos clientes B2B

Mensaje:
Presentación comercial

Resultado esperado:
Generar Leads
35. Flujo Comercial Completo
┌──────────────────────┐
│ FUENTE DE PROSPECTOS │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ VALIDACIÓN           │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ SEGMENTACIÓN         │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ CAMPAÑA              │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ CONTACTO             │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ RESPUESTA            │
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ IA CLASIFICA         │
└──────────┬───────────┘
           ▼
      ¿INTERÉS?
       /     \
     NO       SÍ
     │         │
     ▼         ▼
   CERRAR     LEAD
                │
                ▼
             HUMANO
                │
                ▼
             COTIZAR
                │
          ┌─────┴─────┐
          ▼           ▼
        VENTA       PERDIDA
          │           │
          ▼           ▼
        WON          LOST
36. Objetivo Operativo V1

El sistema debe conseguir que:

1 persona

pueda gestionar una cantidad de prospectos significativamente superior a la que podría contactar manualmente.

Ejemplo:

Sin Hunter
100 prospectos
    ↓
muchas horas

Con Hunter
1000 prospectos
    ↓
automatización
    ↓
humano atiende solo interesados

La ventaja no está únicamente en enviar más mensajes.

La ventaja es:

Eliminar el trabajo repetitivo y permitir que el equipo humano se concentre en vender.

37. Regla de Oro
BOT:
Busca
Contacta
Detecta

HUMANO:
Escucha
Cotiza
Negocia
Cierra

HUNTER:
Registra
Mide
Optimiza
38. Criterio de Éxito Operativo

La operación será considerada exitosa si:

Prospectos
    ↓
Contactados
    ↓
Respuestas
    ↓
Leads
    ↓
Ventas

y el costo de conseguir una venta es económicamente viable.

El objetivo no es alcanzar una cantidad arbitraria de mensajes.

El objetivo es encontrar la combinación óptima:

Volumen
+
Calidad
+
Costo
+
Conversión
=
Rentabilidad
39. Criterio de Aceptación

La V1 debe permitir:

[ ] Crear prospectos
[ ] Importar prospectos
[ ] Segmentar
[ ] Crear campañas
[ ] Contactar
[ ] Detectar respuestas
[ ] Detectar interés
[ ] Crear Leads
[ ] Asignar Leads
[ ] Abrir WhatsApp
[ ] Registrar seguimiento
[ ] Registrar cotización
[ ] Marcar WON
[ ] Marcar LOST
[ ] Registrar motivo de pérdida
[ ] Medir resultados