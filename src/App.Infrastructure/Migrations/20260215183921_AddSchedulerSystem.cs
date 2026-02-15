using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Zipcode",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppointmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    BufferTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    BookingHorizonDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentTypes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppointmentTypes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SchedulerConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableHoursJson = table.Column<string>(type: "text", nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultBufferTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultBookingHorizonDays = table.Column<int>(type: "integer", nullable: false),
                    MinCancellationNoticeHours = table.Column<int>(type: "integer", nullable: false),
                    ReminderLeadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultCoverageZonesJson = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchedulerConfigurations_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SchedulerConfigurations_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SchedulerEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchedulerEmailTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SchedulerEmailTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SchedulerStaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanManageOthersCalendars = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AvailabilityJson = table.Column<string>(type: "text", nullable: true),
                    CoverageZonesJson = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerStaffMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchedulerStaffMembers_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SchedulerStaffMembers_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SchedulerStaffMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedStaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MeetingLink = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CoverageZoneOverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationNoticeOverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_AppointmentTypes_AppointmentTypeId",
                        column: x => x.AppointmentTypeId,
                        principalTable: "AppointmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_SchedulerStaffMembers_AssignedStaffMemberId",
                        column: x => x.AssignedStaffMemberId,
                        principalTable: "SchedulerStaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_CreatedByStaffId",
                        column: x => x.CreatedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentTypeStaffEligibilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchedulerStaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTypeStaffEligibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentTypeStaffEligibilities_AppointmentTypes_Appointm~",
                        column: x => x.AppointmentTypeId,
                        principalTable: "AppointmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentTypeStaffEligibilities_SchedulerStaffMembers_Sch~",
                        column: x => x.SchedulerStaffMemberId,
                        principalTable: "SchedulerStaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentTypeStaffEligibilities_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppointmentTypeStaffEligibilities_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    OverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentHistories_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistories_AppointmentId",
                table: "AppointmentHistories",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistories_ChangedByUserId",
                table: "AppointmentHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistories_Timestamp",
                table: "AppointmentHistories",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentTypeId",
                table: "Appointments",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AssignedStaffMemberId",
                table: "Appointments",
                column: "AssignedStaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ContactId",
                table: "Appointments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CreatedByStaffId",
                table: "Appointments",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CreationTime",
                table: "Appointments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CreatorUserId",
                table: "Appointments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LastModifierUserId",
                table: "Appointments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReminderSentAt_Status_ScheduledStartTime",
                table: "Appointments",
                columns: new[] { "ReminderSentAt", "Status", "ScheduledStartTime" },
                filter: "\"ReminderSentAt\" IS NULL AND \"Status\" IN ('scheduled','confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledStartTime",
                table: "Appointments",
                column: "ScheduledStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_CreatorUserId",
                table: "AppointmentTypes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_IsActive",
                table: "AppointmentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_LastModifierUserId",
                table: "AppointmentTypes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_SortOrder",
                table: "AppointmentTypes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypeStaffEligibilities_AppointmentTypeId_Schedul~",
                table: "AppointmentTypeStaffEligibilities",
                columns: new[] { "AppointmentTypeId", "SchedulerStaffMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypeStaffEligibilities_CreatorUserId",
                table: "AppointmentTypeStaffEligibilities",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypeStaffEligibilities_LastModifierUserId",
                table: "AppointmentTypeStaffEligibilities",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypeStaffEligibilities_SchedulerStaffMemberId",
                table: "AppointmentTypeStaffEligibilities",
                column: "SchedulerStaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerConfigurations_CreatorUserId",
                table: "SchedulerConfigurations",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerConfigurations_LastModifierUserId",
                table: "SchedulerConfigurations",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerEmailTemplates_CreatorUserId",
                table: "SchedulerEmailTemplates",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerEmailTemplates_LastModifierUserId",
                table: "SchedulerEmailTemplates",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerEmailTemplates_TemplateType_Channel",
                table: "SchedulerEmailTemplates",
                columns: new[] { "TemplateType", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerStaffMembers_CreatorUserId",
                table: "SchedulerStaffMembers",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerStaffMembers_IsActive",
                table: "SchedulerStaffMembers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerStaffMembers_LastModifierUserId",
                table: "SchedulerStaffMembers",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerStaffMembers_UserId",
                table: "SchedulerStaffMembers",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentHistories");

            migrationBuilder.DropTable(
                name: "AppointmentTypeStaffEligibilities");

            migrationBuilder.DropTable(
                name: "SchedulerConfigurations");

            migrationBuilder.DropTable(
                name: "SchedulerEmailTemplates");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AppointmentTypes");

            migrationBuilder.DropTable(
                name: "SchedulerStaffMembers");

            migrationBuilder.DropColumn(
                name: "Zipcode",
                table: "Contacts");
        }
    }
}
