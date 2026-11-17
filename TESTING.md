# Pruebas unitarias — backend-dotnet

Proyecto de pruebas: [`BackendVBNet.Tests`](BackendVBNet.Tests) (xUnit + Moq + EF Core InMemory
+ `Microsoft.AspNetCore.Mvc.Testing`).

## Requisitos

- .NET SDK 8.0

## Comandos

```bash
# 1) Todas las pruebas + cobertura (umbral 93% configurado en el .vbproj)
dotnet test BackendVBNet.sln

# 2) Solo pruebas
dotnet test BackendVBNet.sln /p:CollectCoverage=false

# 3) Cobertura en formato SonarQube / Jenkins (opencover + cobertura)
dotnet test BackendVBNet.sln --collect:"XPlat Code Coverage"
```

Los reportes quedan en `BackendVBNet.Tests/TestResults/`:

- `coverage.cobertura.xml`
- `coverage.opencover.xml` (el que consume el `Jenkinsfile` para SonarQube)

## Estado actual

| Métrica  | Resultado |
| -------- | --------- |
| Pruebas  | 98        |
| Líneas   | **97,5 %** |
| Ramas    | 67,2 %    |
| Métodos  | 100 %     |

El umbral de **93 % de líneas** está fijado en
[`BackendVBNet.Tests.vbproj`](BackendVBNet.Tests/BackendVBNet.Tests.vbproj)
(`<Threshold>93</Threshold>`, `<ThresholdType>line</ThresholdType>`): si la
cobertura baja, `dotnet test` termina con error.

> Sobre las ramas: el compilador de VB.NET genera una rama por cada `Await`
> (completado de forma síncrona o suspensión). Esas ramas internas mantienen el
> porcentaje de ramas en ~67 % aunque el código de negocio esté prácticamente al 100 %.

## Organización de las pruebas

| Archivo | Cubre |
| ------- | ----- |
| `TestSupport.vb` | Contexto InMemory, catálogo de prueba (`SeedCatalog`) y emisión de JWT |
| `Repositories/RepositoryTests.vb` | Game/Platform/Review/SimilarGame/Stats: filtros avanzados, whitelist de orden, búsqueda, aleatorio, top-games, paginación, CRUD y distribuciones |
| `Services/ServicesTests.vb` | GameService (normalización, mapeo del aleatorio), Platform/Review, SimilarGames (parseo JSON, corrupción, límites, porcentajes), Stats (caché), AuthService (bcrypt, JWT) y `GameFilters` |
| `Security/SecurityConfigTests.vb` | Entorno, expiración JWT, CORS, docs por entorno y validación fail-closed |
| `Security/MiddlewareTests.vb` | Cabeceras, límite de cuerpo (413), rate limit (429/headers/proxy) y errores JSON (404/500) |
| `AppDbContextTests.vb` | DbSets, CRUD básico en memoria y aislamiento entre contextos |
| `ProgramTests.vb` | DI, configuración, opciones de rate limit y pipeline HTTP real (docs, 401, 404) |
| `IntegrationTests.vb` | `ApiWebFactory` (InMemory + JWT real): login, `/me`, catálogo completo, escrituras y roles (401/403), 413, 429, cabeceras y Swagger |

## Cómo se evita la red en las pruebas

- Repositorios/servicios: EF Core InMemory (`TestSupport.CreateContext()`).
- Integración: `ApiWebFactory` reemplaza el `DbContext` de MySQL por InMemory y firma los
  tokens con el mismo `JWT_SECRET` de prueba; no se abre ningún socket a MySQL.
- `ProgramTests.Pipeline_*` arranca Kestrel en `127.0.0.1:0` (puerto efímero) y solo
  ejercita endpoints públicos/401/404.

## Integración continua (Jenkins)

`Jenkinsfile` ejecuta: checkout → preparar SDK → restore → `dotnet build` → `dotnet test` con
cobertura → `dotnet pack`.

- **Agente**: `agent any`.
- **SDK y libicu**: stage "Preparar entorno .NET" idempotente (instala `libicu-dev` o activa
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`) y escribe `.ci-env` para los demás stages.
- **Umbral**: lo aplica `BackendVBNet.Tests.vbproj` (`Threshold=93`).
- **Restore**: intenta `nuget.org` y, si no hay salida, Nexus.
- **SonarQube**: `dotnet-sonarscanner end` en `finally` para publicar el análisis aunque
  fallen build o pruebas.
- **Artefactos**: `.trx` y `coverage.*.xml`.
- **Saltos de línea**: `.gitattributes` fuerza LF en `Jenkinsfile`, `Dockerfile` y `*.sh`.
