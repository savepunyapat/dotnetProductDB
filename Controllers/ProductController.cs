using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreAPI.Data;
using StoreAPI.Models;
namespace StoreAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase {
    public readonly ApplicationDbContext _context;

    private readonly IWebHostEnvironment _env;

    public ProductController(ApplicationDbContext context, IWebHostEnvironment env) {
        _context = context;
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet("testconnectdb")]
    public void TestConnectDB() {
        if (_context.Database.CanConnect()) {
            Response.StatusCode = 200;
            Response.WriteAsync("Connected to database");
        } else {
            Response.StatusCode = 500;
            Response.WriteAsync("Failed to connect to database");
        }
    }

    
    [HttpGet]
    public ActionResult GetProducts() {
        //var products = _context.products.ToList();
        
        //var products = _context.products.Where(p => p.unit_price > 45000).ToList();
        
        var products = _context.products
            .Join(
                _context.categories,
                p => p.category_id,
                c => c.category_id,
                (p, c) => new {
                    p.product_id,
                    p.product_name,
                    p.unit_price,
                    p.unit_in_stock,
                    c.category_name
                }
            ).ToList();
        
        return Ok(products);
    }

    [HttpGet("{id}")]
    public ActionResult GetProduct(int id) {
        var product = _context.products.FirstOrDefault(p => p.product_id == id);
        if (product == null) {
            return NotFound();
        }
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<product>> CreateProduct([FromForm] product product, IFormFile image) {
        _context.products.Add(product);

        // Check if image is uploaded
        if (image != null) {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            string uploadFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadFolder)) {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create)) {
                await image.CopyToAsync(fileStream);
            }

            // save file name to database

            product.product_picture = fileName;
        }

        
        _context.SaveChanges();
        return Ok(product);
    }

    [HttpPut("{id}")]
    public ActionResult<product> UpdateProduct(int id, product product) {
        
        var existingProduct = _context.products.FirstOrDefault(p => p.product_id == id);


        if(existingProduct == null) {
            return NotFound();
        }
        
        existingProduct.product_name = product.product_name;
        existingProduct.unit_price = product.unit_price;
        existingProduct.unit_in_stock = product.unit_in_stock;
        existingProduct.category_id = product.category_id;
        
        _context.SaveChanges();

        return Ok(existingProduct);
    }

    [HttpDelete("{id}")]
    public ActionResult<product> DeleteProduct(int id) {
        var product = _context.products.FirstOrDefault(p => p.product_id == id);
        if (product == null) {
            return NotFound();
        }
        _context.products.Remove(product);
        _context.SaveChanges();
        return Ok(product);
    }
}