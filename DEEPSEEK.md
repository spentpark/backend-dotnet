# GEMINI.md — BackendVBNet

Contexto de instrucciones para agentes que trabajen en este repositorio.

## 1. Visión general

API REST de catálogo de videojuegos escrita en **Visual Basic .NET sobre ASP.NET Core 8**.
Expone los **mismos endpoints que `backend-node20`** (bajo `/api`) y aplica **la misma
seguridad**: JWT HS256 (secreto/`issuer`/`audience` compartidos con `backend-node20` y
`rss-game-node`), rol `admin` en escrituras, cabeceras tipo Helmet, CORS con allowlist,
cuerpo JSON de 10 kb, rate limiting y errores JSON. Persiste en **MySQL/MariaDB** con
**EF Core 8 + Pomelo** y documenta con Swagger en `/api-docs`.

- La aplicación escucha en el puerto **5000** (su puerto original; el backend Node usa 3000).
- El frontend [`frontend-csharp`](../frontend-csharp) consume este backend con los mismos
  endpoints y credenciales.

### Stack

| Componente | Versión / tecnología |
|---|---|
| Runtime | .NET 8.0 (`net8.0`) |
| Lenguaje | Visual Basic .NET (`BackendVBNet.vbproj`) |
| Web | ASP.NET Core MVC / `ControllerBase` |
| ORM | EF Core 8.0 + `Pomelo.EntityFrameworkCore.MySql` 8.0 |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0 + `BCrypt.Net-Next` 4.0 |
| Documentación | Swashbuckle.AspNetCore 7.0 (`/api-docs`, `/api-docs.json`) |
| Tests | xUnit 2.4.2, Moq 4.20, EF Core InMemory, `Microsoft.AspNetCore.Mvc.Testing` |
| Cobertura | coverlet (collector + msbuild), umbral **93 % de líneas** |
| CI | Jenkins + SonarQube |

## 2. Estructura

```
Program.vb                  # composition root: DI, JWT, CORS, pipeline, Swagger, EF Core
Controllers/                # Main, Auth, Games, SimilarGames, Platforms, Reviews, Stats
Security/                   # SecurityConfig, AuthService y middlewares (headers, body, rate limit, errores)
Services/                   # I*Service + implementaciones (reglas de negocio)
Repositories/               # I*Repository + implementaciones (EF Core)
Data/AppDbContext.vb        # DbSet(Of Game/Platform/Review/SimilarGame)
Models/                     # entidades + DTOs de salida (GameSummary, PaginatedResult(Of T), ...)
BackendVBNet.Tests/         # xUnit: repositorios, servicios, seguridad, Program e integración HTTP
docs/                       # guías de desarrollo y quick start
Dockerfile / Jenkinsfile    # imagen de runtime (puerto 5000) y pipeline CI
```

`RootNamespace` efectivo: **`BackendVBNet`** (los archivos no declaran `Namespace`).

## 3. Arquitectura

Flujo de una petición:

```
Middleware (errores JSON -> headers -> CORS -> rate limit -> body limit)
    → JwtBearer (401/403 JSON)
        → Controller (atributos de ruta, sin lógica)
            → I*Service
                → I*Repository (EF Core)
                    → AppDbContext → MySQL/MariaDB
```

- Cada dependencia se resuelve por interfaz (`IGameService`, `IGameRepository`, …) y se
  registra con `AddScoped` en `Program.vb`.
- Los controladores de negocio llevan `<Authorize>`; las escrituras añaden
  `<Authorize(Roles:="admin")>`. Solo `MainController` (saludo/hello) y el login son
  `<AllowAnonymous>`.
- La paginación usa `PaginatedResult(Of T)` con el contrato de Node:
  `{ "data": [...], "pagination": { currentPage, perPage, totalRecords, totalPages } }`.
  Los servicios normalizan `page >= 1` y `limit` (por defecto 10).

### Endpoints (mismos nombres que backend-node20)

