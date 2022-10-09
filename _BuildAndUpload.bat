dotnet publish -c release -r ubuntu.20.04-x64 --self-contained
putty -ssh IP_ADDRESS -pw PASSWORD -m ./cmd/preUpload.txt
pscp -P 22 -pw PASSWORD -r DiscordAttendanceBot\bin\Release\netcoreapp3.1\ubuntu.20.04-x64\publish\* IP_ADDRESS:/MY_APP
putty -ssh IP_ADDRESS -pw PASSWORD -m ./cmd/postUpload.txt
echo "Success"
pause
