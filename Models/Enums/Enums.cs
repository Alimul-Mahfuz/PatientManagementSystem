namespace PatientManagementSystem.Models.Enums
{
    public enum Gender
    {
        Unknown = 0,
        Male = 1,
        Female = 2,
        Other = 3
    }

    public enum PatientStatus
    {
        Outpatient = 0,    // not currently admitted
        Admitted = 1,      // has an active bed assignment
        Discharged = 2     // most recent assignment discharged (still on file)
    }

    public enum BedStatus
    {
        Available = 0,
        Occupied = 1,
        Maintenance = 2
    }

    public enum InvoiceStatus
    {
        Pending = 0,
        Paid = 1,
        Cancelled = 2
    }

    public enum Severity
    {
        Mild = 0,
        Moderate = 1,
        Severe = 2,
        Critical = 3
    }
}