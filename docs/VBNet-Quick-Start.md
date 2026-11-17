# Quick Start: Desarrollo VB.NET en BackendVBNet

## 1. Clonar proyecto

```powershell
cd C:\temp
git clone <repo-url> BackendVBNet
cd BackendVBNet
```

## 2. Instalar/Verificar .NET SDK

```powershell
dotnet --list-sdks
# debe incluir 8.0.x
```

## 3. Configurar base de datos y JWT

- Editar `appsettings.json` con el connection string correcto
  (`ConnectionStrings:MariaDB`).
- Los valores de desarrollo de JWT (`JWT_SECRET`, `AUTH_PASSWORD_HASH`, etc.) ya están
  en `appsettings.json`; en otros entornos se sobreescriben con variables de entorno.

## 4. Iniciar y probar

```powershell
dotnet restore
dotnet build
dotnet run
```

Abrir `http://localhost:5000/api-docs` y validar endpoints. El catálogo exige JWT:
primero `POST /api/auth/login` con `{"username":"admin","password":"Dev-aekiu4-9mHsG"}`.

## 5. Modificar endpoints

En `Controllers/` crear/modificar acciones:
- `GET /api/games?platform=&page=&limit=` (`GamesController`)
- `GET /api/games/search?title=` (`GamesController`)
- `GET /api/games/{id}` (`GamesController`)
- `GET /api/games/{id}/similar` (`SimilarGamesController`)
- `GET /api/platforms`, `/api/platforms/{id}`, `/api/platforms/{id}/top-games`
- `GET /api/reviews/{id}`, `GET /api/stats`

Recuerda `<Authorize>` en los controladores de negocio y `<Authorize(Roles:="admin")>` en
las escrituras.

## 6. Ejecutar pruebas manuales

```powershell
# Login y token
curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" `
  -d '{"username":"admin","password":"Dev-aekiu4-9mHsG"}'

# Catálogo (con Authorization: Bearer <token>)
curl "http://localhost:5000/api/games?platform=PS5&page=1&limit=10" -H "Authorization: Bearer <token>"
curl "http://localhost:5000/api/games/search?title=Zelda" -H "Authorization: Bearer <token>"
curl "http://localhost:5000/api/games/1" -H "Authorization: Bearer <token>"
```

## 7. Pasos de commit

- `git checkout -b feature/mi-cambio`
- `git add .`
- `git commit -m "Añade ..."`
- `git push origin feature/mi-cambio`

## 8. Notas finales

Usar el guide principal `docs/VBNet-Development-Guide.md` como referencia para mantener
estándares del proyecto.
