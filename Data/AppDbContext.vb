Imports Microsoft.EntityFrameworkCore

Public Class AppDbContext
    Inherits DbContext

    Public Sub New(options As DbContextOptions(Of AppDbContext))
        MyBase.New(options)
    End Sub

    Public Property games As DbSet(Of Game)
    Public Property platform As DbSet(Of Platform)
    Public Property review As DbSet(Of Review)

    Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
        If Not optionsBuilder.IsConfigured Then
            Dim conn As String = "server=192.168.1.13;port=3306;database=game;user=admin;password=password;"
            optionsBuilder.UseMySql(conn, ServerVersion.AutoDetect(conn))
        End If
    End Sub
End Class