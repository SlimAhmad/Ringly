using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ringly.Samples.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BridgeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RecordingName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    StartedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_RecordingName",
                table: "Recordings",
                column: "RecordingName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recordings");
        }
    }
}
