using Microsoft.AspNetCore.Mvc;
using PatientManagementSystem.Models.ViewModels;

namespace PatientManagementSystem.ViewComponents;

public sealed class SidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var items = new[]
        {
            new SidebarItem { C = "Home", A = "Index", Label = "Dashboard", Icon = "bi-speedometer2" },
            new SidebarItem { C = "Patients", A = "Index", Label = "Patients", Icon = "bi-people" },
            new SidebarItem { C = "Conditions", A = "Index", Label = "Conditions", Icon = "bi-clipboard2-pulse" },
            new SidebarItem { C = "Wards", A = "Index", Label = "Wards", Icon = "bi-building" },
            new SidebarItem { C = "Invoices", A = "Index", Label = "Billing", Icon = "bi-receipt" },
            new SidebarItem { C = "Users", A = "Index", Label = "Users", Icon = "bi-person-gear" }
        };

        return View(items);
    }
}
