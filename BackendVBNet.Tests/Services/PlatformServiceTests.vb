Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Moq
Imports Xunit

Public Class PlatformServiceTests

    Private ReadOnly _repositoryMock As Mock(Of IPlatformRepository)
    Private ReadOnly _platformService As PlatformService

    Public Sub New()
        ' Inicializamos el mock del repositorio y el servicio
        _repositoryMock = New Mock(Of IPlatformRepository)()
        _platformService = New PlatformService(_repositoryMock.Object)
    End Sub

    ''' <summary>
    ''' Test para GetActivePlatformsAsync: Debe llamar al repositorio y retornar la lista sin alteraciones
    ''' </summary>
    <Fact>
    Public Async Function GetActivePlatformsAsync_ReturnsDataFromRepository() As Task
        ' Arrange (Preparar)
        Dim mockData As New List(Of PlatformSummary) From {
            New PlatformSummary() With { .Id = 1, .Name = "Atari 800" },
            New PlatformSummary() With { .Id = 2, .Name = "PS1" }
        }

        ' Configuramos el repositorio simulado
        _repositoryMock.
            Setup(Function(r) r.GetAllWithUrlAsync()).
            ReturnsAsync(mockData)

        ' Act (Ejecutar)
        Dim result = Await _platformService.GetActivePlatformsAsync()

        ' Assert (Verificar)
        Assert.NotNull(result)
        Assert.Same(mockData, result) ' Corrobora que devuelve exactamente la misma instancia
        _repositoryMock.Verify(Function(r) r.GetAllWithUrlAsync(), Times.Once()) ' Asegura que se llamó al repositorio una sola vez
    End Function

End Class