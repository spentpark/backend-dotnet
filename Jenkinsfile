pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
            args '-u root'
        }
    }

    environment {
        NEXUS_URL           = "172.17.0.1:8081"
        NEXUS_REPOSITORY    = "nuget-nexus-repo"
        NEXUS_CREDENTIAL_ID = "nexus"
        SONAR_HOST_URL      = "http://172.17.0.1:9000"
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore Dependencies') {
            steps {
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
                        echo "==> Limpiando entornos previos..."
                        rm -rf ./TestResults ./tools

                        echo "==> Instalando SonarScanner..."
                        dotnet tool install dotnet-sonarscanner --tool-path ./tools

                        echo "==> Iniciando análisis de SonarQube..."
                        ./tools/dotnet-sonarscanner begin \
                          /k:backend-vbnet \
                          /d:sonar.host.url=${SONAR_HOST_URL} \
                          /d:sonar.token=${SONAR_TOKEN} \
                          /d:sonar.exclusions=**/bin/**,**/obj/**,**/*.Tests/** \
                          /d:sonar.vbnet.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml \
                          /d:sonar.vbnet.vstest.reportsPaths=**/TestResults/*.trx

                        echo "==> Compilando..."
                        dotnet build BackendVBNet.sln --no-restore

                        echo "==> Ejecutando pruebas y generando cobertura..."
                        dotnet test BackendVBNet.sln \
                          --no-restore \
                          --results-directory ./TestResults \
                          --logger:trx;LogFileName=resultado_pruebas.trx \
                          --collect:"XPlat Code Coverage" \
                          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

                        echo "==> Finalizando análisis de SonarQube..."
                        ./tools/dotnet-sonarscanner end /d:sonar.token=${SONAR_TOKEN}
                    '''
                }
            }
        }

        stage('Package') {
            steps {
                sh '''
                    mkdir -p ./nupkg
                    dotnet pack BackendVBNet.vbproj \
                      --configuration Release \
                      --output ./nupkg
                '''
            }
        }

        stage('Publish to Nexus') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: "${NEXUS_CREDENTIAL_ID}",
                    usernameVariable: 'NEXUS_USER',
                    passwordVariable: 'NEXUS_PASS'
                )]) {
                    sh '''
                        echo "==> Registrando repositorio Nexus..."
                        dotnet nuget add source http://${NEXUS_URL}/repository/${NEXUS_REPOSITORY}/ \
                          --name NexusRepo \
                          --username ${NEXUS_USER} \
                          --password ${NEXUS_PASS} \
                          --store-password-in-clear-text

                        echo "==> Subiendo paquete a Nexus..."
                        dotnet nuget push ./nupkg/*.nupkg --source NexusRepo
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
            echo "Pipeline succeeded!"
        }
        failure {
            echo "Pipeline failed!"
        }
    }
}