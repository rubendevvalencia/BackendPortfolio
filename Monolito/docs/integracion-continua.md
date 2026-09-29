# Integración continua

[← Volver al README](../README.md)

El proyecto compila y pasa sus tests en una máquina que no es la mía. Es lo que separa *en mi equipo
funciona* de una afirmación comprobable: tres workflows independientes se disparan en cada `push` y en
cada *pull request* contra `dev` y `main`, sobre `ubuntu-latest` y con el SDK de .NET 10.

| Workflow | Qué hace | Cuándo se dispara | Qué protege |
|---|---|---|---|
| [`ci-monolito.yaml`](../../.github/workflows/ci-monolito.yaml) | `dotnet restore` → `dotnet build --configuration Release` → `dotnet test --configuration Release`, sobre `Monolito/Ecommerce.slnx` | Solo si cambia algo bajo `Monolito/` (o el propio workflow) | Que la solución compile fuera de Visual Studio y que los 271 tests sigan en verde |
| [`docker-monolito.yaml`](../../.github/workflows/docker-monolito.yaml) | `docker build` de [`Monolito/Docker/Dockerfile`](../Docker/Dockerfile) con contexto `Monolito/`, sin publicar la imagen | Solo si cambia algo bajo `Monolito/` (o el propio workflow) | Que el Dockerfile sigue construyendo: rutas de `COPY` y `restore`/`publish` dentro del contenedor |
| [`secret-scan.yaml`](../../.github/workflows/secret-scan.yaml) | [`gitleaks/gitleaks-action@v2`](https://github.com/gitleaks/gitleaks-action), con `fetch-depth: 0` en el checkout | Siempre, cambie lo que cambie | Que no entre al repositorio una credencial nueva |

**Por qué están en ficheros separados y no en jobs del mismo workflow.** El repositorio es un
monorepo (`Monolito/` hoy, `Microservicios/` mañana): filtrar por `paths` es una propiedad del
*workflow*, no del *job*, así que un único fichero no puede hacer que `build-and-test` solo corra para
`Monolito/` mientras `secret-scan` sigue corriendo siempre. Separarlos en dos ficheros consigue justo
eso, y de paso deja el patrón listo: cuando `Microservicios/Identity` tenga código, su propio
`ci-identity.yaml` se añade igual, con su `paths` propio, sin tocar el de `Monolito` ni el de
`secret-scan`.

**Se construye en `Release`, que es la configuración con la que se publicaría.** Compilar en `Debug` y
desplegar en `Release` deja fuera de la verificación justo las diferencias que importan —símbolos de
compilación, optimizaciones, aserciones—, y son las que aparecen en el despliegue y no antes.

**El punto de entrada es la solución, no cada proyecto.** `Monolito/Ecommerce.slnx` es el único fichero
de solución de este proyecto, y los tres pasos lo reciben como argumento, de modo que añadir un
proyecto nuevo dentro de `Monolito/` mañana no obliga a tocar el workflow.

**Los tests corren sin dependencias externas, y eso no es casualidad.** Los de `Infrastructure` usan el
proveedor InMemory de EF Core y los de `Application` doblan los límites de la capa con NSubstitute: el
runner no necesita SQL Server, ni Redis, ni un solo secreto. Es una propiedad de la suite que hoy sale
gratis y que dejará de salirlo con los tests de integración con `WebApplicationFactory`
(pendiente nº 8): o levantan sus dependencias como *services* del job —contenedores de SQL Server y de
Redis— o el job deja de ser autosuficiente. Conviene decidirlo entonces, y no descubrirlo.

**Permisos mínimos, y elevados solo donde hacen falta.** Los tres workflows declaran
`permissions: contents: read` a nivel raíz, así que el `GITHUB_TOKEN` de cualquier job nace sin poder
escribir en el repositorio aunque una acción de terceros lo intente. El job `secret-scan` es el único
que sube ese mínimo —`pull-requests: write`— porque gitleaks publica el hallazgo como comentario en el
*pull request*, y ese permiso está declarado en el propio job, no heredado del workflow.

**Sobre el escaneo de secretos.** No hay `.gitleaks.toml`: se usan las reglas por defecto. En `push` y en
`pull_request` la acción revisa los commits de ese evento —que es exactamente lo que se quiere, impedir que
entre una credencial nueva— y `fetch-depth: 0` es lo que le permite resolver el rango de commits; con el
clon superficial que hace `actions/checkout` por defecto no podría. El escaneo del historial completo solo
ocurre en ejecuciones manuales o programadas, que hoy no están configuradas.

**Lo que la CI todavía no hace**, por si el hueco se lee como descuido:

- **No cachea los paquetes NuGet** (`setup-dotnet` admite `cache: true` con un `packages.lock.json`), así
  que cada ejecución restaura desde cero.
- **`dotnet test` vuelve a compilar la solución** en lugar de reutilizar la salida del paso anterior. Con
  `--no-build --no-restore` el trabajo se haría una sola vez; en esta solución cuesta segundos, pero es
  trabajo repetido igualmente.
- **No publica nada de lo que produce**: ni el `.trx` de los tests, ni cobertura, ni artefactos.
- **Las acciones están fijadas por etiqueta mayor** (`@v4`, `@v2`), no por SHA. Una etiqueta se puede mover;
  un SHA no.
- **Un workflow en rojo informa, pero no bloquea.** Convertirlo en requisito para mezclar es configuración
  de *branch protection* en GitHub, no YAML — el repositorio todavía no la tiene.
- **No se levanta el `docker-compose`** en la CI: en un runner efímero no aportaría nada.
- **No cachea las capas de Docker** en `docker-monolito.yaml`, y **la imagen no se publica** en ningún
  registro: solo se comprueba que construye.
- **Falta la otra mitad del bloque E** de la [hoja de ruta](../README.md#hoja-de-ruta): el despliegue en Azure.
