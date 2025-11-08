using FocusDeck.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers;

[ApiController]
[Route("v1/encryption")]
[Authorize]
public class EncryptionController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionController(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    [HttpDelete("key")]
    public IActionResult DeleteKey()
    {
        _encryptionService.DeleteKey();
        return NoContent();
    }
}
