Imports System.Threading.Tasks

''' <summary>Acceso a datos de juegos (EF Core).</summary>
Public Interface IGameRepository
    ''' <summary>Listado paginado con filtros avanzados y orden (contrato de Node).</summary>
    Function FindSummariesPaginatedAsync(filters As GameFilters, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary))

    ''' <summary>Búsqueda paginada por prefijo de título.</summary>
    Function FindByTitlePaginatedAsync(title As String, page As Integer, limit As Integer) As Task(Of PaginatedResult(Of GameSummary))

    ''' <summary>Juego al azar con tráiler (con filtros opcionales de plataforma/género).</summary>
    Function FindRandomAsync(filters As GameFilters) As Task(Of Game)

    Function GetByIdAsync(id As Integer) As Task(Of Game)
    Function CreateAsync(game As Game) As Task(Of Game)
    Function UpdateAsync(id As Integer, game As Game) As Task(Of Boolean)
    Function DeleteAsync(id As Integer) As Task(Of Boolean)
End Interface
