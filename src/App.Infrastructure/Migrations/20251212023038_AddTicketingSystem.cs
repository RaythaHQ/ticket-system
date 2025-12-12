using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccessReports",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageTickets",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManageTeams",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumbersJson = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    OrganizationAccount = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DmeIdentifiersJson = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contacts_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_StaffAdminId",
                        column: x => x.StaffAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConditionsJson = table.Column<string>(type: "text", nullable: true),
                    TargetResolutionMinutes = table.Column<int>(type: "integer", nullable: false),
                    TargetCloseMinutes = table.Column<int>(type: "integer", nullable: true),
                    BusinessHoursEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BusinessHoursConfigJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    BreachBehaviorJson = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaRules_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlaRules_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RoundRobinEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Teams_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionsJson = table.Column<string>(type: "text", nullable: true),
                    SortPrimaryField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortPrimaryDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SortSecondaryField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortSecondaryDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VisibleColumnsJson = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketViews_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketViews_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketViews_Users_OwnerStaffId",
                        column: x => x.OwnerStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactChangeLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldChangesJson = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactChangeLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactChangeLogEntries_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactChangeLogEntries_Users_ActorStaffId",
                        column: x => x.ActorStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContactComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactComments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactComments_Users_AuthorStaffId",
                        column: x => x.AuthorStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactComments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactComments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAssignable = table.Column<bool>(type: "boolean", nullable: false),
                    LastAssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMemberships_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMemberships_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMemberships_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMemberships_Users_StaffAdminId",
                        column: x => x.StaffAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OwningTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactId = table.Column<long>(type: "bigint", nullable: true),
                    TagsJson = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlaDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaBreachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_SlaRules_SlaRuleId",
                        column: x => x.SlaRuleId,
                        principalTable: "SlaRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Teams_OwningTeamId",
                        column: x => x.OwningTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Users_CreatedByStaffId",
                        column: x => x.CreatedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Users_UploadedByStaffId",
                        column: x => x.UploadedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketChangeLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldChangesJson = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketChangeLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketChangeLogEntries_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketChangeLogEntries_Users_ActorStaffId",
                        column: x => x.ActorStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketComments_Users_AuthorStaffId",
                        column: x => x.AuthorStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketComments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketComments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeLogEntries_ActorStaffId",
                table: "ContactChangeLogEntries",
                column: "ActorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeLogEntries_ContactId",
                table: "ContactChangeLogEntries",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeLogEntries_CreationTime",
                table: "ContactChangeLogEntries",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_ContactComments_AuthorStaffId",
                table: "ContactComments",
                column: "AuthorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactComments_ContactId",
                table: "ContactComments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactComments_CreationTime",
                table: "ContactComments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_ContactComments_CreatorUserId",
                table: "ContactComments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactComments_LastModifierUserId",
                table: "ContactComments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CreationTime",
                table: "Contacts",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CreatorUserId",
                table: "Contacts",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                table: "Contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LastModifierUserId",
                table: "Contacts",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Name",
                table: "Contacts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_CreatorUserId",
                table: "NotificationPreferences",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_LastModifierUserId",
                table: "NotificationPreferences",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_StaffAdminId_EventType",
                table: "NotificationPreferences",
                columns: new[] { "StaffAdminId", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CreatorUserId",
                table: "SlaRules",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_IsActive",
                table: "SlaRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_LastModifierUserId",
                table: "SlaRules",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_Name",
                table: "SlaRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_Priority",
                table: "SlaRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_CreatorUserId",
                table: "TeamMemberships",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_LastModifierUserId",
                table: "TeamMemberships",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_StaffAdminId",
                table: "TeamMemberships",
                column: "StaffAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId_IsAssignable_LastAssignedAt",
                table: "TeamMemberships",
                columns: new[] { "TeamId", "IsAssignable", "LastAssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_TeamId_StaffAdminId",
                table: "TeamMemberships",
                columns: new[] { "TeamId", "StaffAdminId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatorUserId",
                table: "Teams",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LastModifierUserId",
                table: "Teams",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_CreatorUserId",
                table: "TicketAttachments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_LastModifierUserId",
                table: "TicketAttachments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_TicketId",
                table: "TicketAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_UploadedByStaffId",
                table: "TicketAttachments",
                column: "UploadedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeLogEntries_ActorStaffId",
                table: "TicketChangeLogEntries",
                column: "ActorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeLogEntries_CreationTime",
                table: "TicketChangeLogEntries",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_TicketChangeLogEntries_TicketId",
                table: "TicketChangeLogEntries",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_AuthorStaffId",
                table: "TicketComments",
                column: "AuthorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_CreationTime",
                table: "TicketComments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_CreatorUserId",
                table: "TicketComments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_LastModifierUserId",
                table: "TicketComments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId",
                table: "TicketComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssigneeId",
                table: "Tickets",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ContactId",
                table: "Tickets",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedByStaffId",
                table: "Tickets",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreationTime",
                table: "Tickets",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatorUserId",
                table: "Tickets",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LastModifierUserId",
                table: "Tickets",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_OwningTeamId",
                table: "Tickets",
                column: "OwningTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Priority",
                table: "Tickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SlaDueAt",
                table: "Tickets",
                column: "SlaDueAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SlaRuleId",
                table: "Tickets",
                column: "SlaRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_CreatorUserId",
                table: "TicketViews",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_IsDefault",
                table: "TicketViews",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_IsSystem",
                table: "TicketViews",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_LastModifierUserId",
                table: "TicketViews",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_OwnerStaffId",
                table: "TicketViews",
                column: "OwnerStaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactChangeLogEntries");

            migrationBuilder.DropTable(
                name: "ContactComments");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "TeamMemberships");

            migrationBuilder.DropTable(
                name: "TicketAttachments");

            migrationBuilder.DropTable(
                name: "TicketChangeLogEntries");

            migrationBuilder.DropTable(
                name: "TicketComments");

            migrationBuilder.DropTable(
                name: "TicketViews");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "SlaRules");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropColumn(
                name: "AccessReports",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanManageTickets",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManageTeams",
                table: "Users");
        }
    }
}
