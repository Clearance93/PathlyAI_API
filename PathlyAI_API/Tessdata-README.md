Tessdata usage and download

Purpose
- Provide instructions to download Tesseract language data (tessdata) needed at runtime (e.g. eng.traineddata).

Included script
- PathlyAI_API/tools/Download-TessData.ps1 — PowerShell script to download traineddata files from the official tesseract-ocr/tessdata repo.

How to download (from repository root)
- pwsh .\PathlyAI_API\tools\Download-TessData.ps1 -Languages eng

Or run from PathlyAI_API folder
- pwsh .\tools\Download-TessData.ps1 -Languages eng

What it does
- Downloads <language>.traineddata into PathlyAI_API\tessdata (creates folder if missing).
- Default language: eng. You may pass a comma-separated list: -Languages "eng,spa".

Project integration
- PathlyAI_API.csproj is configured to copy tessdata\**\* to the build output (CopyToOutputDirectory=PreserveNewest). Rebuild the project after downloading.

Verify
- Confirm files exist after download:
  - PathlyAI_API\tessdata\eng.traineddata
  - Or after build: bin\Debug\net10.0\tessdata\eng.traineddata

Notes
- The script uses the official tesseract-ocr/tessdata raw URLs. Ensure network access and that your environment allows PowerShell scripts.
- If you prefer manual installation, place the tessdata folder at PathlyAI_API\tessdata with the required .traineddata files.

If you want, I can add a short step to automatically run the download script as part of pre-build.
