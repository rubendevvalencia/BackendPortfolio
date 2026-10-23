# Backend Portfolio

Monorepo en migración de monolito a microservicios, usando el **patrón Strangler Fig**: el sistema
viejo se queda funcionando tal cual mientras el nuevo crece a su lado, servicio a servicio, sin
reescritura de golpe.

| Carpeta | Qué es | Estado |
|---|---|---|
| [`Monolito/`](Monolito/README.md) | API Ecommerce en .NET, Clean Architecture. El laboratorio de aprendizaje original: cuatro versiones del mismo recurso conviviendo. | Vigente, es la referencia mientras dura la migración |
| [`Microservicios/`](Microservicios/) | La arquitectura de destino. Cada subcarpeta es un microservicio independiente. | Empezando por [`Identity/`](Microservicios/Identity/README.md), pendiente de construir |
| [`Microservices-Events/`](Microservices-Events/README.md) | Proyecto independiente de microservicios dirigidos por eventos: servicio `Ticketing` con CQRS (Command/Query), Event Sourcing, transacciones sobre MongoDB y publicación de eventos en Kafka. No forma parte de la migración del monolito. | En construcción: lado de escritura con un caso de uso que publica en Kafka (sin consumidor aún), documentado en [`docs/`](Microservices-Events/docs/arquitectura.md) |
| [`Prototipo/WpfPrototipe/`](Prototipo/WpfPrototipe/README.md) | Cliente de escritorio WPF para consumir la API. | Prototipo, ya consume la API (registro e inicio de sesión con token); las URLs salen de `appsettings.json` |

## Cómo se construye

1. `Monolito/` no se reescribe: se usa como referencia y se van extrayendo piezas.
2. Cada pieza extraída nace como su propio microservicio dentro de `Microservicios/`.
3. Cuando todas las piezas relevantes estén fuera, `Monolito/` se retira o se reduce a lo que quede sin migrar.

La migración Strangler Fig afecta a `Monolito/` y `Microservicios/`. `Microservices-Events/` es un proyecto
aparte, con su propia solución y su propio Docker.

## CI

`.github/workflows/` es compartido y vive en la raíz, no dentro de cada carpeta de proyecto. Cada
proyecto tiene su propio workflow, y **solo se ejecuta si cambia algo dentro de su propia carpeta**:

| Workflow | Se dispara con cambios en | Qué hace |
|---|---|---|
| `ci-monolito.yaml` | `Monolito/` | Restaura, compila `Ecommerce.slnx` y ejecuta `Ecommerce.Test`; los tests de integración quedan fuera porque necesitan un SQL Server real (`ubuntu-latest`) |
| `docker-monolito.yaml` | `Monolito/` | Comprueba que la imagen Docker del monolito se construye; no la publica |
| `ci-micro-events.yaml` | `Microservices-Events/` | Restaura, compila `Ticketing.slnx` y ejecuta el proyecto de tests (`ubuntu-latest`) |
| `ci-wpf.yaml` | `Prototipo/` | Compila `RegistroPerf.slnx` y comprueba que `appsettings.json` se copia a la salida, sin el cual la app no arranca (`windows-latest`, porque WPF sobre .NET Framework 4.8 solo compila en Windows) |
| `ci-identity.yaml` *(pendiente)* | `Microservicios/Identity/` | Aún no existe |

Todos se ejecutan en `push` y `pull_request` hacia `dev` y `main`.

El escaneo de secretos (`secret-scan.yaml`) es la excepción: no tiene filtro de rutas y corre sobre todo
el repo en cada `push` y `pull_request` hacia `dev` y `main`, cambie lo que cambie.
