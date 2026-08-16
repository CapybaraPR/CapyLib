namespace Capy.Core.DRM;

/// <summary>
/// DTO модель ответа от сервера валидации лицензий.
/// </summary>
public class LicenseResponse
{
    public string Status { get; set; } = string.Empty;
    public int Code { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;

    public bool IsValid => string.Equals(Status, "valid", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase) ||
                           Code == 200;
}
