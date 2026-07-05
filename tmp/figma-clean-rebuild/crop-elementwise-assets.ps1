$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = (Get-Location).Path
$outDir = Join-Path $root 'tmp\figma-elementwise\crops'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Save-Crop {
    param(
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][int]$X,
        [Parameter(Mandatory=$true)][int]$Y,
        [Parameter(Mandatory=$true)][int]$Width,
        [Parameter(Mandatory=$true)][int]$Height
    )

    $srcPath = if ([System.IO.Path]::IsPathRooted($Source)) { $Source } else { Join-Path $root $Source }
    $src = [System.Drawing.Bitmap]::FromFile($srcPath)
    try {
        $cropRect = New-Object System.Drawing.Rectangle $X, $Y, $Width, $Height
        $dest = New-Object System.Drawing.Bitmap $Width, $Height
        try {
            $g = [System.Drawing.Graphics]::FromImage($dest)
            try {
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $g.DrawImage($src, 0, 0, $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $g.Dispose()
            }
            $outPath = Join-Path $outDir $Name
            $dest.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-Output $outPath
        }
        finally {
            $dest.Dispose()
        }
    }
    finally {
        $src.Dispose()
    }
}

$buildings = 'Assets\Concepts\ui_concept_buildings_01.png'
Save-Crop $buildings 'building-barracks.png'       355 245 210 185
Save-Crop $buildings 'building-workshop.png'       780 245 210 185
Save-Crop $buildings 'building-water-collector.png' 1205 245 210 185
Save-Crop $buildings 'building-infirmary.png'      355 580 210 185
Save-Crop $buildings 'building-radio-tower.png'    780 580 210 185

$expedition = 'Assets\Concepts\ui_concept_expedition_setup_01.png'
Save-Crop $expedition 'zone-abandoned-store.png'  38 166 212 96
Save-Crop $expedition 'zone-dry-suburb.png'       38 302 212 96
Save-Crop $expedition 'zone-ruined-clinic.png'    38 444 212 96
Save-Crop $expedition 'zone-police-outpost.png'   38 588 212 96
Save-Crop $expedition 'zone-mutant-tunnel.png'    38 730 212 96

$dashboard = 'Assets\Concepts\ui_concept_camp_dashboard_01.png'
Save-Crop $dashboard 'expedition-gas-station.png' 1268 188 158 116
Save-Crop $dashboard 'expedition-wind-farm.png'   1268 372 158 132
Save-Crop $dashboard 'dashboard-alert-survivor.png' 498 700 105 115
Save-Crop $dashboard 'dashboard-idle-survivor.png' 815 700 105 115
Save-Crop $dashboard 'dashboard-upgrade-building.png' 1135 700 115 110

$workshop = 'Assets\Concepts\ui_concept_workshop_01.png'
Save-Crop $workshop 'workshop-rifle-large.png'    578 252 205 195
Save-Crop $workshop 'workshop-workbench.png'      1170 805 110 75

$world = 'Assets\Concepts\ui_concept_world_map_01.png'
Save-Crop $world 'map-location-dry-suburb.png'    1300 214 300 172
