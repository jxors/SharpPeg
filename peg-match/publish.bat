# VS2017 publishing tools are broken. Use this bat script instead!

dotnet publish -c Release -r win-x86 -o bin\Release\PublishOutput\win\x86
dotnet publish -c Release -r win-x64 -o bin\Release\PublishOutput\win\x64
dotnet publish -c Release -r linux-x64 -o bin\Release\PublishOutput\linux\x64
dotnet publish -c Release -r osx-x64 -o bin\Release\PublishOutput\osx\x64