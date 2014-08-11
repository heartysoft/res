@echo off
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& { Import-Module .\tools\psake\psake.psm1; Invoke-psake .\build\build.ps1 %*; exit !($psake.build_success) }" 
