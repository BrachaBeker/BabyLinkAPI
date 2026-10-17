# Dockerfile לפריסה ב-Render (אין ל-Render Runtime מובנה ל-.NET).
# שלב build: mcr.microsoft.com/dotnet/sdk:10.0 -> dotnet restore + dotnet publish של BabyLink.Api.
# שלב runtime: mcr.microsoft.com/dotnet/aspnet:10.0 -> הרצת BabyLink.Api.dll.
# ההאזנה על הפורט ש-Render נותן (משתנה הסביבה PORT / ASPNETCORE_HTTP_PORTS).
# הסודות (ConnectionStrings__Default, Jwt__Key) מוגדרים כ-Environment Variables ב-Render, לא כאן.
