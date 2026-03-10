using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace client.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoredRelativePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PublicAccessToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TolerancePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MinimumSimilarityPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OutcomeStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SimilarityPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    DefectsJson = table.Column<string>(type: "jsonb", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionImages_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionImages_CreatedAtUtc",
                table: "InspectionImages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionImages_OwnerUserId",
                table: "InspectionImages",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionImages_ProcessingStatus",
                table: "InspectionImages",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionImages_PublicAccessToken",
                table: "InspectionImages",
                column: "PublicAccessToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionImages");
        }
    }
}
