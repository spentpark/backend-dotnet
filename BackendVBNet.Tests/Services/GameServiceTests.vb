Imports System.Threading.Tasks
Imports Moq
Imports Xunit

Public Class GameServiceTests

    Private ReadOnly _repositoryMock As Mock(Of IGameRepository)
    Private ReadOnly _gameService As GameService

    Public Sub New()
        ' Inicializamos el mock del repositorio y el servicio de negocio
        _repositoryMock = New Mock(Of IGameRepository)()
        _gameService = New GameService(_repositoryMock.Object)
    End Sub

    ' ==========================================
    ' PRUEBAS PARA GetGamesByPlatformAsync
    ' ==========================================

    <Fact>
    Public Async Function GetGamesByPlatformAsync_WithValidParams_CallsRepositoryWithSameParams() As Task
        ' Arrange
        Dim platform As String = "Dreamcast"
        Dim page As Integer = 2
        Dim limit As Integer = 15
        Dim expectedResult As New PagedResult(Of GameSummary)()

        _repositoryMock.
            Setup(Function(r) r.FindByPlatformPaginatedAsync(platform, page, limit)).
            ReturnsAsync(expectedResult)

        ' Act
        Dim result = Await _gameService.GetGamesByPlatformAsync(platform, page, limit)

        ' Assert
        Assert.Same(expectedResult, result)
        _repositoryMock.Verify(Function(r) r.FindByPlatformPaginatedAsync(platform, page, limit), Times.Once())
    End Function

    <Fact>
    Public Async Function GetGamesByPlatformAsync_WithInvalidParams_CorrectsToDefaults() As Task
        ' Arrange
        Dim platform As String = "PS2"
        Dim invalidPage As Integer = 0
        Dim invalidLimit As Integer = -5
        Dim expectedPage As Integer = 1
        Dim expectedLimit As Integer = 20
        Dim expectedResult As New PagedResult(Of GameSummary)()

        _repositoryMock.
            Setup(Function(r) r.FindByPlatformPaginatedAsync(platform, expectedPage, expectedLimit)).
            ReturnsAsync(expectedResult)

        ' Act
        Dim result = Await _gameService.GetGamesByPlatformAsync(platform, invalidPage, invalidLimit)

        ' Assert
        Assert.Same(expectedResult, result)
        ' Verificamos que el repositorio recibió los valores corregidos (1 y 20)
        _repositoryMock.Verify(Function(r) r.FindByPlatformPaginatedAsync(platform, expectedPage, expectedLimit), Times.Once())
    End Function

    ' ==========================================
    ' PRUEBAS PARA SearchGamesByTitleAsync
    ' ==========================================

    <Fact>
    Public Async Function SearchGamesByTitleAsync_WithValidParams_CallsRepositoryWithSameParams() As Task
        ' Arrange
        Dim title As String = "Zelda"
        Dim page As Integer = 1
        Dim limit As Integer = 10
        Dim expectedResult As New PagedResult(Of GameSummary)()

        _repositoryMock.
            Setup(Function(r) r.FindByTitlePaginatedAsync(title, page, limit)).
            ReturnsAsync(expectedResult)

        ' Act
        Dim result = Await _gameService.SearchGamesByTitleAsync(title, page, limit)

        ' Assert
        Assert.Same(expectedResult, result)
        _repositoryMock.Verify(Function(r) r.FindByTitlePaginatedAsync(title, page, limit), Times.Once())
    End Function

    <Fact>
    Public Async Function SearchGamesByTitleAsync_WithInvalidParams_CorrectsToDefaults() As Task
        ' Arrange
        Dim title As String = "Sonic"
        Dim invalidPage As Integer = -10
        Dim invalidLimit As Integer = 0
        Dim expectedPage As Integer = 1
        Dim expectedLimit As Integer = 20
        Dim expectedResult As New PagedResult(Of GameSummary)()

        _repositoryMock.
            Setup(Function(r) r.FindByTitlePaginatedAsync(title, expectedPage, expectedLimit)).
            ReturnsAsync(expectedResult)

        ' Act
        Dim result = Await _gameService.SearchGamesByTitleAsync(title, invalidPage, invalidLimit)

        ' Assert
        Assert.Same(expectedResult, result)
        _repositoryMock.Verify(Function(r) r.FindByTitlePaginatedAsync(title, expectedPage, expectedLimit), Times.Once())
    End Function

    ' ==========================================
    ' PRUEBAS PARA GetGameByIdAsync
    ' ==========================================

    <Fact>
    Public Async Function GetGameByIdAsync_ReturnsGame_WhenExists() As Task
        ' Arrange
        Dim gameId As Integer = 7
        Dim expectedGame As New Game() With { .Id = gameId, .Title = "Chrono Trigger" }

        _repositoryMock.
            Setup(Function(r) r.GetByIdAsync(gameId)).
            ReturnsAsync(expectedGame)

        ' Act
        Dim result = Await _gameService.GetGameByIdAsync(gameId)

        ' Assert
        Assert.NotNull(result)
        Assert.Equal(gameId, result.Id)
        _repositoryMock.Verify(Function(r) r.GetByIdAsync(gameId), Times.Once())
    End Function

End Class