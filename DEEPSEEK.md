# GEMINI.md — BackendVBNet

Contexto de instrucciones para agentes que trabajen en este repositorio.

## 1. Visión general

API REST de catálogo de videojuegos escrita en **Visual Basic .NET sobre ASP.NET Core 8**.
Expone consultas de juegos (por plataforma, por título y por id), plataformas activas y
reseñas por juego. Persiste en **MySQL/MariaDB** mediante **EF Core 8 + Pomelo** y documenta
con **Swagger (Swashbuckle)**.

No hay frontend ni capa de autenticación en este repositorio.

### Stack

| Componente | Versión / tecnología |
|---|---|
| Runtime | .NET 8.0 (`net8.0`) |
| Lenguaje | Visual Basic .NET (`BackendVBNet.vbproj`) |
| Web | ASP.NET Core MVC / `ControllerBase` |
| ORM | EF Core 8.0 + `Pomelo.EntityFrameworkCore.MySql` 8.0 |
| Documentación | Swashbuckle.AspNetCore 7.0 |
| Tests | xUnit 2.4.2, Moq 4.20, EF Core InMemory, `Microsoft.AspNetCore.Mvc.Testing` |
| Cobertura | coverlet (collector + msbuild), umbral **93 % de líneas** |
| CI | Jenkins (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`) + SonarQube |

## 2. Estructura

```
Program.vb                  # composition root: DI, DbContext, Swagger, pipeline
Controllers/                # GamesController, PlatformsController, ReviewsController
Services/                   # I*Service + implementaciones (reglas de negocio ligeras)
Repositories/               # I*Repository + implementaciones (EF Core / consultas)
Data/AppDbContext.vb        # DbSet(Of Game/Platform/Review)
Models/                     # entidades + DTOs de salida (GameSummary, PagedResult(Of T), ...)
BackendVBNet.Tests/         # xUnit: controllers, services, repositories, DbContext, integración
docs/                       # guías de desarrollo y quick start (en español)
Dockerfile / Jenkinsfile    # imagen de runtime y pipeline CI
```

`RootNamespace` efectivo: **`BackendVBNet`** (los archivos no declaran `Namespace`; los tests
importan `BackendVBNet.Controllers`, `BackendVBNet.Models`, etc.).

## 3. Arquitectura

Flujo de una petición:

```
Controller (ControllerBase, atributos de ruta, sin lógica)
    → I*Service (normaliza parámetros, orquesta)
        → I*Repository (EF Core: Where/OrderBy/Skip/Take, proyección a *Summary)
            → AppDbContext → MySQL/MariaDB
```

Reglas observadas:

- Cada dependencia se resuelve por **interfaz** (`IGameService`, `IGameRepository`, …) y se
  registra con `AddScoped` en `Program.vb`. Toda nueva pieza debe seguir ese patrón y
  registrarse allí.
- Los repositorios devuelven **DTOs** (`GameSummary`, `PlatformSummary`, `ReviewSummary`)
  para listados y la entidad completa solo en consultas por id.
- La paginación se modela con `PagedResult(Of T)` (`page`, `limit`, `total`, `data`); el
  service fuerza `page = 1` y `limit = 20` cuando llegan valores `<= 0`.
- Los controladores usan rutas por atributo (`<Route("api/...")>` + `<ApiController>`) y
  `Async Function ... As Task(Of IActionResult)`.

### Endpoints actuales

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/games/platform?platform=&page=&limit=` | Juegos paginados por plataforma |
| GET | `/api/games/search?title=&page=&limit=` | Búsqueda paginada por título |
| GET | `/api/games/{id}` | Detalle; `404` si no existe |
| GET | `/api/platforms` | Plataformas activas |
| GET | `/api/reviews/{gameId}` | Reseñas de un juego |

> `docs/VBNet-Quick-Start.md` menciona `/api/games/platform-search`; la ruta real es
> `/api/games/search`.

## 4. Build, run y test

Prerrequisitos: **.NET 8 SDK**, acceso a una instancia **MySQL/MariaDB** con el esquema
`game`. No hay carpeta `Migrations`: el esquema se gestiona fuera del proyecto.

```powershell
dotnet restore
dotnet build
dotnet run                      # Kestrel por defecto: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

No existe `Properties/launchSettings.json`; para fijar puerto:

```powershell
dotnet run --urls "http://localhost:5000"
```

Tests (incluye cobertura; el umbral de 93 % puede hacer fallar la ejecución):

```powershell
dotnet test BackendVBNet.sln
dotnet test BackendVBNet.sln --filter FullyQualifiedName~GamesControllerTests
```

Docker:

```powershell
docker build -t backend-vbnet .
docker run -d -p 5000:5000 --name backend-vbnet backend-vbnet
```

Empaquetado (usado por el pipeline):

```powershell
dotnet pack BackendVBNet.vbproj -c Release -o ./nupkg
```

## 5. Configuración de base de datos

`appsettings.json` declara `ConnectionStrings:MariaDB`, pero **`Program.vb` no la usa**:
construye el connection string como literal y llama `UseMySql` directamente. Además,
`AppDbContext.OnConfiguring` contiene otro connection string hardcodeado como fallback
(con credenciales distintas) que solo aplica si el contexto se instancia sin opciones.

Al tocar la configuración de BD, ten en cuenta:

- Cambiar `appsettings.json` **no** altera la conexión de la app.
- El patrón correcto (documentado en `docs/VBNet-Development-Guide.md`) es
  `builder.Configuration.GetConnectionString("MariaDB")`.
- Hay credenciales reales en texto plano en `Program.vb` y `appsettings.json`. No las
  reproduzcas en logs, tests ni commits; para cambios nuevos usa variables de entorno
  (`ConnectionStrings__MariaDB`) o user-secrets.

## 6. Convenciones de código

- **VB.NET idiomático**: `Imports` explícitos, `Async/Await`, `Function`/`Sub` con tipos
  explícitos, `ReadOnly` para dependencias inyectadas.
- **Nombres**: clases/métodos/propiedades públicas en `PascalCase`; las propiedades de los
  DTOs y de las entidades (`id`, `title`, `page`, `limit`, `data`) van en **camelCase**
  porque definen el contrato JSON de la API. Mantén esa distinción.
- **Mapeo de columnas legacy**: las entidades usan `<Column("...")>` para columnas con
  nombres no idiomáticos (`image_Large`, `releasedate`, `3djuegos_url` → `_3djuegos_Url`,
  `esbr_message`). No renombres esas propiedades sin actualizar el atributo.
- **Interfaces**: prefijo `I` + nombre del servicio/repositorio, declaradas en el mismo
  directorio que su implementación (`IGameService.vb` junto a `GameService.vb`).
- **Sin lógica de negocio en controllers** ni acceso directo a `AppDbContext` desde ellos.
- `Option Strict` no está configurado en el `.vbproj` (queda en el default `Off`). El código
  nuevo debe ser tipado explícitamente de todos modos.
- Comentarios y documentación en **español**.

## 7. Convenciones de tests

- Framework **xUnit**; patrón `Arrange / Act / Assert` con esos comentarios.
- Nombres de test: `Metodo_Condicion_ResultadoEsperado`
  (ej. `GetById_ReturnsNotFound_WhenGameDoesNotExist`).
- Unitarios de controller/service/repository: `TestAppDbContext` sobre
  `UseInMemoryDatabase(Guid.NewGuid().ToString())` y sobreescritura vacía de
  `OnConfiguring`; `Using context = ...` para el dispose.
- `Moq` disponible para dobles de servicio/repositorio.
- Integración: `WebApplicationFactory(Of Program)` para validar el contenedor DI y
  `/swagger/v1/swagger.json`.
- Cobertura: `Include = [BackendVBNet]*`, `Threshold = 93`, `ThresholdType = line`,
  `ThresholdStat = total`. Al añadir código, añade tests que mantengan el umbral.

> Nota: `BackendVBNet.Tests/ProgramIntegrationTests` **no tiene extensión `.vb`**, por lo que
> el glob de compilación por defecto (`**/*.vb`) no lo incluye. Si se esperan esos tests en
> la suite, el archivo debe renombrarse a `ProgramIntegrationTests.vb`.

## 8. CI/CD

`Jenkinsfile` (agente Docker `mcr.microsoft.com/dotnet/sdk:8.0`):

1. `dotnet restore` de ambos proyectos.
2. `dotnet-sonarscanner begin` → `dotnet build BackendVBNet.sln --no-restore` →
   `dotnet test` con logger TRX y cobertura **opencover** en `./TestResults` →
   `dotnet-sonarscanner end`.
3. `dotnet pack -c Release -o ./nupkg`.
4. Publicación a Nexus comentada.

SonarQube excluye `**/bin/**`, `**/obj/**`, `**/*.Tests/**` y consume
`**/TestResults/**/coverage.opencover.xml`.

## 9. Al modificar el proyecto

1. `dotnet restore` y `dotnet build` antes de dar un cambio por terminado.
2. `dotnet test BackendVBNet.sln` (verifica también el umbral de cobertura).
3. Si añades un servicio/repositorio: crea interfaz + implementación, regístralo en
   `Program.vb` y añade su test.
4. Si añades un endpoint: ruta bajo `api/`, `Async`, `IActionResult`, `404` con
   `NotFound()` y sin lógica en el controller.
5. No commitees `bin/`, `obj/`, `TestResults/` ni credenciales.
