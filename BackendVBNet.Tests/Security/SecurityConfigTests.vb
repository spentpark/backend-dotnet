Imports System
Imports System.Collections.Generic
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging.Abstractions
Imports Xunit

''' <summary>Pruebas de la configuración de seguridad (JWT, CORS, rate limit y docs).</summary>
Public Class SecurityConfigTests

    Private Shared Function CreateConfig(Optional values As Dictionary(Of String, String) = Nothing) As IConfiguration
        Return New ConfigurationBuilder().
            AddInMemoryCollection(If(values, New Dictionary(Of String, String)())).
            Build()
    End Function

    Private Shared ReadOnly CompleteAuth As Dictionary(Of String, String) = New Dictionary(Of String, String) From {
        {"JWT_SECRET", "7456dd0b8487a4477dd5ef9078d8f9c264f82301e034f669bffa31e2b7dfe0d8"},
        {"AUTH_PASSWORD_HASH", "$2b$12$21YmeGUU8ULL3ngZm3btLeSMswy9oOBQNXhQAdrvGTLGKP1zClT32"}
    }

    <Fact>
    Public Sub EnvironmentName_UsesConfiguredValueOrDefault()
        Assert.Equal("development", SecurityConfig.GetEnvironmentName(CreateConfig()))
        Assert.Equal("production", SecurityConfig.GetEnvironmentName(
            CreateConfig(New Dictionary(Of String, String) From {{"ENVIRONMENT", "production"}})))
        Assert.Equal("production", SecurityConfig.GetEnvironmentName(
            CreateConfig(New Dictionary(Of String, String) From {{"NODE_ENV", "production"}})))
        Assert.Equal("staging", SecurityConfig.GetEnvironmentName(
            CreateConfig(New Dictionary(Of String, String) From {{"ASPNETCORE_ENVIRONMENT", "staging"}})))
    End Sub

    <Fact>
    Public Sub IsProduction_OnlyForProductionEnvironment()
        Assert.False(SecurityConfig.IsProduction(CreateConfig()))
        Assert.True(SecurityConfig.IsProduction(
            CreateConfig(New Dictionary(Of String, String) From {{"ENVIRONMENT", "production"}})))
    End Sub

    <Fact>
    Public Sub GetValue_And_GetPositiveInt_FallBackWhenInvalid()
        Dim config = CreateConfig(New Dictionary(Of String, String) From {
            {"BLANK", "   "},
            {"NUMBER", "25"},
            {"ZERO", "0"},
            {"NEGATIVE", "-3"},
            {"TEXT", "abc"}
        })

        Assert.Equal("fallback", SecurityConfig.GetValue(config, "MISSING", "fallback"))
        Assert.Equal("fallback", SecurityConfig.GetValue(config, "BLANK", "fallback"))
        Assert.Equal("25", SecurityConfig.GetValue(config, "NUMBER", "fallback"))

        Assert.Equal(10, SecurityConfig.GetPositiveInt(config, "MISSING", 10))
        Assert.Equal(25, SecurityConfig.GetPositiveInt(config, "NUMBER", 10))
        Assert.Equal(10, SecurityConfig.GetPositiveInt(config, "ZERO", 10))
        Assert.Equal(10, SecurityConfig.GetPositiveInt(config, "NEGATIVE", 10))
        Assert.Equal(10, SecurityConfig.GetPositiveInt(config, "TEXT", 10))
    End Sub

    <Fact>
    Public Sub ExpiresInSeconds_ParsesDurations()
        Assert.Equal(3600, SecurityConfig.GetExpiresInSeconds(CreateConfig()))
        Assert.Equal(60, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "60"}})))
        Assert.Equal(1800, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "30m"}})))
        Assert.Equal(7200, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "2h"}})))
        Assert.Equal(86400, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "1d"}})))
        Assert.Equal(3600, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "no"}})))
        Assert.Equal(3600, SecurityConfig.GetExpiresInSeconds(
            CreateConfig(New Dictionary(Of String, String) From {{"JWT_EXPIRES_IN", "0"}})))
    End Sub

    <Fact>
    Public Sub JwtAndAuth_GettersUseNodeDefaults()
        Dim config = CreateConfig()

        Assert.Equal("", SecurityConfig.GetJwtSecret(config))
        Assert.Equal("rss-game-node", SecurityConfig.GetJwtIssuer(config))
        Assert.Equal("rss-game-node-api", SecurityConfig.GetJwtAudience(config))
        Assert.Equal("1h", SecurityConfig.GetJwtExpiresIn(config))
        Assert.Equal("admin", SecurityConfig.GetAuthUsername(config))
        Assert.Equal("admin", SecurityConfig.GetAuthRole(config))
        Assert.Equal("", SecurityConfig.GetAuthPasswordHash(config))
    End Sub

    <Fact>
    Public Sub AllowedOrigins_CustomOverrideAndDefaults()
        Dim custom = CreateConfig(New Dictionary(Of String, String) From {
            {"CORS_ORIGINS", " http://a.test , http://b.test ,, "}
        })

        Assert.Equal(New String() {"http://a.test", "http://b.test"}, SecurityConfig.GetAllowedOrigins(custom))
        Assert.Contains("http://localhost:8065", SecurityConfig.GetAllowedOrigins(CreateConfig()))
    End Sub

    <Fact>
    Public Sub IsAllowedOrigin_UsesAllowlistAndLocalhostInDevelopment()
        Dim development = CreateConfig()
        Dim production = CreateConfig(New Dictionary(Of String, String) From {{"ENVIRONMENT", "production"}})

        Assert.True(SecurityConfig.IsAllowedOrigin("http://localhost:8065", development))
        Assert.True(SecurityConfig.IsAllowedOrigin("http://127.0.0.1:4321", development))
        Assert.True(SecurityConfig.IsAllowedOrigin("https://tudominio.com", development))
        Assert.False(SecurityConfig.IsAllowedOrigin("http://evil.test", development))
        Assert.False(SecurityConfig.IsAllowedOrigin("", development))

        Assert.False(SecurityConfig.IsAllowedOrigin("http://127.0.0.1:4321", production))
        Assert.True(SecurityConfig.IsAllowedOrigin("https://tudominio.com", production))
    End Sub

    <Fact>
    Public Sub IsDocsEnabled_RespectsEnvironmentAndFlag()
        Assert.True(SecurityConfig.IsDocsEnabled(CreateConfig()))
        Assert.False(SecurityConfig.IsDocsEnabled(
            CreateConfig(New Dictionary(Of String, String) From {{"API_DOCS_ENABLED", "false"}})))

        Dim production = New Dictionary(Of String, String) From {{"ENVIRONMENT", "production"}}
        Assert.False(SecurityConfig.IsDocsEnabled(CreateConfig(production)))
        Assert.True(SecurityConfig.IsDocsEnabled(CreateConfig(New Dictionary(Of String, String) From {
            {"ENVIRONMENT", "production"}, {"API_DOCS_ENABLED", "true"}
        })))
    End Sub

    <Fact>
    Public Sub ValidateAuthConfiguration_CompleteConfigIsValid()
        Assert.True(SecurityConfig.ValidateAuthConfiguration(CreateConfig(CompleteAuth), NullLogger.Instance))
    End Sub

    <Fact>
    Public Sub ValidateAuthConfiguration_DevelopmentOnlyWarns()
        Assert.False(SecurityConfig.ValidateAuthConfiguration(CreateConfig(), NullLogger.Instance))

        Dim shortSecret = New Dictionary(Of String, String) From {
            {"JWT_SECRET", "corto"},
            {"AUTH_PASSWORD_HASH", "hash"}
        }
        Assert.True(SecurityConfig.ValidateAuthConfiguration(CreateConfig(shortSecret), NullLogger.Instance))
    End Sub

    <Fact>
    Public Sub ValidateAuthConfiguration_ProductionFailsClosed()
        Dim production = New Dictionary(Of String, String) From {{"ENVIRONMENT", "production"}}

        Assert.Throws(Of InvalidOperationException)(
            Function() SecurityConfig.ValidateAuthConfiguration(CreateConfig(production), NullLogger.Instance))

        Dim shortSecret = New Dictionary(Of String, String) From {
            {"ENVIRONMENT", "production"},
            {"JWT_SECRET", "corto"},
            {"AUTH_PASSWORD_HASH", "hash"}
        }
        Assert.Throws(Of InvalidOperationException)(
            Function() SecurityConfig.ValidateAuthConfiguration(CreateConfig(shortSecret), NullLogger.Instance))
    End Sub
End Class
