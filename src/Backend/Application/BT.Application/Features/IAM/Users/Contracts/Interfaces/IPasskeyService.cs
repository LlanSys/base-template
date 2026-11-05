using System.Threading;
using System.Threading.Tasks;
using BT.Domain.Features.IAM.Users.Entities;
using System.Text.Json;

namespace BT.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IPasskeyService
{
    Task<JsonElement> RequestNewCredentialAsync(AppUser user, IEnumerable<Fido2Credential> existingCredentials, CancellationToken cancellationToken = default);
    Task<Fido2Credential> MakeNewCredentialAsync(AppUser user, JsonElement attestationResponse, JsonElement originalOptions, CancellationToken cancellationToken = default);

    Task<JsonElement> RequestAssertionAsync(string username, CancellationToken cancellationToken = default);
    Task<uint?> MakeAssertionAsync(AppUser user, JsonElement assertionResponse, JsonElement originalOptions, byte[] storedPublicKey, uint storedSignCount, CancellationToken cancellationToken = default);
}
