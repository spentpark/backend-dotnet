Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.EntityFrameworkCore

Module Program

    Sub Main(args As String())

        Dim builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

        Dim conn As String =
            "server=192.168.1.13;" &
            "port=3306;" &
            "database=game;" &
            "user=root;" &
            "password=Felipe1981;"

        builder.Services.AddDbContext(Of AppDbContext)(
            Sub(opt)
                opt.UseMySql(conn, ServerVersion.AutoDetect(conn))
            End Sub)

        Dim app = builder.Build()

        'If app.Environment.IsDevelopment() Then
            app.UseSwagger()
            app.UseSwaggerUI()
        'End If

        app.MapControllers()

        app.Run()

    End Sub

End Module