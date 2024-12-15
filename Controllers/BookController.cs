
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class BooksController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public BooksController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
    {
        return await _context.Books.ToListAsync();
    }
    [HttpGet("user-books")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksForUsers()
    {
        var books = await _context.Books
            .Select(book => new BookDto
            {
                Name = book.Name,
                Price = book.Price,
                Author = book.Author
            })
            .ToListAsync();

        return Ok(books);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Book>> GetBook(int id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound();
        }

        return book;
    }
    
    [HttpGet("search/{name}")]
    public async Task<ActionResult<IEnumerable<Book>>> GetBooksByName(string name)
    {
        var books = await _context.Books
                                  .Where(b => b.Name.Contains(name))
                                  .ToListAsync();

        if (books == null || books.Count == 0)
        {
            return NotFound(new { Message = "No books found with the specified name." });
        }

        return Ok(books);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Book>> PostBook(Book book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutBook(int id, Book book)
    {
        if (id != book.Id)
        {
            return BadRequest("Book ID mismatch");
        }

        // Bookni o'zgartirish
        _context.Entry(book).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Xatolikni konsolga chiqarish (debug uchun)
            Console.WriteLine($"Error: {ex.Message}");

            if (!BookExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("purchase")]
    [Authorize(Roles = "User")]
    [Authorize(Roles = "IsSuperAdmin")]
    public async Task<IActionResult> PurchaseBook([FromBody] PurchaseRequest request)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Name == request.BookName);

        if (book == null)
        {
            return NotFound("Kitob topilmadi.");
        }

        if (book.Count < request.Quantity)
        {
            return BadRequest("Kitobning yetarli miqdori mavjud emas.");
        }

        book.Count -= request.Quantity;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // JWT token orqali UserId olish
        if (userId == null)
        {
            return Unauthorized("Foydalanuvchi autentifikatsiya qilinmagan.");
        }

        var archivedBook = new ArchivedBook
        {
            Name = book.Name,
            Price = book.Price,
            Author = book.Author,
            Quantity = request.Quantity,
            ArchivedDate = DateTime.Now,
            UserId = userId
        };

        _context.ArchivedBooks.Add(archivedBook);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Kitob muvaffaqiyatli sotib olindi va arxivlandi." });
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(e => e.Id == id);
    }
}

