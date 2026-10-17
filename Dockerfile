# Dockerfile לפריסה ב-Render (אין ל-Render Runtime מובנה ל-.NET).
# הסודות (ConnectionStrings__Default, Jwt__Key) מוגדרים כ-Environment Variables ב-Render, לא כאן.

# ---------- שלב build: SDK -> restore + publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# קודם רק קבצי הפרויקט - כך שכבת ה-restore נשמרת ב-cache כל עוד התלויות לא השתנו.
COPY src/BabyLink.Domain/BabyLink.Domain.csproj src/BabyLink.Domain/
COPY src/BabyLink.Application/BabyLink.Application.csproj src/BabyLink.Application/
COPY src/BabyLink.Infrastructure/BabyLink.Infrastructure.csproj src/BabyLink.Infrastructure/
COPY src/BabyLink.Api/BabyLink.Api.csproj src/BabyLink.Api/
RUN dotnet restore src/BabyLink.Api/BabyLink.Api.csproj

COPY src/ src/
RUN dotnet publish src/BabyLink.Api/BabyLink.Api.csproj -c Release -o /app/publish --no-restore

# ---------- שלב runtime: ASP.NET בלבד (תמונה קטנה, בלי SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# תיקיית הלוגים של NLog, והרצה כמשתמש שאינו root.
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID

# Render מעביר את הפורט במשתנה PORT (Program.cs מאזין לו); 8080 הוא ברירת המחדל להרצה מקומית.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BabyLink.Api.dll"]
