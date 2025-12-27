@echo off
echo Starting Database Migration Setup...

echo Step 1: Building project...
dotnet build

echo Step 2: Creating migration...
dotnet ef migrations add InitialCreate

echo Step 3: Updating database...
dotnet ef database update

echo Step 4: Running application...
dotnet run

echo Setup Complete!
pause