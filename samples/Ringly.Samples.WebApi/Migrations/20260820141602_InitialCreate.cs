using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ringly.Samples.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelephonyIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SipUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SipCredential = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelephonyIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelephonyCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallerIdentityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientIdentityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AsteriskChannelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AsteriskBridgeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelephonyCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelephonyCalls_TelephonyIdentities_CallerIdentityId",
                        column: x => x.CallerIdentityId,
                        principalTable: "TelephonyIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelephonyCalls_TelephonyIdentities_RecipientIdentityId",
                        column: x => x.RecipientIdentityId,
                        principalTable: "TelephonyIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelephonyDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    LastRegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUnregisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelephonyDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelephonyDevices_TelephonyIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "TelephonyIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelephonyCalls_CallerIdentityId",
                table: "TelephonyCalls",
                column: "CallerIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_TelephonyCalls_RecipientIdentityId",
                table: "TelephonyCalls",
                column: "RecipientIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_TelephonyDevices_IdentityId",
                table: "TelephonyDevices",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_TelephonyIdentities_UserId_Type",
                table: "TelephonyIdentities",
                columns: new[] { "UserId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelephonyCalls");

            migrationBuilder.DropTable(
                name: "TelephonyDevices");

            migrationBuilder.DropTable(
                name: "TelephonyIdentities");
        }
    }
}
