# Patient Management System — Task Plan

**Stack**: ASP.NET Core 10 MVC | EF Core + SQL Server | Bootstrap 5 admin layout | FluentValidation-style | Classic MVC structure

## Phase 0 — Project Foundation
1. **Add NuGet packages**
   - `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.UI`, `FluentValidation.AspNetCore`.
2. **Set up connection string** in `appsettings.json` (`DefaultConnection`) and `appsettings.Development.json`.
3. **Restructure Models folder** into subfolders:
   - `Models/Entities/` (domain entities), `Models/ViewModels/` (DTOs), `Models/Enums/`.
4. **Create `Data/` folder** for `ApplicationDbContext` + seeders. Register it in `Program.cs` with `AddDbContext` + `AddIdentity<IdentityUser, IdentityRole>` + cookie auth.
5. **Wire FluentValidation**: register validators assembly-wide in `Program.cs`; remove default DataAnnotations overposting where dual validation conflicts.
6. **Centralized error handling**: add `app.UseExceptionHandler` with a custom `ErrorController`/middleware returning friendly views; add global try/catch via `IExceptionHandlerFilter`.
7. **Bootstrap 5 admin layout** in `Views/Shared/_Layout.cshtml`: top navbar + left sidebar (Dashboard, Patients, Conditions, Wards, Billing, Users), toastr-toasted alerts, partial `_Sidebar.cshtml`.

## Phase 1 — Patient CRUD (core module)
8. **Entities**: `Patient` (Id, FullName, Gender, DOB, Phone, Email, Address, CreatedAt, UpdatedAt, IsActive).
9. **ViewModels**: `PatientCreateViewModel`, `PatientEditViewModel`, `PatientListViewModel`, `PatientDetailsViewModel`, `PatientIndexQuery` (search + paging).
10. **Validators**: `PatientCreateViewModelValidator`, `PatientEditViewModelValidator` (unique email, age >= 0, phone regex).
11. **`Data/Seeders/PatientSeeder`** — 10–15 sample patients.
12. **`PatientsController`** (async): Index (search/paging/sort), Details, Create (GET/POST), Edit, Delete (POST with anti-forgery).
13. **Views**: `Views/Patients/{Index,Details,Create,Edit,Delete}.cshtml` with table, cards, form partials (`_PatientForm.cshtml`).
14. **EF migrations**: `InitialCreate` + apply `dotnet ef database update`.

## Phase 2 — Disease & Condition Tracking
15. **Entities**: `Condition` (Id, Name, ICD10Code), `PatientCondition` (PatientId, ConditionId, DiagnosedDate, Severity, Notes) — many-to-many.
16. **ViewModels + Validators** for assigning/removing conditions on a patient.
17. **`ConditionsController`** (CRUD for catalog) and `PatientConditionsController` (manage per-patient): add diagnosis, list active/resolved, mark resolved, view history.
18. **Views**: `Views/Conditions/Index.cshtml` (catalog) and `Views/Patients/Conditions.cshtml` (per-patient tab on Details).
19. **Migration** `AddConditions` + seeder (common ICD-10 codes).

## Phase 3 — Ward & Bed Assignment
20. **Entities**: `Ward` (Id, Name, Floor, Capacity), `Bed` (Id, WardId, Number, Status enum: Available/Occupied/Maintenance), `BedAssignment` (Id, PatientId, BedId, AdmissionDate, DischargeDate, Notes).
21. **ViewModels + Validators** (prevent double-booking on AdmissionDate, force discharge before reassign).
22. **Controllers**: `WardsController` (CRUD + capacity summary), `BedsController` (status board), `BedAssignmentsController` (admit/discharge/transfer).
23. **Views**: Ward list, bed grid (color-coded status), assignment modal/partial. Add "Admit" button on Patient Details.
24. **Migration** `AddWardsBeds` + seeder.

## Phase 4 — Billing & Invoices
25. **Entities**: `Invoice` (Id, PatientId, Number, Date, TotalAmount, Status enum: Pending/Paid/Cancelled), `InvoiceItem` (Id, InvoiceId, Description, Quantity, UnitPrice, Total).
26. **ViewModels + Validators** (totals reconcile with line items, no negative prices).
27. **`InvoicesController`**: Index (filter by patient/status/date), Create (add line items via partial), Details (printable), MarkPaid, Cancel.
28. **Views**: invoice list, create form with dynamic line items (jQuery), printable `Invoice.cshtml` (print CSS).
29. **Migration** `AddBilling`.

## Phase 5 — User Login & Roles
30. **Identity setup**: `ApplicationDbContext : IdentityDbContext<IdentityUser>`. Configure password rules, lockout, claims.
31. **Roles**: `Admin`, `Doctor`, `Staff`, `Accountant` — seeded via `RoleSeeder` on startup.
32. **Authorization**:
    - `[Authorize]` global via convention; allow anonymous only for `/Account/Login`, `/Account/AccessDenied`, `/Home/Error`.
    - Role-gated actions: Patients → Admin/Doctor/Staff; Billing → Admin/Accountant; Users → Admin.
33. **`AccountController`**: Login, Logout, AccessDenied, Register (admin-only).
34. **`UsersController`** (admin): list users, assign/revoke roles, enable/disable.
35. **Views**: `_LoginPartial.cshtml`, `Views/Account/{Login,AccessDenied}.cshtml`, `Views/Users/Index.cshtml`.
36. **Migration** `AddIdentity` (Identity tables).

## Phase 6 — Dashboard, Polish & Seed
37. **`HomeController.Index` dashboard**: counts of patients, occupied beds, pending invoices, recent admissions. Cards + simple Chart.js sparklines via CDN.
38. **Toastr** notifications (success/error) driven by `TempData["Notification"]`.
39. **Seeders**: orchestrate via `SeedInitializer.Initialize(app)` on startup (idempotent).
40. **`appsettings.Production.json`** guidance + README notes for SQL Server connection.

## Phase 7 — Cross-cutting / Verification
41. **Run**: `dotnet build`, `dotnet ef migrations add Initial` (combined) or per-phase, `dotnet ef database update`, `dotnet run`.
42. **Manual smoke test checklist**: login as each role, patient CRUD flow, admit-to-bed-to-bill-to-pay cycle.
43. **Lint/typecheck**: `dotnet build` is the project's typecheck; no separate linter. Add `dotnet format` to AGENTS.md.
44. **AGENTS.md**: record build/run/migration commands for future sessions.

## Suggested execution order
0 → 1 (Patient CRUD + migrations) → Dashboard stub → each feature module in dependency order (Conditions ⊂ Patient; Wards → Beds → Assignments; Billing after Patient; Users/Identity last because it touches auth globally) → Seed → Verify.

> Loose ends to confirm before implementation:
> - **SQL Server connection string**: is LocalDB (`(localdb)\MSSQLLocalDB`) acceptable, or do you want a named SQL Server instance?
> - **Identity user store**: reuse `IdentityUser` or add an `ApplicationUser` with `FullName`/`Role` display fields?
> - **Delete policy**: hard delete patients, or soft delete (`IsActive=false`) given referential data (invoices, conditions)?
> - **Print invoice**: HTML print view, or PDF generation (e.g., `QuestPDF`)? Adds a package.