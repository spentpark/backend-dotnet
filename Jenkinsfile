pipeline {
    // El resto de pipelines del entorno usan 'agent any'. Con un agente docker
    // (image 'mcr.microsoft.com/dotnet/sdk:8.0') Jenkins no lo aprovisionaba y
    // el build fallaba sin nodo ni workspace:
    //   MissingContextVariableException: Required context class hudson.FilePath
    //   is missing ... surround the step with a step that provides this, such as: node
    agent any

    parameters {
        booleanParam(name: 'DEPLOY_ENABLED', defaultValue: true,
            description: 'Activa o desactiva el despliegue en el servidor Docker de 192.168.1.82.')

        string(name: 'DEPLOY_HOST', defaultValue: '192.168.1.82',
            description: 'IP o host del servidor Docker donde se despliega la aplicación.')

        string(name: 'SSH_CREDENTIAL_ID', defaultValue: 'docker-server-ssh',
            description: 'Credencial Jenkins "SSH Username with private key" con acceso al servidor Docker.')

        string(name: 'DEPLOY_DIR', defaultValue: '/home/spentpark/gitDeploy/gitdotnet',
            description: 'Carpeta del servidor donde está el docker-compose.yml del stack .NET. Vacío = se detecta desde las labels del contenedor backend-dotnet.')

        string(name: 'COMPOSE_FILE', defaultValue: '',
            description: 'Archivo compose (vacío = se detecta desde el contenedor; si no, docker-compose.yml).')

        string(name: 'COMPOSE_SERVICE', defaultValue: 'backend-dotnet',
            description: 'Nombre del servicio en docker-compose.yml (el compose del stack .NET usa backend-dotnet / frontend-dotnet).')

        string(name: 'CONTAINER_NAME', defaultValue: 'backend-dotnet',
            description: 'Nombre del contenedor que se detiene/levanta.')

        string(name: 'APP_PORT', defaultValue: '5000',
            description: 'Puerto del host publicado por el contenedor, usado para verificar que la aplicación arrancó.')

        string(name: 'PROBE_PATH', defaultValue: '/api/saludo',
            description: 'Ruta HTTP de verificación (se exige respuesta 2xx). /api/saludo no toca la base de datos; /api/hello además comprueba MariaDB.')
    }

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

        stage('Preparar entorno .NET') {
            steps {
                // 1) libicu: sin el, cualquier binario .NET aborta con
                //    "Couldn't find a valid ICU package installed on the system".
                // 2) SDK 8: se reutiliza si ya esta, si no se instala en $HOME/.dotnet.
                // 3) .ci-env: variables que los demas stages cargan con '. ./.ci-env'
                //    (cada sh de Jenkins arranca en un shell nuevo).
                sh '''
                    echo "==> Comprobando libicu..."
                    ICU_OK=0
                    if ldconfig -p 2>/dev/null | grep -q 'libicu'; then
                        ICU_OK=1
                        echo "==> libicu presente."
                    elif command -v apt-get >/dev/null 2>&1 && command -v sudo >/dev/null 2>&1; then
                        echo "==> Instalando libicu-dev (necesario para .NET)..."
                        if sudo -n apt-get update >/dev/null 2>&1 && sudo -n apt-get install -y libicu-dev >/dev/null 2>&1; then
                            ICU_OK=1
                            echo "==> libicu-dev instalado."
                        fi
                    fi

                    echo "==> Comprobando .NET SDK..."
                    # El SDK instalado por builds anteriores vive en $HOME/.dotnet:
                    # se pone en el PATH antes de comprobar para reutilizarlo.
                    if [ -x "$HOME/.dotnet/dotnet" ]; then
                        export DOTNET_ROOT="$HOME/.dotnet"
                        export PATH="$DOTNET_ROOT:$PATH"
                    fi

                    HAS_SDK8=0
                    if [ "$ICU_OK" = "1" ] && command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^8\\.'; then
                        HAS_SDK8=1
                    fi
                    if [ "$HAS_SDK8" = "0" ]; then
                        echo "==> Instalando .NET SDK ${DOTNET_CHANNEL} en $HOME/.dotnet ..."
                        curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
                        bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$HOME/.dotnet" --no-path
                    fi

                    {
                        echo 'export DOTNET_ROOT="$HOME/.dotnet"'
                        echo 'export PATH="$DOTNET_ROOT:$PATH"'
                        if [ "$ICU_OK" != "1" ]; then
                            echo 'export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1'
                        fi
                    } > .ci-env

                    echo "==> Entorno CI generado (.ci-env):"
                    cat .ci-env
                    . ./.ci-env
                    echo "==> dotnet: $(dotnet --version)"
                '''
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    sh '''
                        . ./.ci-env

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
                            . ./.ci-env
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
                                    . ./.ci-env
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
                            . ./.ci-env

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
                                . ./.ci-env

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
                                . ./.ci-env
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
                    . ./.ci-env
                    mkdir -p ./nupkg
                    dotnet pack BackendVBNet.vbproj \
                      --configuration Release \
                      --output ./nupkg
                '''
            }
        }

        stage('Deploy backend-dotnet en servidor Docker (192.168.1.82)') {
            when {
                expression { return params.DEPLOY_ENABLED == null || params.DEPLOY_ENABLED }
            }
            steps {
                withCredentials([sshUserPrivateKey(
                        credentialsId: params.SSH_CREDENTIAL_ID ?: 'docker-server-ssh',
                        keyFileVariable: 'SSH_KEY_FILE',
                        usernameVariable: 'SSH_USERNAME')]) {
                    withEnv([
                        "DEPLOY_HOST=${params.DEPLOY_HOST ?: '192.168.1.82'}",
                        "DEPLOY_DIR=${params.DEPLOY_DIR ?: ''}",
                        "COMPOSE_FILE=${params.COMPOSE_FILE ?: ''}",
                        "COMPOSE_SERVICE=${params.COMPOSE_SERVICE ?: 'backend-dotnet'}",
                        "CONTAINER_NAME=${params.CONTAINER_NAME ?: 'backend-dotnet'}",
                        "APP_PORT=${params.APP_PORT ?: '5000'}",
                        "PROBE_PATH=${params.PROBE_PATH ?: '/api/saludo'}"
                    ]) {
                        sh '''set -eu

# ---------------------------------------------------------------------------
# Despliegue del backend .NET por SSH en el servidor Docker remoto.
#
#  1. Comprueba SSH, Docker y Docker Compose en el servidor.
#  2. Localiza el docker-compose.yml del stack .NET (por defecto
#     ~/gitDeploy/gitdotnet) y valida el servicio backend-dotnet.
#  3. Sincroniza el código al contexto de build (DEPLOY_DIR/backend-dotnet),
#     excluyendo las salidas del CI (bin, obj, TestResults, tools, nupkg...).
#  4. Construye la imagen en el servidor ANTES de detener nada: si la
#     compilación Docker falla, la versión anterior sigue corriendo.
#  5. Detiene (si estaba) y levanta el contenedor con la imagen recién
#     construida, comprueba que usa ESA imagen y espera el health check HTTP.
# ---------------------------------------------------------------------------

: "${DEPLOY_HOST:?DEPLOY_HOST no definido}"
: "${SSH_KEY_FILE:?SSH_KEY_FILE no definido}"
: "${SSH_USERNAME:?SSH_USERNAME no definido}"
: "${COMPOSE_SERVICE:?COMPOSE_SERVICE no definido}"
: "${CONTAINER_NAME:?CONTAINER_NAME no definido}"
: "${APP_PORT:?APP_PORT no definido}"
: "${PROBE_PATH:?PROBE_PATH no definido}"
: "${DEPLOY_DIR:=}"
: "${COMPOSE_FILE:=}"

WORKSPACE="${WORKSPACE:-$(pwd)}"
REMOTE_REPO_NAME="backend-dotnet"

SSH_OPTS="-i ${SSH_KEY_FILE} -o BatchMode=yes -o IdentitiesOnly=yes -o PreferredAuthentications=publickey -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR -o ConnectTimeout=20 -o ServerAliveInterval=30 -o ServerAliveCountMax=4"
SSH_TARGET="${SSH_USERNAME}@${DEPLOY_HOST}"

echo "================================================================"
echo " 1/5 Verificando servidor ${SSH_TARGET}"
echo "================================================================"

ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s" <<'REMOTE_SSH'
set -eu
echo "  - Servidor: $(hostname)"
docker --version
if docker compose version >/dev/null 2>&1; then
    echo "  - Docker Compose v2 disponible (comando: docker compose)"
else
    docker-compose --version
    echo "  - Docker Compose v1 disponible (comando: docker-compose)"
fi
REMOTE_SSH

echo "================================================================"
echo " 2/5 Detectando docker-compose.yml del stack .NET"
echo "================================================================"

# Si el contenedor ya existe, sus labels dicen dónde está el compose; si no
# (primer despliegue), se usa la ruta documentada del stack: ~/gitDeploy/gitdotnet.
DETECTED="$(ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${CONTAINER_NAME}" <<'REMOTE_DETECT'
CF="$(docker inspect -f '{{index .Config.Labels "com.docker.compose.project.config_files"}}' "$1" 2>/dev/null || true)"
WD="$(docker inspect -f '{{index .Config.Labels "com.docker.compose.project.working_dir"}}' "$1" 2>/dev/null || true)"
echo "CONFIG_FILES=${CF}"
echo "COMPOSE_WORKDIR=${WD}"
DEFAULT_DIR="${HOME}/gitDeploy/gitdotnet"
if [ -f "${DEFAULT_DIR}/docker-compose.yml" ]; then
    echo "DEFAULT_DEPLOY_DIR=${DEFAULT_DIR}"
else
    echo "DEFAULT_DEPLOY_DIR="
fi
REMOTE_DETECT
)"

CONFIG_FILES="$(echo "${DETECTED}" | sed -n 's/^CONFIG_FILES=//p')"
COMPOSE_WORKDIR="$(echo "${DETECTED}" | sed -n 's/^COMPOSE_WORKDIR=//p')"
DEFAULT_DEPLOY_DIR="$(echo "${DETECTED}" | sed -n 's/^DEFAULT_DEPLOY_DIR=//p')"
case "${CONFIG_FILES}" in
    "<no value>"|"null") CONFIG_FILES="" ;;
esac
case "${COMPOSE_WORKDIR}" in
    "<no value>"|"null") COMPOSE_WORKDIR="" ;;
esac

# El parámetro DEPLOY_DIR manda si viene informado; si no, se detecta.
if [ -z "${DEPLOY_DIR}" ] && [ -n "${CONFIG_FILES}" ]; then
    FIRST_COMPOSE="$(echo "${CONFIG_FILES}" | cut -d, -f1)"
    DEPLOY_DIR="$(dirname "${FIRST_COMPOSE}")"
    if [ -z "${COMPOSE_FILE}" ]; then
        COMPOSE_FILE="$(basename "${FIRST_COMPOSE}")"
    fi
    echo "  - Detectado desde labels Docker: ${DEPLOY_DIR}/${COMPOSE_FILE}"
elif [ -z "${DEPLOY_DIR}" ] && [ -n "${COMPOSE_WORKDIR}" ]; then
    DEPLOY_DIR="${COMPOSE_WORKDIR}"
    echo "  - DEPLOY_DIR detectado desde working_dir Docker: ${DEPLOY_DIR}"
elif [ -z "${DEPLOY_DIR}" ] && [ -n "${DEFAULT_DEPLOY_DIR}" ]; then
    DEPLOY_DIR="${DEFAULT_DEPLOY_DIR}"
    echo "  - DEPLOY_DIR por defecto del stack .NET: ${DEPLOY_DIR}"
elif [ -z "${DEPLOY_DIR}" ]; then
    echo "ERROR: no se pudo detectar el docker-compose.yml en el servidor." >&2
    echo "       El contenedor ${CONTAINER_NAME} no existe o no fue creado con docker compose." >&2
    echo "       Relanza el job indicando el parámetro DEPLOY_DIR (ej: /home/spentpark/gitDeploy/gitdotnet)." >&2
    exit 1
else
    echo "  - DEPLOY_DIR indicado por parámetro: ${DEPLOY_DIR}"
fi

# Si COMPOSE_FILE llega como ruta absoluta, se separa carpeta + archivo
case "${COMPOSE_FILE}" in
    */*)
        DEPLOY_DIR="$(dirname "${COMPOSE_FILE}")"
        COMPOSE_FILE="$(basename "${COMPOSE_FILE}")"
        ;;
esac

if [ -z "${COMPOSE_FILE}" ]; then
    COMPOSE_FILE="docker-compose.yml"
    echo "  - COMPOSE_FILE por defecto: ${COMPOSE_FILE}"
fi

# Validaciones de seguridad: DEPLOY_DIR se usa para borrar/crear el contexto
# de build, así que nunca debe quedar vacío, ser "/" ni contener "..".
case "${DEPLOY_DIR}" in
    "/") echo "ERROR: DEPLOY_DIR no puede ser /" >&2; exit 1 ;;
    *..*) echo "ERROR: DEPLOY_DIR no puede contener '..': ${DEPLOY_DIR}" >&2; exit 1 ;;
    *[!A-Za-z0-9._/-]*) echo "ERROR: DEPLOY_DIR tiene caracteres no permitidos: ${DEPLOY_DIR}" >&2; exit 1 ;;
    /*) ;;
    *) echo "ERROR: DEPLOY_DIR debe ser una ruta absoluta: ${DEPLOY_DIR}" >&2; exit 1 ;;
esac

case "${CONTAINER_NAME}" in
    ""|*[!A-Za-z0-9._-]*) echo "ERROR: CONTAINER_NAME inválido: ${CONTAINER_NAME}" >&2; exit 1 ;;
esac
case "${COMPOSE_SERVICE}" in
    ""|*[!A-Za-z0-9._-]*) echo "ERROR: COMPOSE_SERVICE inválido: ${COMPOSE_SERVICE}" >&2; exit 1 ;;
esac
case "${COMPOSE_FILE}" in
    ""|*[!A-Za-z0-9._-]*) echo "ERROR: COMPOSE_FILE inválido: ${COMPOSE_FILE}" >&2; exit 1 ;;
esac
case "${APP_PORT}" in
    ""|*[!0-9]*) echo "ERROR: APP_PORT inválido: ${APP_PORT}" >&2; exit 1 ;;
esac
case "${PROBE_PATH}" in
    /*) ;;
    *) echo "ERROR: PROBE_PATH debe empezar con /: ${PROBE_PATH}" >&2; exit 1 ;;
esac
case "${PROBE_PATH}" in
    *[!A-Za-z0-9._/?=-]*) echo "ERROR: PROBE_PATH tiene caracteres no permitidos: ${PROBE_PATH}" >&2; exit 1 ;;
esac

REMOTE_REPO="${DEPLOY_DIR}/${REMOTE_REPO_NAME}"
REMOTE_TMP="${DEPLOY_DIR}/.${REMOTE_REPO_NAME}.tmp"

echo "  - Contexto de build: ${REMOTE_REPO}"
echo "  - Servicio compose: ${COMPOSE_SERVICE} (contenedor ${CONTAINER_NAME})"

echo "================================================================"
echo " 3/5 Sincronizando código a ${SSH_TARGET}:${REMOTE_REPO}"
echo "================================================================"

# El Dockerfile reconstruye la imagen en el servidor: se excluyen las salidas
# del CI y del tooling local (bin, obj, TestResults, tools, nupkg, .ci-env).
# appsettings.json y .dockerignore SÍ se copian (los necesita el build).
TAR_EXCLUDES=""
for EXCL in ./.git ./.vscode ./.idea ./bin ./obj ./BackendVBNet.Tests/bin ./BackendVBNet.Tests/obj ./TestResults ./BackendVBNet.Tests/TestResults ./tools ./nupkg ./.ci-env; do
    TAR_EXCLUDES="${TAR_EXCLUDES} --exclude=${EXCL}"
done

tar czf - -C "${WORKSPACE}" ${TAR_EXCLUDES} . | ssh ${SSH_OPTS} "${SSH_TARGET}" "set -e; mkdir -p '${DEPLOY_DIR}'; rm -rf '${REMOTE_TMP}'; mkdir -p '${REMOTE_TMP}'; tar xzf - -C '${REMOTE_TMP}'; rm -rf '${REMOTE_REPO}'; mv '${REMOTE_TMP}' '${REMOTE_REPO}'; echo '  - Código sincronizado correctamente (backend-dotnet)'"

# Se valida el compose después de sincronizar para que el contexto de build exista.
echo "  - Validando compose y servicio del backend..."
SERVICE_META="$(ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${DEPLOY_DIR} ${COMPOSE_FILE} ${COMPOSE_SERVICE}" <<'REMOTE_PREFLIGHT'
set -eu
compose_dir="$1"; compose_file="$2"; service="$3"
cd "${compose_dir}"
if [ ! -f "${compose_file}" ]; then
    echo "ERROR: no existe ${compose_dir}/${compose_file} en el servidor." >&2
    exit 1
fi
if docker compose version >/dev/null 2>&1; then
    DC="docker compose"
else
    DC="docker-compose"
fi
services="$($DC -f "${compose_file}" config --services)"
echo "  - Servicios definidos: $(echo ${services})"
echo "${services}" | grep -qx "${service}" || {
    echo "ERROR: el servicio '${service}' no está definido en ${compose_dir}/${compose_file}." >&2
    exit 1
}
service_config="$($DC -f "${compose_file}" config "${service}")"
if echo "${service_config}" | grep -qE '^[[:space:]]+build:'; then
    echo "HAS_BUILD=1"
else
    echo "HAS_BUILD=0"
fi
image="$(echo "${service_config}" | sed -n 's/^[[:space:]]*image:[[:space:]]*//p' | head -n 1)"
echo "COMPOSE_IMAGE=${image}"
REMOTE_PREFLIGHT
)"
echo "${SERVICE_META}"
HAS_BUILD="$(echo "${SERVICE_META}" | sed -n 's/^HAS_BUILD=//p')"
COMPOSE_IMAGE="$(echo "${SERVICE_META}" | sed -n 's/^COMPOSE_IMAGE=//p')"

if [ -n "${COMPOSE_IMAGE}" ]; then
    BUILD_MODE="docker"
    IMAGE_TAG="${COMPOSE_IMAGE}"
    echo "  - Imagen definida en el compose: ${IMAGE_TAG}"
elif [ "${HAS_BUILD}" = "1" ]; then
    BUILD_MODE="compose"
    IMAGE_TAG=""
    echo "  - El servicio no define 'image:'; se usará docker compose build"
else
    echo "ERROR: el servicio '${COMPOSE_SERVICE}' no define 'image:' ni 'build:' en ${COMPOSE_FILE}." >&2
    echo "       Añádelo al docker-compose.yml del servidor y reintenta." >&2
    exit 1
fi

if [ -n "${IMAGE_TAG}" ]; then
    case "${IMAGE_TAG}" in
        *[!A-Za-z0-9._/:@-]*) echo "ERROR: nombre de imagen inválido: '${IMAGE_TAG}'" >&2; exit 1 ;;
    esac
fi

echo "================================================================"
echo " 4/5 Construyendo imagen en el servidor"
echo "================================================================"

BUILT_IMAGE_ID=""
if [ "${BUILD_MODE}" = "docker" ]; then
    # Se construye ANTES de detener el contenedor: si falla, sigue la versión vieja.
    ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${REMOTE_REPO} ${IMAGE_TAG}" <<'REMOTE_BUILD'
set -eu
repo="$1"; image="$2"
if [ ! -f "${repo}/Dockerfile" ]; then
    echo "ERROR: no se encontró ${repo}/Dockerfile en el servidor." >&2
    exit 1
fi
docker build -t "${image}" "${repo}"
echo "  - Imagen ${image} construida correctamente"
REMOTE_BUILD
    BUILT_IMAGE_ID="$(ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${IMAGE_TAG}" <<'REMOTE_IMAGE_ID'
set -eu
docker image inspect -f '{{.Id}}' "$1"
REMOTE_IMAGE_ID
)"
    echo "  - ID de la imagen construida: ${BUILT_IMAGE_ID}"
else
    ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${DEPLOY_DIR} ${COMPOSE_FILE} ${COMPOSE_SERVICE}" <<'REMOTE_COMPOSE_BUILD'
set -eu
compose_dir="$1"; compose_file="$2"; service="$3"
cd "${compose_dir}"
if docker compose version >/dev/null 2>&1; then
    DC="docker compose"
else
    DC="docker-compose"
fi
$DC -f "${compose_file}" build "${service}"
echo "  - Imagen construida correctamente (docker compose build)"
REMOTE_COMPOSE_BUILD
fi

echo "================================================================"
echo " 5/5 Deteniendo (si aplica) y levantando ${CONTAINER_NAME}"
echo "================================================================"

DEPLOY_LOG="$(mktemp 2>/dev/null || echo /tmp/deploy-backend-dotnet.log)"
ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${DEPLOY_DIR} ${COMPOSE_FILE} ${COMPOSE_SERVICE} ${CONTAINER_NAME}" > "${DEPLOY_LOG}" <<'REMOTE_DEPLOY'
set -eu
compose_dir="$1"; compose_file="$2"; service="$3"; container="$4"
cd "${compose_dir}"
if docker compose version >/dev/null 2>&1; then
    DC="docker compose"
else
    DC="docker-compose"
fi

if [ "$(docker inspect -f "{{.State.Running}}" "${container}" 2>/dev/null || true)" = "true" ]; then
    echo "  - El contenedor ${container} ESTABA EN EJECUCIÓN: se detiene para desplegar la nueva versión."
    $DC -f "${compose_file}" stop "${service}"
    WAS_RUNNING="si"
else
    echo "  - El contenedor ${container} NO estaba corriendo (o no existe): se levanta desde cero."
    WAS_RUNNING="no"
fi

echo "  - Levantando ${service} con la imagen recién construida..."
$DC -f "${compose_file}" up -d --force-recreate "${service}"

sleep 2
status="$(docker inspect -f "{{.State.Status}}" "${container}" 2>/dev/null || echo desconocido)"
image="$(docker inspect -f "{{.Image}}" "${container}" 2>/dev/null || echo desconocida)"
echo "  - Contenedor ${container}: estado=${status} imagen=${image} (corría antes: ${WAS_RUNNING})"
echo "CONTAINER_IMAGE_ID=${image}"
REMOTE_DEPLOY
cat "${DEPLOY_LOG}"
CONTAINER_IMAGE_ID="$(sed -n 's/^CONTAINER_IMAGE_ID=//p' "${DEPLOY_LOG}")"
rm -f "${DEPLOY_LOG}"

# Si construimos una imagen concreta, el contenedor debe estar corriendo ESA imagen.
if [ -n "${BUILT_IMAGE_ID}" ] && [ -n "${CONTAINER_IMAGE_ID}" ] && [ "${BUILT_IMAGE_ID}" != "${CONTAINER_IMAGE_ID}" ]; then
    echo "ERROR: el contenedor ${CONTAINER_NAME} no está usando la imagen recién construida." >&2
    echo "       Imagen construida:    ${BUILT_IMAGE_ID}" >&2
    echo "       Imagen del contenedor: ${CONTAINER_IMAGE_ID}" >&2
    echo "       Revisa que el servicio '${COMPOSE_SERVICE}' use 'image: ${IMAGE_TAG}' en ${COMPOSE_FILE}." >&2
    exit 1
fi

echo "  - Esperando respuesta HTTP en el puerto ${APP_PORT}${PROBE_PATH}..."
HEALTH_OK=1
ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${APP_PORT} ${PROBE_PATH}" <<'REMOTE_HEALTH' || HEALTH_OK=0
set -eu
port="$1"; path="$2"
attempt=0
while [ "${attempt}" -lt 30 ]; do
    attempt=$((attempt + 1))
    if curl -fsS -o /dev/null --max-time 5 "http://127.0.0.1:${port}${path}" 2>/dev/null; then
        echo "  - La aplicación respondió correctamente (intento ${attempt})."
        exit 0
    fi
    sleep 5
done
echo "ERROR: la aplicación no respondió en el puerto ${port}${path} tras 150 segundos." >&2
exit 1
REMOTE_HEALTH

if [ "${HEALTH_OK}" != "1" ]; then
    echo "ERROR: health check fallido; últimos logs de ${CONTAINER_NAME}:" >&2
    ssh ${SSH_OPTS} "${SSH_TARGET}" "docker logs --tail 100 '${CONTAINER_NAME}'" >&2 || true
    exit 1
fi

ssh ${SSH_OPTS} "${SSH_TARGET}" "sh -s -- ${CONTAINER_NAME}" <<'REMOTE_CLEANUP'
set -eu
container="$1"
docker image prune -f >/dev/null 2>&1 || true
docker ps --filter "name=${container}" --format "  - {{.Names}} | {{.Image}} | {{.Status}} | {{.Ports}}"
REMOTE_CLEANUP

echo "================================================================"
echo " Despliegue completado: http://${DEPLOY_HOST}:${APP_PORT}${PROBE_PATH}"
echo "================================================================"
'''
                    }
                }
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
            // script {} es obligatorio para usar if/def dentro de post.
            script {
                if (params.DEPLOY_ENABLED == null || params.DEPLOY_ENABLED) {
                    echo "Si el fallo fue en el despliegue: ssh spentpark@${params.DEPLOY_HOST ?: '192.168.1.82'} docker logs --tail 200 ${params.CONTAINER_NAME ?: 'backend-dotnet'}"
                    echo "Compose del stack .NET: ${params.DEPLOY_DIR ?: '/home/spentpark/gitDeploy/gitdotnet'}/docker-compose.yml"
                }
            }
        }
    }
}
