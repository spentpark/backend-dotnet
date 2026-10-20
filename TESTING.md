# Pruebas unitarias — backend-dotnet

Proyecto de pruebas: [`BackendVBNet.Tests`](BackendVBNet.Tests) (xUnit + Moq + EF Core InMemory).

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
| Pruebas  | 61        |
| Líneas   | **97,7 %** |
| Ramas    | 67,4 %    |
| Métodos  | 97,0 %    |

El umbral de **93 % de líneas** está fijado en
[`BackendVBNet.Tests.vbproj`](BackendVBNet.Tests/BackendVBNet.Tests.vbproj)
(`<Threshold>93</Threshold>`, `<ThresholdType>line</ThresholdType>`): si la
cobertura baja, `dotnet test` termina con error.

> Sobre las ramas: el compilador de VB.NET genera una rama por cada `Await`
> (completado de forma síncrona o suspensión). Esas ramas internas son las que
> mantienen el porcentaje de ramas en ~67 % aunque el código de negocio esté al
> 100 %; se cubren en parte con pruebas de asincronía real (ver `GameServiceTests`).

## Organización de las pruebas

| Archivo | Cubre |
| ------- | ----- |
| `Controllers/GamesControllerTests.vb` | `GamesController` de punta a punta con EF InMemory (200 / 404, paginación y búsqueda) |
| `Controllers/PlatformsControllerTests.vb` | `PlatformsController` con `IPlatformService` mockeado |
| `Controllers/ReviewsControllerTests.vb` | `ReviewsController` con `IReviewService` mockeado |
| `Services/*Tests.vb` | Reglas de paginación por defecto (page/limit) y delegación a repositorios |
| `Repositories/GameRepositoryTests.vb` | Paginación, orden por título, filtro por plataforma y búsqueda por título |
| `Repositories/PlatformRepositoryTests.vb` | Filtro de plataformas con URL (`null`, vacía, sin datos) |
| `Repositories/ReviewRepositoryTests.vb` | Filtro por `GameId` y mapeo a `ReviewSummary` |
| `AppDbContextTests.vb` | DbSets, CRUD básico en memoria y aislamiento entre contextos |
| `ProgramTests.vb` | Composición de la app: DI, resolución de configuración, Swagger y pipeline HTTP real |

## Detalles de diseño para poder probar

`Program.vb` se refactorizó en piezas verificables, sin cambiar el comportamiento:

- `Program.CreateApp(args)` construye la aplicación (servicios + pipeline) **sin** arrancar Kestrel.
- `Program.ConfigureServices / ConfigurePipeline / ConfigureDatabase` son públicos y se prueban por separado.
- `Program.RegisterApplicationPart` garantiza que los controladores se descubran aunque el
  ensamblado de entrada sea el del host de pruebas.
- Configuración por `IConfiguration` en lugar de valores incrustados:
  - `ConnectionStrings:GameDb` o `ConnectionStrings:MariaDB` (la que ya existía en
    `appsettings.json`) → connection string. Si ninguna está definida se usa la histórica.
  - `Database:ServerVersion` → evita `ServerVersion.AutoDetect` (y con ello necesitar un MySQL
    real) en pruebas y CI. En producción, si no se define, se sigue autodetectando.

## Líneas no cubiertas (6 de 217)

- `Program.Main` — solo arranca Kestrel (`CreateApp(args).Run()`); se prueba `CreateApp`.
- `AppDbContext.OnConfiguring` — rama de *design-time* (solo se ejecuta sin `DbContextOptions`).

Ambas son envoltorios sin lógica propia.

## Integración continua (Jenkins)

`Jenkinsfile` ejecuta: checkout → preparar SDK → restore → `dotnet build` → `dotnet test` con
cobertura → `dotnet pack`.

- **Agente**: `agent any`, igual que el resto de pipelines del entorno. Se intentó un agente
  Docker (`image 'mcr.microsoft.com/dotnet/sdk:8.0'`) y Jenkins no lo aprovisionaba: el build se
  quedaba sin nodo ni workspace y moría con
  `MissingContextVariableException: Required context class hudson.FilePath is missing`, además de
  ese mismo error en el bloque `post`.
- **SDK y libicu**: el stage "Preparar entorno .NET" es idempotente. Instala `libicu-dev`
  (sin ICU cualquier binario .NET aborta con `Couldn't find a valid ICU package installed on
  the system`) usando `sudo apt-get`, igual que otros pipelines del entorno; si no hay
  `sudo`/`apt`, activa `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. Después reutiliza el SDK 8.x
  si existe y, si no, lo instala en `$HOME/.dotnet`.
- **Variables entre stages**: cada `sh` de Jenkins arranca en un shell nuevo, así que el stage
  de preparación escribe `.ci-env` (`DOTNET_ROOT`, `PATH`, e invariante si hace falta) y los
  demás stages lo cargan con `. ./.ci-env`.
- **Umbral**: lo aplica el propio `BackendVBNet.Tests.vbproj` (`Threshold=93`), así que un
  `dotnet test` con cobertura por debajo del 93 % rompe el stage.
- **Restore**: intenta `nuget.org`; si el agente no tiene salida a internet, reintenta contra
  Nexus (`NEXUS_HOST_URL`, credencial `nexus`).
- **SonarQube**: `dotnet-sonarscanner end` se ejecuta en un bloque `finally`, de modo que el
  análisis se publica aunque fallen la compilación o las pruebas (antes no se llamaba y el
  proyecto nunca aparecía en Sonar).
- **Artefactos**: se archivan el `.trx` y los `coverage.*.xml` para poder diagnosticar un build
  fallido sin depender de la consola. El `post` comprueba `env.WORKSPACE` antes de archivar o
  limpiar, para no tapar el error real con el `hudson.FilePath is missing`.
- **Saltos de línea**: `.gitattributes` fuerza LF en `Jenkinsfile`, `Dockerfile` y `*.sh` como
  prevención: con CRLF, bash recibe el último argumento con un `\r` pegado y `dotnet restore`
  falla con `MSB1009: El archivo de proyecto no existe` (el `\r` es invisible en la consola).
- **Diagnóstico**: el stage de restore imprime `dotnet --version`, los SDKs, los orígenes NuGet y
  el commit construido, para que un fallo de entorno se vea directamente en el log.

