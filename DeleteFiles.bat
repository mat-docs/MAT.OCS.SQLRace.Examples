@ECHO OFF
cls
rmdir /s /q "bin"
rmdir /s /q "Debug"
rmdir /s /q "packages"
rmdir /s /q "Release"
rmdir /s /q "TestResults"
rmdir /s /q ".vs"
del /s /q "*.user"
del /q "*.db"
del /q "*.opendb"
del /q "*.htm"
del /q "*.psess"
del /q "*.vsp"

for /r /D %%d in (*) do if exist "%%d\.vs" rd /s /q "%%d\.vs"
for /r /D %%d in (*) do if exist "%%d\Debug" rd /s /q "%%d\Debug"
for /r /D %%d in (*) do if exist "%%d\Release" rd /s /q "%%d\Release"
for /r /D %%d in (*) do if exist "%%d\bin" rd /s /q "%%d\bin"
for /r /D %%d in (*) do if exist "%%d\obj" rd /s /q "%%d\obj"

pause
exit