using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ringly.Samples.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportQueues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BridgeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MusicOnHoldClass = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportQueues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportQueues_QueueName",
                table: "SupportQueues",
                column: "QueueName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportQueues");
        }
    }
}
