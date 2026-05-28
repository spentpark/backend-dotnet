Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Mvc
Imports Moq
Imports Xunit
' Asegúrate de importar el namespace donde residen tus modelos/DTOs y la interfaz del servicio
Imports BackendVBNet

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
        ' Cambiado de List(Of Object) a List(Of PlatformSummary) para evitar el error de InvalidCastException
        Dim mockPlatforms As New List(Of PlatformSummary) From {
            New PlatformSummary() With { .Id = 1, .Name = "NES", .Company = "Nintendo" },
            New PlatformSummary() With { .Id = 2, .Name = "SNES", .Company = "Nintendo" },
            New PlatformSummary() With { .Id = 3, .Name = "Atari 800", .Company = "Atari" },
            New PlatformSummary() With { .Id = 4, .Name = "PS1", .Company = "Sony" }
        }

        ' Configuramos el Mock para que devuelva la lista fuertemente tipada
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