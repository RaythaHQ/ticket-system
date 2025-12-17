using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Request = table.Column<string>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Args = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusInfo = table.Column<string>(type: "text", nullable: true),
                    PercentComplete = table.Column<int>(type: "integer", nullable: false),
                    NumberOfRetries = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaskStep = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FailedLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailedAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedLoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JwtLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jti = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtLogins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: true),
                    DateFormat = table.Column<string>(type: "text", nullable: true),
                    SmtpOverrideSystem = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "text", nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: true),
                    SmtpUsername = table.Column<string>(type: "text", nullable: true),
                    SmtpPassword = table.Column<string>(type: "text", nullable: true),
                    SmtpDefaultFromAddress = table.Column<string>(type: "text", nullable: true),
                    SmtpDefaultFromName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBuiltInAuth = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabledForUsers = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabledForAdmins = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationSchemeType = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    MagicLinkExpiresInSeconds = table.Column<int>(type: "integer", nullable: false),
                    SamlCertificate = table.Column<string>(type: "text", nullable: true),
                    SamlIdpEntityId = table.Column<string>(type: "text", nullable: true),
                    JwtSecretKey = table.Column<string>(type: "text", nullable: true),
                    JwtUseHighSecurity = table.Column<bool>(type: "boolean", nullable: false),
                    SignInUrl = table.Column<string>(type: "text", nullable: true),
                    LoginButtonText = table.Column<string>(type: "text", nullable: true),
                    SignOutUrl = table.Column<string>(type: "text", nullable: true),
                    BruteForceProtectionMaxFailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    BruteForceProtectionWindowInSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoggedInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SsoId = table.Column<string>(type: "text", nullable: true),
                    AuthenticationSchemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    IsEmailAddressConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CustomAttribute1 = table.Column<string>(type: "text", nullable: true),
                    CustomAttribute2 = table.Column<string>(type: "text", nullable: true),
                    CustomAttribute3 = table.Column<string>(type: "text", nullable: true),
                    CustomAttribute4 = table.Column<string>(type: "text", nullable: true),
                    CustomAttribute5 = table.Column<string>(type: "text", nullable: true),
                    PlaySoundOnNotification = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AuthenticationSchemes_AuthenticationSchemeId",
                        column: x => x.AuthenticationSchemeId,
                        principalTable: "AuthenticationSchemes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    LastName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
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
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Cc = table.Column<string>(type: "text", nullable: true),
                    Bcc = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileStorageProvider = table.Column<string>(type: "text", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_LastModifierUserId",
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
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "OneTimePasswords",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePasswords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimePasswords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    SystemPermissions = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Roles_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                name: "TicketPriorityConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeveloperName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "secondary"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPriorityConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPriorityConfigs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketPriorityConfigs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketStatusConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeveloperName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "secondary"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    StatusType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "open"),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketStatusConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketStatusConfigs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketStatusConfigs_Users_LastModifierUserId",
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
                    Description = table.Column<string>(type: "text", nullable: true),
                    OwnerStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionsJson = table.Column<string>(type: "text", nullable: true),
                    SortPrimaryField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortPrimaryDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SortSecondaryField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortSecondaryDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SortLevelsJson = table.Column<string>(type: "text", nullable: true),
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
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    VerificationCodeType = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Webhooks_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Webhooks_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                name: "EmailTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Cc = table.Column<string>(type: "text", nullable: true),
                    Bcc = table.Column<string>(type: "text", nullable: true),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactAttachments_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressStage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SnapshotPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCleanedUp = table.Column<bool>(type: "boolean", nullable: false),
                    BackgroundTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportJobs_BackgroundTasks_BackgroundTaskId",
                        column: x => x.BackgroundTaskId,
                        principalTable: "BackgroundTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExportJobs_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDryRun = table.Column<bool>(type: "boolean", nullable: false),
                    SourceMediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressStage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    RowsProcessed = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RowsSkipped = table.Column<int>(type: "integer", nullable: false),
                    RowsWithErrors = table.Column<int>(type: "integer", nullable: false),
                    ErrorMediaItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCleanedUp = table.Column<bool>(type: "boolean", nullable: false),
                    BackgroundTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_BackgroundTasks_BackgroundTaskId",
                        column: x => x.BackgroundTaskId,
                        principalTable: "BackgroundTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportJobs_MediaItems_ErrorMediaItemId",
                        column: x => x.ErrorMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportJobs_MediaItems_SourceMediaItemId",
                        column: x => x.SourceMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Id = table.Column<long>(type: "bigint", nullable: false),
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
                name: "UserFavoriteViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_TicketViews_TicketViewId",
                        column: x => x.TicketViewId,
                        principalTable: "TicketViews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUserGroup",
                columns: table => new
                {
                    UserGroupsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUserGroup", x => new { x.UserGroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserUserGroup_UserGroups_UserGroupsId",
                        column: x => x.UserGroupsId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUserGroup_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookLogs_Webhooks_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "Webhooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id");
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
                        name: "FK_TicketAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
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

            migrationBuilder.CreateTable(
                name: "TicketFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    StaffAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_StaffAdminId",
                        column: x => x.StaffAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ApiKeyHash",
                table: "ApiKeys",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatorUserId",
                table: "ApiKeys",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreationTime",
                table: "AuditLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_ContactId",
                table: "ContactAttachments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_CreatorUserId",
                table: "ContactAttachments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_LastModifierUserId",
                table: "ContactAttachments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_MediaItemId",
                table: "ContactAttachments",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_UploadedByUserId",
                table: "ContactAttachments",
                column: "UploadedByUserId");

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
                name: "IX_Contacts_FirstName",
                table: "Contacts",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LastModifierUserId",
                table: "Contacts",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LastName",
                table: "Contacts",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_CreatorUserId",
                table: "EmailTemplateRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_EmailTemplateId",
                table: "EmailTemplateRevisions",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_LastModifierUserId",
                table: "EmailTemplateRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CreatorUserId",
                table: "EmailTemplates",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_DeveloperName",
                table: "EmailTemplates",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_LastModifierUserId",
                table: "EmailTemplates",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_BackgroundTaskId",
                table: "ExportJobs",
                column: "BackgroundTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_CreatorUserId",
                table: "ExportJobs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExpiresAt",
                table: "ExportJobs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExpiresAt_IsCleanedUp",
                table: "ExportJobs",
                columns: new[] { "ExpiresAt", "IsCleanedUp" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_LastModifierUserId",
                table: "ExportJobs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_MediaItemId",
                table: "ExportJobs",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_RequesterUserId",
                table: "ExportJobs",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status",
                table: "ExportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FailedLoginAttempts_EmailAddress",
                table: "FailedLoginAttempts",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_BackgroundTaskId",
                table: "ImportJobs",
                column: "BackgroundTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatorUserId",
                table: "ImportJobs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_EntityType",
                table: "ImportJobs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ErrorMediaItemId",
                table: "ImportJobs",
                column: "ErrorMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ExpiresAt",
                table: "ImportJobs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ExpiresAt_IsCleanedUp",
                table: "ImportJobs",
                columns: new[] { "ExpiresAt", "IsCleanedUp" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_LastModifierUserId",
                table: "ImportJobs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_RequesterUserId",
                table: "ImportJobs",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_SourceMediaItemId",
                table: "ImportJobs",
                column: "SourceMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CreatorUserId",
                table: "MediaItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_LastModifierUserId",
                table: "MediaItems",
                column: "LastModifierUserId");

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
                name: "IX_OneTimePasswords_UserId",
                table: "OneTimePasswords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatorUserId",
                table: "Roles",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_LastModifierUserId",
                table: "Roles",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

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
                name: "IX_TicketAttachments_MediaItemId",
                table: "TicketAttachments",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_TicketId",
                table: "TicketAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_UploadedByUserId",
                table: "TicketAttachments",
                column: "UploadedByUserId");

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
                name: "IX_TicketFollowers_CreatorUserId",
                table: "TicketFollowers",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_LastModifierUserId",
                table: "TicketFollowers",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_StaffAdminId",
                table: "TicketFollowers",
                column: "StaffAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_TicketId",
                table: "TicketFollowers",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_TicketId_StaffAdminId",
                table: "TicketFollowers",
                columns: new[] { "TicketId", "StaffAdminId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_CreatorUserId",
                table: "TicketPriorityConfigs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_DeveloperName",
                table: "TicketPriorityConfigs",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_IsActive",
                table: "TicketPriorityConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_IsDefault",
                table: "TicketPriorityConfigs",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_LastModifierUserId",
                table: "TicketPriorityConfigs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_SortOrder",
                table: "TicketPriorityConfigs",
                column: "SortOrder");

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
                name: "IX_TicketStatusConfigs_CreatorUserId",
                table: "TicketStatusConfigs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_DeveloperName",
                table: "TicketStatusConfigs",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_IsActive",
                table: "TicketStatusConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_LastModifierUserId",
                table: "TicketStatusConfigs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_SortOrder",
                table: "TicketStatusConfigs",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_StatusType",
                table: "TicketStatusConfigs",
                column: "StatusType");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_CreatorUserId",
                table: "UserFavoriteViews",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_LastModifierUserId",
                table: "UserFavoriteViews",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_TicketViewId",
                table: "UserFavoriteViews",
                column: "TicketViewId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_UserId",
                table: "UserFavoriteViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_UserId_TicketViewId",
                table: "UserFavoriteViews",
                columns: new[] { "UserId", "TicketViewId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_CreatorUserId",
                table: "UserGroups",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_LastModifierUserId",
                table: "UserGroups",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthenticationSchemeId",
                table: "Users",
                column: "AuthenticationSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatorUserId",
                table: "Users",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastModifierUserId",
                table: "Users",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users",
                columns: new[] { "SsoId", "AuthenticationSchemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                table: "UserUserGroup",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_CreatorUserId",
                table: "VerificationCodes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_LastModifierUserId",
                table: "VerificationCodes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_CreatedAt",
                table: "WebhookLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_Success",
                table: "WebhookLogs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_TicketId",
                table: "WebhookLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_WebhookId",
                table: "WebhookLogs",
                column: "WebhookId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_WebhookId_CreatedAt",
                table: "WebhookLogs",
                columns: new[] { "WebhookId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_CreatorUserId",
                table: "Webhooks",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_IsActive",
                table: "Webhooks",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_LastModifierUserId",
                table: "Webhooks",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_TriggerType",
                table: "Webhooks",
                column: "TriggerType");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_TriggerType_IsActive",
                table: "Webhooks",
                columns: new[] { "TriggerType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_CreatorUserId",
                table: "ApiKeys",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_UserId",
                table: "ApiKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ContactAttachments");

            migrationBuilder.DropTable(
                name: "ContactChangeLogEntries");

            migrationBuilder.DropTable(
                name: "ContactComments");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "EmailTemplateRevisions");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "FailedLoginAttempts");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "JwtLogins");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "OneTimePasswords");

            migrationBuilder.DropTable(
                name: "OrganizationSettings");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "TeamMemberships");

            migrationBuilder.DropTable(
                name: "TicketAttachments");

            migrationBuilder.DropTable(
                name: "TicketChangeLogEntries");

            migrationBuilder.DropTable(
                name: "TicketComments");

            migrationBuilder.DropTable(
                name: "TicketFollowers");

            migrationBuilder.DropTable(
                name: "TicketPriorityConfigs");

            migrationBuilder.DropTable(
                name: "TicketStatusConfigs");

            migrationBuilder.DropTable(
                name: "UserFavoriteViews");

            migrationBuilder.DropTable(
                name: "UserUserGroup");

            migrationBuilder.DropTable(
                name: "VerificationCodes");

            migrationBuilder.DropTable(
                name: "WebhookLogs");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "BackgroundTasks");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "MediaItems");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketViews");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "Webhooks");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "SlaRules");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AuthenticationSchemes");
        }
    }
}
