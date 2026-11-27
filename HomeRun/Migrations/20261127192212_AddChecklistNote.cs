using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeRun.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Checklist",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Checklist");
        }
    }
}
