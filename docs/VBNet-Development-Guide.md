# Guía de Desarrollo para BackendVBNet (Visual Basic .NET)

## 1) Objetivo

Este documento describe los pasos necesarios para arrancar con el desarrollo del proyecto `BackendVBNet` en Visual Basic .NET (ASP.NET Core). Incluye configuración local, estructura de archivos y comandos básicos.

## 2) Prerrequisitos

- .NET SDK (versión 8.0 o superior)
- Visual Studio 2022/2023 o Visual Studio Code
- MySQL/MariaDB disponible
- Git (opcional)

## 3) Estructura del proyecto

- `Program.vb`: configuración de la aplicación y servicios (DbContext, Swagger, MVC)
- `Controllers/`: controladores Web API (`GamesController.vb`, `PlatformsController.vb`, `ReviewsController.vb`)
- `Data/AppDbContext.vb`: contexto EF Core y DbSet
- `Models/`: modelos de datos (`Game.vb`, `Platform.vb`, `Review.vb`)
- `appsettings.json`: cadenas de conexión y configuración de logging

## 4) Configuración de conexión

1. Copia tu cadena en `appsettings.json`:
```json
"ConnectionStrings": {
  "MariaDB": "server=...;port=...;database=...;user=...;password=...;"
}
```
2. `Program.vb` debe usar:
```vb
Dim conn As String = builder.Configuration.GetConnectionString("MariaDB")
```

## 5) Comandos útiles

- `dotnet restore`
- `dotnet build`
- `dotnet run`
- `dotnet ef migrations add <Nombre>` (si se agregan migraciones)
- `dotnet ef database update`

## 6) Endpoints clave

- `GET /api/games/platform?platform=x&page=1&limit=20`
- `GET /api/games/platform-search?platform=x&title=foo&page=1&limit=20`
- `GET /api/games/{id}`

## 7) Suggested workflow

1. `git pull` + `dotnet restore`
2. Ejecutar tests (si existen)
3. Implementar cambios en controlador/modelo
4. `dotnet build` y ejecutar local
5. Verificar en Swagger (a `http://localhost:5000/swagger` si está habilitado)

## 8) Buenas prácticas

- Usar DTOs para salida de API en lugar de entidades EF directas
- Manejo de errores con `try/catch` y `ProblemDetails`
- Validar parámetros de entrada (`[FromQuery]` y `[FromBody]`)
