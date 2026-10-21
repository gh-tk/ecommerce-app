namespace ECommerce.Web.Services;

/// <summary>
/// Talks to the Catalog service through the gateway ("/catalog/...").
/// Base address is resolved by Aspire service discovery.
/// </summary>
public class CatalogApiClient(HttpClient httpClient)
{
    public async Task<PagedResultDto<ProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<PagedResultDto<ProductDto>>(
            $"/catalog/api/products?page={page}&pageSize={pageSize}", ct)
        ?? new PagedResultDto<ProductDto>([], page, pageSize, 0);
}

public record ProductDto(int Id, string Name, string? Description, decimal Price, int AvailableStock);

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
