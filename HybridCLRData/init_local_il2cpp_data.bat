@echo off
rem git clone https://github.com/focus-creative-games/hybridclr
git clone -- https://gitee.com/focus-creative-games/hybridclr hybridclr_repo

rem git clone https://github.com/focus-creative-games/il2cpp_huatuo
git clone https://gitee.com/focus-creative-games/il2cpp_plus il2cpp_plus_repo


rem set default branch
set IL2CPP_BRANCH=2020.3.33
cd il2cpp_plus_repo

git switch %IL2CPP_BRANCH%

cd ..

rem replace with right Unity Editor Install path
set IL2CPP_PATH=C:\Program Files\Unity\Hub\Editor\2020.3.26f1c1\Editor\Data\il2cpp

if not exist "%IL2CPP_PATH%" (
    echo "please set correct IL2CPP_PATH value"
    goto EXIT
)

set LOCAL_IL2CPP_DATA=LocalIl2CppData

if not exist %LOCAL_IL2CPP_DATA% (
    mkdir %LOCAL_IL2CPP_DATA%
)

rem need copdy MonoBleedingEdge
set MBE=%LOCAL_IL2CPP_DATA%\MonoBleedingEdge
if not exist %MBE% (
    xcopy /q /i /e "%IL2CPP_PATH%\..\MonoBleedingEdge" %MBE%
)


rem copy il2cpp
set IL2CPP=%LOCAL_IL2CPP_DATA%\il2cpp
if not exist %IL2CPP% (
    xcopy /q /i /e "%IL2CPP_PATH%" %IL2CPP%
)

set HYBRIDCLR_REPO_DIR=hybridclr_repo

set IL2CPP_PLUS_REPO_DIR=il2cpp_plus_repo

set LIBIL2CPP_PATH=%LOCAL_IL2CPP_DATA%\il2cpp\libil2cpp
rd /s /q %LIBIL2CPP_PATH%

xcopy /q /i /e %IL2CPP_PLUS_REPO_DIR%\libil2cpp %LIBIL2CPP_PATH%
xcopy /q /i /e %HYBRIDCLR_REPO_DIR%\huatuo %LIBIL2CPP_PATH%\huatuo

rem clean il2cpp build cache
set IL2CPP_CACHE=..\Library\Il2cppBuildCache
echo clean %IL2CPP_CACHE%
if exist "%IL2CPP_CACHE%" rd /s /q "%IL2CPP_CACHE%"

echo succ

:EXIT

PAUSE