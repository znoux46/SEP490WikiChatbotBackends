using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WikiChatbotBackends.Infrastructure.Migrations;

public partial class AddContentToDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE documents ADD COLUMN IF NOT EXISTS content text;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE documents DROP COLUMN IF EXISTS content;");
    }
}
