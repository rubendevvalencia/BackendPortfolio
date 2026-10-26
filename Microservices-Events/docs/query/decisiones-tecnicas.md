# Decisiones técnicas de Ticketing.Query

[← Volver al README](../../README.md) · [Decisiones generales](../decisiones-tecnicas.md)

Las del servicio de consulta que tienen algo que explicar.

### PostgreSQL con EF Core para el modelo de lectura

`Ticketing.Query` guarda el estado ya proyectado en tablas relacionales (`tickets`, `employees`,
`ticket_types` y la tabla intermedia `ticket_employees`), mientras que `Command` mantiene la secuencia de
eventos en MongoDB. Cada lado usa el almacén que encaja con su trabajo: eventos inmutables añadidos al final
frente a consultas con relaciones. Las dos bases son independientes: `Query` nunca lee la de `Command`.

- **Esquema por migraciones:** `Program.cs` ejecuta `Migrate()` al arrancar; la migración vive en
  `Infrastructure`, y `Api` es el proyecto de inicio para `dotnet ef`.
- **Value object `Address` como `ComplexType`:** sus columnas van en `employees`, sin tabla ni `Id` propios.
- **Cadena de conexión fuera del repo:** `PostgresConnection` va vacía en `appsettings.json` y se aporta por
  secretos.

Estructura y modelo completo en [*Arquitectura de Ticketing.Query*](arquitectura.md).