| Método | Ruta | Auth |
|---|---|---|
| GET | `/api/saludo` | pública |
| GET | `/api/hello` | pública |
| POST | `/api/auth/login` | pública |
| GET | `/api/auth/me` | JWT |
| GET | `/api/games` (`platform`, `genre`, `year`, `decade`, `players`, `sort`, `page`, `limit`) | JWT |
| GET | `/api/games/search?title=` | JWT |
| GET | `/api/games/random` (`platform`, `genre`) | JWT |
| GET | `/api/games/{id}` | JWT |
| GET | `/api/games/{id}/similar?limit=` | JWT |
| POST / PUT / DELETE | `/api/games[/{id}]` | JWT admin |
| GET | `/api/platforms` (array) | JWT |
| GET | `/api/platforms/{id}` | JWT |
| GET | `/api/platforms/{id}/top-games` | JWT |
| POST / PUT / DELETE | `/api/platforms[/{id}]` | JWT admin |
| GET | `/api/reviews/{id}` | JWT |
| GET | `/api/stats` | JWT |
| GET | `/api-docs`, `/api-docs.json` | pública (por entorno) |

Contrato detallado de referencia: `backend-node20/endpoints.md` y
`backend-python/endpoints.md` (esta app replica el mismo contrato HTTP).

## 4. Seguridad

- **JWT HS256** (`Security/SecurityConfig.vb` + `Security/AuthService.vb`): mismas variables
  que Node (`JWT_SECRET`, `JWT_EXPIRES_IN`, `JWT_ISSUER`, `JWT_AUDIENCE`, `AUTH_USERNAME`,
  `AUTH_ROLE`, `AUTH_PASSWORD_HASH`). El login valida con **bcrypt** y firma el token con
  claims `sub` y `role`. `MapInboundClaims = False` para conservar los nombres de claim.
- **401/403 JSON** con `{"error": "No autorizado"}` / `{"error": "Acceso denegado"}` y
  cabecera `WWW-Authenticate: Bearer`.
- **Cabeceras Helmet** (CSP, HSTS, nosniff, X-Frame-Options, COOP/CORP…). Sin CSP en
  `/api-docs`. `X-Powered-By` no se emite.
- **CORS**: allowlist (`CORS_ORIGINS` la sobreescribe) + cualquier localhost en desarrollo.
- **Rate limiting** en memoria por IP: API 300/15 min y login 5/15 min
  (`API_RATE_LIMIT_*`, `AUTH_RATE_LIMIT_*`); responde 429 con `RateLimit-*` y `Retry-After`.
  Con `TRUST_PROXY=true` usa `X-Forwarded-For`.
- **Cuerpo** máximo 10 kb → 413; errores 404/500 JSON sin stack trace.
- **Swagger por entorno**: activo por defecto; en producción solo con
  `API_DOCS_ENABLED=true`; `false` lo apaga siempre. La validación de arranque falla cerrada
  en producción si faltan `JWT_SECRET` o `AUTH_PASSWORD_HASH`.
- Login y `/api/auth/me` añaden `Cache-Control: no-store` y `Pragma: no-cache`.

## 5. Build, run y test

Prerrequisitos: **.NET 8 SDK** y acceso a una instancia MySQL/MariaDB con el esquema `game`.

```powershell
dotnet restore
dotnet build
dotnet run                      # Kestrel: http://localhost:5000
# Swagger: http://localhost:5000/api-docs
# OpenAPI: http://localhost:5000/api-docs.json
```

Tests (incluye cobertura; el umbral de 93 % puede hacer fallar la ejecución):

```powershell
dotnet test BackendVBNet.sln
dotnet test BackendVBNet.sln --filter FullyQualifiedName~ApiIntegrationTests
```

Docker:

```powershell
docker build -t backend-vbnet .
docker run -d -p 5000:5000 --env-file .env --name backend-vbnet backend-vbnet
```

## 6. Configuración

`appsettings.json` incluye valores de desarrollo (connection string, secreto JWT, hash
bcrypt). En un entorno real se sobreescriben con variables de entorno:

- `ConnectionStrings__MariaDB` (o `ConnectionStrings__GameDb`) → cadena de conexión.
- `JWT_SECRET`, `JWT_EXPIRES_IN`, `JWT_ISSUER`, `JWT_AUDIENCE`, `AUTH_USERNAME`,
  `AUTH_ROLE`, `AUTH_PASSWORD_HASH`.
