<img src="https://github.com/gordon-matt/ReelSoon/blob/master/_Misc/Logo.png" alt="Logo" width="250" />

ReelSoon is a self-hosted list of movies, TV shows, and games you want to know about when they come out. Search for a title, keep the poster and release date, and get a notification when that date arrives.

It does not track what you have already watched, and it does not collect ratings.

## Screenshots

<img src="https://github.com/gordon-matt/ReelSoon/blob/master/_Misc/Screenshots/Games.png" alt="Games" />

<img src="https://github.com/gordon-matt/ReelSoon/blob/master/_Misc/Screenshots/Movies.png" alt="Movies" />

## What it does

- Separate lists for movies, TV shows, and games.
- Search that covers movies and TV shows together (TMDB) and games separately (IGDB). You can still type a title in yourself when a search misses it.
- A notification when a saved title's release date is today or earlier. The bell in the header lists those notices, and each one can be opened or deleted.
- A daily refresh of posters, descriptions, links, and release dates for titles that were added from search.
- Sign-in with ASP.NET Identity or Keycloak.
- SQLite, SQL Server, PostgreSQL, or MySQL.

## API keys

ReelSoon does not ship with catalog keys. Online search stays empty until you add your own.

| What you want to search | Where to create the key | Settings |
| --- | --- | --- |
| Movies and TV shows | [TMDB API settings](https://www.themoviedb.org/settings/api) | `Catalog:Tmdb:ApiKey` |
| Games | A [Twitch application](https://dev.twitch.tv/console/apps). IGDB uses that client id and secret. | `Catalog:Igdb:ClientId` and `Catalog:Igdb:ClientSecret` |

Titles can still be added by hand when a key is missing. The daily refresh only updates titles that were added from a catalog.

## Docker

Example stack. Fill in your own API keys and change the administrator password before you deploy it. The app is at http://localhost:8092. Sign in as **admin**.

```yaml
services:
  app:
    build:
      context: .
      dockerfile: ReelSoon/Dockerfile
    ports:
      - "8092:8080"
    environment:
      ASPNETCORE_URLS: http://+:8080
      Database__Provider: Npgsql
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=reelsoon;Username=postgres;Password=postgres"
      Authentication__Provider: Identity
      Seed__Admin__Email: "admin@reelsoon.local"
      Seed__Admin__UserName: "admin"
      Seed__Admin__Password: "change-me"
      Catalog__Tmdb__ApiKey: ""
      Catalog__Igdb__ClientId: ""
      Catalog__Igdb__ClientSecret: ""
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:18-alpine
    ports:
      - "5439:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: reelsoon
    volumes:
      - reelsoon-pgdata:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  reelsoon-pgdata:
```

## Run without Docker

The project targets .NET 10. From `ReelSoon/`:

```bash
dotnet run --launch-profile http
```

That uses SQLite (`reelsoon.db`) and listens on http://localhost:5290. The seeded administrator is **admin** / **Admin123!** unless you change `Seed:Admin` in `appsettings.json`.

Put your catalog keys in `appsettings.json`, user secrets, or environment variables (`Catalog__Tmdb__ApiKey`, `Catalog__Igdb__ClientId`, `Catalog__Igdb__ClientSecret`).

`Database:Provider` accepts `Sqlite`, `SqlServer`, `Npgsql`, or `MySql`. `Authentication:Provider` accepts `Identity`, `Keycloak`, or `None`. `None` signs every request in as a single local administrator and is only for a machine nobody else can reach.

## Background jobs

Hangfire runs two jobs:

- **release-check**, daily at 05:00 UTC, creates a notification when a release date has arrived. Saving a title that is already out still notifies immediately.
- **catalog-refresh**, daily at 04:00 UTC, updates details for titles that came from TMDB or IGDB.

Administrators can open the dashboard at `/hangfire`.
