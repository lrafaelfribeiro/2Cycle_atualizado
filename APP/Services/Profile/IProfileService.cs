using APP.Core;
using APP.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Services.Profile
{
    public interface IProfileService
    {
        Task<Result<ProfileResponseDTO>> CreateProfileAsync(CreateProfileRequestDTO request, CancellationToken cancellationToken);
        Task<Result<ProfileResponseDTO>> GetMyProfileAsync(CancellationToken cancellationToken);
    }
}
