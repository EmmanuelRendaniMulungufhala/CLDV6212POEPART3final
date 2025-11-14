using ABC_Retailer.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ABC_Retailer.Models;

namespace ABC_Retailer.Controllers
{
    // Upload requires authentication - both Customer and Admin can upload
    [Authorize(Roles = "Customer,Admin")]
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _functions;
        private readonly ILogger<UploadController> _logger;

        // Persistent list to track uploads during application lifetime
        // In production, this should be stored in a database
        private static List<FileUpload> _uploads = new List<FileUpload>();

        public UploadController(IFunctionsApi functions, ILogger<UploadController> logger)
        {
            _functions = functions;
            _logger = logger;
        }

        // View uploads - Customer sees their own, Admin sees all
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var uploads = _uploads;

                // If customer, filter to show only their uploads
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name ?? "";
                    uploads = _uploads
                        .Where(u => u.CustomerName.Contains(username, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    _logger.LogInformation("Customer {Username} viewing {Count} uploads",
                        username, uploads.Count);
                }
                else
                {
                    _logger.LogInformation("Admin viewing all {Count} uploads", uploads.Count);
                }

                return View(uploads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading uploads");
                TempData["ErrorMessage"] = "Error loading uploads: " + ex.Message;
                return View(new List<FileUpload>());
            }
        }

        // Upload proof of payment - Customer and Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IFormFile ProofOfPayment, string CustomerName, string OrderId)
        {
            if (ProofOfPayment == null || ProofOfPayment.Length == 0)
            {
                ViewBag.Message = "Please select a file.";
                ViewBag.IsSuccess = false;

                // Return filtered uploads based on role
                var uploads = GetFilteredUploads();
                return View(uploads);
            }

            // Validate file size (10MB limit)
            if (ProofOfPayment.Length > 10 * 1024 * 1024)
            {
                ViewBag.Message = "File size must be less than 10MB.";
                ViewBag.IsSuccess = false;

                var uploads = GetFilteredUploads();
                return View(uploads);
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(ProofOfPayment.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewBag.Message = "Allowed file types: PDF, JPG, JPEG, PNG, DOC, DOCX";
                ViewBag.IsSuccess = false;

                var uploads = GetFilteredUploads();
                return View(uploads);
            }

            try
            {
                // Use actual user info if customer is uploading
                var actualCustomerName = CustomerName;
                if (User.IsInRole("Customer"))
                {
                    actualCustomerName = User.Identity?.Name ?? CustomerName;
                }

                _logger.LogInformation("User {Username} uploading file: {FileName} for Customer: {CustomerName}, Order: {OrderId}",
                    User.Identity?.Name, ProofOfPayment.FileName, actualCustomerName, OrderId);

                var fileName = await _functions.UploadProofOfPaymentAsync(ProofOfPayment, OrderId, actualCustomerName);

                // Create and add to persistent uploads list
                var upload = new FileUpload
                {
                    PartitionKey = "UPLOADS",
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerName = actualCustomerName ?? "Unknown Customer",
                    OrderId = OrderId ?? "No Order ID",
                    FileUrl = $"/uploads/{fileName}",
                    Timestamp = DateTimeOffset.UtcNow
                };

                _uploads.Add(upload);

                _logger.LogInformation("File uploaded successfully: {FileName}. Total uploads: {Count}",
                    fileName, _uploads.Count);

                ViewBag.Message = $"File '{Path.GetFileName(fileName)}' uploaded successfully!";
                ViewBag.IsSuccess = true;
                TempData["SuccessMessage"] = "Proof of payment uploaded successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for user {Username}", User.Identity?.Name);
                ViewBag.Message = "Error uploading file: " + ex.Message;
                ViewBag.IsSuccess = false;
                TempData["ErrorMessage"] = "Error uploading file: " + ex.Message;
            }

            var finalUploads = GetFilteredUploads();
            return View(finalUploads);
        }

        // View upload details - Customer can view their own, Admin can view all
        [HttpGet]
        public IActionResult Details(string id)
        {
            try
            {
                var upload = _uploads.FirstOrDefault(u => u.RowKey == id);

                if (upload == null)
                {
                    TempData["ErrorMessage"] = "Upload not found.";
                    return RedirectToAction(nameof(Index));
                }

                // If customer, verify they own this upload
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name ?? "";
                    if (!upload.CustomerName.Contains(username, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Customer {Username} attempted to access upload {UploadId} they don't own",
                            username, id);
                        return RedirectToAction("AccessDenied", "Account");
                    }
                }

                return View(upload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading upload details {UploadId}", id);
                TempData["ErrorMessage"] = "Error loading upload: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Delete upload - Customer can delete their own, Admin can delete any
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            try
            {
                var upload = _uploads.FirstOrDefault(u => u.RowKey == id);

                if (upload == null)
                {
                    TempData["ErrorMessage"] = "Upload not found.";
                    return RedirectToAction(nameof(Index));
                }

                // If customer, verify they own this upload
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name ?? "";
                    if (!upload.CustomerName.Contains(username, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Customer {Username} attempted to delete upload {UploadId} they don't own",
                            username, id);
                        return RedirectToAction("AccessDenied", "Account");
                    }
                }

                _uploads.Remove(upload);
                TempData["SuccessMessage"] = "Upload deleted successfully!";

                _logger.LogInformation("Upload {UploadId} deleted by {Username}", id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting upload {UploadId}", id);
                TempData["ErrorMessage"] = "Error deleting upload: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Download file - Customer can download their own, Admin can download any
        [HttpGet]
        public IActionResult Download(string id)
        {
            try
            {
                var upload = _uploads.FirstOrDefault(u => u.RowKey == id);

                if (upload == null)
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction(nameof(Index));
                }

                // If customer, verify they own this upload
                if (User.IsInRole("Customer"))
                {
                    var username = User.Identity?.Name ?? "";
                    if (!upload.CustomerName.Contains(username, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Customer {Username} attempted to download upload {UploadId} they don't own",
                            username, id);
                        return RedirectToAction("AccessDenied", "Account");
                    }
                }

                // Mock file download - in real app, you'd return the actual file
                TempData["SuccessMessage"] = $"Download initiated for: {Path.GetFileName(upload.FileUrl)}";

                _logger.LogInformation("Upload {UploadId} downloaded by {Username}", id, User.Identity?.Name);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading upload {UploadId}", id);
                TempData["ErrorMessage"] = "Error downloading file: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to filter uploads based on user role
        private List<FileUpload> GetFilteredUploads()
        {
            if (User.IsInRole("Customer"))
            {
                var username = User.Identity?.Name ?? "";
                return _uploads
                    .Where(u => u.CustomerName.Contains(username, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return _uploads;
        }
    }
}