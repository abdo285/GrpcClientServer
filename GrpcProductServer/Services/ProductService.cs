using Grpc.Core;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GrpcProductServer.Protos;
using static GrpcProductServer.Protos.ProductService; 

public class ProductService : ProductServiceBase
{
    private static readonly List<Product> Products = new List<Product>
    {
        new Product { Id = 1, Name = "Product 1", Price = 10.99 },
        new Product { Id = 2, Name = "Product 2", Price = 20.99 }
    };
    public override Task<ProductExistResponse> GetProductById(ProductRequest request, ServerCallContext context)
    {
        var exists = Products.Exists(p => p.Id == request.Id);
        var response = new ProductExistResponse { Exists = exists };
        return Task.FromResult(response);
    }

    public override Task<ProductResponse> CreateProduct(Product request, ServerCallContext context)
    {
        // Check if the product with the same ID already exists
        if (Products.Exists(p => p.Id == request.Id))
        {
            return Task.FromResult(new ProductResponse { Success = false, Message = "Product already exists." });
        }

        Products.Add(request);
        return Task.FromResult(new ProductResponse { Success = true, Message = "Product created successfully." });
    }

    public override Task<ProductResponse> UpdateProduct(Product request, ServerCallContext context)
    {
        var existingProduct = Products.Find(p => p.Id == request.Id);
        if (existingProduct != null)
        {
            existingProduct.Name = request.Name;
            existingProduct.Price = request.Price;
            existingProduct.Category = request.Category;
            return Task.FromResult(new ProductResponse { Success = true, Message = "Product updated successfully." });
        }
        else
        {
            return Task.FromResult(new ProductResponse { Success = false, Message = "Product not found." });
        }
    }
    public override async Task<BulkProductResponse> AddBulkProducts(IAsyncStreamReader<Product> requestStream, ServerCallContext context)
    {
        int count = 0;
        await foreach (var product in requestStream.ReadAllAsync())
        {
            if (!Products.Exists(p => p.Id == product.Id))
            {
                Products.Add(product);
                count++;
            }
        }
        return new BulkProductResponse { Count = count };
    }

    public override async Task GetProductReport(ProductReportRequest request, IServerStreamWriter<Product> responseStream, ServerCallContext context)
    {
        var filteredProducts = Products.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Category))
        {
            filteredProducts = filteredProducts.Where(p => p.Category == request.Category);
        }

        if (request.OrderByPrice)
        {
            filteredProducts = filteredProducts.OrderBy(p => p.Price);
        }

        foreach (var product in filteredProducts)
        {
            await responseStream.WriteAsync(product);
        }
    }
}


