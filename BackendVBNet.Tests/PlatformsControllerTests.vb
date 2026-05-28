Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Mvc
Imports Moq
Imports Xunit

Public Class PlatformsControllerTests

    Private ReadOnly _platformServiceMock As Mock(Of IPlatformService)
    Private ReadOnly _controller As PlatformsController

    Public Sub New()
        ' Inicializamos el mock del servicio de plataformas y el controlador
        _platformServiceMock = New Mock(Of IPlatformService)()
        _controller = New PlatformsController(_platformServiceMock.Object)
    End Sub

    ''' <summary>
    ''' Test para GetPlatforms: Debe retornar OkObjectResult con la lista de plataformas activas simuladas
    ''' </summary>
    <Fact>
    Public Async Function GetPlatforms_ReturnsOkWithListOfPlatforms() As Task
        ' Arrange (Preparar los datos de prueba)
        ' Simulamos una lista de plataformas retro comunes en tu base de datos
        Dim mockPlatforms As New List(Of Object) From {
            New With { .Id = 1, .Name = "NES", .Company = "Nintendo" },
            New With { .Id = 2, .Name = "SNES", .Company = "Nintendo" },
            New With { .Id = 3, .Name = "Atari 800", .Company = "Atari" },
            New With { .Id = 4, .Name = "PS1", .Company = "Sony" }
        }

        ' Configuramos el Mock para que cuando se llame al método asíncrono, devuelva nuestra lista
        _platformServiceMock.
            Setup(Function(s) s.GetActivePlatformsAsync()).
            ReturnsAsync(mockPlatforms)

        ' Act (Ejecutar la acción del controlador)
        Dim actionResult = Await _controller.GetPlatforms()

        ' Assert (Verificar que todo responda como corresponde)
        Dim okResult = Assert.IsType(Of OkObjectResult)(actionResult)
        Assert.Equal(200, okResult.StatusCode)
        Assert.Equal(mockPlatforms, okResult.Value)
    End Function

End Class