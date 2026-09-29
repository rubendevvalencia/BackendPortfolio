# syntax=docker/dockerfile:1

# --- Etapa 1: build ---------------------------------------------------------
# Imagen con el SDK completo (compilador, NuGet...). Solo se usa para construir;
# no viaja a produccion, de ahi que se descarte al final con el multi-stage build.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copian primero solo los .csproj (respetando la estructura de carpetas) para
# que "dotnet restore" quede en su propia capa de Docker: mientras no cambien las
# dependencias de los proyectos, esta capa se reutiliza de la cache aunque cambie
# el codigo, y el restore (lento) no se repite en cada build.
COPY Ecommerce/Ecommerce.Api.csproj Ecommerce/
COPY Ecommerce.Application/Ecommerce.Application.csproj Ecommerce.Application/
COPY Ecommerce.Domain/Ecommerce.Domain.csproj Ecommerce.Domain/
COPY Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj Ecommerce.Infrastructure/
COPY Ecommerce.Transversal/Ecommerce.Transversal.csproj Ecommerce.Transversal/

RUN dotnet restore Ecommerce/Ecommerce.Api.csproj

# Ahora si el resto del codigo fuente de los proyectos que forman el monolito.
# Los proyectos de test (Ecommerce.Test, Ecommerce.IntegrationTest) se quedan
# fuera a proposito: no forman parte del artefacto publicado.
COPY Ecommerce/ Ecommerce/
COPY Ecommerce.Application/ Ecommerce.Application/
COPY Ecommerce.Domain/ Ecommerce.Domain/
COPY Ecommerce.Infrastructure/ Ecommerce.Infrastructure/
COPY Ecommerce.Transversal/ Ecommerce.Transversal/

RUN dotnet publish Ecommerce/Ecommerce.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Etapa 2: runtime --------------------------------------------------------
# Imagen solo con el runtime de ASP.NET Core: mucho mas ligera que la del SDK
# porque no incluye el compilador ni las herramientas de build.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASPNETCORE_HTTP_PORTS reemplaza a ASPNETCORE_URLS desde .NET 8 para el caso
# simple (un solo puerto HTTP en http:// sin esquema/host explicitos). El TLS
# se asume terminado delante (ver comentario de UseForwardedHeaders en Program.cs).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Las imagenes oficiales de aspnet (desde .NET 8) traen creado un usuario sin
# privilegios llamado "app": se usa en vez de root para reducir el impacto de
# una posible vulnerabilidad dentro del contenedor.
USER app

ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
