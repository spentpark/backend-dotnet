# Guía de Desarrollo para BackendVBNet (Visual Basic .NET)

## 1) Objetivo

Este documento describe los pasos necesarios para arrancar con el desarrollo del proyecto
`BackendVBNet` en Visual Basic .NET (ASP.NET Core). La API replica los endpoints y la
seguridad de `backend-node20`: rutas bajo `/api`, JWT HS256 y rol `admin` en escrituras.

## 2) Prerrequisitos

- .NET SDK (versión 8.0 o superior)
- Visual Studio 2022/2023 o Visual Studio Code
- MySQL/MariaDB disponible
- Git (opcional)

## 3) Estructura del proyecto

- `Program.vb`: composition root (EF Core, JWT Bearer, CORS, pipeline y Swagger)
- `Controllers/`: `MainController`, `AuthController`, `GamesController`,
  `SimilarGamesController`, `PlatformsController`, `ReviewsController`, `StatsController`
- `Security/`: `SecurityConfig`, `AuthService` y middlewares (headers, body limit,
  rate limiting y errores JSON)
- `Data/AppDbContext.vb`: contexto EF Core y DbSet
- `Models/`: entidades (`Game`, `Platform`, `Review`, `SimilarGame`) y DTOs de salida
- `appsettings.json`: cadena de conexión, JWT, rate limiting y logging

## 4) Configuración de conexión y seguridad

1. Copia tu cadena en `appsettings.json`:
```json
"ConnectionStrings": {
  "MariaDB": "server=...;port=...;database=...;user=...;password=...;"
}
```
2. Para JWT usa las mismas variables que Node: `JWT_SECRET`, `JWT_ISSUER`,
   `JWT_AUDIENCE`, `AUTH_USERNAME`, `AUTH_ROLE` y `AUTH_PASSWORD_HASH` (bcrypt).
3. En producción el arranque falla cerrado si falta `JWT_SECRET` o `AUTH_PASSWORD_HASH`.

## 5) Comandos útiles

- `dotnet restore`
- `dotnet build`
- `dotnet run` (Kestrel en `http://localhost:5000`)
- `dotnet test BackendVBNet.sln`

## 6) Endpoints clave (mismos nombres que backend-node20)

| Método | Ruta | Auth |
|---|---|---|
| GET | `/api/saludo`, `/api/hello` | pública |
| POST | `/api/auth/login` | pública |
| GET | `/api/auth/me` | JWT |
| GET | `/api/games` (`platform`, `genre`, `year`, `decade`, `players`, `sort`, `page`, `limit`) | JWT |
| GET | `/api/games/search?title=` | JWT |
| GET | `/api/games/random` | JWT |
| GET | `/api/games/{id}` | JWT |
| GET | `/api/games/{id}/similar` | JWT |
| POST/PUT/DELETE | `/api/games[/{id}]` | JWT admin |
| GET | `/api/platforms`, `/api/platforms/{id}`, `/api/platforms/{id}/top-games` | JWT |
| POST/PUT/DELETE | `/api/platforms[/{id}]` | JWT admin |
| GET | `/api/reviews/{id}` | JWT |
| GET | `/api/stats` | JWT |
| GET | `/api-docs`, `/api-docs.json` | pública (por entorno) |

## 7) Suggested workflow

1. `git pull` + `dotnet restore`
2. Ejecutar `dotnet test BackendVBNet.sln`
3. Implementar cambios respetando las capas Controller → Service → Repository
4. `dotnet build` y ejecutar local
5. Verificar en Swagger (`http://localhost:5000/api-docs`)

## 8) Buenas prácticas

- Usar DTOs para salida de API en lugar de entidades EF directas.
- Errores con el contrato JSON `{"error": "<mensaje en español>"}`.
- Validar parámetros de entrada (`[FromQuery]` y `[FromBody]`).
- Añadir pruebas para cualquier endpoint o servicio nuevo (umbral de cobertura 93 %).
