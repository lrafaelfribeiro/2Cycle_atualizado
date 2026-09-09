using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundTripToSuggestedRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRoundTrip",
                table: "SuggestedRoutes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "RequestedDistanceMeters",
                table: "SuggestedRoutes",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRoundTrip",
                table: "SuggestedRoutes");

            migrationBuilder.DropColumn(
                name: "RequestedDistanceMeters",
                table: "SuggestedRoutes");
        }
    }
}
