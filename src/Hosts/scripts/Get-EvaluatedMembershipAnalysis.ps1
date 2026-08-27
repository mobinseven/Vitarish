[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ConnectionString,
    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

if ($ConnectionString -match '(?i)(password|pwd)\s*=\s*[^;]+') {
    throw 'Do not pass a connection string containing a secret on a command line.'
}

$query = @'
SELECT md5(u."Id") AS user_hash,
       CASE WHEN position(':' IN r."Name") > 0
            THEN md5(split_part(r."Name", ':', 1)) END AS tenant_hash,
       CASE WHEN position(':' IN r."Name") > 0
            THEN split_part(r."Name", ':', 2)
            ELSE r."Name" END AS role_name,
       count(*) OVER (
           PARTITION BY u."Id", CASE WHEN position(':' IN r."Name") > 0
                                      THEN split_part(r."Name", ':', 1) END
       ) AS membership_count
FROM "Users"."AspNetUserRoles" ur
JOIN "Users"."AspNetUsers" u ON u."Id" = ur."UserId"
JOIN "Users"."AspNetRoles" r ON r."Id" = ur."RoleId"
ORDER BY user_hash, tenant_hash, role_name;
'@

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw 'psql is required; no database connection was attempted.'
}

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    throw "Output directory does not exist: $outputDirectory"
}

$temporaryQuery = Join-Path ([IO.Path]::GetTempPath()) "vitarish-membership-$([guid]::NewGuid().ToString('N')).sql"
try {
    [IO.File]::WriteAllText($temporaryQuery, $query, [Text.UTF8Encoding]::new($false))
    $result = & psql --no-password --no-psqlrc --set=ON_ERROR_STOP=1 --tuples-only --csv --file=$temporaryQuery $ConnectionString 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Read-only membership analysis failed with exit code $LASTEXITCODE."
    }
    [IO.File]::WriteAllLines($outputFullPath, @(
        '# Sanitized membership analysis output',
        '# Snapshot identity and execution metadata must be recorded by the release operator.',
        $result
    ), [Text.UTF8Encoding]::new($false))
}
finally {
    if (Test-Path -LiteralPath $temporaryQuery) {
        Remove-Item -LiteralPath $temporaryQuery -Force
    }
}
