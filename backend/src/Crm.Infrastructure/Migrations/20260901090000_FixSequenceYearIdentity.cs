using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260901090000_FixSequenceYearIdentity")]
    public partial class FixSequenceYearIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.identity_columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ContractNumberSequences]')
      AND name = 'Year'
)
BEGIN
    CREATE TABLE [dbo].[ContractNumberSequences_Temp]
    (
        [Year] int NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_ContractNumberSequences_Temp] PRIMARY KEY ([Year])
    );

    INSERT INTO [dbo].[ContractNumberSequences_Temp] ([Year], [LastValue])
    SELECT [Year], [LastValue]
    FROM [dbo].[ContractNumberSequences];

    DROP TABLE [dbo].[ContractNumberSequences];
    EXEC sp_rename N'[dbo].[ContractNumberSequences_Temp]', N'ContractNumberSequences';
    EXEC sp_rename N'[dbo].[PK_ContractNumberSequences_Temp]', N'PK_ContractNumberSequences', N'OBJECT';
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.identity_columns
    WHERE object_id = OBJECT_ID(N'[dbo].[OpportunityNumberSequences]')
      AND name = 'Year'
)
BEGIN
    CREATE TABLE [dbo].[OpportunityNumberSequences_Temp]
    (
        [Year] int NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_OpportunityNumberSequences_Temp] PRIMARY KEY ([Year])
    );

    INSERT INTO [dbo].[OpportunityNumberSequences_Temp] ([Year], [LastValue])
    SELECT [Year], [LastValue]
    FROM [dbo].[OpportunityNumberSequences];

    DROP TABLE [dbo].[OpportunityNumberSequences];
    EXEC sp_rename N'[dbo].[OpportunityNumberSequences_Temp]', N'OpportunityNumberSequences';
    EXEC sp_rename N'[dbo].[PK_OpportunityNumberSequences_Temp]', N'PK_OpportunityNumberSequences', N'OBJECT';
END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.identity_columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ContractNumberSequences]')
      AND name = 'Year'
)
BEGIN
    CREATE TABLE [dbo].[ContractNumberSequences_Temp]
    (
        [Year] int IDENTITY(1,1) NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_ContractNumberSequences_Temp] PRIMARY KEY ([Year])
    );

    SET IDENTITY_INSERT [dbo].[ContractNumberSequences_Temp] ON;

    INSERT INTO [dbo].[ContractNumberSequences_Temp] ([Year], [LastValue])
    SELECT [Year], [LastValue]
    FROM [dbo].[ContractNumberSequences];

    SET IDENTITY_INSERT [dbo].[ContractNumberSequences_Temp] OFF;

    DROP TABLE [dbo].[ContractNumberSequences];
    EXEC sp_rename N'[dbo].[ContractNumberSequences_Temp]', N'ContractNumberSequences';
    EXEC sp_rename N'[dbo].[PK_ContractNumberSequences_Temp]', N'PK_ContractNumberSequences', N'OBJECT';
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.identity_columns
    WHERE object_id = OBJECT_ID(N'[dbo].[OpportunityNumberSequences]')
      AND name = 'Year'
)
BEGIN
    CREATE TABLE [dbo].[OpportunityNumberSequences_Temp]
    (
        [Year] int IDENTITY(1,1) NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_OpportunityNumberSequences_Temp] PRIMARY KEY ([Year])
    );

    SET IDENTITY_INSERT [dbo].[OpportunityNumberSequences_Temp] ON;

    INSERT INTO [dbo].[OpportunityNumberSequences_Temp] ([Year], [LastValue])
    SELECT [Year], [LastValue]
    FROM [dbo].[OpportunityNumberSequences];

    SET IDENTITY_INSERT [dbo].[OpportunityNumberSequences_Temp] OFF;

    DROP TABLE [dbo].[OpportunityNumberSequences];
    EXEC sp_rename N'[dbo].[OpportunityNumberSequences_Temp]', N'OpportunityNumberSequences';
    EXEC sp_rename N'[dbo].[PK_OpportunityNumberSequences_Temp]', N'PK_OpportunityNumberSequences', N'OBJECT';
END;");
        }
    }
}
