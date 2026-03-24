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

## 3. Configurar base de datos

- Editar `appsettings.json` en el root con el connection string correcto.
- Confirmar que la base de datos y tablas existen.

## 4. Iniciar y probar

```powershell
dotnet restore
dotnet build
dotnet run
```

Abrir `https://localhost:5001/swagger` (o la URL en consola) y validar endpoints.

## 5. Modificar endpoint de ejemplo

En `Controllers/GamesController.vb` modificar/crear acciones:
- `FindByPlatformPaginated`
- `FindByPlatformAndTitlePaginated`
- `GetById`

## 6. Ejecutar pruebas manuales

- `GET /api/games/platform?platform=PS5&page=1&limit=10`
- `GET /api/games/platform-search?platform=PS5&title=Zelda&page=1&limit=10`
- `GET /api/games/1`

## 7. Pasos de commit

- `git checkout -b feature/mi-cambio`
- `git add .`
- `git commit -m "Añade ..."`
- `git push origin feature/mi-cambio`

## 8. Notas finales

Usar el guide principal `docs/VBNet-Development-Guide.md` como referencia para mantener estándares del proyecto.
