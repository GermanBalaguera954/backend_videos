# Etapa 1: Usa una imagen base de .NET SDK para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establece el directorio de trabajo dentro del contenedor
WORKDIR /app

# Copia el archivo .csproj y restaura las dependencias
COPY Backend.csproj ./
RUN dotnet restore

# Copia el resto del código fuente
COPY . ./

# Publica la aplicación en la carpeta /out (esto compila y publica el proyecto)
RUN dotnet publish -c Release -o /out

# Etapa 2: Usa una imagen base de .NET Runtime para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Establece el directorio de trabajo dentro del contenedor
WORKDIR /app

# Copia los archivos publicados desde la etapa de build
COPY --from=build /out .

# Expone el puerto 80 que utilizará la aplicación
EXPOSE 80

# Define el comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "Backend.dll"]
