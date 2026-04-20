using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CJ.Plug.Models.Migrations.MainDb
{
    /// <inheritdoc />
    public partial class mcptooladdenable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "MCPTools",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "MCPTools");
        }
    }
}
