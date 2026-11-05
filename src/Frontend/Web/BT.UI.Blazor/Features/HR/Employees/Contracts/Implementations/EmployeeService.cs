using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.HR.Employees.Dtos;
using BT.UI.Blazor.Configuration;
using BT.UI.Blazor.Features.Shared.BackendApi;
using BT.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using BT.UI.Blazor.Features.HR.Employees.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace BT.UI.Blazor.Features.HR.Employees.Contracts.Implementations;

internal sealed class EmployeeService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : IEmployeeService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<PagedResponse<EmployeeResponse, Guid>>> SearchAsync(EmployeeSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PagedResponse<EmployeeResponse, Guid>>(
            HttpMethod.Get,
            EndpointFormatter.Format(_apiSettings.Endpoints.Hr.EmployeeSearch, _apiSettings.Version, queryString: request.BuildQueryString()),
            unavailableMessage: "The employee service is unavailable. Please try again.",
            timeoutMessage: "The employee service timed out. Please try again.");
    }

    public Task<AppResponse<EmployeeResponse>> GetByIdAsync(Guid id)
        => apiClient.SendAsync<EmployeeResponse>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Hr.EmployeeDetail, ("id", id.ToString())),
            unavailableMessage: "The employee service is unavailable. Please try again.",
            timeoutMessage: "The employee service timed out. Please try again.");

    public Task<AppResponse<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<EmployeeResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Hr.EmployeeCreate, _apiSettings.Version),
            request,
            unavailableMessage: "The employee service is unavailable. Please try again.",
            timeoutMessage: "The employee service timed out. Please try again.");
    }

    public Task<AppResponse<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<EmployeeResponse>(
            HttpMethod.Put,
            Format(_apiSettings.Endpoints.Hr.EmployeeUpdate, ("id", id.ToString())),
            request,
            unavailableMessage: "The employee service is unavailable. Please try again.",
            timeoutMessage: "The employee service timed out. Please try again.");
    }

    public Task<AppResponse<bool>> DeleteAsync(Guid id)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            Format(_apiSettings.Endpoints.Hr.EmployeeDelete, ("id", id.ToString())),
            unavailableMessage: "The employee service is unavailable. Please try again.",
            timeoutMessage: "The employee service timed out. Please try again.");

    private string Format(string endpoint, params (string Key, string Value)[] parameters)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, parameters.ToDictionary(static item => item.Key, static item => item.Value));
}
