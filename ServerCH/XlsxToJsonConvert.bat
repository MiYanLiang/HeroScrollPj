echo off
.\xlsxtojson\xlsxtojson.exe HeroDataTable.xlsx ..\Uploader\jsons
xcopy Uploader\jsons\*.* ..\SanGuoHuiJuanPj\Assets\Resources\Jsons\
pause
