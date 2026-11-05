Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

''' <summary>Datos y utilidades compartidas por las pruebas del backend.</summary>
Public Module TestSupport

    ''' <summary>Crea un contexto EF Core sobre una base InMemory aislada.</summary>
    Public Function CreateContext() As AppDbContext
        Dim options = New DbContextOptionsBuilder(Of AppDbContext)().
            UseInMemoryDatabase(databaseName:=Guid.NewGuid().ToString()).
            Options

        Return New AppDbContext(options)
    End Function

    ''' <summary>Siembra un catálogo representativo para las pruebas.</summary>
    Public Sub SeedCatalog(context As AppDbContext)
        context.games.AddRange(
            New Game With {
                .Id = 1, .Title = "Alpha", .Platform = "PS5", .Genre = "RPG",
                .ReleaseDate = "2001-01-01", .Players = "1", .Metacritic_Score = "90",
                .Youtube_Trailer = "https://yt/alpha", .Description = "d1"
            },
            New Game With {
                .Id = 2, .Title = "Beta", .Platform = "PS5", .Genre = "RPG",
                .ReleaseDate = "1995-05-05", .Players = "2", .Metacritic_Score = "",
                .Youtube_Trailer = "", .Description = "d2"
            },
            New Game With {
                .Id = 3, .Title = "Gamma", .Platform = "Xbox", .Genre = "Action",
                .ReleaseDate = "2010-10-10", .Players = "1-4", .Metacritic_Score = "85",
                .Youtube_Trailer = "https://yt/gamma", .Description = "d3"
            },
            New Game With {
                .Id = 4, .Title = "Beta Remaster", .Platform = "Xbox", .Genre = "Action",
                .ReleaseDate = "2920-01-01", .Players = "1", .Metacritic_Score = "70",
                .Youtube_Trailer = "https://yt/beta-remaster", .Description = "d4"
            },
            New Game With {
                .Id = 5, .Title = "Omega", .Platform = "PC", .Genre = "RPG",
                .ReleaseDate = "1980-01-01", .Players = "1"
            })

        context.platform.AddRange(
            New Platform With {.Id = 1, .Description = "PS5", .Url = "https://ps5"},
            New Platform With {.Id = 2, .Description = "Xbox", .Url = "https://xbox"},
            New Platform With {.Id = 3, .Description = "Sin URL", .Url = Nothing},
            New Platform With {.Id = 4, .Description = "Vacia", .Url = ""})

        context.review.AddRange(
            New Review With {
                .Id = 1, .IdGame = 1, .Name = "Press", .Source = "Metacritic",
                .Text = "Bueno", .Score = "90", .ReviewDate = "2024-01-01"
            },
            New Review With {
                .Id = 2, .IdGame = 1, .Name = "User", .Source = "Metacritic User",
                .Text = "Regular", .Score = "60", .ReviewDate = "2024-02-01"
            },
            New Review With {
                .Id = 3, .IdGame = 9, .Name = "Otro", .Source = "Otra",
                .Text = "x", .Score = "50", .ReviewDate = ""
            })

        context.similar_game.Add(
            New SimilarGame With {
                .IdGame = 1,
                .Json = "[{""id"":1,""title"":""Alpha"",""similarity"":1.0}," &
                    "{""id"":3,""title"":""Gamma"",""similarity"":0.5}," &
                    "{""id"":2,""title"":""Beta"",""similarity"":0.25}]"
            })

        context.SaveChanges()
    End Sub

    ''' <summary>Genera el JWT de un usuario con el mismo secreto/issuer/audience configurados.</summary>
    Public Function CreateToken(config As Microsoft.Extensions.Configuration.IConfiguration,
                                username As String,
                                role As String) As String
        Return New AuthService(config).IssueToken(
            New AuthUser With {.username = username, .role = role}).token
    End Function
End Module
