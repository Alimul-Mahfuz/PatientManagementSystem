namespace PatientManagementSystem.Data.Permissions
{
    public sealed class PermissionCatalog
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Module { get; init; } = string.Empty;

        public static readonly IReadOnlyList<PermissionCatalog> All =
        [
            new() { Id = 1,  Key = "dashboard.view",          Name = "View dashboard",         Description = "Access the landing dashboard.",                 Module = "Dashboard" },

            new() { Id = 10, Key = "patient.view",            Name = "View patients",           Description = "List and view patient demographics records.",       Module = "Patients" },
            new() { Id = 11, Key = "patient.create",          Name = "Create patient",          Description = "Register a new patient.",                            Module = "Patients" },
            new() { Id = 12, Key = "patient.edit",            Name = "Edit patient",            Description = "Update patient demographic details.",                Module = "Patients" },
            new() { Id = 13, Key = "patient.delete",         Name = "Delete patient",          Description = "Remove a patient record (soft delete).",             Module = "Patients" },

            new() { Id = 20, Key = "condition.view",          Name = "View conditions",         Description = "Browse the condition/diagnosis catalog.",            Module = "Conditions" },
            new() { Id = 21, Key = "condition.create",       Name = "Create condition",        Description = "Add a new condition to the catalog.",                Module = "Conditions" },
            new() { Id = 22, Key = "condition.edit",         Name = "Edit condition",          Description = "Update a condition.",                                Module = "Conditions" },
            new() { Id = 23, Key = "condition.delete",       Name = "Delete condition",        Description = "Remove a condition.",                                Module = "Conditions" },

            new() { Id = 30, Key = "ward.view",               Name = "View wards",              Description = "View ward and bed layout.",                          Module = "Wards & Beds" },
            new() { Id = 31, Key = "ward.manage",            Name = "Manage wards",           Description = "Create/edit/delete wards and beds.",                  Module = "Wards & Beds" },
            new() { Id = 32, Key = "bed.assign",             Name = "Assign beds",            Description = "Admit, transfer, or discharge patients to beds.",     Module = "Wards & Beds" },

            new() { Id = 40, Key = "invoice.view",            Name = "View invoices",           Description = "List and view invoices.",                            Module = "Billing" },
            new() { Id = 41, Key = "invoice.create",          Name = "Create invoice",          Description = "Generate a new invoice.",                            Module = "Billing" },
            new() { Id = 42, Key = "invoice.edit",            Name = "Edit invoice",            Description = "Update an invoice's line items.",                    Module = "Billing" },
            new() { Id = 43, Key = "invoice.delete",         Name = "Delete invoice",          Description = "Remove an invoice.",                                 Module = "Billing" },
            new() { Id = 44, Key = "invoice.mark_paid",     Name = "Mark invoice paid",      Description = "Toggle an invoice's payment status.",                 Module = "Billing" },

            new() { Id = 50, Key = "user.view",              Name = "View users",             Description = "Browse user accounts.",                              Module = "Users" },
            new() { Id = 51, Key = "user.create",            Name = "Create user",            Description = "Invite a new user.",                                 Module = "Users" },
            new() { Id = 52, Key = "user.edit",              Name = "Edit user",              Description = "Edit a user's profile, role, and status.",            Module = "Users" },
            new() { Id = 53, Key = "user.delete",            Name = "Delete user",            Description = "Remove a user account.",                             Module = "Users" },
            new() { Id = 54, Key = "user.reset_password",   Name = "Reset password",        Description = "Force-reset another user's password.",                Module = "Users" },

            new() { Id = 60, Key = "role.view",               Name = "View roles",              Description = "List roles and their permissions.",                  Module = "Roles" },
            new() { Id = 61, Key = "role.create",            Name = "Create role",            Description = "Create a new role.",                                 Module = "Roles" },
            new() { Id = 62, Key = "role.edit",              Name = "Edit role",              Description = "Rename a role or change its permissions.",           Module = "Roles" },
            new() { Id = 63, Key = "role.delete",            Name = "Delete role",            Description = "Remove a role.",                                     Module = "Roles" },
            new() { Id = 64, Key = "role.assign_permissions", Name = "Grant role permissions", Description = "Change a role's permission set.",                   Module = "Roles" },

            new() { Id = 70, Key = "profile.view_own",      Name = "View own profile",       Description = "See own account page.",                               Module = "Profile" },
            new() { Id = 71, Key = "profile.edit_own",       Name = "Edit own profile",        Description = "Update own name, phone, bio, password.",             Module = "Profile" },
        ];
    }
}