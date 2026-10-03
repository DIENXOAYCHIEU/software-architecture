using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;


#nullable disable

namespace MatchingService.Infrastructure.Migrations;

[DbContext(typeof(Persistence.MatchingDbContext))]
[Migration("20261003000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Drivers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                Latitude = table.Column<double>(type: "double", nullable: false),
                Longitude = table.Column<double>(type: "double", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Drivers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MatchingRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                TripId = table.Column<Guid>(type: "char(36)", nullable: false),
                DriverId = table.Column<Guid>(type: "char(36)", nullable: true),
                PickupLatitude = table.Column<double>(type: "double", nullable: false),
                PickupLongitude = table.Column<double>(type: "double", nullable: false),
                Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                MatchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MatchingRequests", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MatchingRequests_TripId",
            table: "MatchingRequests",
            column: "TripId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MatchingRequests");
        migrationBuilder.DropTable(name: "Drivers");
    }
}
