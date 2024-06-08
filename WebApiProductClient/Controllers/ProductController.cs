
using Grpc.Net.Client;
using WebApiProductClient.Protos; 
using Microsoft.AspNetCore.Mvc;
using Grpc.Core;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ProductService.ProductServiceClient _grpcClient;

    public ProductController()
    {
        var channel = GrpcChannel.ForAddress("https://localhost:7205"); // Update with your server address and port
        _grpcClient = new ProductService.ProductServiceClient(channel);
    }

    [HttpPost]
    public async Task<IActionResult> PostProduct([FromBody] Product product)
    {   
        //check if product exist
        var productRequest = new ProductRequest { Id = product.Id };
        var getProductResponse = await _grpcClient.GetProductByIdAsync(productRequest);

        //if exist update the product
        if (getProductResponse.Exists)
        {
            var updateProductResponse = await _grpcClient.UpdateProductAsync(product);
            if (updateProductResponse.Success)
            {
                return Ok("Product updated successfully.");
            }
            else
            {
                return StatusCode(500, "Failed to update product.");
            }
        }
        //if not exist create the product
        else
        {
            var createProductResponse = await _grpcClient.CreateProductAsync(product);
            if (createProductResponse.Success)
            {
                return Ok("Product created successfully.");
            }
            else
            {
                return StatusCode(500, "Failed to create product.");
            }
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> AddProducts([FromBody] IEnumerable<Product> products)
    {
        using var call = _grpcClient.AddBulkProducts();

        foreach (var product in products)
        {
            await call.RequestStream.WriteAsync(product);
        }

        await call.RequestStream.CompleteAsync();
        var response = await call.ResponseAsync;

        return Ok($"Successfully added {response.Count} products.");
    }
    [HttpGet("generateproductreport")]
    public async Task<IActionResult> GenerateProductReport([FromQuery] string category, [FromQuery] bool orderByPrice)
    {
        var request = new ProductReportRequest
        {
            Category = category,
            OrderByPrice = orderByPrice
        };

        using var call = _grpcClient.GetProductReport(request);
        var products = new List<Product>();

        await foreach (var product in call.ResponseStream.ReadAllAsync())
        {
            products.Add(product);
        }

        return Ok(products);
    }
}
