# Backend Portfolio

Monorepo en migración de monolito a microservicios, usando el **patrón Strangler Fig**: el sistema
viejo se queda funcionando tal cual mientras el nuevo crece a su lado, servicio a servicio, sin
reescritura de golpe.

| Carpeta | Qué es | Estado |
|---|---|---|
| [`Monolito/`](Monolito/README.md) | API Ecommerce en .NET, Clean Architecture. El laboratorio de aprendizaje original: cuatro versiones del mismo recurso conviviendo. | Vigente, es la referencia mientras dura la migración |
| [`Microservicios/`](Microservicios/) | La arquitectura de destino. Cada subcarpeta es un microservicio independiente. | Empezando por [`Identity/`](Microservicios/Identity/README.md), pendiente de construir |

## Cómo se construye

1. `Monolito/` no se reescribe: se usa como referencia y se van extrayendo piezas.
2. Cada pieza extraída nace como su propio microservicio dentro de `Microservicios/`.
3. Cuando todas las piezas relevantes estén fuera, `Monolito/` se retira o se reduce a lo que quede sin migrar.

## CI

`.github/workflows/` es compartido y vive en la raíz, no dentro de cada carpeta de proyecto. Cada
proyecto tiene su propio workflow, y **solo se ejecuta si cambia algo dentro de su propia carpeta**:

| Workflow | Se dispara con cambios en |
|---|---|
| `ci-monolito.yaml` | `Monolito/` |
| `ci-identity.yaml` *(pendiente)* | `Microservicios/Identity/` |

El escaneo de secretos (`secret-scan.yaml`) es la excepción: corre siempre, en todo el repo, cambie lo
que cambie.
