using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace client.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "InspectionImages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InspectionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FriendlyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoredRelativePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PublicAccessToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TolerancePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionTemplates_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionImages_TemplateId",
                table: "InspectionImages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_CreatedAtUtc",
                table: "InspectionTemplates",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_OwnerUserId",
                table: "InspectionTemplates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionTemplates_PublicAccessToken",
                table: "InspectionTemplates",
                column: "PublicAccessToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionImages_InspectionTemplates_TemplateId",
                table: "InspectionImages",
                column: "TemplateId",
                principalTable: "InspectionTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionImages_InspectionTemplates_TemplateId",
                table: "InspectionImages");

            migrationBuilder.DropTable(
                name: "InspectionTemplates");

            migrationBuilder.DropIndex(
                name: "IX_InspectionImages_TemplateId",
                table: "InspectionImages");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "InspectionImages");
        }
    }
}
