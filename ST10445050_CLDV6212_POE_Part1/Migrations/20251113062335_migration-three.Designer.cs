using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST10445050_CLDV6212_POE_Part1.Migrations
{
    public partial class migrationthree : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the necessary tables and foreign keys here.
            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Order",
                table: "Order");

            // Rename the tables to correct ones (if required)
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Order",
                newName: "Orders");

            // Add new primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "orderID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes made in Up() method (rollback)
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Order");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Order",
                table: "Order",
                column: "orderID");
        }
    }
}
