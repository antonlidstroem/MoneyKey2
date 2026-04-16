# STAGE 1: THE BUILDER
# We use the big "SDK" image because it has the compilers needed to build code.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project file (.csproj) from your PC into the container's /app folder.
# We do this first so Docker can "cache" your NuGet packages.
COPY *.csproj ./
RUN dotnet restore

# Now copy every other file from your API folder into the container.
COPY . ./

# Tell .NET to compile the app into a folder called 'out' in Release mode.
RUN dotnet publish -c Release -o out

# STAGE 2: THE RUNNER
# We don't need the huge compilers anymore. We switch to a tiny "Runtime" image.
# This makes your final deployment much faster and smaller.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Take the compiled 'out' folder from the Builder stage and bring it here.
COPY --from=build-env /app/out .

# Tell the container which port to open (8080 is the standard for .NET 8).
EXPOSE 8080

# This is the "Start" button. When the container starts, it runs this command.
# REPLACE "YourApiProjectName" with the actual name of your .dll file!
ENTRYPOINT ["dotnet", "MoneyKey.API.dll"]