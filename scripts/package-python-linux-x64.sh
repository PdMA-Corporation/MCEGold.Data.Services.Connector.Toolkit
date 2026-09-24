#!/usr/bin/env bash
set -euo pipefail

version="${1:-${PACKAGE_VERSION:-1.0.0}}"
configuration="${CONFIGURATION:-Release}"
rid="linux-x64"
package_name="mcegold-python-linux-x64-v${version}"

host_os="$(uname -s 2>/dev/null || true)"
if [[ "${host_os}" != "Linux" ]]; then
    echo "This package must be created from Linux or WSL so tar preserves POSIX executable permissions." >&2
    exit 1
fi

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "${script_dir}/.." && pwd)"

cli_project="${repo_root}/samples/csharp/MCEGold.Data.Services.Connector.Cli/MCEGold.Data.Services.Connector.Cli.csproj"
artifacts_dir="${repo_root}/artifacts"
work_dir="${artifacts_dir}/${package_name}-work"
publish_dir="${work_dir}/publish"
stage_dir="${work_dir}/stage"
verify_dir="${work_dir}/verify"
package_root="${stage_dir}/${package_name}"
archive_path="${artifacts_dir}/${package_name}.tar.gz"
cli_executable="${package_root}/MCEGold.Data.Services.Connector.Cli"

require_file() {
    local path="$1"
    if [[ ! -f "${path}" ]]; then
        echo "Required file not found: ${path}" >&2
        exit 1
    fi
}

require_dir() {
    local path="$1"
    if [[ ! -d "${path}" ]]; then
        echo "Required directory not found: ${path}" >&2
        exit 1
    fi
}

remove_inside_artifacts() {
    local path="$1"
    mkdir -p "${artifacts_dir}"

    local artifacts_real
    artifacts_real="$(cd -- "${artifacts_dir}" && pwd -P)"

    if [[ -e "${path}" ]]; then
        local path_real
        path_real="$(cd -- "$(dirname -- "${path}")" && pwd -P)/$(basename -- "${path}")"
        case "${path_real}" in
            "${artifacts_real}"/*) rm -rf -- "${path}" ;;
            *)
                echo "Refusing to remove outside artifacts: ${path}" >&2
                exit 1
                ;;
        esac
    fi
}

copy_file() {
    local source="$1"
    local destination="$2"
    require_file "${source}"
    mkdir -p -- "$(dirname -- "${destination}")"
    cp -- "${source}" "${destination}"
}

echo "Toolkit repository: ${repo_root}"
echo "Package version:    ${version}"
echo "Configuration:      ${configuration}"
echo "Runtime:            ${rid}"

require_file "${cli_project}"
require_file "${repo_root}/README.md"
require_file "${repo_root}/LICENSE"
require_file "${repo_root}/configs/connector.config.example.json"
require_dir "${repo_root}/payloads/requests"
require_dir "${repo_root}/samples/python"

remove_inside_artifacts "${work_dir}"
rm -f -- "${archive_path}"
mkdir -p -- "${publish_dir}" "${package_root}"

echo "Publishing self-contained CLI..."
dotnet publish "${cli_project}" \
    --configuration "${configuration}" \
    --runtime "${rid}" \
    --self-contained true \
    --output "${publish_dir}" \
    -p:BaseOutputPath="${work_dir}/bin/" \
    -p:BaseIntermediateOutputPath="${work_dir}/obj/" \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:RestorePackagesPath="${work_dir}/packages"

require_file "${publish_dir}/MCEGold.Data.Services.Connector.Cli"

echo "Staging package contents..."
cp -a -- "${publish_dir}/." "${package_root}/"

copy_file "${repo_root}/README.md" "${package_root}/README.md"
copy_file "${repo_root}/LICENSE" "${package_root}/LICENSE"
copy_file "${repo_root}/configs/connector.config.example.json" "${package_root}/configs/connector.config.example.json"
mkdir -p -- "${package_root}/payloads"
cp -a -- "${repo_root}/payloads/requests" "${package_root}/payloads/"
mkdir -p -- "${package_root}/samples"
cp -a -- "${repo_root}/samples/python" "${package_root}/samples/"
mkdir -p -- "${package_root}/docs"
copy_file "${repo_root}/docs/calling-mcegold-cli.md" "${package_root}/docs/calling-mcegold-cli.md"
copy_file "${repo_root}/docs/request-types.md" "${package_root}/docs/request-types.md"

rm -f -- "${package_root}/samples/python/sample_state.local.json"
find "${package_root}" -type d -name '__pycache__' -prune -exec rm -rf -- {} +
find "${package_root}" -type f -name '*.pyc' -delete

chmod 755 "${cli_executable}"

echo "Creating archive: ${archive_path}"
mkdir -p -- "${artifacts_dir}"
tar -C "${stage_dir}" -czf "${archive_path}" "${package_name}"

echo "Validating archive metadata..."
permission_line="$(tar -tvf "${archive_path}" | grep 'MCEGold.Data.Services.Connector.Cli$' || true)"
if [[ -z "${permission_line}" ]]; then
    echo "CLI executable was not found in archive metadata." >&2
    exit 1
fi
echo "${permission_line}"
case "${permission_line}" in
    -rwx*) ;;
    *)
        echo "CLI executable does not have executable mode in archive metadata." >&2
        exit 1
        ;;
esac

echo "Validating clean extraction..."
remove_inside_artifacts "${verify_dir}"
mkdir -p -- "${verify_dir}"
tar -xzf "${archive_path}" -C "${verify_dir}"
extracted_root="${verify_dir}/${package_name}"
extracted_cli="${extracted_root}/MCEGold.Data.Services.Connector.Cli"

require_file "${extracted_cli}"
require_file "${extracted_root}/configs/connector.config.example.json"
require_file "${extracted_root}/samples/python/06_post_request.py"
require_file "${extracted_root}/samples/python/cli_helpers.py"
require_file "${extracted_root}/samples/python/README.md"
require_file "${extracted_root}/payloads/requests/get-sites.example.json"

if [[ ! -x "${extracted_cli}" ]]; then
    echo "Extracted CLI is not executable: ${extracted_cli}" >&2
    exit 1
fi
echo "test -x: passed (${extracted_cli})"

"${extracted_cli}" --help >/dev/null
echo "CLI --help: passed"

echo "Package created successfully: ${archive_path}"