- `API_RATE_LIMIT_*`, `AUTH_RATE_LIMIT_*`, `TRUST_PROXY`, `CORS_ORIGINS`,
  `API_DOCS_ENABLED`, `ENVIRONMENT`/`NODE_ENV`.
- `Database:ServerVersion` opcional: evita `ServerVersion.AutoDetect` (lo usan pruebas/CI).

`Program.ResolveConnectionString` prioriza `GameDb`, luego `MariaDB` y, si no hay ninguna,
usa la cadena histórica.

## 7. Convenciones de código

- **VB.NET idiomático**: `Imports` explícitos, `Async/Await`, tipos explícitos, `ReadOnly`.
- **Nombres**: clases/métodos/propiedades públicas en `PascalCase`; las propiedades de los
  DTOs y entidades que definen el contrato JSON van en **camelCase** o llevan
  `<JsonPropertyName>` con el nombre exacto de Node/columna.
- **Mapeo de columnas legacy**: `<Column("...")>` para `image_Large`, `releasedate`,
  `3djuegos_url` → `_3djuegos_Url`, `esbr_message`, `id_game`, `date`, etc.
- **Sin lógica de negocio en controllers** ni acceso directo a `AppDbContext` desde ellos.
- Comentarios y documentación en **español**.

## 8. Convenciones de tests

- xUnit, patrón Arrange / Act / Assert; nombres `Metodo_Condicion_ResultadoEsperado`.
- Repositorios/servicios: `TestSupport.CreateContext()` (InMemory) y `SeedCatalog`.
- Integración: `ApiWebFactory` (`WebApplicationFactory(Of Program)`) sustituye MySQL por
  InMemory, mantiene JWT/CORS/rate limit reales y emite tokens con `CreateToken` para
  probar 401/403/CRUD.
- Cobertura: `Include = [BackendVBNet]*`, `Threshold = 93`, `ThresholdType = line`.

## 9. CI/CD

`Jenkinsfile`: restore → build → `dotnet test` con cobertura opencover → SonarQube →
`dotnet pack` → **deploy** por SSH al servidor Docker `192.168.1.82` (credencial
`docker-server-ssh`, parámetro `DEPLOY_ENABLED`). SonarQube excluye `bin/`, `obj/`,
`*.Tests/**` y consume `TestResults/coverage.opencover.xml`.

### Deploy (stage `Deploy backend-dotnet en servidor Docker`)

- Sincroniza el workspace por `tar` + `ssh` a
  `/home/spentpark/gitDeploy/gitdotnet/backend-dotnet` (excluye `bin/`, `obj/`,
  `TestResults/`, `tools/`, `nupkg/`, `.ci-env` y `.git/`).
- Construye la imagen **en el servidor** (`docker build -t backend-dotnet`) **antes** de
  tocar el contenedor: si la compilación falla, la versión anterior sigue corriendo.
- `docker compose up -d --force-recreate backend-dotnet` con
  `/home/spentpark/gitDeploy/gitdotnet/docker-compose.yml` (servicio y contenedor
  `backend-dotnet`, puerto 5000), comprueba que el contenedor usa la imagen recién
  construida y espera respuesta 2xx de `GET /api/saludo` (sonda sin base de datos;
  `/api/hello` sí consulta MariaDB).
- Parámetros del job: `DEPLOY_ENABLED`, `DEPLOY_HOST`, `SSH_CREDENTIAL_ID`, `DEPLOY_DIR`,
  `COMPOSE_FILE`, `COMPOSE_SERVICE`, `CONTAINER_NAME`, `APP_PORT`, `PROBE_PATH`.

## 10. Al modificar el proyecto

1. `dotnet build` y `dotnet test BackendVBNet.sln` antes de dar un cambio por terminado.
2. Si añades un endpoint: ruta bajo `api/`, `Async`, `IActionResult`, `<Authorize>`
   (y `<Authorize(Roles:="admin")>` si es escritura), errores JSON `{"error": "..."}`.
3. Si añades un servicio/repositorio: interfaz + implementación, registro en `Program.vb`
   y pruebas que mantengan el umbral de cobertura.
4. No commitees `bin/`, `obj/`, `TestResults/` ni credenciales reales.
