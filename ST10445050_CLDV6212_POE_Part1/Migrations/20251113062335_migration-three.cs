using Microsoft.EntityFrameworkCore.Migrations;

public partial class MigrationThree : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop previous primary keys
        migrationBuilder.DropPrimaryKey(
            name: "PK_Users",
            table: "Users");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Orders",
            table: "Orders");

        // Renaming the tables properly
        migrationBuilder.RenameTable(
            name: "Users",
            newName: "Users");

        migrationBuilder.RenameTable(
            name: "Orders",
            newName: "Orders");

        // Add primary keys with correct names
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
        migrationBuilder.DropPrimaryKey(
            name: "PK_Users",
            table: "Users");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Orders",
            table: "Orders");

        // Reverting back table names
        migrationBuilder.RenameTable(
            name: "Users",
            newName: "Users");

        migrationBuilder.RenameTable(
            name: "Orders",
            newName: "Order");

        // Add the previous primary keys
        migrationBuilder.AddPrimaryKey(
            name: "PK_User",
            table: "Users",
            column: "Id");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Order",
            table: "Order",
            column: "orderID");
    }
}
