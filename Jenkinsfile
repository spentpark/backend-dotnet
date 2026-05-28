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
                sh 'dotnet restore'
            }
        }

        stage('Build & SonarQube Analysis') {
            steps {
                withCredentials([string(credentialsId: 'sonar-token', variable: 'SONAR_TOKEN')]) {
                    sh '''
                    echo "==> Limpiando entornos previos..."
                    rm -rf ./TestResults ./tools

                    echo "==> Instalando SonarScanner de manera local..."
                    dotnet tool install dotnet-sonarscanner --tool-path ./tools

                    echo "==> Iniciando análisis de SonarQube..."
                    ./tools/dotnet-sonarscanner begin \
                      /k:"backend-vbnet" \
                      /d:sonar.host.url="http://172.17.0.1:9000" \
                      /d:sonar.token="${SONAR_TOKEN}" \
                      /d:sonar.exclusions="**/bin/**,**/obj/**,**/*.Tests/**" \
                      /d:sonar.cs.opencover.reportsPaths="TestResults/coverage.xml"

                    echo "==> Compilando el proyecto principal..."
                    dotnet build --no-restore

                    echo "==> Ejecutando pruebas unitarias y generando cobertura..."
                    # Quitamos --no-build y apuntamos directo al proyecto usando los flags limpios de coverlet
                    dotnet test ./BackendVBNet.Tests/BackendVBNet.Tests.vbproj \
                      --results-directory ./TestResults \
                      --collect:"XPlat Code Coverage" \
                      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

                    echo "==> Buscando y unificando archivos de cobertura..."
                    mkdir -p ./TestResults
                    if [ -f ./TestResults/*/coverage.opencover.xml ]; then
                        cp ./TestResults/*/coverage.opencover.xml ./TestResults/coverage.xml
                        echo "¡Archivo de cobertura unificado con éxito en ./TestResults/coverage.xml!"
                    else
                        echo "ERROR: ¡El archivo de cobertura no fue generado!"
                        exit 1
                    fi

                    echo "==> Finalizando análisis y enviando datos a SonarQube..."
                    ./tools/dotnet-sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
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