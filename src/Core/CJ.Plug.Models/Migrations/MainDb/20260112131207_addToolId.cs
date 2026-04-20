using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CJ.Plug.Models.Migrations.MainDb
{
    /// <inheritdoc />
    public partial class addToolId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolId",
                table: "MCPTools",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolId",
                table: "MCPTools");
        }
    }
}
