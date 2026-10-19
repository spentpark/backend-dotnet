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
