namespace InventoryManagement.Web.ViewModels
{

    public class ResetPasswordViewModel
    {
        public required string Email { get; set; }
        public string? Code { get; set; }
    }
}
