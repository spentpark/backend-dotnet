pipeline {
    // El resto de pipelines del entorno usan 'agent any'. Con un agente docker
    // (image 'mcr.microsoft.com/dotnet/sdk:8.0') Jenkins no lo aprovisionaba y
    // el build fallaba sin nodo ni workspace:
    //   MissingContextVariableException: Required context class hudson.FilePath
    //   is missing ... surround the step with a step that provides this, such as: node
    agent any

    environment {
        NEXUS_HOST_URL      = "192.168.1.82:8081"
        NEXUS_REPOSITORY    = "nuget-nexus-repo"
        NEXUS_CREDENTIAL_ID = "nexus"

        SONAR_HOST_URL      = "http://192.168.1.82:9000"
        SONAR_PROJECT_KEY   = "backend-vbnet"

        NUGET_OFFICIAL      = "https://api.nuget.org/v3/index.json"
        MIN_COVERAGE        = "93"
        DOTNET_CHANNEL      = "8.0"
    }

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20'))
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Preparar SDK .NET') {
            steps {
                // Idempotente: si el agente ya tiene un SDK 8.x no hace nada; si no,
                // lo instala en $HOME/.dotnet (sobrevive al cleanWs del final).
                sh '''
                    if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^8\\.'; then
                        echo "==> SDK .NET disponible: $(dotnet --version)"
                    else
                        echo "==> Instalando .NET SDK ${DOTNET_CHANNEL} en $HOME/.dotnet ..."
                        curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
                        bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$HOME/.dotnet" --no-path
                        "$HOME/.dotnet/dotnet" --version
                    fi
                '''
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    sh '''
                        if [ -x "$HOME/.dotnet/dotnet" ]; then
                            export DOTNET_ROOT="$HOME/.dotnet"
                            export PATH="$DOTNET_ROOT:$PATH"
                        fi

                        echo "==> Diagnostico del agente:"
                        dotnet --version
                        dotnet --list-sdks
                        echo "--- Origenes NuGet configurados ---"
                        dotnet nuget list source
                        echo "--- Revision construida ---"
                        git rev-parse HEAD || true
                    '''

                    // 1) Camino normal: nuget.org.
                    def direct = sh(
                        script: '''
                            if [ -x "$HOME/.dotnet/dotnet" ]; then
                                export DOTNET_ROOT="$HOME/.dotnet"
                                export PATH="$DOTNET_ROOT:$PATH"
                            fi
                            dotnet restore BackendVBNet.sln --source "$NUGET_OFFICIAL"
                        ''',
                        returnStatus: true
                    )

                    if (direct == 0) {
                        echo 'Restore completado con nuget.org.'
                    } else {
                        // 2) Plan B: si el agente no sale a internet, usar Nexus.
                        echo 'El restore directo fallo. Reintentando contra Nexus...'
                        def nexusSource = "http://${env.NEXUS_HOST_URL}/repository/${env.NEXUS_REPOSITORY}/index.json"

                        try {
                            withCredentials([usernamePassword(
                                credentialsId: env.NEXUS_CREDENTIAL_ID,
                                usernameVariable: 'NEXUS_USER',
                                passwordVariable: 'NEXUS_PASS'
                            )]) {
                                sh """
                                    export DOTNET_ROOT="\$HOME/.dotnet"
                                    export PATH="\$DOTNET_ROOT:\$PATH"
                                    dotnet nuget remove source nuget.org || true
                                    dotnet nuget add source '${nexusSource}' \
                                      --name nexus \
                                      --username "\$NEXUS_USER" \
                                      --password "\$NEXUS_PASS" \
                                      --store-password-in-clear-text
                                    dotnet restore BackendVBNet.sln --source '${nexusSource}'
                                """
                            }
                        } catch (err) {
                            error("""Restore de paquetes fallido.
  - nuget.org no es accesible desde el agente.
  - Nexus (${nexusSource}) tampoco respondio, o falta la credencial '${env.NEXUS_CREDENTIAL_ID}'.
  Revisa NEXUS_HOST_URL / NEXUS_REPOSITORY / esa credencial, o habilita salida a internet.
  Error original: ${err.message}""")
                        }
                    }
                }
            }
        }

        stage('Build & SonarQube Analysis') {
            steps {
                // try/finally no es un "step": va dentro de script {} (Declarative).
                script {
                    withCredentials([string(credentialsId: 'sonar-token', variable: 'SONAR_TOKEN')]) {
                        sh '''
                            if [ -x "$HOME/.dotnet/dotnet" ]; then
                                export DOTNET_ROOT="$HOME/.dotnet"
                                export PATH="$DOTNET_ROOT:$PATH"
                            fi

                            echo "==> Limpiando salidas previas..."
                            rm -rf ./TestResults ./tools ./nupkg

                            echo "==> Instalando SonarScanner..."
                            dotnet tool install dotnet-sonarscanner --tool-path ./tools

                            echo "==> Iniciando analisis de SonarQube..."
                            ./tools/dotnet-sonarscanner begin \
                              /k:"${SONAR_PROJECT_KEY}" \
                              /d:sonar.host.url="${SONAR_HOST_URL}" \
                              /d:sonar.token="${SONAR_TOKEN}" \
                              '/d:sonar.exclusions=**/bin/**,**/obj/**,**/*.Tests/**' \
                              '/d:sonar.vbnet.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml' \
                              '/d:sonar.vbnet.vstest.reportsPaths=**/TestResults/*.trx'
                        '''

                        try {
                            sh '''
                                if [ -x "$HOME/.dotnet/dotnet" ]; then
                                    export DOTNET_ROOT="$HOME/.dotnet"
                                    export PATH="$DOTNET_ROOT:$PATH"
                                fi

                                echo "==> Compilando..."
                                dotnet build BackendVBNet.sln --no-restore --configuration Debug

                                echo "==> Ejecutando pruebas y generando cobertura..."
                                dotnet test BackendVBNet.sln \
                                  --no-restore \
                                  --results-directory ./TestResults \
                                  "--logger:trx;LogFileName=resultado_pruebas.trx" \
                                  "--collect:XPlat Code Coverage" \
                                  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

                                echo "==> Reportes generados:"
                                find ./TestResults -name "coverage.*.xml" -type f
                            '''

                            // Informativo: el umbral de 93% lo aplica el propio
                            // proyecto de pruebas (coverlet.msbuild: Threshold).
                            def reports = findFiles(glob: '**/coverage.cobertura.xml')
                            if (reports.length == 0) {
                                echo 'AVISO: no se encontro coverage.cobertura.xml.'
                            } else {
                                def matcher = readFile(reports[0].path) =~ /line-rate="([0-9.]+)"/
                                if (matcher.find()) {
                                    def pct = (matcher.group(1) as Double) * 100
                                    echo "Cobertura de lineas: ${pct}% (minimo exigido: ${env.MIN_COVERAGE}%)"
                                } else {
                                    echo 'AVISO: no se pudo leer line-rate del reporte de cobertura.'
                                }
                            }
                        } finally {
                            // Se finaliza el analisis aunque fallen build o pruebas, para
                            // que SonarQube reciba los datos (antes no se llamaba a end).
                            sh '''
                                if [ -x "$HOME/.dotnet/dotnet" ]; then
                                    export DOTNET_ROOT="$HOME/.dotnet"
                                    export PATH="$DOTNET_ROOT:$PATH"
                                fi
                                echo "==> Finalizando analisis de SonarQube..."
                                ./tools/dotnet-sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
                            '''
                        }
                    }
                }
            }
        }

        stage('Package') {
            steps {
                sh '''
                    if [ -x "$HOME/.dotnet/dotnet" ]; then
                        export DOTNET_ROOT="$HOME/.dotnet"
                        export PATH="$DOTNET_ROOT:$PATH"
                    fi
                    mkdir -p ./nupkg
                    dotnet pack BackendVBNet.vbproj \
                      --configuration Release \
                      --output ./nupkg
                '''
            }
        }
    }

    post {
        always {
            // script {} es obligatorio para usar if/def dentro de post.
            script {
                // Sin workspace (agente perdido) estos pasos lanzan
                // "Required context class hudson.FilePath is missing" y tapan
                // el error real: se comprueba antes.
                if (env.WORKSPACE) {
                    archiveArtifacts artifacts: 'TestResults/**/*.trx,TestResults/**/coverage.*.xml,BackendVBNet.Tests/TestResults/**/coverage.*.xml', allowEmptyArchive: true
                    cleanWs()
                } else {
                    echo 'Sin workspace en este build: no hay artefactos que archivar.'
                }
            }
            echo "Pipeline finalizado"
        }
        success {
            echo "Pipeline OK"
        }
        failure {
            echo "Pipeline FAILED"
        }
    }
}
