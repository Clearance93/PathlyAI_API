<#
Downloads Tesseract tessdata files (e.g. eng.traineddata) into the project's tessdata folder.
Usage (from repository root or PathlyAI_API folder):
  pwsh .\tools\Download-TessData.ps1 -Languages eng

By default downloads 'eng'.
#>
param(
	[string]$Languages = 'eng',
	[string]$TessDataPath = "$(Split-Path -Path $PSScriptRoot -Parent)\tessdata"
)

if (-not (Test-Path $TessDataPath)) {
	New-Item -ItemType Directory -Path $TessDataPath | Out-Null
}

$repoBase = 'https://github.com/tesseract-ocr/tessdata/raw/main'

foreach ($lang in $Languages.Split(',')) {
	$lang = $lang.Trim()
	if ([string]::IsNullOrWhiteSpace($lang)) { continue }

	$outFile = Join-Path $TessDataPath "$lang.traineddata"
	$url = "$repoBase/$lang.traineddata"

	Write-Host "Downloading $lang.traineddata from $url -> $outFile"
	try {
		Invoke-WebRequest -Uri $url -OutFile $outFile -UseBasicParsing -ErrorAction Stop
		Write-Host "Downloaded: $outFile"
	}
	catch {
		Write-Error "Failed to download $lang.traineddata: $_"
	}
}

Write-Host "Done. Ensure PathlyAI_API.csproj already copies tessdata to output (it does)."
