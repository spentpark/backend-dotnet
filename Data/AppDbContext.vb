Imports Microsoft.EntityFrameworkCore

''' <summary>
''' Contexto EF Core de la aplicación. Solo declara los DbSet; la cadena de
''' conexión y el proveedor se configuran en <c>Program.ConfigureDatabase</c>
''' (o con el proveedor InMemory en las pruebas).
''' </summary>
Public Class AppDbContext
    Inherits DbContext

    Public Sub New(options As DbContextOptions(Of AppDbContext))
        MyBase.New(options)
    End Sub

    Public Property games As DbSet(Of Game)
    Public Property platform As DbSet(Of Platform)
    Public Property review As DbSet(Of Review)
    Public Property similar_game As DbSet(Of SimilarGame)
End Class
