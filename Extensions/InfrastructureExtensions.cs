using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using TourViet.Data;
using Microsoft.EntityFrameworkCore;

namespace TourViet.Extensions
{
    /// <summary>
    /// Extension methods for configuring required infrastructure components.
    /// </summary>
    public static class InfrastructureExtensions
    {
        /// <summary>
        /// Ensures the Uploads folder exists, configures static file serving for it,
        /// and drops the legacy UNIQUE index on ISO3 if it still exists.
        /// Call this from Program.cs after app is built.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        public static void UseInfrastructureSetup(this WebApplication app)
        {
            // 1. Ensure Uploads folder exists
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // 2. Serve files from the Uploads folder
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = "/Uploads"
            });


            // 3. Update any existing Countries with NULL ISO2 to have unique values
            // (SQL Server UNIQUE constraint allows only ONE NULL value)
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TourBookingDbContext>();
            
            var updateIso2Sql = @"
-- Update Countries with NULL ISO2 to have unique generated values
DECLARE @CountryID UNIQUEIDENTIFIER;
DECLARE @CountryName NVARCHAR(200);
DECLARE @NewISO2 NVARCHAR(2);
DECLARE @Counter INT;

DECLARE country_cursor CURSOR FOR
SELECT CountryID, CountryName
FROM Countries
WHERE ISO2 IS NULL;

OPEN country_cursor;
FETCH NEXT FROM country_cursor INTO @CountryID, @CountryName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Generate ISO2 from first 2 chars of country name
    SET @NewISO2 = UPPER(LEFT(@CountryName, 2));
    SET @Counter = 1;
    
    -- Ensure uniqueness
    WHILE EXISTS (SELECT 1 FROM Countries WHERE ISO2 = @NewISO2)
    BEGIN
        SET @NewISO2 = UPPER(LEFT(@CountryName, 1)) + CAST(@Counter AS NVARCHAR(1));
        SET @Counter = @Counter + 1;
    END
    
    -- Update the country
    UPDATE Countries SET ISO2 = @NewISO2 WHERE CountryID = @CountryID;
    
    FETCH NEXT FROM country_cursor INTO @CountryID, @CountryName;
END

CLOSE country_cursor;
DEALLOCATE country_cursor;
";
            db.Database.ExecuteSqlRaw(updateIso2Sql);

            // 4. Drop legacy UNIQUE index on ISO3 (for databases that still have it)
            // This is safe to run on every start – if the index does not exist the script does nothing.
            var dropSql = @"
DECLARE @ConstraintName NVARCHAR(200);

-- 1. Try to find a Unique Constraint
SELECT TOP 1 @ConstraintName = name
FROM sys.key_constraints
WHERE parent_object_id = OBJECT_ID('Countries')
  AND type = 'UQ'
  AND name LIKE '%ISO3%';

IF @ConstraintName IS NOT NULL
BEGIN
    DECLARE @sqlConstraint NVARCHAR(MAX);
    SET @sqlConstraint = 'ALTER TABLE dbo.Countries DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
    EXEC(@sqlConstraint);
END
ELSE
BEGIN
    -- 2. If no constraint, look for a plain Unique Index
    SELECT TOP 1 @ConstraintName = name
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('Countries')
      AND name LIKE '%ISO3%'
      AND is_unique = 1
      AND is_primary_key = 0
      AND is_unique_constraint = 0;

    IF @ConstraintName IS NOT NULL
    BEGIN
        DECLARE @sqlIndex NVARCHAR(MAX);
        SET @sqlIndex = 'DROP INDEX ' + QUOTENAME(@ConstraintName) + ' ON dbo.Countries';
        EXEC(@sqlIndex);
    END
END
";

            db.Database.ExecuteSqlRaw(dropSql);
        }
    }
}
