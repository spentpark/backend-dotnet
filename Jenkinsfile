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
        NEXUS_REPOSITORY    = "nuget-nexus-repo" // Asegúrate de crear un repo tipo 'nuget' en Nexus
        NEXUS_CREDENTIAL_ID = "nexus"

        // Sonar Config
        SONAR_HOST_URL = "http://172.17.0.1:9000"
        SONAR_TOKEN    = credentials('sonar-token') //"squ_d27dacd45a6c18772d7e941fd44e1617cf5c4c38"

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

        stage('Build') {
            steps {
                sh 'dotnet build --no-restore'
            }
        }

        stage('Run Tests') {
            steps {
                sh 'dotnet test --no-build'
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withCredentials([string(credentialsId: 'sonar-token', variable: 'SONAR_TOKEN')]) {
                    sh '''
                    SONAR_VERSION="5.0.1.3006"
                    SONAR_DIR="sonar-scanner-${SONAR_VERSION}-linux"

                    # 1. Asegurar que unzip esté instalado en este contenedor efímero
                    if [ ! -f "${SONAR_DIR}/bin/sonar-scanner" ]; then
                        echo "Instalando herramientas de descompresión..."
                        apt-get update && apt-get install -y unzip
                        
                        echo "Descargando sonar-scanner..."
                        curl -fL "https://binaries.sonarsource.com/Distribution/sonar-scanner-cli/sonar-scanner-cli-${SONAR_VERSION}-linux.zip" -o sonar-scanner.zip
                        unzip -q sonar-scanner.zip
                        rm sonar-scanner.zip
                    fi

                    # 2. Ejecutar el scanner
                    ./${SONAR_DIR}/bin/sonar-scanner \
                    -Dsonar.projectKey=backend-vbnet \
                    -Dsonar.sources=. \
                    -Dsonar.exclusions=**/bin/**,**/obj/** \
                    -Dsonar.host.url=http://172.17.0.1:9000 \
                    -Dsonar.token=${SONAR_TOKEN}
                    '''
                }
            }
        }

        stage('Package') {
            steps {
                sh 'dotnet pack --configuration Release --output ./nupkg'
            }
        }

        stage('Publish to Nexus') {
            steps {
                withCredentials([usernamePassword(credentialsId: "${NEXUS_CREDENTIAL_ID}", usernameVariable: 'USER', passwordVariable: 'PASS')]) {
                    sh '''
                        dotnet nuget push ./nupkg/*.nupkg \
                          --source http://${NEXUS_URL}/repository/${NEXUS_REPOSITORY}/ \
                          --api-key $PASS
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