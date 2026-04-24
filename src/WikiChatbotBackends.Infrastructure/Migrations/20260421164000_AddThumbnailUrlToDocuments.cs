using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WikiChatbotBackends.Infrastructure.Migrations;

public partial class AddThumbnailUrlToDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE documents ADD COLUMN IF NOT EXISTS thumbnail_url character varying(2000);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE documents DROP COLUMN IF EXISTS thumbnail_url;");
    }
}