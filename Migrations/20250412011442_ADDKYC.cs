#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace FYPBidNetra.Migrations
{
    public partial class ADDKYC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Company",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Bank",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Bank");
        }
    }
}