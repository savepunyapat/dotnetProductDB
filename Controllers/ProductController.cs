using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreAPI.Data;
using StoreAPI.Models;
using Microsoft.EntityFrameworkCore;    
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
    public ActionResult GetProducts(
        [FromQuery] int page=1,
        [FromQuery] int limit=100,
        [FromQuery] int? selectedCategory=null,
        [FromQuery] string? searchQuery=null
    ) {
        int skip = (page - 1) * limit;
        
        var query = _context.products
        .Join(
            _context.categories,
            p => p.category_id,
            c => c.category_id,
            (p, c) => new
            {
                p.product_id,
                p.product_name,
                p.unit_price,
                p.unit_in_stock,
                p.product_picture,
                p.created_date,
                p.modified_date,
                p.category_id,
                c.category_name
            }
        );
        if(selectedCategory.HasValue)
        {
            query = query.Where(p => p.category_id == selectedCategory.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p => EF.Functions.ILike(p.product_name!, $"%{searchQuery}%"));
        }
         var totalRecords = query.Count();
         var products = query
            .OrderByDescending(p => p.product_id)
            .Skip(skip)
            .Take(limit)
            .ToList();
            return Ok(new {Total = totalRecords, Products = products});
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
    public async Task<ActionResult<product>> CreateProduct([FromForm] product product, IFormFile? image)
    {
        _context.products.Add(product);

        if(image != null){
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

           
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            product.product_picture = fileName;
        } else {
            product.product_picture = "noimg.jpg";
        }

        _context.SaveChanges();

        return Ok(product);
    }


    [HttpPut("{id}")]
    public async Task<ActionResult<product>> UpdateProduct(
        int id, [FromForm] 
        product product, 
        IFormFile? image
    )
    {
        var existingProduct = _context.products.FirstOrDefault(p => p.product_id == id);

        if (existingProduct == null)
        {
            return NotFound();
        }

        existingProduct.product_name = product.product_name;
        existingProduct.unit_price = product.unit_price;
        existingProduct.unit_in_stock = product.unit_in_stock;
        existingProduct.category_id = product.category_id;
        existingProduct.modified_date = product.modified_date;

        if(image != null){
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

         
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            if(existingProduct.product_picture != "noimg.jpg"){
                System.IO.File.Delete(Path.Combine(uploadFolder, existingProduct.product_picture!));
            }

            existingProduct.product_picture = fileName;
        }

        _context.SaveChanges();

        return Ok(existingProduct);
    }

    [HttpDelete("{id}")]
    public ActionResult<product> DeleteProduct(int id) {
        var product = _context.products.FirstOrDefault(p => p.product_id == id);
        if (product == null) {
            return NotFound();
        }
        if(product.product_picture != "noimg.jpg"){
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            System.IO.File.Delete(Path.Combine(uploadFolder, product.product_picture!));
        }
        _context.products.Remove(product);
        _context.SaveChanges();
        return Ok(product);
    }
}