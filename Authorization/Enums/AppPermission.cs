using System.ComponentModel;

namespace LoungeClubsApi.Authorization.Enums;

public enum AppPermission
{
    [Description("Roles.View")]
    RolesView,

    [Description("Roles.Create")]
    RolesCreate,

    [Description("Roles.Update")]
    RolesUpdate,

    [Description("Roles.Delete")]
    RolesDelete,

    [Description("Users.View")]
    UsersView,

    [Description("Users.Create")]
    UsersCreate,

    [Description("Users.Update")]
    UsersUpdate,

    [Description("Users.Delete")]
    UsersDelete,

    [Description("Users.ManageRoles")]
    UsersManageRoles,

    [Description("Users.ViewActivity")]
    UsersViewActivity,

    [Description("Users.Deactivate")]
    UsersDeactivate,

    [Description("Users.Activate")]
    UsersActivate,

    [Description("Establishments.ViewAll")]
    EstablishmentsViewAll,

    [Description("Establishments.Review")]
    EstablishmentsReview,

    [Description("Establishments.AssignTier")]
    EstablishmentsAssignTier,

    [Description("Establishments.Suspend")]
    EstablishmentsSuspend,

    [Description("Tiers.Manage")]
    TiersManage,

    [Description("Levies.ViewAll")]
    LeviesViewAll,

    [Description("Levies.Waive")]
    LeviesWaive,

    [Description("Certificates.ViewAll")]
    CertificatesViewAll,

    [Description("Certificates.Revoke")]
    CertificatesRevoke,

    [Description("Inspections.ViewAll")]
    InspectionsViewAll,

    [Description("Inspections.Conduct")]
    InspectionsConduct,

    [Description("Fines.ViewAll")]
    FinesViewAll,

    [Description("Fines.Issue")]
    FinesIssue,

    [Description("Fines.Waive")]
    FinesWaive,

    [Description("Reports.View")]
    ReportsView,

    [Description("Audit.View")]
    AuditView,
}
