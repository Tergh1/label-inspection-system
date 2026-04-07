using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace client.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMlResultsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MlResultsJson",
                table: "InspectionImages",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MlResultsJson",
                table: "InspectionImages");
        }
    }
}
