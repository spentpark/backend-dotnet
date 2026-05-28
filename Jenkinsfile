pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
            args '-u root'
        }
    }

    environment {
        // Nexus Config
        NEXUS_VERSION       = "nexus3"
        NEXUS_PROTOCOL      = "http"
        NEXUS_URL           = "172.17.0.1:8081"
        NEXUS_REPOSITORY    = "nuget-nexus-repo"
        NEXUS_CREDENTIAL_ID = "nexus"

        // Sonar Config
        SONAR_HOST_URL = "http://172.17.0.1:9000"
        SONAR_TOKEN    = credentials('sonar-token')

        DOTNET_CLI_HOME = '/tmp/dotnet_cli_home'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore Dependencies') {
            steps {
                // Restauramos el principal y luego el de pruebas
                sh '''
                dotnet restore BackendVBNet.vbproj
                dotnet restore ./BackendVBNet.Tests/BackendVBNet.Tests.vbproj
                '''
            }
        }

        stage('Build & SonarQube Analysis') {
    steps {
        withCredentials([string(credentialsId: 'sonar-token', variable: 'SONAR_TOKEN')]) {
            sh '''
            echo ==> Limpiando entornos previos...
            rm -rf ./TestResults ./tools

            echo ==> Instalando SonarScanner...
            dotnet tool install dotnet-sonarscanner --tool-path ./tools

            echo ==> Iniciando análisis de SonarQube...
            ./tools/dotnet-sonarscanner begin /k:backend-vbnet \
              /d:sonar.host.url=http://172.17.0.1:9000 \
              /d:sonar.token=$SONAR_TOKEN \
              /d:sonar.exclusions="**/bin/**,**/obj/**,**/*.Tests/**" \
              /d:sonar.cs.vscoveragexml.reportsPaths="**/TestResults/**/coverage.cobertura.xml" \
              /d:sonar.vbcsharp.vstest.reportsPaths="**/TestResults/*.trx"

            echo ==> Compilando el proyecto principal...
            dotnet build BackendVBNet.sln --no-restore

            echo ==> Ejecutando pruebas unitarias y generando cobertura...
            # Cambiamos el formato a "cobertura" y añadimos un logger trx para que SonarQube también vea qué tests pasaron
            dotnet test BackendVBNet.sln --no-restore \
              --results-directory ./TestResults \
              --logger:"trx;LogFileName=resultado_pruebas.trx" \
              --collect:"XPlat Code Coverage" \
              -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

            echo ==> Finalizando análisis de SonarQube...
            ./tools/dotnet-sonarscanner end /d:sonar.token=$SONAR_TOKEN
            '''
        }
    }
}

        stage('Package') {
            steps {
                sh '''
                mkdir -p ./nupkg
                dotnet pack --configuration Release --output ./nupkg
                '''
            }
        }

        stage('Publish to Nexus') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'nexus', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASS')]) {
                    sh '''
                    echo "==> Registrando repositorio Nexus en la configuración de NuGet..."
                    dotnet nuget add source http://172.17.0.1:8081/repository/nuget-nexus-repo/ \
                      --name NexusRepo \
                      --username "${NEXUS_USER}" \
                      --password "${NEXUS_PASS}" \
                      --store-password-in-clear-text

                    echo "==> Subiendo paquete a Nexus..."
                    dotnet nuget push ./nupkg/*.nupkg \
                      --source NexusRepo
                    '''
                }
            }
        }
    }

    post {
        always {
            cleanWs()
            echo "Pipeline finished"
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
        }
    }
}